using Avalonia;
using Avalonia.Media;

namespace CPClipboard.App;

/// <summary>行の上下の輪郭を左から右へ一度だけ走る送信信号。</summary>
internal sealed class SendSignal : SignalPulse
{
    public override void Render(DrawingContext context)
    {
        if (Progress < 0 || Progress > 1 || Stroke is null || Bounds.Width < 8) return;
        double head = 2 + (Bounds.Width - 4) * Progress;
        for (int i = 0; i < 12; i++)
        {
            double x = head - i * 2;
            if (x < 2) continue;
            using (context.PushOpacity((1 - i / 12.0) * Math.Min(1, (1 - Progress) * 8)))
            {
                context.DrawEllipse(Stroke, null, new Point(x, 2), i == 0 ? 2 : 1, 1);
                context.DrawEllipse(Stroke, null, new Point(x, Bounds.Height - 2), i == 0 ? 2 : 1, 1);
            }
        }
    }
}
