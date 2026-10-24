using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FileLauncher.App;

/// <summary>A scalable illustration: application and folder icons dragged into a launcher slot.</summary>
internal sealed class StartupDropIllustration : Control
{
    public StartupDropIllustration()
    {
        Height = 150;
        IsHitTestVisible = false;
        Avalonia.Automation.AutomationProperties.SetName(this, Strings.StartupGuide_Description);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        IBrush Brush(string key) => this.TryFindResource(key, ActualThemeVariant, out var value)
            && value is IBrush brush ? brush : Brushes.Cyan;
        var primary = Brush("FlAccent");
        var secondary = Brush("FlAccent2");
        var surface = Brush("FlWindowBackground");
        var pen = new Pen(primary, 2);
        var accentPen = new Pen(secondary, 2);
        using var scale = context.PushTransform(Matrix.CreateScale(Bounds.Width / 392, 1));

        void Line(IPen stroke, double x1, double y1, double x2, double y2) =>
            context.DrawLine(stroke, new Point(x1, y1), new Point(x2, y2));
        void Folder(double x, double y, double size, IBrush color)
        {
            using var transform = context.PushTransform(Matrix.CreateScale(size / 60, size / 60)
                * Matrix.CreateTranslation(x, y));
            context.DrawGeometry(surface, new Pen(color, 2), Geometry.Parse(
                "M 0,8 L 23,8 L 30,16 L 60,16 L 60,50 L 0,50 Z M 0,23 L 60,23"));
        }

        // App window, with a recognizable four-tile application symbol.
        context.DrawRectangle(surface, pen, new Rect(18, 12, 62, 56), 6, 6);
        Line(pen, 18, 25, 80, 25);
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 2; col++)
                context.DrawRectangle(primary, null, new Rect(35 + col * 15, 34 + row * 13, 10, 8), 1, 1);
        Folder(49, 81, 66, primary);

        // Both source icons lead to the highlighted empty slot.
        context.DrawGeometry(null, new Pen(secondary, 2, DashStyle.Dash),
            Geometry.Parse("M 91,40 C 139,40 132,66 181,66 M 123,111 C 151,111 149,79 181,79"));
        Line(accentPen, 166, 60, 181, 66);
        Line(accentPen, 170, 79, 181, 66);

        // Miniature launcher with a highlighted drop target.
        context.DrawRectangle(surface, pen, new Rect(206, 9, 176, 132), 8, 8);
        Line(pen, 206, 32, 382, 32);
        Line(pen, 220, 21, 254, 21);
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 3; col++)
            {
                var slot = new Rect(220 + col * 51, 44 + row * 45, 40, 35);
                context.DrawRectangle(null, row == 0 && col == 0 ? accentPen : new Pen(primary, 1), slot, 3, 3);
            }
        Folder(225, 46, 30, secondary);

        // Pointer overlaps the destination, making the drag-and-drop gesture explicit.
        context.DrawGeometry(surface, accentPen, Geometry.Parse(
            "M 250,65 L 250,101 L 259,92 L 266,106 L 274,102 L 267,88 L 280,87 Z"));
    }
}
