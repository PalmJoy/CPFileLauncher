using System.Diagnostics;
using CPClipboard.Core.Model;
using CPClipboard.Core.Paste;
using CPClipboard.Platform;

namespace CPClipboard.App;

internal sealed class PasteController
{
    private readonly CancellationTokenSource _shutdown = new();
    private readonly PasteFlow _flow;
    private readonly Operations _operations;
    private readonly HashSet<Task> _active = [];
    internal Action<string, bool> Notify { get; set; } = (text, error) => Toast.Show(text, error);

    public PasteController(IPlatformServices platform, Func<bool> canRestore)
    {
        _operations = new Operations(platform, canRestore);
        _flow = new(_operations);
    }
    public async Task<PasteResult> ExecuteAsync(PasteRequest request)
    {
        AppLog.Info($"paste start target={request.Target} copyOnly={request.CopyOnly} auto={request.AutoPaste} restore={request.RestoreClipboard} modifiers={_operations.PressedModifiers}");
        var task = _flow.ExecuteAsync(request, _shutdown.Token);
        _active.Add(task);
        PasteResult result;
        try { result = await task; }
        catch (Exception ex) { AppLog.Info($"貼り付け処理の失敗: {ex.Message}"); result = PasteResult.ClipboardBusy; }
        finally { _active.Remove(task); }
        if (!_shutdown.IsCancellationRequested && Message(result, request) is { } message) Notify(message, result == PasteResult.ClipboardBusy);
        AppLog.Info($"paste result={result}");
        return result;
    }
    public async Task StopAsync()
    {
        _shutdown.Cancel();
        try { await Task.WhenAll(_active.ToArray()); }
        catch (Exception ex) { AppLog.Info($"貼り付け終了待ち: {ex.Message}"); }
    }
    internal static string? Message(PasteResult result, PasteRequest request) => result switch
    {
        PasteResult.NoTarget => Strings.Toast_Copied,
        PasteResult.ClipboardChanged => Strings.Toast_ClipboardChanged,
        PasteResult.Copied when (request.AutoPaste && request.CopyOnly) || (request.AutoPaste && request.Target == 0) => Strings.Toast_Copied,
        PasteResult.Elevated => Strings.Toast_CopiedOnly_Elevated,
        PasteResult.SecureInput => Strings.Toast_CopiedOnly_SecureInput,
        PasteResult.NoPermission => Strings.Toast_CopiedOnly_NoPermission,
        PasteResult.ModifierHeld => Strings.Toast_CopiedOnly_ModifierHeld,
        PasteResult.FocusFailed => Strings.Toast_CopiedOnly_Focus,
        PasteResult.SendFailed => Strings.Toast_CopiedOnly_SendFailed,
        PasteResult.CheckFailed => Strings.Toast_CopiedOnly_CheckFailed,
        PasteResult.ClipboardBusy => Strings.Toast_ClipboardBusy,
        _ => null,
    };
    private sealed class Operations(IPlatformServices platform, Func<bool> canRestore) : IPasteOperations
    {
        public long ChangeToken => platform.Clipboard.ChangeToken;
        public long NowMs => (long)Stopwatch.GetElapsedTime(0).TotalMilliseconds;
        public KeyModifiers PressedModifiers => platform.InputHook.PressedModifiers;
        public bool IsTargetValid(nint target) => platform.Window.IsTargetValid(target);
        public bool IsForeground(nint target) => canRestore() && platform.Window.IsForeground(target);
        public bool RestoreForeground(nint target)
        {
            bool result = canRestore() && platform.Window.RestoreForeground(target);
            AppLog.Info($"paste focus target={target} requested={result} {FocusState()}");
            return result;
        }
        public PasteGuardResult Check(nint target)
        {
            if (!platform.InputHook.IsRunning)
                return platform.Permissions.Accessibility == PermissionState.Denied ? PasteGuardResult.NoPermission : PasteGuardResult.Unknown;
            try { return platform.PasteGuard.Check(target); }
            catch { return PasteGuardResult.Unknown; }
        }
        public Task<ClipboardSnapshot?> SnapshotAsync() => platform.Clipboard.SnapshotAsync();
        public async Task<long?> SetTextAsync(string text, bool transient, long? expectedToken = null)
        {
            var token = await platform.Clipboard.SetTextAsync(text, transient, expectedToken);
            AppLog.Info($"paste clipboard written={token.HasValue} token={token} transient={transient}");
            return token;
        }
        public async Task<bool> RestoreAsync(ClipboardSnapshot snapshot, long expectedToken)
        {
            bool restored = await platform.Clipboard.RestoreAsync(snapshot, expectedToken);
            AppLog.Info($"paste clipboard restored={restored} expected={expectedToken}");
            return restored;
        }
        public bool SendPaste()
        {
            AppLog.Info($"paste send modifiers={PressedModifiers} {FocusState()}");
            bool sent = canRestore() && platform.KeySender.SendPaste();
            AppLog.Info($"paste send accepted={sent}");
            return sent;
        }
        private string FocusState() => OperatingSystem.IsMacOS() && platform.Window is CPClipboard.Platform.MacOS.MacWindowService mac
            ? mac.DescribeInputFocus() : "";
        public Task DelayAsync(int milliseconds, CancellationToken cancellationToken) => Task.Delay(milliseconds, cancellationToken);
    }
}
