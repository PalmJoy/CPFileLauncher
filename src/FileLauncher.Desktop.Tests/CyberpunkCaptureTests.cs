using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FileLauncher.App;
using FileLauncher.Core.Model;

namespace FileLauncher.Desktop.Tests;

public sealed class CyberpunkCaptureTests
{
    private static BoardWindow Create(bool animation = true, Action<Board>? configure = null)
    {
        var data = Board.CreateDefault();
        var page = data.Pages[0];
        page.Name = "WORKSPACE"; page.Rows = 3; page.Cols = 5;
        string[] names = ["Terminal", "Projects", "Browser", "Design", "Music", "Notes", "Downloads", "Settings"];
        for (int i = 0; i < names.Length; i++)
            page.Items.Add(new LauncherItem { Name = names[i], Target = "https://example.com/" + i, Kind = ItemKind.Url, Row = i / 5, Col = i % 5 });
        data.Pages.Add(new Page { Name = "CREATIVE", Rows = 3, Cols = 5 });
        configure?.Invoke(data);
        var appearance = new AppearanceSettings { Animation = animation, ButtonSize = 48 };
        appearance.Hud.Clock = ClockMode.Off;
        appearance.Hud.Date = false;
        var window = new BoardWindow();
        window.ApplyEffects(appearance);
        window.Render(data, appearance, 0);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void Target_lock_follows_hover_and_respects_animation_off()
    {
        foreach (bool animation in new[] { true, false })
        {
            var window = Create(animation);
            try
            {
                var slot = window.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("slot") && !b.Classes.Contains("empty"));
                var center = slot.TranslatePoint(new Point(slot.Bounds.Width / 2, slot.Bounds.Height / 2), window)!.Value;
                window.MouseMove(center);
                Dispatcher.UIThread.RunJobs();
                var target = slot.GetVisualDescendants().OfType<TargetLock>().SingleOrDefault();
                if (!animation) { Assert.Null(target); continue; }
                Assert.NotNull(target);
                Assert.True(slot.IsPointerOver);
                Assert.True(target.Bounds.Width > 20);
                // 遷移の着地点を確認し、同じ状態を実描画してキャプチャする。
                target.Transitions = null;
                window.MouseMove(new Point(1, 1));
                window.MouseMove(center);
                Assert.Equal(1, target.Progress);
                Capture(window, "01-target-lock-hover.png");
                window.MouseMove(new Point(1, 1));
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(0, target.Progress);
                Capture(window, "01-target-lock-idle.png");
            }
            finally { window.AllowClose = true; window.Close(); }
        }
    }

    [AvaloniaFact]
    public void Launch_signal_starts_on_success_and_stops_on_detach()
    {
        LauncherItem item = null!;
        var window = Create(configure: b => item = b.Pages[0].Items[0]);
        try
        {
            var slot = window.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("slot") && !b.Classes.Contains("empty"));
            var signal = slot.GetVisualDescendants().OfType<SignalPulse>().Single(s => s is not DataSweep);
            Assert.False(signal.IsRunning);
            // 実際の起動成功通知と同じ API。テストでは外部アプリは開かない。
            window.PlayLaunch(item);
            Assert.True(signal.IsRunning);
            signal.Progress = 0.38; // 輪郭を回る途中のフレーム
            Capture(window, "02-launch-signal.png");
            window.Render(Core.Model.Board.CreateDefault(), new AppearanceSettings(), 0);
            Dispatcher.UIThread.RunJobs();
            Assert.False(signal.IsRunning);
            Assert.Equal(-1, signal.Progress);
        }
        finally { window.AllowClose = true; window.Close(); }
    }

    [AvaloniaFact]
    public void Circuit_pattern_follows_grid_visibility()
    {
        var window = Create();
        try
        {
            var circuit = window.GetVisualDescendants().OfType<CircuitLayer>().Single();
            Assert.True(circuit.IsVisible);
            Assert.Equal(0.12, circuit.Opacity, 3);
            Capture(window, "03-circuit-pattern.png");
            var appearance = new AppearanceSettings();
            appearance.Hud.Grid = false;
            window.Render(Core.Model.Board.CreateDefault(), appearance, 0);
            Assert.False(circuit.IsVisible);
        }
        finally { window.AllowClose = true; window.Close(); }
    }

    [AvaloniaFact]
    public void Capture_frame_markings()
    {
        var window = Create();
        try { Capture(window, "04-frame-markings.png"); }
        finally { window.AllowClose = true; window.Close(); }
    }

    [AvaloniaFact]
    public void Page_sweep_follows_switch_direction()
    {
        var window = Create();
        try
        {
            var sweep = window.GetVisualDescendants().OfType<DataSweep>().Single(s => !s.Vertical);
            window.SwitchPageTo(1);
            Assert.True(sweep.IsRunning);
            Assert.False(sweep.Reverse);
            window.SwitchPageTo(0);
            Assert.True(sweep.Reverse);
            sweep.Progress = 0.45;
            Capture(window, "05-page-sweep.png");
            window.Render(Core.Model.Board.CreateDefault(), new AppearanceSettings(), 0);
            Assert.False(sweep.IsRunning);
        }
        finally { window.AllowClose = true; window.Close(); }
    }

    [AvaloniaFact]
    public void Registration_scan_targets_only_registered_item_and_stops_when_hidden()
    {
        LauncherItem item = null!;
        var window = Create(configure: b => item = b.Pages[0].Items[6]);
        try
        {
            window.PlayRegistration([item]);
            var scans = window.GetVisualDescendants().OfType<DataSweep>().Where(s => s.Vertical).ToList();
            var active = Assert.Single(scans, s => s.IsRunning);
            active.Progress = 0.55;
            Capture(window, "06-registration-scan.png");
            window.Hide();
            Assert.All(scans, s => Assert.False(s.IsRunning));
        }
        finally { window.AllowClose = true; window.Close(); }
    }

    [AvaloniaFact]
    public void Capture_hologram_edges()
    {
        var window = Create();
        try { Capture(window, "07-hologram-edges.png"); }
        finally { window.AllowClose = true; window.Close(); }
    }

    [AvaloniaFact]
    public async Task Signal_clock_finishes_without_leaving_a_timer_running()
    {
        var window = Create();
        try
        {
            var signal = window.GetVisualDescendants().OfType<SignalPulse>().First(s => s is not DataSweep);
            signal.Play(80);
            Assert.True(signal.IsRunning);
            await Task.Delay(400);
            Dispatcher.UIThread.RunJobs();
            Assert.False(signal.IsRunning);
            Assert.Equal(-1, signal.Progress);
        }
        finally { window.AllowClose = true; window.Close(); }
    }

    private static void Capture(BoardWindow window, string filename)
    {
        var dir = Environment.GetEnvironmentVariable("CPFL_CAPTURE_DIR");
        if (string.IsNullOrEmpty(dir)) return;
        Directory.CreateDirectory(dir);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame.Save(Path.Combine(dir, filename));
    }
}
