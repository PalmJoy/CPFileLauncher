using CPClipboard.Core.Paste;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CPClipboard.Core.Input;
using CPClipboard.Core.Model;
using CPClipboard.Core.Popup;
using CPClipboard.Core.Storage;
using CPClipboard.Platform;

namespace CPClipboard.App;

/// <summary>
/// 盤面の表示・非表示（SPEC §3.3）。入力フックのイベントはフックスレッドから来るので UI スレッドへ Post する。
/// 盤面外クリックはフック座標で判定する（非アクティブ化イベントは補助）。外クリック・非アクティブ化で閉じたときは
/// フォーカスを返さない（クリック先が持つべきなので）。
/// </summary>
internal sealed class PopupController
{
    private static readonly long DebounceTicks = Stopwatch.Frequency / 4; // 250ms: 連続発火の抑制

    private readonly BoardWindow _window;
    private readonly PasteController _paste;
    private readonly IPlatformServices _platform;
    private readonly SettingsHub _hub;
    private readonly Func<TemplateBook> _book;
    private readonly OutsideClickTracker _outside = new();
    private readonly MouseLeaveTracker _leave = new();
    private readonly DispatcherTimer _leaveTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    // フックスレッドから書く: 下のアプリへ通したボタンが押されている間 true。
    // 盤面外でつかんだ瞬間に非アクティブ化が先に届いても閉じないようにする（D&D 登録のため）
    private volatile bool _mouseHeld;
    private long _previewClickTicks;
    private bool _previewClickHeld;

    // 確認ダイアログ表示中は外クリック・非アクティブ化で閉じない
    private bool _modal;

    private int? _requestedGroup;
    private nint _previousForeground;
    private bool _focusMovedElsewhere;
    private long _lastTriggerTicks;
    private long _lastHideTicks;
    private long _lastShowTicks;
    private bool _boardConfigured;

    // 表示直後の非アクティブ化は閉じずに前面化し直す猶予。Win で、別のウィンドウを使った後のホットキーで
    // 表示した直後に Deactivated が届き、1 回目が出ない（すぐ閉じる）ことがあった（2026-10-07 ログで確認）。
    // 本当の外クリックはフックの座標判定で閉じるので、猶予中も閉じられる
    private const double ShowGraceMs = 500;

    // Hide() で自分から隠している最中。macOS は Hide() の途中で Deactivated が来るので、
    // それを「他へフォーカスが移った」と誤判定しないようにする（SPEC §3.3）
    private bool _hiding;

    // 非表示演出の再生中（ウィンドウはまだ見えているが「閉じた」扱い）。演出中に再表示されたら打ち切る
    private bool _hidingAnimation;
    private CancellationTokenSource? _effectCts;

    public PopupController(BoardWindow window, IPlatformServices platform, SettingsHub hub, Func<TemplateBook> book)
    {
        _window = window;
        _platform = platform;
        _hub = hub;
        _book = book;
        _paste = new PasteController(platform, () => !IsShown);
    }

    private AppSettings _settings => _hub.Current;

    /// <summary>盤面が開いているか（非表示演出中は閉じた扱い）。</summary>
    public bool IsShown => _window.IsVisible && !_hidingAnimation && !_warming;

    // 起動直後の準備運動中（画面外・透明で 1 回表示している）
    private bool _warming;

