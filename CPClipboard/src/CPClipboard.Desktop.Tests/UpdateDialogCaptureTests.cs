using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CPClipboard.App;

namespace CPClipboard.Desktop.Tests;

public sealed class UpdateDialogCaptureTests
{
    [AvaloniaFact]
    public void Capture_update_dialog()
    {
        var output = Environment.GetEnvironmentVariable("CPCB_CAPTURE_DIR");
        if (string.IsNullOrEmpty(output)) return;
        Directory.CreateDirectory(output);
        try
        {
            Loc.ApplyStartupLanguage("ja");
            var app = new CPClipboard.App.App();
            var window = app.CreateUpdateWindow("v1.0.2");
            window.Show();
            Dispatcher.UIThread.RunJobs();
            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            frame.Save(Path.Combine(output, "update-available-ja.png"));
            window.Close();
        }
        finally { Loc.ApplyStartupLanguage("en"); }
    }
}
