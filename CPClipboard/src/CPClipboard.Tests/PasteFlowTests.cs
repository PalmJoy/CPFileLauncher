using CPClipboard.Core.Model;
using CPClipboard.Core.Paste;

namespace CPClipboard.Tests;

public class PasteFlowTests
{
    private static PasteRequest Request(string text = "snippet") => new(text, 42, false, true, true, 500);
    private sealed class Operations : IPasteOperations
    {
        public List<string> Calls { get; } = [];
        public long NowMs { get; private set; }
        public long ChangeToken { get; set; }
        public KeyModifiers PressedModifiers => NowMs < ReleaseAt ? KeyModifiers.Ctrl : KeyModifiers.None;
        public long ReleaseAt, FocusAt;
        public bool Valid = true, Focus = true, RestoreFocus = true, Send = true;
        public bool SnapshotBusy, WriteBusy, RestoreBusy, ThrowSend, ThrowGuard;
        public int Checks, Restores, Writes;
        public PasteGuardResult Guard = PasteGuardResult.Ok;
        public PasteGuardResult? SecondGuard;
        public ClipboardSnapshot? Snapshot = new([new(new Dictionary<string, byte[]> { ["original"] = [1, 2, 3] })]);
        public string Text = "original";
        public bool Transient;
        public Action<int>? OnDelay;
        public bool HoldRestore;
        public TaskCompletionSource<bool> Waiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsTargetValid(nint target) => Valid && target != 0;
        public bool IsForeground(nint target) => Focus && NowMs >= FocusAt;
        public bool RestoreForeground(nint target) { Calls.Add("focus"); return RestoreFocus; }
        public PasteGuardResult Check(nint target)
        {
            Calls.Add("guard"); Checks++;
            if (ThrowGuard) throw new InvalidOperationException();
            return Checks > 1 ? SecondGuard ?? Guard : Guard;
        }
        public Task<ClipboardSnapshot?> SnapshotAsync()
        {
            Calls.Add("snapshot");
            if (SnapshotBusy) throw new IOException();
            return Task.FromResult(Snapshot);
        }
        public Task<long?> SetTextAsync(string text, bool transient, long? expectedToken = null)
        {
            Calls.Add("write:" + text + ":" + transient);
            if (WriteBusy) throw new IOException();
            if (expectedToken is { } token && token != ChangeToken) return Task.FromResult<long?>(null);
            Writes++; Text = text; Transient = transient;
            return Task.FromResult<long?>(++ChangeToken);
        }
        public Task<bool> RestoreAsync(ClipboardSnapshot snapshot, long expectedToken)
        {
            Calls.Add("restore");
            if (RestoreBusy) throw new IOException();
            if (ChangeToken != expectedToken) return Task.FromResult(false);
            Restores++; Text = snapshot.Items.Count == 0 ? "" : "original"; ChangeToken++;
            return Task.FromResult(true);
        }
        public bool SendPaste() { Calls.Add("send"); if (ThrowSend) throw new InvalidOperationException(); return Send; }
        public async Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
        {
            if (milliseconds >= 200 && HoldRestore)
            {
                Waiting.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            NowMs += milliseconds; OnDelay?.Invoke(milliseconds);
        }
    }

