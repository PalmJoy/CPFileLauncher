using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CPClipboard.Core.Model;

namespace CPClipboard.App;

/// <summary>盤面と編集一覧で共有する1行。キー表示は本文と独立させる。</summary>
internal sealed class SnippetRowContent : Grid
{
    internal TextBlock Text { get; } = new()
    {
        Classes = { "snippet-text" }, TextWrapping = TextWrapping.NoWrap,
        TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center,
        [!TextBlock.ForegroundProperty] = Themed.Res("FlRowText"),
    };
    internal TextBlock Shortcut { get; } = new()
    {
        Classes = { "shortcut" }, Width = 20, TextAlignment = TextAlignment.Right,
        VerticalAlignment = VerticalAlignment.Center, FontSize = 11,
        [!TextBlock.FontFamilyProperty] = Themed.Res("FlMonoFontFamily"),
        [!TextBlock.ForegroundProperty] = Themed.Res("FlTextMuted"),
    };
    public SnippetRowContent()
    {
        var marker = new TextBlock
        {
            Text = "▸", Classes = { "marker" }, Width = 12, Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            [!TextBlock.ForegroundProperty] = Themed.Res("FlAccent"),
        };
        var content = new DockPanel { Margin = new Thickness(0, 0, 8, 0) };
        DockPanel.SetDock(marker, Dock.Left); DockPanel.SetDock(Shortcut, Dock.Right);
        content.Children.Add(marker); content.Children.Add(Shortcut); content.Children.Add(Text);
        Children.Add(content);
    }
    internal void Update(string text, int index, double fontSize)
    {
        Text.Text = SnippetLine.Format(text); Text.FontSize = fontSize;
        Shortcut.Text = index is >= 0 and < 26 ? ((char)('a' + index)).ToString() : "";
    }
}
