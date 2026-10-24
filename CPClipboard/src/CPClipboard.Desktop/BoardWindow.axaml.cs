using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using CPClipboard.Core.Effects;
using CPClipboard.Core.Model;
using InputModifiers = Avalonia.Input.KeyModifiers;

namespace CPClipboard.App;

/// <summary>
/// 盤面ウィンドウ（SPEC §3.1）。上にグループタブ 0〜9、下に選んでいるグループの定型文を 1 件 1 行で並べる。
/// Close せず Hide/Show で再利用する（表示遅延 100ms 以内のため）。中身は Render で組み立てる（データが変わったときとグループ切替時のみ）。
/// </summary>
public partial class BoardWindow : Window
{
    private const int FramePadding = 6;
    private const int HeaderHeight = 34;    // ヘッダー 30 + 下の余白 3 + 区切り線 1
    private const int ListTopMargin = 3;
    private const int StatusBarHeight = 21; // ステータス行 19 + 上の余白 2

    /// <summary>空のグループのときの行数ぶんの高さ（SPEC §3.1）。</summary>
    internal const int MinVisibleRows = 3;

    private TemplateBook _book = TemplateBook.CreateEmpty();
    private AppearanceSettings _appearance = new();
    private int _groupKey;
    private double _renderedScaling;
    private Size _boardSize;
    private EffectSpec _showEffect = EffectSpec.None;
    private EffectSpec _hideEffect = EffectSpec.None;
    private EffectSpec _hoverEffect = EffectSpec.None;
    private EffectSpec _groupEffect = EffectSpec.None;
    private EffectSpec _tabEffect = EffectSpec.None;
    private EffectSpec _pressEffect = EffectSpec.None;
    private EffectSpec _pasteEffect = EffectSpec.None;
    private readonly Dictionary<int, Button> _rows = new();
    private CancellationTokenSource? _groupCts;

    /// <summary>true にするまで Close は Hide に置き換える（終了時のみ true）。</summary>
    public bool AllowClose { get; set; }

    /// <summary>定型文を選んだ（クリック / Enter）。bool は「コピーだけ」（Shift。SPEC §5.2）。</summary>
    public event Action<Snippet, bool>? SnippetInvoked;
    public event Action? EscapePressed;

    /// <summary>盤面表示中の Ctrl+, / ⌘,（SPEC §7 設定画面の入口）。</summary>
    public event Action? SettingsRequested;

    /// <summary>空のグループの「編集で追加…」（SPEC §3.1。そのグループで編集ウィンドウを開く）。</summary>
    public event Action<int>? AddRequested;

    /// <summary>グループが変わった（前回のグループの保存用。SPEC §3.2）。</summary>
    public event Action<int>? GroupChanged;
    public event Action? BoardSizeChanged;

    // 枠の部品（Themes/FrameChrome）の各部。Root = 余白込みの枠全体、Frame = 面
    private FrameChrome Root => Chrome;
    private Border Frame => Chrome.Frame;
    /// <summary>グリッチ演出の静止画層（spec/EFFECTS.md「glitch の詳細」。普段は非表示）。</summary>
    private readonly Canvas GlitchLayer = new() { Name = "GlitchLayer", IsVisible = false, IsHitTestVisible = false, ClipToBounds = false };

    /// <summary>
    /// 背景画像（LOOK §3.7）。image が null なら面の色だけ。画像があるときは面の色の代わりに画像（不透明度 imageOpacity）を敷き、
    /// その上に面の色（FlBoardBackground、配色に追従）を覆いとして重ねる。描き直し（Render）は要らない。
    /// </summary>
    public void ApplyBackground(BackgroundSettings bg, Avalonia.Media.Imaging.Bitmap? image)
    {
        if (image is null)
        {
            Backdrop.Source = null;
            Overlay.IsVisible = false;
            Chrome.SetSurfaceVisible(true);
            return;
        }
        // 画像があるときは地の色を描かない。暗さは覆いだけで決まり、画像の不透明度を下げるとデスクトップが透ける
        Chrome.SetSurfaceVisible(false);
        Backdrop.Fit = bg.Fit;
        Backdrop.ImageOpacity = bg.ImageOpacity / 100.0;
        Backdrop.Source = image;
        Overlay.Opacity = bg.Overlay / 100.0;
        Overlay.IsVisible = bg.Overlay > 0;
    }