    [Fact]
    public async Task 正常時は退避して送信後だけ復元する()
    {
        var o = new Operations();
        Assert.Equal(PasteResult.Pasted, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.Equal(new[] { "guard", "snapshot", "write:snippet:True", "focus", "guard", "send", "restore" }, o.Calls);
        Assert.Equal("original", o.Text);
        Assert.Equal(500, o.NowMs);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task コピーだけは退避送信復元せず印を付けない(bool copyOnly, bool autoPaste)
    {
        var o = new Operations();
        Assert.Equal(PasteResult.Copied, await new PasteFlow(o).ExecuteAsync(Request() with { CopyOnly = copyOnly, AutoPaste = autoPaste }));
        Assert.Equal(new[] { "write:snippet:False", "focus" }, o.Calls);
        Assert.False(o.Transient);
    }

    [Fact]
    public async Task 対象が消えていたらコピーだけ()
    {
        var o = new Operations { Valid = false };
        Assert.Equal(PasteResult.NoTarget, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.Equal(new[] { "write:snippet:False" }, o.Calls);
    }

    [Theory]
    [InlineData(PasteGuardResult.Elevated, PasteResult.Elevated)]
    [InlineData(PasteGuardResult.SecureInput, PasteResult.SecureInput)]
    [InlineData(PasteGuardResult.NoPermission, PasteResult.NoPermission)]
    [InlineData(PasteGuardResult.Unknown, PasteResult.CheckFailed)]
    public async Task ガードで止まったらコピーだけ(PasteGuardResult guard, PasteResult expected)
    {
        var o = new Operations { Guard = guard };
        Assert.Equal(expected, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.False(o.Transient); Assert.DoesNotContain("snapshot", o.Calls); Assert.DoesNotContain("send", o.Calls);
    }

    [Theory]
    [InlineData(300, true)]
    [InlineData(301, false)]
    public async Task 前面化の境界を評価する(int at, bool expected)
    {
        var o = new Operations { FocusAt = at };
        Assert.Equal(expected ? PasteResult.Pasted : PasteResult.FocusFailed, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.Equal(expected, o.Calls.Contains("send"));
        if (!expected) { Assert.Equal("snippet", o.Text); Assert.False(o.Transient); Assert.Equal(0, o.Restores); }
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public async Task 修飾キー解放の境界を評価する(int at, bool expected)
    {
        var o = new Operations { ReleaseAt = at };
        Assert.Equal(expected ? PasteResult.Pasted : PasteResult.ModifierHeld, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.Equal(expected, o.Calls.Contains("send"));
    }

    [Fact]
    public async Task 待機中に別アプリへ移ったら送信しない()
    {
        var o = new Operations { ReleaseAt = 50 };
        o.OnDelay = _ => o.Focus = false;
        Assert.Equal(PasteResult.FocusFailed, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.DoesNotContain("send", o.Calls);
    }

    [Fact]
    public async Task 送信直前にもガードを確認する()
    {
        var o = new Operations { SecondGuard = PasteGuardResult.SecureInput };
        Assert.Equal(PasteResult.SecureInput, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.Equal(0, o.Restores); Assert.False(o.Transient);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task キー送信の失敗と例外は復元せずコピーだけ(bool throws)
    {
        var o = new Operations { Send = false, ThrowSend = throws };
        Assert.Equal(PasteResult.SendFailed, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.Equal("snippet", o.Text); Assert.False(o.Transient); Assert.Equal(0, o.Restores);
    }

    [Fact]
    public async Task ガード例外を許可扱いにしない()
    {
        var o = new Operations { ThrowGuard = true };
        Assert.Equal(PasteResult.CheckFailed, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.DoesNotContain("send", o.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task 書込前のクリップボードエラーは送信しない(bool snapshot)
    {
        var o = new Operations { SnapshotBusy = snapshot, WriteBusy = !snapshot };
        Assert.Equal(PasteResult.ClipboardBusy, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.DoesNotContain("send", o.Calls); Assert.Equal("original", o.Text);
    }

    [Fact]
    public async Task 送信待ち中の別コピーを貼らない()
    {
        var o = new Operations { ReleaseAt = 20 };
        o.OnDelay = _ => { o.ChangeToken++; o.Text = "user"; };
        Assert.Equal(PasteResult.ClipboardChanged, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.Equal("user", o.Text); Assert.DoesNotContain("send", o.Calls); Assert.Equal(0, o.Restores);
    }

    [Fact]
    public async Task 復元待ち中の別コピーを上書きしない()
    {
        var o = new Operations();
        o.OnDelay = _ => { o.ChangeToken++; o.Text = "user"; };
        Assert.Equal(PasteResult.Pasted, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.Equal("user", o.Text); Assert.Equal(0, o.Restores);
    }

    [Fact]
    public async Task 復元不可やOFFでも貼り付けは続行する()
    {
        foreach (bool restore in new[] { true, false })
        {
            var o = new Operations { Snapshot = null };
            Assert.Equal(PasteResult.Pasted, await new PasteFlow(o).ExecuteAsync(Request() with { RestoreClipboard = restore }));
            Assert.DoesNotContain("restore", o.Calls);
            Assert.Equal(restore, o.Calls.Contains("snapshot"));
        }
    }

    [Fact]
    public async Task 空だったクリップボードは空へ戻す()
    {
        var o = new Operations { Snapshot = new([]) };
        Assert.Equal(PasteResult.Pasted, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.Equal("", o.Text);
    }

    [Fact]
    public async Task 復元失敗でキーを再送信しない()
    {
        var o = new Operations { RestoreBusy = true };
        Assert.Equal(PasteResult.Pasted, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.Equal(1, o.Calls.Count(c => c == "send"));
    }

    [Fact]
    public async Task 次の選択は前回の復元を済ませてから書き込む()
    {
        var o = new Operations { HoldRestore = true };
        var flow = new PasteFlow(o);
        var first = flow.ExecuteAsync(Request("first"));
        await o.Waiting.Task;
        var second = flow.ExecuteAsync(Request("second") with { CopyOnly = true });
        Assert.Equal(PasteResult.Pasted, await first);
        Assert.Equal(PasteResult.Copied, await second);
        Assert.True(o.Calls.IndexOf("restore") < o.Calls.IndexOf("write:second:False"));
        Assert.Equal("second", o.Text);
    }

    [Fact]
    public async Task 終了時は送信後の復元待機を短縮する()
    {
        var o = new Operations { HoldRestore = true };
        using var cancel = new CancellationTokenSource();
        var task = new PasteFlow(o).ExecuteAsync(Request(), cancel.Token);
        await o.Waiting.Task;
        cancel.Cancel();
        Assert.Equal(PasteResult.Pasted, await task);
        Assert.Equal(1, o.Restores);
    }

    [Fact]
    public async Task 送信前のキャンセルは復元せず定型文を残す()
    {
        var o = new Operations { ReleaseAt = 1000 };
        using var cancel = new CancellationTokenSource();
        o.OnDelay = _ => cancel.Cancel();
        Assert.Equal(PasteResult.Cancelled, await new PasteFlow(o).ExecuteAsync(Request(), cancel.Token));
        Assert.DoesNotContain("send", o.Calls); Assert.Equal(0, o.Restores); Assert.False(o.Transient);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task 退避上限は32MiBを含み超過は復元しない(int extra, bool restores)
    {
        var o = new Operations { Snapshot = new([new(new Dictionary<string, byte[]> { ["large"] = new byte[(int)ClipboardSnapshot.MaxBytes + extra] })]) };
        Assert.Equal(PasteResult.Pasted, await new PasteFlow(o).ExecuteAsync(Request()));
        Assert.Equal(restores ? 1 : 0, o.Restores);
    }
}
