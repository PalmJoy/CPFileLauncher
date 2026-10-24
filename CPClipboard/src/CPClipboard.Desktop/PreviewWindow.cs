using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CPClipboard.Core.Effects;
using CPClipboard.Core.Model;
using CPClipboard.Core.Popup;
using CPClipboard.Platform;

namespace CPClipboard.App;

/// <summary>フォーカスも入力も受け取らない、全文の小窓。</summary>
internal sealed class PreviewWindow : Window
{
    private readonly Border _surface;
    private readonly TextBlock _metrics;
    private readonly TextBlock _body;
    private CancellationTokenSource? _animation;

    public PreviewWindow()
    {
        Title = AppInfo.ProductName;
        this[!FontFamilyProperty] = Themed.Res("FlFontFamily");
        Width = 360; Height = 240;
        CanResize = false; ShowActivated = false; ShowInTaskbar = false;
        WindowDecorations = WindowDecorations.None;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent;
        IsHitTestVisible = false;
        _metrics = new TextBlock { FontSize = 9, [!TextBlock.FontFamilyProperty] = Themed.Res("FlMonoFontFamily"), [!TextBlock.ForegroundProperty] = Themed.Res("FlHudText") };
        _body = new TextBlock
        {
            FontSize = 12, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 12, [!TextBlock.ForegroundProperty] = Themed.Res("FlText"),
        };
        _surface = new Border
        {
            Margin = new Thickness(8), Padding = new Thickness(10), CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Colors.Black, 0.96),
            [!Border.BorderBrushProperty] = Themed.Res("FlAccent"), BorderThickness = new Thickness(1),
            [!Border.BoxShadowProperty] = Themed.Res("FlToastGlow"),
            Child = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 6, Children = { _metrics, _body } },
        };
        Grid.SetRow(_body, 1);
        _surface[!Border.BackgroundProperty] = Themed.Res("FlSurface");
        Content = _surface;
    }

    public void Display(Snippet snippet, BoardWindow owner, IWindowService? platform, EffectSpec effect)
    {
        _animation?.Cancel();
        _animation = new();
        Opacity = owner.Opacity;
        if (owner.TryFindResource("FlSurface", owner.ActualThemeVariant, out var surface) && surface is ISolidColorBrush brush)
            _surface.Background = new SolidColorBrush(brush.Color, 0.96);
        _metrics.Text = $"LEN {snippet.Text.Length}  /  LINES {SnippetPreview.LineCount(snippet.Text)}";
        _body.Text = snippet.Text;
        _body.Measure(new Size(322, double.PositiveInfinity));
        Height = Math.Clamp(Math.Ceiling(_body.DesiredSize.Height + 60), 74, 240);
        Topmost = !OperatingSystem.IsMacOS();
        var screen = owner.Screens.ScreenFromPoint(owner.Position) ?? owner.Screens.Primary;
        double scale = platform?.UsesLogicalScreenCoordinates == true ? 1 : screen?.Scaling ?? 1;
        var area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
        var p = SnippetPreview.Place(new(owner.Position.X, owner.Position.Y, (int)Math.Ceiling(owner.Width * scale), (int)Math.Ceiling(owner.Height * scale)),
            (int)Math.Ceiling(Width * scale), (int)Math.Ceiling(Height * scale), new(area.X, area.Y, area.Width, area.Height), (int)(8 * scale));
        Position = new PixelPoint(p.X, p.Y);
        Show(owner);
        if (TryGetPlatformHandle()?.Handle is { } handle) platform?.ConfigurePreviewWindow(handle);
        _ = EffectRunner.ShowAsync(_surface, effect, _animation.Token);
    }
    public void Dismiss()
    {
        _animation?.Cancel();
        Hide();
    }
}
