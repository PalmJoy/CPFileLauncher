using CPClipboard.Core.Input;
using CPClipboard.Core.Popup;

namespace CPClipboard.Tests;

public class SnippetPreviewTests
{
    [Theory]
    [InlineData("plain", false, false)]
    [InlineData("plain", true, true)]
    [InlineData("a\nb", false, true)]
    [InlineData("a\tb", false, true)]
    public void 改行タブまたは実際の省略だけが対象(string text, bool ellipsized, bool expected) =>
        Assert.Equal(expected, SnippetPreview.NeedsPreview(text, ellipsized));

    [Fact]
    public void 右優先で入らなければ左へ退避し画面に収める()
    {
        var area = new ScreenRect(0, 0, 1920, 1080);
        Assert.Equal(new ScreenPoint(508, 100), SnippetPreview.Place(new(100, 100, 400, 300), 360, 240, area));
        Assert.Equal(new ScreenPoint(1232, 840), SnippetPreview.Place(new(1600, 900, 300, 100), 360, 240, area));
        Assert.Equal(new ScreenPoint(0, 0), SnippetPreview.Place(new(50, 0, 400, 100), 360, 240, new(0, 0, 600, 600)));
        Assert.Equal(new ScreenPoint(-792, -240), SnippetPreview.Place(new(-1200, -200, 400, 200), 360, 240, new(-1920, -1080, 1920, 1080)));
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("a", 1)]
    [InlineData("a\nb\n", 3)]
    public void 行数には末尾の空行も含む(string text, int count) => Assert.Equal(count, SnippetPreview.LineCount(text));
}
