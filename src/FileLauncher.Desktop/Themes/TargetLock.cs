using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FileLauncher.App;

/// <summary>操作中のスロットの四隅に収束する照準。待機中は何も描かない。</summary>
internal sealed class TargetLock : Control
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<TargetLock, double>(nameof(Progress));
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<TargetLock, IBrush?>(nameof(Stroke));

    static TargetLock() => AffectsRender<TargetLock>(ProgressProperty, StrokeProperty);

    public TargetLock() { IsHitTestVisible = false; ClipToBounds = true; }
    public double Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public IBrush? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    public override void Render(DrawingContext context)
    {
        double p = Math.Clamp(Progress, 0, 1);
        if (p <= 0 || Stroke is null || Bounds.Width < 24 || Bounds.Height < 24) return;
        double inset = 1 + 5 * p, arm = 8;
        var pen = new Pen(Stroke, 1.5);
        using (context.PushOpacity(p))
        foreach (var (x, y, dx, dy) in new[] {
            (inset, inset, 1, 1), (Bounds.Width-inset, inset, -1, 1),
            (inset, Bounds.Height-inset, 1, -1), (Bounds.Width-inset, Bounds.Height-inset, -1, -1) })
        {
            context.DrawLine(pen, new Point(x, y), new Point(x + arm * dx, y));
            context.DrawLine(pen, new Point(x, y), new Point(x, y + arm * dy));
        }
    }
}
