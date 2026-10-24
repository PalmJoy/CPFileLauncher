using CPClipboard.Core.Model;

namespace CPClipboard.Core.Paste;

public sealed record ClipboardItem(IReadOnlyDictionary<string, byte[]> Formats);
public sealed record ClipboardSnapshot(IReadOnlyList<ClipboardItem> Items)
{
    public const long MaxBytes = 32L * 1024 * 1024;
    public long ByteCount => Items.Sum(i => i.Formats.Values.Sum(v => (long)v.Length));
}
public enum PasteGuardResult { Ok, Elevated, SecureInput, NoPermission, Unknown }
public enum PasteResult { Pasted, Copied, NoTarget, ClipboardChanged, Elevated, SecureInput, NoPermission, ModifierHeld, FocusFailed, ClipboardBusy, SendFailed, CheckFailed, Cancelled }
public sealed record PasteRequest(string Text, nint Target, bool CopyOnly, bool AutoPaste, bool RestoreClipboard, int RestoreDelayMs);

/// <summary>Core の判定が利用する操作。App が Platform の実装へ委譲する。</summary>
public interface IPasteOperations
{
    long NowMs { get; }
    long ChangeToken { get; }
    KeyModifiers PressedModifiers { get; }
    bool IsTargetValid(nint target);
    bool IsForeground(nint target);
    bool RestoreForeground(nint target);
    PasteGuardResult Check(nint target);
    Task<ClipboardSnapshot?> SnapshotAsync();
    Task<long?> SetTextAsync(string text, bool transient, long? expectedToken = null);
    Task<bool> RestoreAsync(ClipboardSnapshot snapshot, long expectedToken);
    bool SendPaste();
    Task DelayAsync(int milliseconds, CancellationToken cancellationToken);
}
