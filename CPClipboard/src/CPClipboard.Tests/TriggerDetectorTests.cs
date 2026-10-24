using CPClipboard.Core.Input;
using CPClipboard.Core.Model;

namespace CPClipboard.Tests;

public class TriggerDetectorTests
{
    private const KeyModifiers CtrlAlt = KeyModifiers.Ctrl | KeyModifiers.Alt;

    /// <summary>ホットキー Ctrl+Alt+Space（既定は OFF なので有効にして試す）と既定のマウス操作（Alt+中クリック）。</summary>
    private static TriggerDetector WithDefaults()
    {
        var s = new AppSettings().Triggers;
        s.Hotkey = new HotkeySetting { Enabled = true, Modifiers = CtrlAlt, Key = "Space" };
        var d = new TriggerDetector();
        d.SetHotkey(s.Hotkey);
        d.SetMouse(s);
        return d;
    }

    private static TriggerDetector WithMouse(params MouseTriggerSetting[] triggers)
    {
        var d = new TriggerDetector();
        d.SetMouse(new TriggerSettings { Mouse = triggers.ToList() });
        return d;
    }

    // ---------------- ホットキー ----------------

    [Fact]
    public void CtrlAltSpaceで発火し_スペースは抑止する()
    {
        var d = WithDefaults();

        Assert.Equal(new KeyDecision(Suppress: true, Fire: true), d.OnKeyDown("Space", CtrlAlt));
    }

    [Fact]
    public void 修飾キーが違えば素通し()
    {
        var d = WithDefaults();

        Assert.Equal(default, d.OnKeyDown("Space", KeyModifiers.Ctrl));
        Assert.Equal(default, d.OnKeyDown("Space", CtrlAlt | KeyModifiers.Shift));
        Assert.Equal(default, d.OnKeyDown("A", CtrlAlt));
    }

    [Fact]
    public void 押しっぱなしのキーリピートは発火しないが抑止は続け_離すと次を受け付ける()
    {
        var d = WithDefaults();
        d.OnKeyDown("Space", CtrlAlt);

        for (int i = 0; i < 19; i++)
            Assert.Equal(new KeyDecision(Suppress: true, Fire: false), d.OnKeyDown("Space", CtrlAlt));
        // 修飾キーを先に離してもリピート分のスペースは漏らさない
        Assert.Equal(new KeyDecision(Suppress: true, Fire: false), d.OnKeyDown("Space", KeyModifiers.None));

        Assert.True(d.OnKeyUp("Space"));
        Assert.True(d.OnKeyDown("Space", CtrlAlt).Fire);
    }

    [Fact]
    public void 関係ないキーの離上は抑止しない()
    {
        var d = WithDefaults();

        Assert.False(d.OnKeyUp("Space"));
        Assert.False(d.OnKeyUp("A"));
    }

    [Fact]
    public void ホットキー無効なら発火しない()
    {
        var d = new TriggerDetector();
        d.SetHotkey(new HotkeySetting { Enabled = false });

        Assert.Equal(default, d.OnKeyDown("Space", CtrlAlt));
    }

    // ---------------- 修飾キー + クリック ----------------

    [Fact]
    public void 既定のAlt中クリックで発火し_押下と離上を抑止する()
    {
        var d = WithDefaults();

        var down = d.OnMouseDown(InputButton.Middle, KeyModifiers.Alt);
        Assert.True(down.Fire);
        Assert.True(down.Suppress);
        Assert.False(down.PassedThrough);

        // Alt を先に離していても離上は対で抑止
        var up = d.OnMouseUp(InputButton.Middle);
        Assert.True(up.Suppress);
        Assert.False(up.PassedThrough);
    }

    [Fact]
    public void 修飾キーなしの中クリックや左クリックは素通しして外クリック判定に回す()
    {
        var d = WithDefaults();

        foreach (var (b, m) in new[] { (InputButton.Middle, KeyModifiers.None), (InputButton.Left, KeyModifiers.Ctrl), (InputButton.Left, KeyModifiers.None) })
        {
            var down = d.OnMouseDown(b, m);
            Assert.False(down.Fire);
            Assert.False(down.Suppress);
            Assert.True(down.PassedThrough);
            var up = d.OnMouseUp(b);
            Assert.False(up.Suppress);
            Assert.True(up.PassedThrough);
        }
    }

