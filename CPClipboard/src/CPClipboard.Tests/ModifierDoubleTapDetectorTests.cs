using CPClipboard.Core.Input;
using CPClipboard.Core.Model;

namespace CPClipboard.Tests;

public class ModifierDoubleTapDetectorTests
{
    private static bool Key(ModifierDoubleTapDetector d, ModifierKey key, bool down, long time, bool simulated = false) =>
        d.OnKey(key.ToString(), key, down, time, simulated);
    private static bool Tap(ModifierDoubleTapDetector d, long time, ModifierKey key = ModifierKey.LeftCtrl, int duration = 50)
    {
        Assert.False(Key(d, key, true, time));
        return Key(d, key, false, time + duration);
    }

    [Theory]
    [InlineData(ModifierKey.LeftCtrl, ModifierKey.LeftCtrl)]
    [InlineData(ModifierKey.LeftCtrl, ModifierKey.RightCtrl)]
    [InlineData(ModifierKey.RightCtrl, ModifierKey.LeftCtrl)]
    public void 二度目の離上で発火する(ModifierKey first, ModifierKey second)
    {
        var d = new ModifierDoubleTapDetector();
        Assert.False(Tap(d, 0, first));
        Assert.True(Tap(d, 100, second));
        Assert.False(Tap(d, 200, first));
        Assert.True(Tap(d, 300, second));
    }

    [Theory]
    [InlineData(250, 300, 250, true)]
    [InlineData(251, 300, 50, false)]
    [InlineData(50, 301, 50, false)]
    [InlineData(50, 300, 251, false)]
    public void 時間の境界を含み超過は不成立(int firstHold, int gap, int secondHold, bool expected)
    {
        var d = new ModifierDoubleTapDetector();
        Assert.False(Tap(d, 0, duration: firstHold));
        Assert.Equal(expected, Tap(d, firstHold + gap, duration: secondHold));
    }

    [Theory]
    [InlineData(200)]
    [InlineData(500)]
    public void 設定した間隔を使う(int interval)
    {
        var d = new ModifierDoubleTapDetector();
        d.Configure(new() { IntervalMs = interval });
        Tap(d, 0);
        Assert.True(Tap(d, 50 + interval));
    }

    [Fact]
    public void リピートは保持時間を延ばさず押下を増やさない()
    {
        var d = new ModifierDoubleTapDetector();
        Key(d, ModifierKey.LeftCtrl, true, 0);
        Key(d, ModifierKey.LeftCtrl, true, 100);
        Key(d, ModifierKey.LeftCtrl, false, 251);
        Assert.False(Tap(d, 300));
        Assert.True(Tap(d, 400));
    }

    [Fact]
    public void 左右重複は両方離すまで候補にしない()
    {
        var d = new ModifierDoubleTapDetector();
        Tap(d, 0);
        Key(d, ModifierKey.LeftCtrl, true, 100);
        Key(d, ModifierKey.RightCtrl, true, 110);
        Assert.False(Key(d, ModifierKey.LeftCtrl, false, 120));
        Assert.False(Key(d, ModifierKey.RightCtrl, false, 130));
        Assert.False(Tap(d, 200));
        Assert.True(Tap(d, 300));
    }

    [Theory]
    [InlineData("key")]
    [InlineData("modifier")]
    [InlineData("mouse")]
    [InlineData("wheel")]
    public void 途中の別入力で中断する(string kind)
    {
        var d = new ModifierDoubleTapDetector();
        Tap(d, 0);
        if (kind == "key") { d.OnKey("C", null, true, 60); d.OnKey("C", null, false, 70); }
        if (kind == "modifier") { Key(d, ModifierKey.LeftAlt, true, 60); Key(d, ModifierKey.LeftAlt, false, 70); }
        if (kind == "mouse") { d.OnMouseButton(InputButton.Left, true); d.OnMouseButton(InputButton.Left, false); }
        if (kind == "wheel") d.OnWheel();
        Assert.False(Tap(d, 100));
        Assert.True(Tap(d, 200));
    }

    [Theory]
    [InlineData("key")]
    [InlineData("modifier")]
    [InlineData("mouse")]
    public void 先に他の入力を保持している間は開始しない(string kind)
    {
        var d = new ModifierDoubleTapDetector();
        if (kind == "key") d.OnKey("C", null, true, 0);
        if (kind == "modifier") Key(d, ModifierKey.LeftAlt, true, 0);
        if (kind == "mouse") d.OnMouseButton(InputButton.Left, true);
        Assert.False(Tap(d, 10));
        Assert.False(Tap(d, 100));
        if (kind == "key") d.OnKey("C", null, false, 160);
        if (kind == "modifier") Key(d, ModifierKey.LeftAlt, false, 160);
        if (kind == "mouse") d.OnMouseButton(InputButton.Left, false);
        Assert.False(Tap(d, 200));
        Assert.True(Tap(d, 300));
    }