    /// <summary>
    /// 初回表示の重さ対策: 起動直後に透明・クリック不可・非アクティブで 1 回表示してすぐ隠し、ネイティブウィンドウの生成と
    /// 描画・演出コードの JIT を済ませておく（Mac Debug で初回 700 ms → 150〜350 ms。残りは初回のアプリのアクティブ化と
    /// 最初の描画で、Release / .app で測り直す: issue/MAC_SUPPORT.md）。
    /// 準備中にユーザーがトリガーしたら準備を打ち切ってそのまま本表示にする。
    /// </summary>
    public void Warmup()
    {
        Dispatcher.UIThread.Post(async () =>
        {
            if (_window.IsVisible) return;
            _warming = true;
            _window.Opacity = 0;
            _window.ShowActivated = false;
            // 画面外に置く（画面内に透明で出す案も試したが初回の短縮効果は同程度だった）。macOS は画面外のウィンドウを
            // 0×0 に縮めるので EndWarmup で大きさを戻す
            _window.IsHitTestVisible = false;
            _window.Position = new PixelPoint(-32000, -32000);
            _window.PrepareShow();
            _window.Show();
            if (!_boardConfigured && _window.TryGetPlatformHandle()?.Handle is { } handle)
            {
                _platform.Window.ConfigureBoardWindow(handle);
                _boardConfigured = true;
            }
            _ = _window.PlayShowAsync(CancellationToken.None);
            await Task.Delay(400);
            if (!_warming) return; // 途中で本表示に切り替わった
            _window.Hide();
            EndWarmup();
            AppLog.Info("盤面の準備運動 完了");
            MemoryTrim.Schedule(() => !IsShown); // 起動時の読み込みの一時領域を返す
        }, DispatcherPriority.Background);
    }

    private void EndWarmup()
    {
        _warming = false;
        _window.IsHitTestVisible = true;
        _window.ShowActivated = true;
        // macOS は画面外に置いたウィンドウの大きさを 0×0 に縮めることがあり、その値が Width/Height に残る。
        // 大きさと不透明度（準備中に設定が変わっても今の値）を戻す。中身は作り直さない（作り直すと初回表示が重くなる）
        _window.RestoreSize();
        _window.ApplyOpacity(_settings.Appearance.Opacity);
    }

    public void Attach()
    {
        // Win: 盤面は最前面。Mac はウィンドウレベルで制御（ConfigureBoardWindow）
        _window.WindowService = _platform.Window;
        _window.BoardSizeChanged += ClampVisibleBoard;
        _window.Topmost = !OperatingSystem.IsMacOS();
        _platform.Hotkey.Pressed += ts => Dispatcher.UIThread.Post(() => OnTrigger(null, ts, "hotkey", PopupTriggerKind.Keyboard));
        _platform.MouseTrigger.Triggered += (p, ts) => Dispatcher.UIThread.Post(() => OnTrigger(p, ts, "mouse", PopupTriggerKind.Mouse));
        _platform.InputHook.DoubleTapped += ts => Dispatcher.UIThread.Post(() => OnTrigger(null, ts, "doubleTap", PopupTriggerKind.Keyboard));
        _platform.MouseTrigger.PassedThroughDown += p =>
        {
            _mouseHeld = true; // Post より先に立てる
            Dispatcher.UIThread.Post(() =>
            {
                _previewClickHeld = _window.IsOverPreview(p);
                if (_previewClickHeld) { _previewClickTicks = Stopwatch.GetTimestamp(); _outside.Reset(); return; }
                if (_window.ContextMenuOpen) { _outside.Reset(); return; }
                _outside.OnDown(p, IsShown, BoardBounds());
            });
        };
        _platform.MouseTrigger.PassedThroughUp += p =>
        {
            _mouseHeld = false;
            Dispatcher.UIThread.Post(() => OnPassedThroughUp(p));
        };

        _window.Deactivated += (_, _) => OnDeactivated();
        _leaveTimer.Tick += (_, _) => OnLeaveTick();
        _window.EscapePressed += () => Hide("Esc", restoreFocus: true);
        _window.SnippetInvoked += (snippet, copyOnly) => _ = SelectAsync(snippet, copyOnly);
    }

    /// <summary>
    /// 開いたときのグループ（SPEC §3.2）: 前回のグループ。空になっていたら番号の小さい順で最初の空でないグループ。すべて空なら前回のまま。
    /// </summary>
    internal static int StartGroup(TemplateBook book, int last)
    {
        if (book[last].Snippets.Count > 0) return last;
        for (int key = 0; key < TemplateBook.GroupCount; key++)
            if (book[key].Snippets.Count > 0) return key;
        return last;
    }

    /// <summary>トレイアイコンのクリック・2 つ目のインスタンス起動から。</summary>
    public void Toggle(string source)
    {
        // 表示中にトレイをクリックすると、先に外クリックで閉じてからこのクリックが届く → 再表示しない
        if (!IsShown && Stopwatch.GetElapsedTime(_lastHideTicks).TotalMilliseconds < 300) return;
        if (IsShown) Hide(source, restoreFocus: false);
        else Show(_platform.Window.GetCursorPosition(), Stopwatch.GetTimestamp(), source, PopupTriggerKind.Mouse);
    }