    [Fact]
    public void サイドボタンも修飾キー付きクリックとして使える()
    {
        var d = WithMouse(new MouseTriggerSetting { Button = MouseButtonKind.X1, Modifiers = KeyModifiers.None });

        Assert.True(d.OnMouseDown(InputButton.X1, KeyModifiers.None).Fire);
        Assert.False(d.OnMouseDown(InputButton.X2, KeyModifiers.None).Fire);
    }


    [Theory]
    [InlineData(MouseButtonKind.Left, InputButton.Left)]
    [InlineData(MouseButtonKind.Right, InputButton.Right)]
    [InlineData(MouseButtonKind.Middle, InputButton.Middle)]
    [InlineData(MouseButtonKind.X1, InputButton.X1)]
    [InlineData(MouseButtonKind.X2, InputButton.X2)]
    public void 各ボタンで完全一致し重複押下は抑止だけ(MouseButtonKind button, InputButton input)
    {
        var d = WithMouse(new MouseTriggerSetting() { Button = button, Modifiers = KeyModifiers.Alt });
        Assert.False(d.OnMouseDown(input, KeyModifiers.Ctrl).Fire);
        Assert.True(d.OnMouseDown(input, KeyModifiers.Alt).Fire);
        Assert.Equal(new MouseDecision(true, false, false), d.OnMouseDown(input, KeyModifiers.None));
        d.SetMouse(new() { Mouse = [] });
        Assert.True(d.OnMouseUp(input).Suppress);
        Assert.False(d.OnMouseDown(input, KeyModifiers.Alt).Fire);
    }

    [Theory]
    [InlineData(MouseButtonKind.Left, InputButton.Left)]
    [InlineData(MouseButtonKind.Right, InputButton.Right)]
    public void 無修飾の左右を設定してもクリックを奪わない(MouseButtonKind button, InputButton input)
    {
        var d = WithMouse(new MouseTriggerSetting() { Button = button });
        Assert.Equal(new MouseDecision(false, false, true), d.OnMouseDown(input, KeyModifiers.None));
    }

    [Fact]
    public void ホットキー変更後も抑止した押下の離上を抑止する()
    {
        var d = WithDefaults();
        Assert.True(d.OnKeyDown("Space", CtrlAlt).Fire);
        d.SetHotkey(new() { Enabled = true, Key = "V", Modifiers = CtrlAlt });
        Assert.True(d.OnKeyDown("V", CtrlAlt).Fire);
        Assert.True(d.OnKeyUp("Space"));
        d.SetHotkey(null);
        Assert.True(d.OnKeyUp("V"));
    }

    [Fact]
    public void 合成入力では発火も抑止状態の消費もしない()
    {
        var d = WithDefaults();
        Assert.Equal(default, d.OnKeyDown("Space", CtrlAlt, simulated: true));
        Assert.True(d.OnKeyDown("Space", CtrlAlt).Fire);
        Assert.False(d.OnKeyUp("Space", simulated: true));
        Assert.True(d.OnKeyUp("Space"));
        Assert.Equal(default, d.OnMouseDown(InputButton.Middle, KeyModifiers.Alt, simulated: true));
        Assert.True(d.OnMouseDown(InputButton.Middle, KeyModifiers.Alt).Fire);
        Assert.Equal(default, d.OnMouseUp(InputButton.Middle, simulated: true));
        Assert.True(d.OnMouseUp(InputButton.Middle).Suppress);
    }

    [Fact]
    public void 記録中はクリックを発火せず抑止済み入力の対だけを維持する()
    {
        var d = WithDefaults();
        Assert.True(d.OnMouseDown(InputButton.Middle, KeyModifiers.Alt).Fire);
        Assert.Equal(new MouseDecision(true, false, false), d.OnMouseDown(InputButton.Middle, KeyModifiers.Alt, allowFire: false));
        Assert.True(d.OnMouseUp(InputButton.Middle).Suppress);
        Assert.Equal(new MouseDecision(false, false, true), d.OnMouseDown(InputButton.Middle, KeyModifiers.Alt, allowFire: false));
    }
}
