using System.Diagnostics;
using FileLauncher.Core.Input;
using FileLauncher.Core.Model;
using SharpHook;
using SharpHook.Data;

namespace FileLauncher.Platform.Input;

/// <summary>
/// SharpHook（libuiohook）によるグローバル入力フック。ホットキーとマウストリガーで 1 つのフックを共有する。
/// Windows / macOS 共通（OS 差はライブラリが吸収）。判定ロジックは Core の TriggerDetector。
/// イベント抑止（SuppressEvent）が要るので SimpleGlobalHook を使う（TaskPoolGlobalHook は抑止できない）。
/// </summary>
public sealed class SharpHookInputService : IInputHookService, IHotkeyService, IMouseTriggerService
{
    private readonly object _gate = new();
    private readonly object _inputGate = new();
    private readonly ModifierDoubleTapDetector _doubleTap = new();
    private SimpleGlobalHook? _hook;
    private readonly TriggerDetector _detector;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private volatile bool _running;

    private readonly Func<KeyModifiers>? _currentModifiers;

    /// <param name="currentModifiers">
    /// マウス押下時の修飾キーを OS から直接読む関数（任意）。macOS では自アプリが前面のときに押した ⌃ などを
    /// フックが修飾キーとして記録しないことがあるため（2026-10-03 確認）、Mac はこれを渡す。null ならフックの値を使う。
    /// </param>
    public SharpHookInputService(Func<ScreenPoint, bool>? isDesktop = null, Func<KeyModifiers>? currentModifiers = null)
    {
        _detector = new TriggerDetector(isDesktop);
        _currentModifiers = currentModifiers;
        _doubleTap.Configure(new());
    }

    public bool IsRunning => _running;
    public event Action<long>? DoubleTapped;
    public void ConfigureDoubleTap(DoubleTapSetting setting) => _doubleTap.Configure(setting);

    public event Action<string>? Failed;
    public event Action<long>? Pressed;
    public event Action<ScreenPoint, long>? Triggered;
    public event Action<ScreenPoint>? PassedThroughDown;
    public event Action<ScreenPoint, long, int>? WheelTriggered;
    public event Action<ScreenPoint>? PassedThroughUp;

    public void Start()
    {
        SimpleGlobalHook hook;
        lock (_gate)
        {
            if (_running) return;
            // 失敗・停止したフックは再利用できないので作り直す（SPEC §10.3。Mac で権限を許可した後の再試行）
            _hook?.Dispose();
            _hook = hook = CreateHook();
        }
        _ = Task.Run(async () =>
        {
            try
            {
                await hook.RunAsync(useBackgroundThread: true);
            }
            catch (HookException ex)
            {
                // macOS: アクセシビリティ未許可だと ErrorAxApiDisabled でここに来る（SPEC §4.3）
                Failed?.Invoke($"{ex.Result}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Failed?.Invoke(ex.ToString());
            }
        });
    }

    private SimpleGlobalHook CreateHook()
    {
        var hook = new SimpleGlobalHook();
        hook.HookEnabled += (_, _) => { ResetInput(); _running = true; };
        hook.HookDisabled += (_, _) => { _running = false; ResetInput(); };
        hook.KeyPressed += OnKeyPressed;
        hook.KeyReleased += OnKeyReleased;
        hook.MousePressed += OnMousePressed;
        hook.MouseReleased += OnMouseReleased;
        hook.MouseWheel += OnMouseWheel;
        return hook;
    }

    // ---------------- IHotkeyService ----------------

    public bool Register(HotkeySetting hotkey, out string? error)
    {
        // フック方式は登録という手続きが無く、他アプリとの競合も検知できない（SPEC §13.1-2）
        _detector.SetHotkey(hotkey);
        error = null;
        return true;
    }

    public void Unregister() => _detector.SetHotkey(null);

    private volatile Action<string, KeyModifiers>? _capture;
    private readonly HashSet<string> _capturedKeys = new();

    public void BeginCapture(Action<string, KeyModifiers> onKey)
    {
        lock (_inputGate)
        {
            _doubleTap.SetCapture(true);
            _detector.CancelPendingGestures();
            _capture = onKey;
        }
    }
    public void EndCapture()
    {
        lock (_inputGate) { _capture = null; _doubleTap.SetCapture(false); }
    }

    // ---------------- IMouseTriggerService ----------------

    public void Configure(TriggerSettings settings) => _detector.SetMouse(settings);

    // ---------------- フックスレッド ----------------

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        lock (_inputGate)
        {
            if (e.IsEventSimulated) return;
            var modifier = ToModifierKey(e.Data.KeyCode);
            _doubleTap.OnKey(KeyName(e.Data.KeyCode), modifier, true, e.EventTime.Ticks / TimeSpan.TicksPerMillisecond);
            if (modifier is not null) return;
            if (_capturedKeys.Contains(KeyName(e.Data.KeyCode))) { e.SuppressEvent = true; return; }
            if (_capture is { } capture)
            {
                e.SuppressEvent = true;
                _capturedKeys.Add(KeyName(e.Data.KeyCode));
                capture(KeyName(e.Data.KeyCode), ToModifiers(e.RawEvent.Mask));
                return;
            }
            var d = _detector.OnKeyDown(KeyName(e.Data.KeyCode), ToModifiers(e.RawEvent.Mask));
            if (d.Suppress) e.SuppressEvent = true;
            if (d.Fire) Pressed?.Invoke(Stopwatch.GetTimestamp());
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        lock (_inputGate)
        {
            if (e.IsEventSimulated) return;
            var modifier = ToModifierKey(e.Data.KeyCode);
            bool tapped = _doubleTap.OnKey(KeyName(e.Data.KeyCode), modifier, false, e.EventTime.Ticks / TimeSpan.TicksPerMillisecond);
            if (tapped) DoubleTapped?.Invoke(Stopwatch.GetTimestamp());
            if (modifier is not null) return;
            if (_capturedKeys.Remove(KeyName(e.Data.KeyCode)))
            {
                e.SuppressEvent = true;
                _detector.OnKeyUp(KeyName(e.Data.KeyCode));
                return;
            }
            if (_detector.OnKeyUp(KeyName(e.Data.KeyCode))) e.SuppressEvent = true;
        }
    }

    private void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        lock (_inputGate)
        {
            if (e.IsEventSimulated) return;
            _doubleTap.OnMouseButton(ToButton(e.Data.Button), true);
            long ts = Stopwatch.GetTimestamp();
            var p = new ScreenPoint(e.Data.X, e.Data.Y);
            var mods = _currentModifiers?.Invoke() ?? ToModifiers(e.RawEvent.Mask);
            var d = _detector.OnMouseDown(ToButton(e.Data.Button), mods, p, _clock.ElapsedMilliseconds, allowFire: _capture is null);
            DebugLog($"mouse down {e.Data.Button} mods={mods} hookMask={e.RawEvent.Mask} → {d}");
            if (d.Suppress) e.SuppressEvent = true;
            if (d.Fire) Triggered?.Invoke(p, ts);
            if (d.PassedThrough) PassedThroughDown?.Invoke(p);
            if (d.LongPressToken is { } token) StartLongPress(token);
        }
    }

