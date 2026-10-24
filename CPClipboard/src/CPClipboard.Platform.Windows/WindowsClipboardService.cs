using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using CPClipboard.Core.Model;
using CPClipboard.Core.Paste;
using static CPClipboard.Platform.Windows.Native.ClipboardNative;

namespace CPClipboard.Platform.Windows;

/// <summary>所有ウィンドウとクリップボード操作を専用 STA スレッドにまとめる。</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsClipboardService : IClipboardService, IDisposable
{
    private readonly BlockingCollection<Action<nint>> _jobs = new();
    private readonly Thread _thread;
    private readonly TaskCompletionSource<nint> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public WindowsClipboardService()
    {
        _thread = new Thread(Worker) { IsBackground = true, Name = "Clipboard STA" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }
    public long ChangeToken => GetClipboardSequenceNumber();
    private void Worker()
    {
        nint owner = CreateWindowEx(0, "STATIC", "CPClipboard Clipboard", 0, 0, 0, 0, 0, new nint(-3), 0, 0, 0);
        if (owner == 0) { _ready.TrySetException(new Win32Exception(Marshal.GetLastWin32Error())); return; }
        _ready.TrySetResult(owner);
        try
        {
            while (!_jobs.IsCompleted)
            {
                if (_jobs.TryTake(out var job, 10)) job(owner);
                while (PeekMessage(out var message, 0, 0, 0, 1)) { TranslateMessage(ref message); DispatchMessage(ref message); }
            }
        }
        finally { DestroyWindow(owner); }
    }
    private async Task<T> Run<T>(Func<T> operation)
    {
        await _ready.Task.ConfigureAwait(false);
        var task = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _jobs.Add(owner =>
        {
            try
            {
                bool opened = false;
                for (int attempt = 0; attempt <= 20; attempt++)
                {
                    if (OpenClipboard(owner)) { opened = true; break; }
                    if (attempt < 20) Thread.Sleep(10);
                }
                if (!opened) throw new IOException("Cannot open clipboard.");
                try { task.SetResult(operation()); }
                finally { CloseClipboard(); }
            }
            catch (Exception ex) { task.TrySetException(ex); }
        });
        return await task.Task.ConfigureAwait(false);
    }
    public Task<ClipboardSnapshot?> SnapshotAsync() => Run<ClipboardSnapshot?>(() =>
    {
        var formats = new Dictionary<string, byte[]>(); long total = 0;
        for (uint format = EnumClipboardFormats(0); format != 0; format = EnumClipboardFormats(format))
        {
            if (format is 2 or 3 or 9 or 14 or 0x80 or 0x82 or 0x83 or 0x8e) continue;
            nint handle = GetClipboardData(format);
            ulong size = (ulong)GlobalSize(handle);
            if (size == 0) continue;
            if (size > (ulong)(ClipboardSnapshot.MaxBytes - total)) { System.Diagnostics.Trace.WriteLine("[clipboard] Snapshot exceeds 32 MiB."); return null; }
            nint data = GlobalLock(handle); if (data == 0) continue;
            try
            {
                var bytes = new byte[(int)size]; Marshal.Copy(data, bytes, 0, bytes.Length);
                formats[format.ToString(System.Globalization.CultureInfo.InvariantCulture)] = bytes; total += bytes.Length;
            }
            finally { GlobalUnlock(handle); }
        }
        return new ClipboardSnapshot(formats.Count == 0 ? [] : [new(formats)]);
    });
    public Task<long?> SetTextAsync(string text, bool transient, long? expectedToken = null) => Run<long?>(() =>
    {
        if (expectedToken is { } token && ChangeToken != token) return null;
        var data = Encoding.Unicode.GetBytes(TemplateBook.NormalizeNewlines(text).Replace("\n", "\r\n") + "\0");
        Write(new Dictionary<uint, byte[]> { [13] = data }, transient);
        return ChangeToken;
    });
    public Task<bool> RestoreAsync(ClipboardSnapshot snapshot, long expectedToken) => Run(() =>
    {
        if (ChangeToken != expectedToken || snapshot.ByteCount > ClipboardSnapshot.MaxBytes) return false;
        var formats = snapshot.Items.SelectMany(i => i.Formats).ToDictionary(p => uint.Parse(p.Key, System.Globalization.CultureInfo.InvariantCulture), p => p.Value);
        Write(formats, formats.Count > 0);
        return true;
    });
    public Task<string?> GetTextAsync() => Run(() =>
    {
        nint memory = GetClipboardData(13), data = GlobalLock(memory);
        if (data == 0) return null;
        try { return Marshal.PtrToStringUni(data); }
        finally { GlobalUnlock(memory); }
    });
    private static void Write(Dictionary<uint, byte[]> formats, bool transient)
    {
        if (transient)
        {
            formats[RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing")] = [0];
            formats[RegisterClipboardFormat("CanIncludeInClipboardHistory")] = new byte[4];
            formats[RegisterClipboardFormat("CanUploadToCloudClipboard")] = new byte[4];
        }
        var prepared = new List<(uint Format, nint Memory)>();
        try
        {
            foreach (var (format, bytes) in formats)
            {
                nint memory = GlobalAlloc(2, (nuint)Math.Max(1, bytes.Length));
                if (memory == 0) throw new IOException("Cannot allocate clipboard data.");
                prepared.Add((format, memory));
                nint data = GlobalLock(memory); if (data == 0) throw new IOException("Cannot lock clipboard data.");
                try { if (bytes.Length > 0) Marshal.Copy(bytes, 0, data, bytes.Length); }
                finally { GlobalUnlock(memory); }
            }
            if (!EmptyClipboard()) throw new IOException("Cannot clear clipboard.");
            for (int i = 0; i < prepared.Count; i++)
            {
                var entry = prepared[i];
                if (SetClipboardData(entry.Format, entry.Memory) == 0) throw new IOException("Cannot write clipboard data.");
                prepared[i] = (entry.Format, 0); // 所有権は OS へ移った
            }
        }
        finally { foreach (var entry in prepared) if (entry.Memory != 0) GlobalFree(entry.Memory); }
    }
    public void Dispose() { _jobs.CompleteAdding(); _thread.Join(TimeSpan.FromSeconds(1)); }
}
