using System.Runtime.Versioning;
using CPClipboard.Core.Model;
using CPClipboard.Platform.Input;
using Microsoft.Win32;

namespace CPClipboard.Platform.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsPlatformServices : IPlatformServices
{
    private readonly SharpHookInputService _input;

    public WindowsPlatformServices()
    {
        _input = new SharpHookInputService();
    }

    public IInputHookService InputHook => _input;
    public IHotkeyService Hotkey => _input;
    public IMouseTriggerService MouseTrigger => _input;
    public IClipboardService Clipboard { get; } = new WindowsClipboardService();
    public IKeySender KeySender { get; } = new SharpHookKeySender(false);
    public IPasteGuard PasteGuard { get; } = new WindowsPasteGuard();
    public IShellService Shell { get; } = new WindowsShellService();
    public IWindowService Window { get; } = new WindowsWindowService();
    public IAutoStartService AutoStart { get; } = new WindowsAutoStartService();
    public IPermissionService Permissions { get; } = new WindowsPermissionService();

    public void Dispose()
    {
        _input.Dispose();
        (Clipboard as IDisposable)?.Dispose();
        (Window as IDisposable)?.Dispose();
    }
}

/// <summary>Windows には入力監視の権限が無いので常に許可済み。</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsPermissionService : IPermissionService
{
    public PermissionState InputMonitoring => PermissionState.Granted;
    public PermissionState Accessibility => PermissionState.Granted;
    public void RequestAccessibility() { }
    public void OpenSystemSettings() { }
}

/// <summary>ログイン時自動起動: HKCU\Software\Microsoft\Windows\CurrentVersion\Run（SPEC §10.3）。</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsAutoStartService : IAutoStartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "CPClipboard";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
    }

    public void SetEnabled(bool enabled, string executablePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(ValueName, $"\"{executablePath}\"");
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
