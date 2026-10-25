using System.Reflection;
using CPClipboard.Core.Model;
using CPClipboard.Platform.Input;
using SharpHook;
using SharpHook.Data;

namespace CPClipboard.Desktop.Tests;

/// <summary>ネイティブフックを起動せず、受信済みイベントから Core と通知までの配線を確かめる。</summary>
public class InputHookRoutingTests
{
    private static void Deliver(SharpHookInputService service, string method, HookEventArgs args) =>
        typeof(SharpHookInputService).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, [null, args]);
    private static KeyboardHookEventArgs Key(SharpHookInputService service, KeyCode key, bool down, long time,
        EventMask mask = EventMask.None)
    {
        var args = new KeyboardHookEventArgs(new UioHookEvent
        {
            Type = down ? EventType.KeyPressed : EventType.KeyReleased,
            Time = (ulong)(1_700_000_000_000 + time), Mask = mask,
            Keyboard = new KeyboardEventData { KeyCode = key },
        });
        Deliver(service, down ? "OnKeyPressed" : "OnKeyReleased", args);
        return args;
    }
    private static MouseHookEventArgs Mouse(SharpHookInputService service, bool down, EventMask mask)
    {
        var args = new MouseHookEventArgs(new UioHookEvent
        {
            Type = down ? EventType.MousePressed : EventType.MouseReleased, Mask = mask,
            Mouse = new MouseEventData { Button = MouseButton.Button3, X = 10, Y = 20 },
        });
        Deliver(service, down ? "OnMousePressed" : "OnMouseReleased", args);
        return args;
    }

    [Fact]
    public void 二度押しは離上で通知し修飾キー状態を左右別に扱う()
    {
        using var service = new SharpHookInputService();
        int fired = 0;
        service.DoubleTapped += _ => fired++;
        Assert.False(Key(service, KeyCode.VcLeftControl, true, 0).SuppressEvent);
        Assert.Equal(KeyModifiers.Ctrl, service.PressedModifiers);
        Key(service, KeyCode.VcLeftControl, false, 50);
        Key(service, KeyCode.VcRightControl, true, 100);
        Assert.Equal(0, fired);
        Assert.False(Key(service, KeyCode.VcRightControl, false, 150).SuppressEvent);
        Assert.Equal(1, fired);
        Assert.Equal(KeyModifiers.None, service.PressedModifiers);
        Key(service, KeyCode.VcLeftControl, true, 200);
        Key(service, KeyCode.VcRightControl, true, 210);
        Key(service, KeyCode.VcLeftControl, false, 220);
        Assert.Equal(KeyModifiers.Ctrl, service.PressedModifiers);
    }

    [Fact]
    public void 合成イベントは通知も抑止も記録もしない()
    {
        using var service = new SharpHookInputService();
        service.Configure(new());
        service.Register(new() { Enabled = true, Key = "V", Modifiers = KeyModifiers.Alt }, out _);
        int fired = 0, passed = 0, captured = 0;
        service.DoubleTapped += _ => fired++;
        service.Pressed += _ => fired++;
        service.Triggered += (_, _) => fired++;
        service.PassedThroughDown += _ => passed++;
        service.PassedThroughUp += _ => passed++;
        var simulated = EventMask.SimulatedEvent;
        Assert.False(Key(service, KeyCode.VcLeftControl, true, 0, simulated).SuppressEvent);
        Assert.Equal(KeyModifiers.None, service.PressedModifiers);
        Assert.False(Key(service, KeyCode.VcV, true, 10, simulated | EventMask.Alt).SuppressEvent);
        Assert.False(Mouse(service, true, simulated | EventMask.Alt).SuppressEvent);
        Assert.False(Mouse(service, false, simulated).SuppressEvent);
        service.BeginCapture((_, _) => captured++);
        Assert.False(Key(service, KeyCode.VcV, true, 20, simulated).SuppressEvent);
        Assert.False(Key(service, KeyCode.VcV, false, 30, simulated).SuppressEvent);
        service.EndCapture();
        Assert.Equal((0, 0, 0), (fired, passed, captured));
    }

    [Fact]
    public void 記録中は発火せず終了後も記録したキーの離上を抑止する()
    {
        using var service = new SharpHookInputService();
        service.Configure(new());
        int fired = 0, captured = 0;
        service.DoubleTapped += _ => fired++;
        service.Triggered += (_, _) => fired++;
        service.BeginCapture((_, _) => captured++);
        Key(service, KeyCode.VcLeftControl, true, 0);
        Key(service, KeyCode.VcLeftControl, false, 50);
        Key(service, KeyCode.VcLeftControl, true, 100);
        Key(service, KeyCode.VcLeftControl, false, 150);
        Assert.False(Mouse(service, true, EventMask.Alt).SuppressEvent);
        Mouse(service, false, EventMask.None);
        Assert.True(Key(service, KeyCode.VcA, true, 200).SuppressEvent);
        Assert.True(Key(service, KeyCode.VcB, true, 210).SuppressEvent);
        service.EndCapture();
        Assert.True(Key(service, KeyCode.VcA, false, 220).SuppressEvent);
        Assert.True(Key(service, KeyCode.VcB, false, 230).SuppressEvent);
        Assert.Equal(2, captured);
        Assert.Equal(0, fired);
        Assert.True(Mouse(service, true, EventMask.Alt).SuppressEvent);
        Assert.Equal(1, fired);
    }

    [Fact]
    public void 横ホイールも二度押しを中断し合成ホイールは無視する()
    {
        using var service = new SharpHookInputService();
        int fired = 0;
        service.DoubleTapped += _ => fired++;
        foreach (bool simulated in new[] { false, true })
        {
            long time = simulated ? 1000 : 0;
            Key(service, KeyCode.VcLeftControl, true, time);
            Key(service, KeyCode.VcLeftControl, false, time + 50);
            Deliver(service, "OnMouseWheel", new MouseWheelHookEventArgs(new UioHookEvent
            {
                Type = EventType.MouseWheel, Mask = simulated ? EventMask.SimulatedEvent : EventMask.None,
                Wheel = new MouseWheelEventData { Direction = MouseWheelScrollDirection.Horizontal, Rotation = 1 },
            }));
            Key(service, KeyCode.VcLeftControl, true, time + 100);
            Key(service, KeyCode.VcLeftControl, false, time + 150);
        }
        Assert.Equal(1, fired);
    }
    [Fact]
    public void OS照会があればフックで欠けた修飾キーの押下と解放を反映する()
    {
        var physical = KeyModifiers.Ctrl;
        using var service = new SharpHookInputService(() => physical);
        Assert.Equal(KeyModifiers.Ctrl, service.PressedModifiers); // フックへ押下が来ていなくても待つ
        physical = KeyModifiers.None;
        Assert.Equal(KeyModifiers.None, service.PressedModifiers);
        Key(service, KeyCode.VcLeftControl, true, 0); // 解放イベントだけ欠けた場合も実状態を使う
        Assert.Equal(KeyModifiers.None, service.PressedModifiers);
        physical = KeyModifiers.Shift | KeyModifiers.Meta;
        Assert.Equal(physical, service.PressedModifiers);
    }

    [Fact]
    public void HistoryTriggersAreIndependentAndConflictsFireOnlyTheSnippetBoard()
    {
        using var service = new SharpHookInputService();
        service.ConfigureHistory(new()
        {
            DoubleTap = new() { Enabled = true, Modifier = DoubleTapModifier.Alt },
            Hotkey = new() { Enabled = true, Key = "H", Modifiers = KeyModifiers.Ctrl },
            Mouse = [new() { Button = MouseButtonKind.Middle, Modifiers = KeyModifiers.Ctrl }],
        });
        service.Configure(new());
        int history = 0, snippets = 0;
        service.HistoryTriggered += (_, _) => history++;
        service.DoubleTapped += _ => snippets++;
        service.Pressed += _ => snippets++;
        service.Triggered += (_, _) => snippets++;
        Key(service, KeyCode.VcLeftAlt, true, 0); Key(service, KeyCode.VcLeftAlt, false, 40);
        Key(service, KeyCode.VcLeftAlt, true, 100); Key(service, KeyCode.VcLeftAlt, false, 140);
        Assert.Equal(1, history); Assert.Equal(0, snippets);
        Assert.True(Key(service, KeyCode.VcH, true, 200, EventMask.Ctrl).SuppressEvent);
        Assert.True(Key(service, KeyCode.VcH, false, 220).SuppressEvent);
        Assert.Equal(2, history);
        Assert.True(Mouse(service, true, EventMask.Ctrl).SuppressEvent);
        Assert.True(Mouse(service, false, EventMask.None).SuppressEvent);
        Assert.Equal(3, history);
        service.Register(new() { Enabled = true, Key = "H", Modifiers = KeyModifiers.Ctrl }, out _);
        Key(service, KeyCode.VcH, true, 300, EventMask.Ctrl); Key(service, KeyCode.VcH, false, 320);
        Assert.Equal(3, history); Assert.Equal(1, snippets);
        service.BeginCapture((_, _) => {});
        Key(service, KeyCode.VcLeftAlt, true, 400); Key(service, KeyCode.VcLeftAlt, false, 440);
        Key(service, KeyCode.VcLeftAlt, true, 500); Key(service, KeyCode.VcLeftAlt, false, 540);
        Mouse(service, true, EventMask.Ctrl); Mouse(service, false, EventMask.None);
        Assert.Equal(3, history); service.EndCapture();
    }

}
