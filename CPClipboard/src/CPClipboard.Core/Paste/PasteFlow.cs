using CPClipboard.Core.Model;

namespace CPClipboard.Core.Paste;

/// <summary>貼り付けを直列化し、前の復元待ちだけを次の要求で短縮する。</summary>
public sealed class PasteFlow(IPasteOperations operations)
{
    public const int ForegroundTimeoutMs = 300, ModifierTimeoutMs = 1000;
    private readonly SemaphoreSlim _serial = new(1);
    private readonly object _gate = new();
    private CancellationTokenSource? _restoreWait;
    private int _waiting;

    public async Task<PasteResult> ExecuteAsync(PasteRequest request, CancellationToken cancellationToken = default)
    {
        lock (_gate) { _waiting++; _restoreWait?.Cancel(); }
        bool acquired = false;
        try
        {
            await _serial.WaitAsync(cancellationToken);
            acquired = true;
            lock (_gate) _waiting--;
            return await RunAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) { return PasteResult.Cancelled; }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        { return PasteResult.ClipboardBusy; }
        finally
        {
            if (acquired) _serial.Release();
            else lock (_gate) _waiting--;
        }
    }

    private async Task<PasteResult> RunAsync(PasteRequest r, CancellationToken ct)
    {
        bool paste = r.AutoPaste && !r.CopyOnly && r.Target != 0 && operations.IsTargetValid(r.Target);
        var guard = paste ? SafeCheck(r.Target) : PasteGuardResult.Ok;
        bool allowed = paste && guard == PasteGuardResult.Ok;
        var snapshot = allowed && r.RestoreClipboard ? await operations.SnapshotAsync() : null;
        if (snapshot?.ByteCount > ClipboardSnapshot.MaxBytes)
        { System.Diagnostics.Trace.WriteLine("[paste] Snapshot exceeds 32 MiB."); snapshot = null; }
        ct.ThrowIfCancellationRequested();
        var written = await operations.SetTextAsync(r.Text, allowed);
        if (written is not { } token) return PasteResult.ClipboardBusy;
        if (!allowed)
        {
            if (r.Target != 0 && operations.IsTargetValid(r.Target)) operations.RestoreForeground(r.Target);
            return !paste && r.AutoPaste && !r.CopyOnly ? PasteResult.NoTarget : GuardResult(guard);
        }

        bool sent = false;
        PasteResult stageFailure = PasteResult.FocusFailed;
        try
        {
            if (!operations.RestoreForeground(r.Target) || !await WaitAsync(() => operations.IsForeground(r.Target), ForegroundTimeoutMs, ct))
                return await FallbackAsync(PasteResult.FocusFailed);
            stageFailure = PasteResult.ModifierHeld;
            if (!await WaitAsync(() => operations.PressedModifiers == KeyModifiers.None, ModifierTimeoutMs, ct))
                return await FallbackAsync(PasteResult.ModifierHeld);
            if (!operations.IsForeground(r.Target)) return await FallbackAsync(PasteResult.FocusFailed);
            guard = SafeCheck(r.Target);
            if (guard != PasteGuardResult.Ok) return await FallbackAsync(GuardResult(guard));
            ct.ThrowIfCancellationRequested();
            if (operations.ChangeToken != token) return PasteResult.ClipboardChanged;
            bool success;
            try { success = operations.SendPaste(); }
            catch { success = false; }
            if (!success) return await FallbackAsync(PasteResult.SendFailed);
            sent = true;
            if (snapshot is null) return PasteResult.Pasted;
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(ct);
            lock (_gate) { _restoreWait = wait; if (_waiting > 0) wait.Cancel(); }
            try { await operations.DelayAsync(r.RestoreDelayMs, wait.Token); }
            catch (OperationCanceledException) { /* 次の選択または終了の前に条件付きで復元 */ }
            finally { lock (_gate) _restoreWait = null; }
            try { await operations.RestoreAsync(snapshot, token); }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"[paste] Restore failed: {ex.Message}"); }
            return PasteResult.Pasted;
        }
        catch (OperationCanceledException)
        {
            if (!sent) await FallbackAsync(PasteResult.Cancelled);
            return PasteResult.Cancelled;
        }
        catch (Exception)
        {
            return await FallbackAsync(stageFailure);
        }

        async Task<PasteResult> FallbackAsync(PasteResult result)
        {
            // ユーザーがコピーした別の内容を上書きせず、自分の定型文の履歴除外印だけ外す。
            try { await operations.SetTextAsync(r.Text, false, token); }
            catch { return PasteResult.ClipboardBusy; }
            return result;
        }
    }

    private PasteGuardResult SafeCheck(nint target)
    {
        try { return operations.Check(target); }
        catch { return PasteGuardResult.Unknown; }
    }

    private async Task<bool> WaitAsync(Func<bool> predicate, int timeout, CancellationToken ct)
    {
        long start = operations.NowMs;
        while (!predicate())
        {
            ct.ThrowIfCancellationRequested();
            if (operations.NowMs - start >= timeout) return false;
            await operations.DelayAsync(10, ct);
        }
        return true;
    }
    private static PasteResult GuardResult(PasteGuardResult guard) => guard switch
    {
        PasteGuardResult.Elevated => PasteResult.Elevated,
        PasteGuardResult.SecureInput => PasteResult.SecureInput,
        PasteGuardResult.NoPermission => PasteResult.NoPermission,
        PasteGuardResult.Unknown => PasteResult.CheckFailed,
        _ => PasteResult.Copied,
    };
}
