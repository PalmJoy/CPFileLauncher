using System.Text.Json;
#if CPTOOLS_CLIPBOARD
using CPClipboard.App;
namespace CPClipboard.Desktop.Tests;
#else
using FileLauncher.App;
namespace FileLauncher.Desktop.Tests;
#endif

public class ProductReleaseTests
{
    [Fact]
    public void 製品別の正式タグを選び更新日時や他製品の版番号に影響されない()
    {
        string own = UpdateChecker.TagPrefix;
        string other = own == "v" ? "cpclipboard-v" : "v";
        var entries = new[]
        {
            new { tag_name = other + "99.0.0", draft = false, prerelease = false },
            new { tag_name = own + "1.9.0", draft = false, prerelease = false },
            new { tag_name = own + "1.10.0", draft = false, prerelease = false },
            new { tag_name = own + "2.0.0", draft = true, prerelease = false },
            new { tag_name = own + "3.0.0", draft = false, prerelease = true },
            new { tag_name = own + "9.0.0-beta", draft = false, prerelease = false },
        };
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(entries));
        Assert.Equal(own + "1.10.0", UpdateChecker.SelectLatestTag(json.RootElement));
        Assert.True(UpdateChecker.IsNewer(own + "1.10.0", "1.9.0"));
        using var otherOnly = JsonDocument.Parse(JsonSerializer.Serialize(new[] { entries[0] }));
        Assert.Null(UpdateChecker.SelectLatestTag(otherOnly.RootElement));
    }
    [Theory]
    [InlineData("[]")]
    [InlineData("[{}]")]
    [InlineData("[null]")]
    public void 対象の正式版がなければ更新なし(string data)
    {
        using var json = JsonDocument.Parse(data);
        Assert.Null(UpdateChecker.SelectLatestTag(json.RootElement));
    }
}