    [Fact]
    public void コピー貼り付け連打は不成立()
    {
        var d = new ModifierDoubleTapDetector();
        foreach (var time in new[] { 0, 150, 300, 450 })
        {
            Key(d, ModifierKey.LeftCtrl, true, time);
            d.OnKey("C", null, true, time + 10);
            d.OnKey("C", null, false, time + 20);
            Assert.False(Key(d, ModifierKey.LeftCtrl, false, time + 30));
        }
    }

    [Fact]
    public void 二回目の押下に続くコピー操作で開かない()
    {
        var d = new ModifierDoubleTapDetector();
        Tap(d, 0);
        Key(d, ModifierKey.LeftCtrl, true, 100);
        d.OnKey("C", null, true, 110);
        d.OnKey("C", null, false, 120);
        Assert.False(Key(d, ModifierKey.LeftCtrl, false, 150));
        Assert.False(Tap(d, 200));
        Assert.True(Tap(d, 300));
    }

    [Fact]
    public void 合成入力は候補も保持集合も変えない()
    {
        var d = new ModifierDoubleTapDetector();
        Assert.False(Tap(d, 0));
        Key(d, ModifierKey.LeftCtrl, true, 60, true);
        Key(d, ModifierKey.LeftCtrl, false, 70, true);
        d.OnKey("V", null, true, 80, true);
        d.OnMouseButton(InputButton.Left, true, true);
        d.OnWheel(true);
        Assert.True(Tap(d, 100));
    }

    [Fact]
    public void 設定変更や記録境界で候補を破棄し保持キーの離上を数えない()
    {
        var d = new ModifierDoubleTapDetector();
        Tap(d, 0);
        d.Configure(new());
        Assert.False(Tap(d, 100));
        d.SetCapture(true);
        Assert.False(Tap(d, 200));
        Key(d, ModifierKey.LeftCtrl, true, 300);
        d.SetCapture(false);
        Assert.False(Key(d, ModifierKey.LeftCtrl, false, 350));
        Assert.False(Tap(d, 400));
        Assert.True(Tap(d, 500));
        d.Reset();
        Assert.False(Key(d, ModifierKey.LeftCtrl, false, 550));
        Assert.False(Tap(d, 600));
    }

    [Theory]
    [InlineData(DoubleTapModifier.Ctrl, ModifierKey.LeftCtrl)]
    [InlineData(DoubleTapModifier.Alt, ModifierKey.RightAlt)]
    [InlineData(DoubleTapModifier.Shift, ModifierKey.LeftShift)]
    [InlineData(DoubleTapModifier.Meta, ModifierKey.RightMeta)]
    public void 選択した修飾キーだけで発火する(DoubleTapModifier modifier, ModifierKey key)
    {
        var d = new ModifierDoubleTapDetector();
        d.Configure(new() { Modifier = modifier });
        Assert.False(Tap(d, 0, key));
        Assert.True(Tap(d, 100, key));
        d.Configure(new() { Modifier = modifier, Enabled = false });
        Assert.False(Tap(d, 200, key));
        Assert.False(Tap(d, 300, key));
    }
    [Fact]
    public void OS照合で欠落した離上を発火せず除去する()
    {
        var d = new ModifierDoubleTapDetector();
        Tap(d, 0);
        Key(d, ModifierKey.LeftCtrl, true, 100); // 二度目の離上が欠落。
        d.ReconcilePressedInputs(_ => false, _ => false);
        Assert.False(Key(d, ModifierKey.LeftCtrl, false, 150));
        Assert.False(Tap(d, 200));
        Assert.True(Tap(d, 300));
    }

    [Fact]
    public void OS照合で保持中のキーと候補を残す()
    {
        var d = new ModifierDoubleTapDetector();
        Tap(d, 0);
        d.ReconcilePressedInputs(_ => true, _ => true);
        Assert.True(Tap(d, 100));
        d.OnKey("A", null, true, 200);
        d.OnMouseButton(InputButton.Left, true);
        d.ReconcilePressedInputs(_ => true, _ => true);
        Assert.False(Tap(d, 10_000));
        Assert.False(Tap(d, 10_100));
        d.ReconcilePressedInputs(_ => false, _ => false);
        Assert.False(Tap(d, 20_000));
        Assert.True(Tap(d, 20_100));
    }

}
