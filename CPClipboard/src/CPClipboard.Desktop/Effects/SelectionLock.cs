using Avalonia;
using Avalonia.Media;

namespace CPClipboard.App;

/// <summary>行を選択したときに四隅が寄る短いターゲット表示。</summary>
internal sealed class SelectionLock : SignalPulse
{
    public static readonly StyledProperty<bool> ActiveProperty = AvaloniaProperty.Register<SelectionLock, bool>(nameof(Active));
    static SelectionLock() => AffectsRender<SelectionLock>(ActiveProperty);
    public bool Active { get => GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }
    public override void Render(DrawingContext context)
    {
        if (!Active || Stroke is null) return;
        double phase = Progress < 0 ? 1 : Progress;
        double inset = 2 + (1 - phase) * 5;
        var pen = new Pen(Stroke, 1);
        using (context.PushOpacity(0.6 + phase * 0.4))
        foreach (var (x, y, dx, dy) in new[] { (inset, inset, 1, 1), (Bounds.Width - inset, inset, -1, 1),
            (inset, Bounds.Height - inset, 1, -1), (Bounds.Width - inset, Bounds.Height - inset, -1, -1) })
        {
            context.DrawLine(pen, new Point(x, y), new Point(x + dx * 6, y));
            context.DrawLine(pen, new Point(x, y), new Point(x, y + dy * 6));
        }
    }
}