    /// <param name="groupKey">このグループで開く（トレイの「グループ ▸」）。null なら前回のグループ（SPEC §3.2）。</param>
    public void ShowFromExternal(string source, int? groupKey = null)
    {
        if (groupKey is { } key)
        {
            _requestedGroup = key;
            if (IsShown) _window.SwitchGroup(key);
        }
        // トレイ・メニュー・2 重起動はマウス操作の表示位置を使う（SPEC §3.3）
        if (!IsShown) Show(_platform.Window.GetCursorPosition(), Stopwatch.GetTimestamp(), source, PopupTriggerKind.Mouse);
        else BringToFront();
    }

    // ---------------- トリガー ----------------

    private void OnTrigger(ScreenPoint? point, long timestamp, string source, PopupTriggerKind kind)
    {
        if (timestamp - _lastTriggerTicks < DebounceTicks) return;
        _lastTriggerTicks = timestamp;

        if (IsShown)
        {
            if (_settings.Popup.ToggleOnTrigger) Hide($"toggle ({source})", restoreFocus: true);
            else BringToFront();
            return;
        }
        Show(point ?? _platform.Window.GetCursorPosition(), timestamp, source, kind);
    }

    private void Show(ScreenPoint anchor, long timestamp, string source, PopupTriggerKind kind)
    {
        CancelHideAnimation();
        if (_warming) EndWarmup();
        // 開くたびに先頭の行を選び直す（SPEC §3.2）。大きさが決まってから位置を計算する
        int group = _requestedGroup ?? StartGroup(_book(), _settings.Board.LastGroup);
        _requestedGroup = null;
        _window.Render(_book(), _settings.Appearance, group);
        _previousForeground = _platform.Window.CaptureForeground();
        _focusMovedElsewhere = false;
        _lastShowTicks = Stopwatch.GetTimestamp();
        _outside.Reset();

        var screens = _window.Screens;
        var anchorScreen = screens.ScreenFromPoint(new PixelPoint(anchor.X, anchor.Y)) ?? screens.Primary;
        double scaling = WindowScaling(anchorScreen?.Scaling ?? 1.0);
        int widthPx = (int)Math.Ceiling(_window.Width * scaling);
        int heightPx = (int)Math.Ceiling(_window.Height * scaling);

        ScreenRect WorkAreaAt(ScreenPoint p)
        {
            var s = screens.ScreenFromPoint(new PixelPoint(p.X, p.Y)) ?? screens.Primary;
            return s is null ? new ScreenRect(0, 0, 1920, 1080) : ToRect(s.WorkingArea);
        }
        var primary = screens.Primary is { } ps ? ToRect(ps.WorkingArea) : WorkAreaAt(anchor);

        var pos = PopupPlacement.Compute(_settings.Popup.PlacementFor(kind), anchor, widthPx, heightPx, WorkAreaAt, primary, _settings.Popup.CursorAnchor, scaling);
        _window.Position = new PixelPoint(pos.X, pos.Y);

        if (!_window.IsVisible)
        {
            _window.PrepareShow(); // 表示演出の開始状態（透明）で最初のフレームを描く
            _window.Show();
        }
        else _window.ResetFrame();
        _window.EnsureScaling(); // 拡大率の違うモニタに出したらアイコンをそのピクセル数で取り直す
        _window.Activate();

        if (_window.TryGetPlatformHandle()?.Handle is { } hwnd)
        {
            if (!_boardConfigured)
            {
                _platform.Window.ConfigureBoardWindow(hwnd);
                _boardConfigured = true;
            }
            bool fg = _platform.Window.BringToForeground(hwnd);
            if (!fg) AppLog.Info("前面化に失敗");
        }
        _window.Focus();

        Dispatcher.UIThread.Post(() =>
        {
            double ms = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
            AppLog.Info($"show ({source}/{kind}) at {pos.X},{pos.Y} size {widthPx}x{heightPx} scale={scaling} → 表示まで {ms:F1} ms");
        }, DispatcherPriority.Background);

        _leave.Reset();
        if (_settings.Popup.CloseOnMouseLeave) _leaveTimer.Start();

        // 表示演出は最初のフレームの後から（表示遅延の計測に含めない。spec/EFFECTS.md）。演出中も操作を受け付ける
        _effectCts = new CancellationTokenSource();
        _ = _window.PlayShowAsync(_effectCts.Token);
    }

