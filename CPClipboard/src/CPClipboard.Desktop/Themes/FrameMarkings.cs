using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CPClipboard.App;

/// <summary>枠の外側の製品刻印・目盛り。実際の通信状態や機器 ID は表さない。</summary>
internal sealed class FrameMarkings : Control
{
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<FrameMarkings, IBrush?>(nameof(Stroke));
    static FrameMarkings() => AffectsRender<FrameMarkings>(StrokeProperty);
    public FrameMarkings() { IsHitTestVisible = false; ClipToBounds = false; }
    public IBrush? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public override void Render(DrawingContext context)
    {
        if (Stroke is null || Bounds.Width < 220) return;
        var pen = new Pen(Stroke, 1);
        var font = new Typeface(this.TryFindResource("FlFontFamily", ActualThemeVariant, out var family) && family is FontFamily f ? f : FontFamily.Default);
        void Label(string text, Point origin) => context.DrawText(new FormattedText(text,
            CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, 8, Stroke), origin);
        using (context.PushOpacity(0.6))
        {
            Label("CPCB // SNIPPET DECK", new Point(16, -18));
            Label("UNIT / 01", new Point(Bounds.Width - 62, -18));
            Label("CP / CLIPBOARD INTERFACE", new Point(16, Bounds.Height + 8));
            for (int i = 0; i < 12; i++)
            {
                double x = Bounds.Width - 80 + i * 4;
                context.DrawLine(pen, new Point(x, Bounds.Height + 8), new Point(x, Bounds.Height + (i % 3 == 0 ? 15 : 12)));
            }
            for (int i = 0; i < 3; i++)
                context.DrawLine(pen, new Point(Bounds.Width - 22 + i * 4, -5), new Point(Bounds.Width - 17 + i * 4, -10));
        }
    }
}
