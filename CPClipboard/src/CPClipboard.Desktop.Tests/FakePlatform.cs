using CPClipboard.Core.Paste;
using CPClipboard.Core.Input;
using CPClipboard.Core.Model;
using CPClipboard.Platform;

namespace CPClipboard.Desktop.Tests;

/// <summary>OS に触らない IPlatformServices。呼ばれた内容を記録する。</summary>
internal sealed class FakePlatform : IPlatformServices, IInputHookService, IHotkeyService, IMouseTriggerService,
    IShellService, IWindowService, IAutoStartService, IPermissionService
{
    public bool HookRunning { get; set; }
    public HotkeySetting? RegisteredHotkey { get; private set; }
    public Action<string, KeyModifiers>? Capture { get; private set; }
    /// <summary>Open に渡されたパス・URL。</summary>
    public List<string> Opened { get; } = new();

    public IInputHookService InputHook => this;
    public IHotkeyService Hotkey => this;
    public IMouseTriggerService MouseTrigger => this;
    public IClipboardService Clipboard { get; } = new FakeClipboard();
    public IKeySender KeySender { get; } = new FakeKeys();
    public IPasteGuard PasteGuard { get; } = new FakeGuard();
    public IShellService Shell => this;
    public IWindowService Window => this;
    public IAutoStartService AutoStart => this;
    public IPermissionService Permissions => this;

    /// <summary>既定は非対応（従来の Avalonia の層）。OS の層のテストでは FakeAmbientLayers を入れる。</summary>
    public IAmbientLayerService Ambient { get; set; } = AmbientLayers.Unsupported;

    // 入力フック
    public void Start() { }
    public bool IsRunning => HookRunning;
    public DoubleTapSetting? ConfiguredDoubleTap { get; private set; }
    public void ConfigureDoubleTap(DoubleTapSetting setting) => ConfiguredDoubleTap = setting;
    public KeyModifiers PressedModifiers => KeyModifiers.None;
    public event Action<long>? DoubleTapped { add { } remove { } }
    public event Action<string>? Failed { add { } remove { } }

    // ホットキー
    public bool Register(HotkeySetting hotkey, out string? error) { RegisteredHotkey = hotkey; error = null; return true; }
    public void Unregister() => RegisteredHotkey = null;
    public void BeginCapture(Action<string, KeyModifiers> onKey) => Capture = onKey;
    public void EndCapture() => Capture = null;
    public event Action<long>? Pressed { add { } remove { } }

    // マウス
    public TriggerSettings? Configured { get; private set; }
    public void Configure(TriggerSettings settings) => Configured = settings;
    public event Action<ScreenPoint, long>? Triggered { add { } remove { } }
    public event Action<ScreenPoint>? PassedThroughDown { add { } remove { } }
    public event Action<ScreenPoint>? PassedThroughUp { add { } remove { } }

    // シェル
    public LaunchResult Open(string pathOrUrl)
    {
        Opened.Add(pathOrUrl);
        return LaunchResult.Ok;
    }

    // ウィンドウ
    public bool UsesLogicalScreenCoordinates => false;
    public void ConfigurePreviewWindow(nint windowHandle) { }
    public void ConfigureBoardWindow(nint windowHandle) { }
    public nint Foreground { get; set; }
    public nint CaptureForeground() => Foreground;
    public bool IsTargetValid(nint target) => target != 0;
    public bool IsForeground(nint target) => target == Foreground;
    public nint PreviousForeground() => Foreground;
    public bool ReducedMotion { get; set; }
    bool IWindowService.PrefersReducedMotion => ReducedMotion;
    public bool BringToForeground(nint windowHandle) => true;
    public List<nint> RestoredTargets { get; } = [];
    public bool RestoreForeground(nint previous) { RestoredTargets.Add(previous); Foreground = previous; return true; }
    public ScreenPoint GetCursorPosition() => new(100, 100);

    // 自動起動・権限
    public bool IsEnabled { get; private set; }
    public void SetEnabled(bool enabled, string executablePath) => IsEnabled = enabled;
    public PermissionState InputMonitoring => PermissionState.Granted;
    public PermissionState Accessibility => PermissionState.Granted;
    public void RequestAccessibility() { }
    public void OpenSystemSettings() { }

    public void Dispose() { }
}

internal sealed class FakeKeys : IKeySender { public int Sent { get; private set; } public bool SendPaste() { Sent++; return true; } }
internal sealed class FakeGuard : IPasteGuard { public PasteGuardResult Check(nint target) => PasteGuardResult.Ok; }
internal sealed class FakeClipboard : IClipboardService
{
    public long ChangeToken { get; private set; }
    public string? Text { get; private set; }
    public bool Transient { get; private set; }
    public Task<ClipboardSnapshot?> SnapshotAsync() => Task.FromResult<ClipboardSnapshot?>(new ClipboardSnapshot([]));
    public Task<long?> SetTextAsync(string text, bool transient, long? expectedToken = null)
    {
        if (expectedToken is { } token && token != ChangeToken) return Task.FromResult<long?>(null);
        Text = text; Transient = transient; return Task.FromResult<long?>(++ChangeToken);
    }
    public Task<bool> RestoreAsync(ClipboardSnapshot snapshot, long expectedToken)
    { if (ChangeToken != expectedToken) return Task.FromResult(false); Text = null; ChangeToken++; return Task.FromResult(true); }
    public Func<Task<string?>>? ReadOverride { get; set; }
    public Task<string?> GetTextAsync() => ReadOverride?.Invoke() ?? Task.FromResult(Text);
}
