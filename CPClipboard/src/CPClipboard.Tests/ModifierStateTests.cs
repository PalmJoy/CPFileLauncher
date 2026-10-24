using CPClipboard.Core.Input;
using CPClipboard.Core.Model;

namespace CPClipboard.Tests;

public class ModifierStateTests
{
    [Theory]
    [InlineData(ModifierKey.LeftCtrl, ModifierKey.RightCtrl, KeyModifiers.Ctrl)]
    [InlineData(ModifierKey.LeftAlt, ModifierKey.RightAlt, KeyModifiers.Alt)]
    [InlineData(ModifierKey.LeftShift, ModifierKey.RightShift, KeyModifiers.Shift)]
    [InlineData(ModifierKey.LeftMeta, ModifierKey.RightMeta, KeyModifiers.Meta)]
    public void 左右を個別に保持する(ModifierKey left, ModifierKey right, KeyModifiers flag)
    {
        var s = new ModifierState();
        s.Update(left, true); s.Update(left, true); s.Update(right, true);
        s.Update(left, false); s.Update(left, false);
        Assert.Equal(flag, s.Pressed);
        s.Update(right, false, simulated: true);
        Assert.Equal(flag, s.Pressed);
        s.Update(right, false);
        Assert.Equal(KeyModifiers.None, s.Pressed);
        s.Update(left, true, simulated: true);
        Assert.Equal(KeyModifiers.None, s.Pressed);
    }

    [Fact]
    public void 組合せとリセット()
    {
        var s = new ModifierState();
        s.Update(ModifierKey.LeftCtrl, true); s.Update(ModifierKey.RightAlt, true);
        Assert.Equal(KeyModifiers.Ctrl | KeyModifiers.Alt, s.Pressed);
        s.Reset();
        Assert.Equal(KeyModifiers.None, s.Pressed);
    }
}
