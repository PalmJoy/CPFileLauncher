using System.Reflection;
using CPClipboard.Core.Input;
using CPClipboard.Core.Model;
using CPClipboard.Platform.Input;
using SharpHook;
using SharpHook.Data;

namespace CPClipboard.Desktop.Tests;

public class InputHookRecoveryTests
{
    private static void Deliver(SharpHookInputService service, string method, HookEventArgs args) =>
        typeof(SharpHookInputService).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, [null, args]);

    private static void Key(SharpHookInputService service, KeyCode key, ushort rawCode, bool down, long time,
        EventMask mask = EventMask.None) => Deliver(service, down ? "OnKeyPressed" : "OnKeyReleased",
        new KeyboardHookEventArgs(new UioHookEvent
        {
            Type = down ? EventType.KeyPressed : EventType.KeyReleased,
            Time = (ulong)(1_700_000_000_000 + time), Mask = mask,
            Keyboard = new KeyboardEventData { KeyCode = key, RawCode = rawCode },
        }));

    private static void Pair(SharpHookInputService service, long time, KeyCode key = KeyCode.VcLeftControl, ushort rawCode = 0xA2)
    {
        foreach (long down in new[] { time, time + 100 })
        {
            Key(service, key, rawCode, true, down);
            Key(service, key, rawCode, false, down + 40);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingKeyReleaseRecoversBothBoardsButPhysicallyHeldKeysStillBlock(bool physicallyHeld)
    {
        var held = new HashSet<ushort>();
        using var service = new SharpHookInputService(isKeyPressed: held.Contains);
        service.ConfigureHistory(new() { DoubleTap = new() { Modifier = DoubleTapModifier.Alt } });
        int snippets = 0, history = 0;
        service.DoubleTapped += _ => snippets++;
        service.HistoryTriggered += (_, _) => history++;
        Key(service, KeyCode.VcA, 0x41, true, 0); // 離上イベントだけ欠ける。
        if (physicallyHeld) held.Add(0x41);
        Pair(service, 10_000);
        Pair(service, 20_000, KeyCode.VcLeftAlt, 0xA4);
        Assert.Equal(physicallyHeld ? 0 : 1, snippets);
        Assert.Equal(physicallyHeld ? 0 : 1, history);
    }

    [Theory]
    [InlineData(InputButton.Left, MouseButton.Button1)]
    [InlineData(InputButton.Right, MouseButton.Button2)]
    [InlineData(InputButton.Middle, MouseButton.Button3)]
    [InlineData(InputButton.X1, MouseButton.Button4)]
    [InlineData(InputButton.X2, MouseButton.Button5)]
    public void MissingMouseReleaseRecoversAfterPhysicalRelease(InputButton button, MouseButton nativeButton)
    {
        bool held = true;
        using var service = new SharpHookInputService(isButtonPressed: b => b == button && held);
        int fired = 0;
        service.DoubleTapped += _ => fired++;
        Deliver(service, "OnMousePressed", new MouseHookEventArgs(new UioHookEvent
        {
            Type = EventType.MousePressed, Mouse = new MouseEventData { Button = nativeButton },
        }));
        Pair(service, 1_000);
        Assert.Equal(0, fired); // 本当に長押し中なら解除しない。
        held = false;
        Pair(service, 10_000);
        Assert.Equal(1, fired);
    }

    [Fact]
    public void CurrentKeyIsNotQueriedBeforeWindowsUpdatesItsAsyncState()
    {
        using var service = new SharpHookInputService(isKeyPressed: _ => throw new InvalidOperationException("current key queried"));
        int fired = 0;
        service.DoubleTapped += _ => fired++;
        Key(service, KeyCode.VcLeftControl, 0xA2, true, 0);
        Key(service, KeyCode.VcLeftControl, 0xA2, true, 100); // リピートで候補を消さない。
        Key(service, KeyCode.VcLeftControl, 0xA2, false, 150);
        Key(service, KeyCode.VcLeftControl, 0xA2, true, 200);
        Key(service, KeyCode.VcLeftControl, 0xA2, false, 240);
        Assert.Equal(1, fired);
    }

    [Fact]
    public void MissingTargetReleaseCannotLeaveDoubleTapPermanentlyBlocked()
    {
        using var service = new SharpHookInputService(isKeyPressed: _ => false);
        int fired = 0;
        service.DoubleTapped += _ => fired++;
        Key(service, KeyCode.VcLeftControl, 0xA2, true, 0);
        Key(service, KeyCode.VcA, 0x41, true, 1_000); // 他の入力で残留 Ctrl を照合する。
        Key(service, KeyCode.VcA, 0x41, false, 1_040);
        Pair(service, 2_000);
        Assert.Equal(1, fired);
    }

    [Fact]
    public void SimulatedEventsDoNotReconcileOrCountAsPhysicalRelease()
    {
        bool released = false;
        using var service = new SharpHookInputService(isKeyPressed: _ => !released);
        int fired = 0;
        service.DoubleTapped += _ => fired++;
        Key(service, KeyCode.VcA, 0x41, true, 0);
        released = true;
        Key(service, KeyCode.VcV, 0x56, true, 50, EventMask.SimulatedEvent);
        released = false;
        Pair(service, 100);
        Assert.Equal(0, fired);
        released = true;
        Pair(service, 1_000);
        Assert.Equal(1, fired);
    }

    [Fact]
    public void WithoutOsQueriesMissingReleaseRemainsBlockedUntilRealRelease()
    {
        using var service = new SharpHookInputService();
        int fired = 0;
        service.DoubleTapped += _ => fired++;
        Key(service, KeyCode.VcA, 0x41, true, 0);
        Pair(service, 1_000);
        Assert.Equal(0, fired);
        Key(service, KeyCode.VcA, 0x41, false, 2_000);
        Pair(service, 3_000);
        Assert.Equal(1, fired);
    }
}