    private void ClampVisibleBoard()
    {
        if (!IsShown) return;
        var screen = _window.Screens.ScreenFromPoint(_window.Position) ?? _window.Screens.Primary;
        if (screen is null) return;
        double scale = WindowScaling(screen.Scaling);
        var p = PopupPlacement.ClampInto(new(_window.Position.X, _window.Position.Y),
            (int)Math.Ceiling(_window.Width * scale), (int)Math.Ceiling(_window.Height * scale), ToRect(screen.WorkingArea));
        _window.Position = new PixelPoint(p.X, p.Y);
    }

    private void CancelHideAnimation()
    {
        _effectCts?.Cancel();
        _effectCts = null;
        if (!_hidingAnimation) return;
        _hidingAnimation = false;
        _hiding = false;
        _window.IsHitTestVisible = true;
        _window.ResetFrame();
    }

    private void BringToFront()
    {
        if (_window.TryGetPlatformHandle()?.Handle is { } hwnd) _platform.Window.BringToForeground(hwnd);
    }

    /// <summary>
    /// 閉じる。入力の受付をやめ、フォーカスの返却（SPEC §3.3）までは即座に行い、ウィンドウを隠すのだけ非表示演出の後にする。
    /// </summary>
    public void Hide(string reason, bool restoreFocus)
    {
        if (!IsShown) return;
        _window.DismissTransientUi();

        // 「前回の位置」を選んでいる組すべてに今の位置を記録する（SPEC §3.3。どの組も選んでいなければ書かない）
        var popupSettings = _settings.Popup;
        if (popupSettings.Keyboard.Position == PopupPosition.LastPosition || popupSettings.Mouse.Position == PopupPosition.LastPosition)
        {
            var pos = _window.Position;
            _hub.Update(s => s.Popup.RememberLastPosition(pos.X, pos.Y), SettingsChange.None);
        }

        // 前回のグループ（次に開くときの既定。SPEC §3.2）
        int group = _window.GroupKey;
        if (_settings.Board.LastGroup != group) _hub.Update(s => s.Board.LastGroup = group, SettingsChange.None);
        // macOS は返却・Hide() の途中で Deactivated が来るので、ここから隠し終わるまで「他へ移った」とみなさない
        _leaveTimer.Stop();
        _hiding = true;
        _hidingAnimation = true;
        _window.IsHitTestVisible = false;
        _outside.Reset();
        _lastHideTicks = Stopwatch.GetTimestamp();

        bool restore = restoreFocus && !_focusMovedElsewhere && _previousForeground != 0;
        bool restored = restore && _platform.Window.RestoreForeground(_previousForeground);
        AppLog.Info($"hide ({reason}) focus: {(restore ? (restored ? "returned" : "return FAILED") : "not returned")}");
        _previousForeground = 0;

        _effectCts?.Cancel();
        var cts = _effectCts = new CancellationTokenSource();
        _ = FinishHideAsync(cts.Token);
    }

    private async Task FinishHideAsync(CancellationToken ct)
    {
        await _window.PlayHideAsync(ct);
        if (ct.IsCancellationRequested) return; // 演出中に再表示された（CancelHideAnimation が後始末済み）
        try { _window.Hide(); }
        finally
        {
            _hiding = false;
            _hidingAnimation = false;
            _window.IsHitTestVisible = true;
        }
        MemoryTrim.Schedule(() => !IsShown);
    }

    // ---------------- 閉じる条件 ----------------

    /// <summary>盤面の上にダイアログを出す間、外クリック・非アクティブ化で盤面が閉じないようにする。</summary>
    public async Task<T> RunModalAsync<T>(Func<Task<T>> dialog)
    {
        _modal = true;
        try { return await dialog(); }
        finally
        {
            _modal = false;
            _outside.Reset();
            if (IsShown) BringToFront();
        }
    }

