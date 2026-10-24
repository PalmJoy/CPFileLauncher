using CPClipboard.Platform.Windows;

namespace CPClipboard.Desktop.Tests;

public class WindowsForegroundTests
{
    [Fact]
    public void Z順で除外対象を飛ばし最初の候補を返す()
    {
        Assert.Equal((nint)4, WindowsWindowService.FindPrevious(1,
            w => w < 5 ? w + 1 : 0, w => w >= 4));
    }

    [Fact]
    public void 候補がない場合と走査中の循環は貼り付け先なしになる()
    {
        Assert.Equal((nint)0, WindowsWindowService.FindPrevious(1, _ => 0, _ => true));
        Assert.Equal((nint)0, WindowsWindowService.FindPrevious(1, w => w == 1 ? 2 : 1, _ => false));
    }
}
