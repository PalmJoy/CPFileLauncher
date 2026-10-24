using System.IO.Pipes;
using CPClipboard.App;

namespace CPClipboard.Desktop.Tests;

public sealed class SingleInstanceTests
{
    [Fact]
    public async Task Idle_client_times_out_and_next_client_can_signal()
    {
        using var instance = new SingleInstance(Guid.NewGuid().ToString("N"));
        var signaled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = instance.Listen(() => signaled.TrySetResult());
        using var idle = new NamedPipeClientStream(".", instance.PipeName, PipeDirection.Out, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await idle.ConnectAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(SingleInstance.SignalTimeout + TimeSpan.FromMilliseconds(300));
        Assert.True(instance.SignalExisting());
        await signaled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        instance.Dispose();
        await listener.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Dispose_cancels_read_from_idle_client()
    {
        var instance = new SingleInstance(Guid.NewGuid().ToString("N"));
        var listener = instance.Listen(() => throw new InvalidOperationException("Unexpected signal"));
        using var idle = new NamedPipeClientStream(".", instance.PipeName, PipeDirection.Out, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await idle.ConnectAsync().WaitAsync(TimeSpan.FromSeconds(5));
        instance.Dispose();
        await listener.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