    private void OnMouseReleased(object? sender, MouseHookEventArgs e)
    {
        lock (_inputGate)
        {
            if (e.IsEventSimulated) return;
            _doubleTap.OnMouseButton(ToButton(e.Data.Button), false);
            var d = _detector.OnMouseUp(ToButton(e.Data.Button));
            DebugLog($"mouse up {e.Data.Button} → {d}");
            if (d.Suppress) e.SuppressEvent = true;
            if (d.PassedThrough) PassedThroughUp?.Invoke(new ScreenPoint(e.Data.X, e.Data.Y));
        }
    }

    private void OnMouseWheel(object? sender, MouseWheelHookEventArgs e)
    {
        lock (_inputGate)
        {
            if (e.IsEventSimulated) return;
            _doubleTap.OnWheel();
            if (_capture is not null) return;
            if (e.Data.Direction != MouseWheelScrollDirection.Vertical) return;
            var p = new ScreenPoint(e.Data.X, e.Data.Y);
            var mods = _currentModifiers?.Invoke() ?? ToModifiers(e.RawEvent.Mask);
            var d = _detector.OnMouseWheel(e.Data.Rotation, mods, p);
            if (d.Suppress) e.SuppressEvent = true;
            if (d.PageDelta != 0) WheelTriggered?.Invoke(p, Stopwatch.GetTimestamp(), d.PageDelta);
        }
    }

    private void StartLongPress(int token)
    {
        _ = Task.Delay(_detector.LongPressMs).ContinueWith(_ =>
        {
            lock (_inputGate)
            {
                if (_capture is not null) return;
                bool ok = _detector.OnLongPressElapsed(token, out var p);
                DebugLog($"long press elapsed token={token} → {ok}");
                if (ok) Triggered?.Invoke(p, Stopwatch.GetTimestamp());
            }
        }, TaskScheduler.Default);
    }

    private void ResetInput()
    {
        lock (_inputGate) { _doubleTap.Reset(); _detector.Reset(); _capturedKeys.Clear(); }
    }

    /// <summary>Debug ビルドだけ、入力の判定を Trace に出す（App の AppLog がファイルへ転送する）。</summary>
    [Conditional("DEBUG")]
    private static void DebugLog(string message) => Trace.WriteLine("[hook] " + message);

    // ---------------- 変換 ----------------

    private static ModifierKey? ToModifierKey(KeyCode key) => key switch
    {
        KeyCode.VcLeftControl => ModifierKey.LeftCtrl, KeyCode.VcRightControl => ModifierKey.RightCtrl,
        KeyCode.VcLeftAlt => ModifierKey.LeftAlt, KeyCode.VcRightAlt => ModifierKey.RightAlt,
        KeyCode.VcLeftShift => ModifierKey.LeftShift, KeyCode.VcRightShift => ModifierKey.RightShift,
        KeyCode.VcLeftMeta => ModifierKey.LeftMeta, KeyCode.VcRightMeta => ModifierKey.RightMeta,
        _ => null,
    };

    /// <summary>KeyCode.VcSpace → "Space"、VcA → "A"、VcF1 → "F1"（設定の HotkeySetting.Key と同じ表記）。</summary>
    internal static string KeyName(KeyCode k)
    {
        var s = k.ToString();
        return s.StartsWith("Vc", StringComparison.Ordinal) ? s[2..] : s;
    }

    private static KeyModifiers ToModifiers(EventMask m)
    {
        var r = KeyModifiers.None;
        if ((m & EventMask.Ctrl) != 0) r |= KeyModifiers.Ctrl;
        if ((m & EventMask.Alt) != 0) r |= KeyModifiers.Alt;
        if ((m & EventMask.Shift) != 0) r |= KeyModifiers.Shift;
        if ((m & EventMask.Meta) != 0) r |= KeyModifiers.Meta;
        return r;
    }

    private static InputButton ToButton(MouseButton b) => b switch
    {
        MouseButton.Button1 => InputButton.Left,
        MouseButton.Button2 => InputButton.Right,
        MouseButton.Button3 => InputButton.Middle,
        MouseButton.Button4 => InputButton.X1,
        MouseButton.Button5 => InputButton.X2,
        _ => InputButton.Other,
    };

    public void Dispose()
    {
        lock (_gate) _hook?.Dispose();
    }
}