    private void OnPassedThroughUp(ScreenPoint p)
    {
        if (_previewClickHeld) { _previewClickHeld = false; _outside.Reset(); return; }
        var result = _outside.OnUp(p, IsShown, BoardBounds());
        if (!_settings.Popup.CloseOnOutsideClick || _modal || _window.ContextMenuOpen) return;
        switch (result)
        {
            case OutsideClickResult.CloseClick:
            case OutsideClickResult.CloseDragEndedOutside:
                _focusMovedElsewhere = true;
                Hide(result == OutsideClickResult.CloseClick ? "outside click" : "drag ended outside", restoreFocus: false);
                break;
            case OutsideClickResult.KeepOpenDroppedOnBoard:
                AppLog.Info("盤面外から盤面へのドロップ → 開いたまま");
                break;
        }
    }

    /// <summary>「マウスが離れたら閉じる」（表示中だけ 100 ms ごとにカーソル位置を見る。SPEC §3.3）。</summary>
    private void OnLeaveTick()
    {
        if (!IsShown || !_settings.Popup.CloseOnMouseLeave)
        {
            _leaveTimer.Stop();
            return;
        }
        if (_modal || _window.ContextMenuOpen || _mouseHeld || _window.IsMovingWindow) return; // ダイアログ中・移動中は判定しない
        if (_leave.Update(_platform.Window.GetCursorPosition(), BoardBounds(), _settings.Popup.MouseLeaveDistance))
            Hide("mouse leave", restoreFocus: true);
    }

    private void OnDeactivated()
    {
        if (_previewClickHeld || (_previewClickTicks != 0 && Stopwatch.GetElapsedTime(_previewClickTicks).TotalMilliseconds < 250)) return;
        if (_hiding || !IsShown || !_settings.Popup.CloseOnOutsideClick || _modal || _window.ContextMenuOpen) return;
        if (_mouseHeld)
        {
            // 盤面外でボタン押下中（つかんでドラッグしようとしている可能性）→ 離上時に判定する
            return;
        }
        double sinceShow = Stopwatch.GetElapsedTime(_lastShowTicks).TotalMilliseconds;
        if (sinceShow < ShowGraceMs)
        {
            // 前面化の取り合いで一瞬フォーカスが戻っただけ → 閉じずに取り返す
            AppLog.Info($"表示直後の非アクティブ化（{sinceShow:F0} ms）→ 閉じずに前面化し直す");
            Dispatcher.UIThread.Post(() => { if (IsShown) BringToFront(); });
            return;
        }
        _focusMovedElsewhere = true;
        Hide("deactivated", restoreFocus: false);
    }

    /// <summary>本文・貼り付け先・設定を非表示の前に固定する。</summary>
    private async Task SelectAsync(Snippet snippet, bool copyOnly)
    {
        if (!IsShown) return;
        var settings = _settings.Paste;
        nint target = _previousForeground;
        if (!_platform.Window.IsTargetValid(target)) target = 0;
        var request = new PasteRequest(snippet.Text, target, copyOnly, settings.AutoPaste, settings.RestoreClipboard, settings.RestoreDelayMs);
        Hide("snippet selected", restoreFocus: false);
        await _paste.ExecuteAsync(request);
    }

    public Task StopPastingAsync() => _paste.StopAsync();

    private ScreenRect BoardBounds()
    {
        double scaling = WindowScaling(_window.RenderScaling);
        return new ScreenRect(_window.Position.X, _window.Position.Y,
            (int)Math.Ceiling(_window.Bounds.Width * scaling), (int)Math.Ceiling(_window.Bounds.Height * scaling));
    }

    /// <summary>画面座標が論理座標（Mac の pt）なら大きさに拡大率を掛けない（SPEC §3.5）。</summary>
    private double WindowScaling(double renderScaling) =>
        _platform.Window.UsesLogicalScreenCoordinates ? 1.0 : renderScaling;

    private static ScreenRect ToRect(PixelRect r) => new(r.X, r.Y, r.Width, r.Height);
}
