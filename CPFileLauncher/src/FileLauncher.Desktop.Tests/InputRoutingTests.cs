using System.Reflection;
using FileLauncher.Core.Input;
using FileLauncher.Core.Model;
using FileLauncher.Platform.Input;
using SharpHook;
using SharpHook.Data;

namespace FileLauncher.Desktop.Tests;

public class InputRoutingTests
{
    private static void Deliver(SharpHookInputService s, string method, HookEventArgs args) =>
        typeof(SharpHookInputService).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(s, [null, args]);
    private static KeyboardHookEventArgs Key(SharpHookInputService s, KeyCode key, bool down, long time, EventMask mask = EventMask.None)
    {
        var a = new KeyboardHookEventArgs(new UioHookEvent { Type = down ? EventType.KeyPressed : EventType.KeyReleased,
            Time = (ulong)(1_700_000_000_000 + time), Mask = mask, Keyboard = new() { KeyCode = key } });
        Deliver(s, down ? "OnKeyPressed" : "OnKeyReleased", a); return a;
    }
    private static void Tap(SharpHookInputService s, long t, EventMask mask = EventMask.None)
    {
        Assert.False(Key(s, KeyCode.VcLeftControl, true, t, mask).SuppressEvent);
        Assert.False(Key(s, KeyCode.VcLeftControl, false, t + 40, mask).SuppressEvent);
    }
    private static MouseHookEventArgs Mouse(SharpHookInputService s, bool down, EventMask mask)
    {
        var a = new MouseHookEventArgs(new UioHookEvent { Type = down ? EventType.MousePressed : EventType.MouseReleased,
            Mask = mask, Mouse = new() { Button = MouseButton.Button3, X = 10, Y = 20 } });
        Deliver(s, down ? "OnMousePressed" : "OnMouseReleased", a); return a;
    }

    [Fact]
    public void 既定では発火せず有効時に二度目の離上で一回通知する()
    {
        using var s = new SharpHookInputService();
        int fired = 0; s.DoubleTapped += _ => fired++;
        Tap(s, 0); Tap(s, 100); Assert.Equal(0, fired);
        s.ConfigureDoubleTap(new() { Enabled = true });
        Tap(s, 200); Tap(s, 300); Assert.Equal(1, fired);
        Tap(s, 400); Assert.Equal(1, fired);
        s.ConfigureDoubleTap(new() { Enabled = true }); // 設定変更で直前候補を捨てる
        Tap(s, 500); Assert.Equal(1, fired);
    }

    [Fact]
    public void 記録中の全呼び出しを止め抑止したキーの離上を最後まで処理する()
    {
        using var s = new SharpHookInputService();
        s.ConfigureDoubleTap(new() { Enabled = true });
        s.Configure(new());
        int fired = 0, captured = 0;
        s.DoubleTapped += _ => fired++; s.Triggered += (_, _) => fired++;
        Tap(s, 0);
        s.BeginCapture((_, _) => captured++);
        Tap(s, 100); Tap(s, 200);
        Assert.False(Mouse(s, true, EventMask.Ctrl).SuppressEvent);
        Mouse(s, false, EventMask.None);
        Assert.True(Key(s, KeyCode.VcA, true, 300).SuppressEvent);
        Assert.True(Key(s, KeyCode.VcB, true, 310).SuppressEvent);
        s.EndCapture();
        Assert.True(Key(s, KeyCode.VcA, false, 320).SuppressEvent);
        Assert.True(Key(s, KeyCode.VcB, false, 330).SuppressEvent);
        Assert.Equal(2, captured); Assert.Equal(0, fired);
        Tap(s, 400); Tap(s, 500); Assert.Equal(1, fired);
        Assert.True(Mouse(s, true, EventMask.Ctrl).SuppressEvent);
        Assert.True(Mouse(s, false, EventMask.None).SuppressEvent);
        Assert.Equal(2, fired);
    }

    [Fact]
    public void 合成入力は新旧トリガーと記録から除外し横ホイールは候補を消す()
    {
        using var s = new SharpHookInputService();
        s.ConfigureDoubleTap(new() { Enabled = true });
        s.Configure(new());
        s.Register(new() { Enabled = true, Key = "V", Modifiers = KeyModifiers.Ctrl }, out _);
        int fired = 0; s.DoubleTapped += _ => fired++; s.Pressed += _ => fired++; s.Triggered += (_, _) => fired++;
        Tap(s, 0, EventMask.SimulatedEvent); Tap(s, 100, EventMask.SimulatedEvent);
        Assert.False(Key(s, KeyCode.VcV, true, 200, EventMask.SimulatedEvent | EventMask.Ctrl).SuppressEvent);
        Assert.False(Mouse(s, true, EventMask.SimulatedEvent | EventMask.Ctrl).SuppressEvent);
        s.BeginCapture((_, _) => fired++);
        Key(s, KeyCode.VcV, true, 210, EventMask.SimulatedEvent);
        s.EndCapture();
        Tap(s, 300);
        Deliver(s, "OnMouseWheel", new MouseWheelHookEventArgs(new UioHookEvent { Type = EventType.MouseWheel,
            Wheel = new() { Direction = MouseWheelScrollDirection.Horizontal, Rotation = 1 } }));
        Tap(s, 400); Assert.Equal(0, fired);
        Tap(s, 500); Assert.Equal(1, fired);
    }

    [Fact]
    public void 記録開始は長押しの待機を取り消し抑止済みボタンの離上を保持する()
    {
        var d = new TriggerDetector();
        d.SetMouse(new() { Mouse = [ new() { Gesture = MouseGesture.LongPress, Button = MouseButtonKind.Middle } ] });
        var down = d.OnMouseDown(InputButton.Middle, KeyModifiers.None, new(0, 0), 0);
        Assert.NotNull(down.LongPressToken);
        d.CancelPendingGestures();
        Assert.False(d.OnLongPressElapsed(down.LongPressToken!.Value, out _));
        d.SetMouse(new());
        Assert.True(d.OnMouseDown(InputButton.Middle, KeyModifiers.Ctrl, new(0, 0), 10).Suppress);
        d.CancelPendingGestures();
        Assert.True(d.OnMouseUp(InputButton.Middle).Suppress);
        Assert.False(d.OnMouseDown(InputButton.Middle, KeyModifiers.Ctrl, new(0, 0), 20, allowFire: false).Fire);
        Assert.False(d.OnMouseDown(InputButton.Left, KeyModifiers.None, new(0, 0), 30).Suppress);
    }
}
