using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CPClipboard.App;
using CPClipboard.Core.Model;
using CPClipboard.Core.Theming;

namespace CPClipboard.Desktop.Tests;

public sealed class AccentAndColorTests
{
    private static void SetPreset(Application app, AccentPreset id)
    {
        var (p, s) = ColorPresets.For(id);
        AppTheme.SetColors(app, p, s);
    }

    private static Color ColorOf(ResourceDictionary d, string key) => ((ISolidColorBrush)d[key]!).Color;

    /// <summary>
    /// Fluent のアクセント濃淡（Dark/Light 1〜3）は旧来の手決めの値と混色計算で各チャンネル最大 4 段階ずれる（見た目では分からない）ので近似で比べる。
    /// </summary>
    private static void AssertClose(string expected, object? actual)
    {
        var e = Color.Parse(expected);
        var a = (Color)actual!;
        Assert.True(Math.Abs(e.R - a.R) <= 4 && Math.Abs(e.G - a.G) <= 4 && Math.Abs(e.B - a.B) <= 4, $"{expected} ≉ {a}");
    }

    [AvaloniaFact]
    public void シアンの配色から導いたトークンはSPECの導出規則どおり()
    {
        var (p, s) = ColorPresets.For(AccentPreset.Cyan);
        var derived = CyberDerivation.Derive(p, s);
        var d = AppTheme.Cyber(CPClipboard.App.CyberPalette.From(derived));
        Assert.Equal(Color.Parse("#CC00E5FF"), ColorOf(d, "FlBoardBorder"));
        Assert.Equal(Color.Parse("#1F00E5FF"), ColorOf(d, "FlRowHoverBackground"));
        Assert.Equal(Color.Parse("#24FF2BD6"), ColorOf(d, "FlAccent2Soft"));
        Assert.Equal(Color.Parse("#3300E5FF"), ColorOf(d, "FlHeaderSeparator"));
        var wb = derived.WindowBackground;
        Assert.Equal(Color.FromArgb(0xF5, wb.R, wb.G, wb.B), ColorOf(d, "FlToastBackground")); // 窓の背景（主色から導出）に 96%
        Assert.Equal(Color.Parse("#9900E5FF"), ColorOf(d, "FlToastBorder"));
        AssertClose("#00B8CC", d["SystemAccentColorDark1"]);
        AssertClose("#008C99", d["SystemAccentColorDark2"]);
        AssertClose("#006066", d["SystemAccentColorDark3"]);
        AssertClose("#4DEDFF", d["SystemAccentColorLight1"]);
        AssertClose("#80F2FF", d["SystemAccentColorLight2"]);
        AssertClose("#B3F7FF", d["SystemAccentColorLight3"]);
        Assert.Equal(4, ((BoxShadows)d["FlBoardGlow"]!).Count); // ネオン管の 3 段 + 内側
        Assert.Equal(Color.Parse("#E600E5FF"), ((BoxShadows)d["FlBoardGlow"]!)[0].Color);
    }

    [AvaloniaFact]
    public void 配色を変えるとアクセントが変わり_戻せる()
    {
        var app = Application.Current!;
        try
        {
            SetPreset(app, AccentPreset.Red);
            Assert.True(app.TryGetResource("FlAccent", AppTheme.Cyberpunk, out var red));
            Assert.Equal(Color.Parse("#FF2E4D"), ((ISolidColorBrush)red!).Color);

            SetPreset(app, AccentPreset.Cyan);
            Assert.True(app.TryGetResource("FlAccent", AppTheme.Cyberpunk, out var cyan));
            Assert.Equal(Color.Parse("#00E5FF"), ((ISolidColorBrush)cyan!).Color);
        }
        finally { SetPreset(app, AccentPreset.Cyan); }
    }

    [AvaloniaFact]
    public void 盤面に表示中でも配色の変更が枠の色へすぐ反映される()
    {
        var app = Application.Current!;
        var board = new BoardWindow();
        try
        {
            var appearance = new AppearanceSettings();
            board.Render(TemplateBook.CreateEmpty(), appearance, 0);
            board.Show();
            Dispatcher.UIThread.RunJobs();
            var chrome = board.GetVisualDescendants().OfType<FrameChrome>().Single();

            SetPreset(app, AccentPreset.Green);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Color.Parse("#CC39FF88"), ((ISolidColorBrush)chrome.OutlineBrush!).Color);
        }
        finally
        {
            SetPreset(app, AccentPreset.Cyan);
            board.AllowClose = true;
            board.Close();
        }
    }

}
