using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace FileLauncher.App;

/// <summary>短時間だけ描く信号。終了・ツリー離脱で時計を停止し、入力や起動を待たせない。</summary>
internal class SignalPulse : Control
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<SignalPulse, double>(nameof(Progress), -1);
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<SignalPulse, IBrush?>(nameof(Stroke));
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _watch = new();
    private int _duration;
    static SignalPulse() => AffectsRender<SignalPulse>(ProgressProperty, StrokeProperty);
    public SignalPulse()
    {
        IsHitTestVisible = false; ClipToBounds = true;
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) =>
        {
            var p = _watch.Elapsed.TotalMilliseconds / _duration;
            if (p >= 1) Stop();
            else Progress = p;
        });
        _timer.Stop();
        DetachedFromVisualTree += (_, _) => Stop();
    }
    public double Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public IBrush? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    internal bool IsRunning => _timer.IsEnabled;
    public void Play(int durationMs)
    {
        _duration = Math.Clamp(durationMs, 80, 250);
        _watch.Restart(); Progress = 0; _timer.Start();
    }
    public void Stop() { _timer.Stop(); _watch.Stop(); Progress = -1; }
    public override void Render(DrawingContext context)
    {
        if (Progress < 0 || Progress > 1 || Stroke is null || Bounds.Width < 8 || Bounds.Height < 8) return;
        double w = Bounds.Width - 4, h = Bounds.Height - 4, perimeter = 2 * (w + h);
        Point At(double phase)
        {
            double d = (phase % 1 + 1) % 1 * perimeter;
            if (d < w) return new Point(2 + d, 2);
            if ((d -= w) < h) return new Point(2 + w, 2 + d);
            if ((d -= h) < w) return new Point(2 + w - d, 2 + h);
            return new Point(2, 2 + h - (d - w));
        }
        using (context.PushOpacity(Math.Min(1, (1 - Progress) * 5)))
        {
            context.DrawRectangle(new Pen(Stroke, 1), new Rect(2, 2, w, h));
            for (int i = 0; i < 24; i++)
            {
                var point = At(Progress - i * 0.006);
                using (context.PushOpacity(1 - i / 24.0))
                    context.DrawEllipse(Stroke, null, point, i == 0 ? 2.5 : 1.5, i == 0 ? 2.5 : 1.5);
            }
        }
    }
}
