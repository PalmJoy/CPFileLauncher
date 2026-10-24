using System.Text.Json;
using CPClipboard.Core.Model;
using CPClipboard.Core.Storage;

namespace CPClipboard.Tests;

public class TriggerSettingsTests
{
    [Fact]
    public void 旧マウス項目を読み飛ばし現在の形式で保存する()
    {
        var settings = JsonSerializer.Deserialize<AppSettings>("""
            {"triggers":{"desktopOnly":true,"longPressMs":800,"simultaneousWindowMs":150,
                "mouse":[{"gesture":"longPress","button":"right","modifiers":"alt"}]}}
            """, JsonDefaults.Options)!;
        settings.Normalize();
        Assert.Equal(MouseButtonKind.Right, Assert.Single(settings.Triggers.Mouse).Button);
        string json = JsonSerializer.Serialize(settings, JsonDefaults.Options);
        Assert.DoesNotContain("gesture", json);
        Assert.DoesNotContain("desktopOnly", json);
        Assert.DoesNotContain("longPressMs", json);
        Assert.DoesNotContain("simultaneousWindowMs", json);
        Assert.DoesNotContain("isValid", json);
        var reloaded = JsonSerializer.Deserialize<AppSettings>(json, JsonDefaults.Options)!;
        Assert.Equal(MouseButtonKind.Right, Assert.Single(reloaded.Triggers.Mouse).Button);
    }

    [Theory]
    [InlineData(MouseButtonKind.Left)]
    [InlineData(MouseButtonKind.Right)]
    public void 左右無修飾は無効でも除去し空のままにする(MouseButtonKind button)
    {
        var s = new AppSettings();
        s.Triggers.Mouse = [new() { Button = button }, new() { Button = button, Enabled = false }];
        s.Normalize();
        Assert.Empty(s.Triggers.Mouse);
    }

    [Theory]
    [InlineData(MouseButtonKind.Middle)]
    [InlineData(MouseButtonKind.X1)]
    [InlineData(MouseButtonKind.X2)]
    public void 中とサイドは無修飾も許容する(MouseButtonKind button)
    {
        var s = new AppSettings();
        s.Triggers.Mouse = [new() { Button = button }];
        s.Normalize();
        Assert.Single(s.Triggers.Mouse);
    }
}