    public BoardWindow()
    {
        InitializeComponent();
        // 常時の演出（明滅 → 走査線の帯 → 光の玉）はグリッチの静止画より下（spec/EFFECTS.md「常時の演出の詳細」）。
        // Frame の外（Overlays）なのでグリッチの静止画には写らない
        _pulseLayer = new PulseLayer();
        _beamLayer = new ScanBeamLayer();
        _orbLayer = new OrbLayer();
        _pulseLayer.Chamfer = _orbLayer.Chamfer = Chrome.Chamfer; // 発光・玉の形を枠（八角形）に合わせる。起動後は変わらない
        _ambientRoot = new Grid { IsHitTestVisible = false, IsVisible = false, Opacity = 0, Children = { _pulseLayer, _beamLayer, _orbLayer } };
        _ambient = new AmbientAnimator(this, _orbLayer, _pulseLayer, _beamLayer);
        // 帯は面（八角形）の中だけ。Overlays 自体は玉の暈のために切り抜かない
        _beamLayer.Clip = Chrome.FaceClip;
        Chrome.FaceClipChanged += (_, _) => _beamLayer.Clip = Chrome.FaceClip;
        _pulseLayer.SizeChanged += (_, _) => { if (_ambient.IsRunning) _pulseLayer.Refresh(); };
        ActualThemeVariantChanged += (_, _) =>
        {
            if (_ambient.IsRunning) Avalonia.Threading.Dispatcher.UIThread.Post(_pulseLayer.Refresh, Avalonia.Threading.DispatcherPriority.Loaded);
        };
        Chrome.Overlays.Children.Add(_markings);
        Chrome.Overlays.Children.Add(_ambientRoot);
        Chrome.Overlays.Children.Add(GlitchLayer);

        EmptyText.Text = Strings.Board_EmptyGroup;
        EmptyAdd.Content = Strings.Board_EmptyGroup_Add;
        EmptyAdd.Click += (_, _) => AddRequested?.Invoke(_groupKey);

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                EscapePressed?.Invoke();
                e.Handled = true;
            }
            else if (e.Key == Key.OemComma && e.KeyModifiers == (OperatingSystem.IsMacOS() ? InputModifiers.Meta : InputModifiers.Control))
            {
                SettingsRequested?.Invoke();
                e.Handled = true;
            }
        };
        AttachKeyboard();
        AttachMoveInput();
        AttachWheel();
        AttachPreview();
        RowsScroller.ScrollChanged += (_, _) =>
        {
            DismissPreview();
            RealizeRows(RowsScroller.Offset.Y);
            SchedulePreview();
        };
        Chrome.ContextMenu = BuildMenu(null);
        Opened += (_, _) => SchedulePreview();
    }

    /// <summary>選んでいるグループ（0〜9）。</summary>
    public int GroupKey => _groupKey;

    public void Render(TemplateBook book, AppearanceSettings appearance, int groupKey)
    {
        DismissTransientUi();
        _book = book;
        _appearance = appearance;
        _groupKey = Math.Clamp(groupKey, 0, TemplateBook.GroupCount - 1);
        _renderedScaling = RenderScaling;
        ApplyOpacity(appearance.Opacity);

        RenderTabs();
        RenderRows();
    }

    /// <summary>盤面の大きさを描画時の値に戻す（OS に縮められたとき用。中身は作り直さない）。</summary>
    public void RestoreSize()
    {
        Width = _boardSize.Width;
        Height = _boardSize.Height;
    }

    /// <summary>不透明度設定（SPEC §3.1）。演出の透明度（Frame）とは別の層（ウィンドウ）に掛ける。</summary>
    public void ApplyOpacity(int percent)
    {
        Opacity = percent / 100.0;
        _native?.SetOpacity(percent / 100.0); // OS の層には Window.Opacity が掛からない
    }

    /// <summary>演出の設定を反映する（spec/EFFECTS.md）。ホバーの色変化は行を作るときに付けるので描き直す。</summary>
    public void ApplyEffects(AppearanceSettings appearance)
    {
        _showEffect = EffectCatalog.Resolve(appearance, EffectCatalog.BoardShow);
        _hideEffect = EffectCatalog.Resolve(appearance, EffectCatalog.BoardHide);
        _glitchLook = GlitchLook.Scaled(appearance.GlitchIntensity);
        _hoverEffect = EffectCatalog.Resolve(appearance, EffectCatalog.RowHover);
        _groupEffect = EffectCatalog.Resolve(appearance, EffectCatalog.GroupSwitch);
        _tabEffect = EffectCatalog.Resolve(appearance, EffectCatalog.TabHighlight);
        _pressEffect = EffectCatalog.Resolve(appearance, EffectCatalog.RowPress);
        _pasteEffect = EffectCatalog.Resolve(appearance, EffectCatalog.PasteFlash);
        var orb = EffectCatalog.Resolve(appearance, EffectCatalog.FrameOrb);
        var beam = EffectCatalog.Resolve(appearance, EffectCatalog.ScanBeam);
        var pulse = EffectCatalog.Resolve(appearance, EffectCatalog.GlowPulse);
        _ambientAny = orb.IsActive || pulse.IsActive || beam.IsActive;
        if (UseNativeAmbient())
        {
            // OS の層（macOS = Core Animation）が描く。Avalonia の層は何も描かない
            _native!.Apply(orb, pulse, beam);
            _native.SetOpacity(Opacity);
            _orbLayer.Spec = _beamLayer.Spec = _ambient.PulseSpec = EffectSpec.None;
            _pulseLayer.IsVisible = false;
        }
        else
        {
            _orbLayer.Spec = orb;
            _ambient.Vsync = appearance.Vsync; // 次のフレームから新しい間隔で間引く
            _beamLayer.Spec = beam;
            _ambient.PulseSpec = pulse;
            _pulseLayer.IsVisible = pulse.IsActive;
        }
        UpdateAmbient();
        if (TabsPanel.Children.Count > 0)
        {
            RenderTabs();
            RenderRows();
        }
    }

    public bool HasShowEffect => _showEffect.IsActive;

    /// <summary>表示演出の開始前の状態にする（Show() の前に呼ぶ。最初のフレームから演出が始まるように）。</summary>
    public void PrepareShow()
    {
        GlitchPlayer.Finish(Frame, GlitchLayer);
        Root.Transitions = null;
        if (_showEffect.Kind == EffectKind.Glitch && _showEffect.IsActive)
        {
            // グリッチは盤面の静止画を撮るので Frame は描いておき、全体（Root）を隠しておく
            EffectRunner.Reset(Frame, visible: true);
            Root.Opacity = 0;
        }
        else if (_showEffect.IsActive)
        {
            Root.Opacity = 1;
            Frame.Opacity = 0;
        }
        else
        {
            Root.Opacity = 1;
            EffectRunner.Reset(Frame, visible: true);
        }
    }

    private readonly GlitchPlayer _glitch = new();

    /// <summary>グリッチの代わりに使う演出（静止画が撮れないとき。spec/EFFECTS.md glitch の詳細 7）。</summary>
    private static readonly EffectSpec GlitchFallback = new() { Kind = EffectKind.Fade, DurationMs = 100, Easing = EasingKind.EaseOut };

    public Task PlayShowAsync(CancellationToken ct) => DuringTransition(PlayShowCoreAsync(ct));

    private Task PlayShowCoreAsync(CancellationToken ct)
    {
        if (_showEffect.Kind == EffectKind.Glitch && _showEffect.IsActive)
        {
            var task = _glitch.Start(Root, Frame, GlitchLayer, _showEffect, _glitchLook, Token("FlAccent"), Token("FlAccent2"), ct);
            if (task is not null)
            {
                AppLog.Info($"glitch: スナップショット {_glitch.LastSnapshotMs:F1} ms");
                return task;
            }
            AppLog.Info("glitch: 静止画を撮れないので fade で代替");
            Root.Opacity = 1;
            return EffectRunner.ShowAsync(Frame, GlitchFallback, ct);
        }
        return EffectRunner.ShowAsync(Frame, _showEffect, ct);
    }

    public Task PlayHideAsync(CancellationToken ct) => DuringTransition(PlayHideCoreAsync(ct));

    private Task PlayHideCoreAsync(CancellationToken ct)
    {
        // 表示のグリッチ中に閉じられたら、まず静止画層を片付ける（二重に見えないように）
        GlitchPlayer.Finish(Frame, GlitchLayer);
        Root.Transitions = null;
        Root.Opacity = 1;
        if (_hideEffect.Kind == EffectKind.Glitch && _hideEffect.IsActive)
        {
            var task = _glitch.Start(Root, Frame, GlitchLayer, _hideEffect, _glitchLook, Token("FlAccent"), Token("FlAccent2"), ct, appearing: false);
            if (task is not null) return task;
            return EffectRunner.HideAsync(Frame, GlitchFallback with { Easing = EasingKind.EaseIn }, ct);
        }
        return EffectRunner.HideAsync(Frame, _hideEffect, ct);
    }

    // ---------------- 常時の演出（光の玉・発光の明滅・走査線の帯。spec/EFFECTS.md「常時の演出の詳細」） ----------------

    private readonly PulseLayer _pulseLayer;
    private readonly OrbLayer _orbLayer;
    private readonly ScanBeamLayer _beamLayer;
    private readonly Grid _ambientRoot;
    private readonly AmbientAnimator _ambient;
    private int _transitions; // 表示・非表示・グリッチの演出中（その間は止める）
    private bool _ambientAny; // 3 つのどれかが「なし」以外
    private GlitchLook _glitchLook = GlitchLook.Base;

    /// <summary>グリッチの強さ（テスト用）。</summary>
    internal GlitchLook GlitchLook => _glitchLook;
    private NativeAmbient? _native;
    private bool _nativeFailed;

    /// <summary>常時の演出を OS の層で描くサービス（App が入れる。null = Avalonia の層。--ambient avalonia）。</summary>
    internal CPClipboard.Platform.IAmbientLayerService? AmbientLayers { get; set; }

    /// <summary>OS の層で描いているとき（テスト用）。</summary>
    internal NativeAmbient? Native => _native;

    /// <summary>OS の層に取り付ける（初回だけ）。非対応・失敗なら false（Avalonia の層で描く）。</summary>
    private bool UseNativeAmbient()
    {
        if (_native is not null) return true;
        if (_nativeFailed || AmbientLayers is not { IsSupported: true } service) return false;
        var native = new NativeAmbient(this, service, _pulseLayer);
        if (!native.TryAttach()) { _nativeFailed = true; return false; }
        _native = native;
        return true;
    }

    /// <summary>OS の「視差効果を減らす」/「アニメーション効果 OFF」（App が Platform から渡す）。true の間は常時の演出を止める。</summary>
    public Func<bool>? ReducedMotion { get; set; }

    /// <summary>常時の演出が動いているか（テスト用）。</summary>
    internal bool AmbientRunning => _ambient.IsRunning;
    internal AmbientAnimator Ambient => _ambient;
    internal OrbLayer Orbs => _orbLayer;
    internal ScanBeamLayer ScanBeam => _beamLayer;
    internal ScanlineLayer ScanlineLayer => Scanlines;
    internal PulseLayer PulseLayer => _pulseLayer;

    private async Task DuringTransition(Task transition)
    {
        _transitions++;
        UpdateAmbient();
        try { await transition; }
        finally
        {
            _transitions--;
            UpdateAmbient();
        }
    }

    /// <summary>動く条件（種類・アニメーション・見えている・演出中でない・OS 設定）を見て、出す / 止める。</summary>
    private void UpdateAmbient()
    {
        if (_ambient is null) return; // 構築中（InitializeComponent の途中で IsVisible が変わる）
        if (_appearance is not null) UpdateClock(); // 時刻のタイマーも同じ条件（見えている）で動かす
        bool run = _ambientAny && IsVisible && _transitions == 0 && ReducedMotion?.Invoke() != true;
        if (_native is not null) { _native.SetRunning(run); return; } // OS の層: 止める・進めるだけ（Avalonia の層は動かさない）
        if (run == _ambient.IsRunning) return;
        if (run)
        {
            _ambientRoot.IsVisible = true;
            if (_ambient.PulseSpec.IsActive)
                Avalonia.Threading.Dispatcher.UIThread.Post(_pulseLayer.Refresh, Avalonia.Threading.DispatcherPriority.Loaded); // 大きさが決まってから焼く
            _ambientRoot.Transitions = [new Avalonia.Animation.DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(250) }];
            _ambientRoot.Opacity = 1;
            _ambient.Start();
        }
        else
        {
            _ambient.Stop();
            _ambientRoot.Transitions = null;
            _ambientRoot.Opacity = 0;
            _ambientRoot.IsVisible = false;
        }
    }

    /// <summary>演出を打ち切ったとき用: 表示状態に戻す（グリッチの静止画層も片付ける）。</summary>
    public void ResetFrame()
    {
        GlitchPlayer.Finish(Frame, GlitchLayer);
        Root.Transitions = null;
        Root.Opacity = 1;
        EffectRunner.Reset(Frame, visible: true);
        UpdateAmbient();
    }

    private IBrush Token(string key) =>
        this.TryFindResource(key, ActualThemeVariant, out var v) && v is IBrush b ? b : Brushes.Cyan;

    /// <summary>表示先モニタの拡大率が前回描画時と違えば描き直す（背景画像の作り置きも）。</summary>
    public void EnsureScaling()
    {
        if (Math.Abs(RenderScaling - _renderedScaling) <= 0.01) return;
        Render(_book, _appearance, _groupKey);
        Backdrop.Refresh(); // 同じ論理サイズのまま拡大率だけ変わったとき（SizeChanged が来ない）
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty)
        {
            UpdateAmbient();
            if (!IsVisible) { GroupSweep?.Stop(); DismissTransientUi(); }
            else SchedulePreview();
        }
    }

    // ---------------- グループタブ（SPEC §3.2） ----------------

    private void RenderTabs()
    {
        TabsPanel.Children.Clear();
        for (int key = 0; key < TemplateBook.GroupCount; key++)
        {
            int k = key;
            var group = _book[key];
            var tab = new ToggleButton
            {
                // 選択中のタブの文字は強く、他は弱く光らせる。空のグループは発光なし
                Content = TabContent(group, selected: key == _groupKey),
                IsChecked = key == _groupKey,
                Classes = { "tab" },
                Focusable = false,
            };
            if (group.Snippets.Count == 0) tab.Classes.Add("empty");
            tab.Click += (_, _) => SwitchGroup(k);
            AttachPresenterTransitions(tab, _tabEffect);
            if (_tabEffect.Kind == EffectKind.Glow) tab.Classes.Add("glow");
            TabsPanel.Children.Add(tab);
        }
        double available = Math.Max(1, _appearance.BoardWidth - FramePadding * 2 - 4);
        double total = 0;
        foreach (var tab in TabsPanel.Children.Cast<ToggleButton>()) { tab.Measure(Size.Infinity); total += tab.DesiredSize.Width; }
        if (total > available)
            for (int i = 0; i < TabsPanel.Children.Count; i++)
            {
                var tab = (ToggleButton)TabsPanel.Children[i];
                tab.Content = TabContent(_book[i], i == _groupKey, false);
                tab.MinWidth = 0;
                tab.Padding = new Thickness(2, 2);
                tab.Width = Math.Max(1, available / TemplateBook.GroupCount - 2);
            }
    }

    /// <summary>番号（等幅）+ 名前（最大 6 文字。超えたら …）。</summary>
    private static Control TabContent(SnippetGroup group, bool selected, bool showName = true)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        panel.Children.Add(new TextBlock
        {
            Text = group.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [!TextBlock.FontFamilyProperty] = new DynamicResourceExtension("FlMonoFontFamily"),
        });
        if (showName && group.Name.Length > 0)
            panel.Children.Add(new TextBlock { Text = ShortTabName(group.Name) });
        return group.Snippets.Count == 0 ? panel : Themed.Glow(panel, strong: selected);
    }

    private static string ShortTabName(string name)
    {
        var text = new System.Globalization.StringInfo(name);
        return text.LengthInTextElements > 6 ? text.SubstringByTextElements(0, 6) + "…" : name;
    }

    /// <summary>グループを切り替える（演出 groupSwitch。slide は切替方向から入ってくる）。</summary>
    public void SwitchGroup(int key)
    {
        key = Math.Clamp(key, 0, TemplateBook.GroupCount - 1);
        int direction = Math.Sign(key - _groupKey);
        Render(_book, _appearance, key);
        GroupChanged?.Invoke(_groupKey);
        _groupCts?.Cancel();
        _groupCts = new CancellationTokenSource();
        if (direction != 0)
        {
            _ = EffectRunner.PageInAsync(ListArea, _groupEffect, direction, _groupCts.Token);
            if (_groupEffect.IsActive)
            {
                GroupSweep.Reverse = direction < 0;
                GroupSweep.Play(_groupEffect.DurationMs);
            }
        }
    }

    /// <summary>前 / 次の空でないグループへ（循環。すべて空なら何もしない。SPEC §3.2）。</summary>
    public void SwitchGroupBy(int delta)
    {
        if (delta == 0) return;
        for (int i = 1; i < TemplateBook.GroupCount; i++)
        {
            int key = ((_groupKey + Math.Sign(delta) * i) % TemplateBook.GroupCount + TemplateBook.GroupCount) % TemplateBook.GroupCount;
            if (_book[key].Snippets.Count > 0)
            {
                SwitchGroup(key);
                return;
            }
        }
    }

    // ---------------- 行（SPEC §3.1） ----------------

    private int RowHeight => (int)Math.Round(_appearance.FontSize * 2.0);

    private void RenderRows()
    {
        DismissPreview();
        GroupSweep.Stop();
        var group = _book[_groupKey];
        DetachRowSignals();
        _rows.Clear();
        _rowStart = _rowEnd = -1;
        _selected = group.Snippets.Count > 0 ? 0 : -1;
        RowsScroller.Offset = default;
        RealizeRows(0, force: true);
        EmptyNotice.IsVisible = group.Snippets.Count == 0;

        BackgroundGrid.IsVisible = _appearance.Hud.Grid && _appearance.Hud.GridOpacity > 0;
        BackgroundGrid.Opacity = _appearance.Hud.GridOpacity / 100.0;
        Circuits.IsVisible = BackgroundGrid.IsVisible;
        Hologram.IsVisible = BackgroundGrid.IsVisible;
        Circuits.Opacity = Math.Min(0.25, _appearance.Hud.GridOpacity / 100.0 * 4);
        Scanlines.IsVisible = _appearance.Hud.Scanlines && _appearance.Hud.ScanlineOpacity > 0;
        Scanlines.Opacity = _appearance.Hud.ScanlineOpacity / 100.0;
        Scanlines.Pitch = _appearance.Hud.ScanlinePitch;
        bool status = _appearance.Hud.StatusBar;
        StatusBar.IsVisible = status;
        _markings.IsVisible = status;
        UpdateStatus();

        // サイズは明示する（表示前に位置計算するため）。ウィンドウ = 盤面 + 発光の余白（LOOK §3.6）
        int visibleRows = Math.Clamp(group.Snippets.Count, MinVisibleRows, _appearance.MaxRows);
        double glow = Chrome.GetMargin() * 2;
        // 枠線 1.5 px はレイアウトの丸めで 1 px 削れることがあるので切り上げる（行が 1 px はみ出してスクロールバーが出ないように）
        double border = Math.Ceiling(this.TryFindResource("FlBoardBorderThickness", ActualThemeVariant, out var bt) && bt is Thickness t ? t.Left : 1);
        _boardSize = new Size(_appearance.BoardWidth + glow,
            visibleRows * RowHeight + ListTopMargin + HeaderHeight + (status ? StatusBarHeight : 0) + (FramePadding + border) * 2 + glow);
        RestoreSize();
        if (IsVisible) BoardSizeChanged?.Invoke();
        UpdateSelection();
    }

    private Button RowButton(Snippet snippet, int rowHeight, int index)
    {
        var content = new SnippetRowContent();
        content.Update(snippet.Text, index, _appearance.FontSize);
        var button = new Button
        {
            Content = content,
            Classes = { "row" },
            Height = rowHeight,
            Focusable = false, // キーボードのフォーカスは盤面が持つ
            Tag = snippet,
        };
        button.ContextRequested += (_, e) =>
        {
            _selected = index;
            UpdateSelection();
            // 使っていない行のメニュー部品は保持しない。
            var menu = BuildMenu(snippet);
            menu.Open(button);
            e.Handled = true;
        };
        button.PointerExited += (_, _) => DismissPreview();
        AttachPresenterTransitions(button, _hoverEffect);
        if (_hoverEffect.Kind == EffectKind.Glow) button.Classes.Add("glow");

        // 押下で少し縮む（演出 rowPress）。Button が押下イベントを処理済みにするので handledEventsToo で受ける
        if (_pressEffect.IsActive)
        {
            content.RenderTransformOrigin = RelativePoint.Center;
            content.Transitions = EffectRunner.StateTransitions(_pressEffect, Visual.RenderTransformProperty);
            button.AddHandler(PointerPressedEvent, (_, e) =>
            {
                if (e.GetCurrentPoint(button).Properties.IsLeftButtonPressed) content.RenderTransform = EffectRunner.RowPressedTransform;
            }, Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
            void Release() => content.RenderTransform = EffectRunner.NoTransform;
            button.AddHandler(PointerReleasedEvent, (_, _) => Release(), Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
            button.PointerCaptureLost += (_, _) => Release();
            button.PointerExited += (_, _) => Release();
        }
        // マウスを乗せた行がそのまま選択になる（キーボードの選択と 1 つ。SPEC §6.1）
        button.PointerEntered += (_, _) =>
        {
            if (_ignoreRowHover) return;
            _selected = index;
            UpdateSelection();
        };
        button.PointerMoved += (_, _) =>
        {
            _ignoreRowHover = false;
            if (_selected == index) return;
            _selected = index;
            UpdateSelection();
        };
        button.AddHandler(PointerPressedEvent, (_, e) => _pressModifiers = e.KeyModifiers,
            Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
        button.Click += (_, _) => Invoke(snippet, _pressModifiers.HasFlag(InputModifiers.Shift));
        return button;
    }

    private InputModifiers _pressModifiers;

    private void Invoke(Snippet snippet, bool copyOnly)
    {
        if (_rows.Values.FirstOrDefault(r => ReferenceEquals(r.Tag, snippet)) is { } row && _pasteEffect.IsActive)
        {
            // 演出 pasteFlash: 行の発光を一瞬強める（盤面の非表示演出と並行。貼り付けは待たせない）
            row.Classes.Add("pasting");
            Avalonia.Threading.DispatcherTimer.RunOnce(() => row.Classes.Remove("pasting"), TimeSpan.FromMilliseconds(_pasteEffect.DurationMs));
        }
        DismissPreview();
        if (_pasteEffect.IsActive && _rows.Values.FirstOrDefault(r => ReferenceEquals(r.Tag, snippet))?.Content is Grid grid)
        {
            _sendSignal ??= new SendSignal { Name = "SendSignal", [!SignalPulse.StrokeProperty] = Themed.Res("FlAccent") };
            MoveSignal(_sendSignal, grid);
            _sendSignal.Play(_pasteEffect.DurationMs);
        }
        SnippetInvoked?.Invoke(snippet, copyOnly);
    }

    private static void AttachPresenterTransitions(ContentControl control, EffectSpec spec)
    {
        if (!spec.IsActive) return;
        control.TemplateApplied += (_, e) =>
        {
            if (e.NameScope.Find<ContentPresenter>("PART_ContentPresenter") is not { } presenter) return;
            presenter.Transitions = EffectRunner.StateTransitions(spec,
                ContentPresenter.BackgroundProperty, ContentPresenter.BorderBrushProperty, ContentPresenter.BoxShadowProperty);
        };
    }

    private readonly FrameMarkings _markings = new() { Name = "FrameMarkings", [!FrameMarkings.StrokeProperty] = Themed.Res("FlAccent") };

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
