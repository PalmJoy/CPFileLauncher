using System.Text.Json;
using FileLauncher.Core.Model;
using FileLauncher.Core.Storage;

namespace FileLauncher.Tests;

public class DoubleTapSettingsTests
{
    [Fact]
    public void 旧設定では二回押しを有効化せず既存値を保つ()
    {
        var settings = JsonSerializer.Deserialize<AppSettings>("{\"triggers\":{\"hotkey\":{\"enabled\":false},\"longPressMs\":650}}", JsonDefaults.Options)!;
        settings.Normalize();
        Assert.False(settings.Triggers.DoubleTap.Enabled);
        Assert.Equal(DoubleTapModifier.Ctrl, settings.Triggers.DoubleTap.Modifier);
        Assert.Equal(300, settings.Triggers.DoubleTap.IntervalMs);
        Assert.False(settings.Triggers.Hotkey.Enabled);
        Assert.Equal(650, settings.Triggers.LongPressMs);
    }
    [Theory]
    [InlineData(1, 200)] [InlineData(255, 260)] [InlineData(999, 500)]
    public void 間隔を正規化し保存で往復できる(int interval, int expected)
    {
        var settings = new AppSettings();
        settings.Triggers.DoubleTap = new() { Enabled = true, Modifier = DoubleTapModifier.Alt, IntervalMs = interval };
        settings.Normalize();
        var copy = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings, JsonDefaults.Options), JsonDefaults.Options)!;
        Assert.True(copy.Triggers.DoubleTap.Enabled);
        Assert.Equal(expected, copy.Triggers.DoubleTap.IntervalMs);
        Assert.Equal(DoubleTapModifier.Alt, copy.Triggers.DoubleTap.Modifier);
    }
}
