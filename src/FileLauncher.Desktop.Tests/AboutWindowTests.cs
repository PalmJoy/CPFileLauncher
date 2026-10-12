using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FileLauncher.App;

namespace FileLauncher.Desktop.Tests;

public sealed class AboutWindowTests
{
    [AvaloniaFact]
    public void バージョンとデータフォルダとフォントのライセンスを出し_二度開いても1枚だけ()
    {
        var platform = new FakePlatform();
        var w = AboutWindow.Show(platform.Shell, "/data/FileLauncher");
        try
        {
            var texts = w.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
            Assert.Contains(texts, t => t?.StartsWith(Strings.FormatAbout_Version("")) == true);
            Assert.Contains(w.GetVisualDescendants().OfType<TextBox>(), t => t.Text == "/data/FileLauncher");
            var license = Assert.IsType<ScrollViewer>(w.GetVisualDescendants().OfType<Expander>().Single().Content);
            Assert.Contains("SIL OPEN FONT LICENSE", ((SelectableTextBlock)license.Content!).Text, StringComparison.OrdinalIgnoreCase);

            Assert.Same(w, AboutWindow.Show(platform.Shell, "/data/FileLauncher"));
        }
        finally { w.Close(); }
    }

    [AvaloniaFact]
    public void 著作権表示とGPLとソースとサードパーティのライセンスを出す()
    {
        var platform = new FakePlatform();
        var w = AboutWindow.Show(platform.Shell, "/data/FileLauncher");
        try
        {
            var texts = w.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
            Assert.Contains(AppInfo.Copyright, texts);
            Assert.Contains(texts, t => t?.Contains(AppInfo.LicenseId) == true);

            var links = w.GetVisualDescendants().OfType<HyperlinkButton>().ToList();
            links.Single(l => (string?)l.Content == Strings.About_Source).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(AppInfo.SourceUrl, platform.Launched[^1]);

            // テストの実行フォルダには notices が無いので、リポジトリの同じファイルの URL を開く
            links.Single(l => (string?)l.Content == Strings.About_ThirdParty).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(AppInfo.ThirdPartyNoticesUrl, platform.Launched[^1]);
        }
        finally { w.Close(); }
    }
}
