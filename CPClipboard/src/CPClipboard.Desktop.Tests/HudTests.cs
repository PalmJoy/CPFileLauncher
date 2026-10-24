using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CPClipboard.App;
using CPClipboard.Core.Model;

namespace CPClipboard.Desktop.Tests;

/// <summary>HUD: ステータス行・背景グリッド・走査線（LOOK §3.9）。</summary>
public sealed class HudTests
{
    private static BoardWindow Board(AppearanceSettings appearance, TemplateBook? data = null)
    {
        var board = new BoardWindow();
        board.ApplyEffects(appearance);
        board.Render(data ?? TemplateBook.CreateEmpty(), appearance, 1);
        return board;
    }

    private static string Text(BoardWindow b, string name) => b.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == name).Text ?? "";

    [AvaloniaFact]
    public void ステータス行にグループと件数と時刻が出て_時刻は見えている間だけ動く()
    {
        var data = TemplateBook.CreateEmpty();
        data[1].Name = "挨拶";
        data[1].Snippets.Add(new Snippet { Text = "a" });
        var board = Board(new AppearanceSettings(), data);
        Assert.False(board.ClockRunning); // まだ出していない

        board.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("GRP 1 · 挨拶", Text(board, "StatusGroup"));
        Assert.Equal("ITEMS 1", Text(board, "StatusItems"));
        Assert.Matches(@"^\d{4}/\d\d/\d\d \d\d:\d\d$", Text(board, "StatusClock")); // 既定は日付付き
        Assert.True(board.ClockRunning);

        board.SwitchGroup(2);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("GRP 2", Text(board, "StatusGroup"));
        Assert.Equal("ITEMS 0", Text(board, "StatusItems"));

        board.Hide();
        Dispatcher.UIThread.RunJobs();
        Assert.False(board.ClockRunning);
        board.AllowClose = true;
        board.Close();
    }

    [AvaloniaFact]
    public void ステータス行を消すと盤面が低くなり_時刻なしならタイマーは動かない()
    {
        var on = new AppearanceSettings();
        var off = new AppearanceSettings { Hud = new HudSettings { StatusBar = false } };
        var noClock = new AppearanceSettings { Hud = new HudSettings { Clock = ClockMode.Off, Date = false } };
        var a = Board(on);
        var b = Board(off);
        Assert.Equal(21, a.Height - b.Height, 3);

        var c = Board(noClock);
        c.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.False(c.ClockRunning);
        Assert.Equal("", Text(c, "StatusClock"));
        foreach (var w in new[] { a, b, c }) { w.AllowClose = true; w.Close(); }
    }

    [AvaloniaFact]
    public void 背景グリッドは設定で出し入れできる()
    {
        var on = Board(new AppearanceSettings());
        var off = Board(new AppearanceSettings { Hud = new HudSettings { Grid = false } });
        on.Show(); off.Show();
        Dispatcher.UIThread.RunJobs();
        var layer = on.GetVisualDescendants().OfType<GridLayer>().Single();
        Assert.True(layer.IsVisible);
        Assert.Equal(0.03, layer.Opacity, 3); // 既定 3%
        Assert.False(off.GetVisualDescendants().OfType<GridLayer>().Single().IsVisible);
        foreach (var w in new[] { on, off }) { w.AllowClose = true; w.Close(); }
    }

    [AvaloniaFact]
    public void 走査線は既定で25パーセント3px間隔で出て_設定で消せて間隔も変わる()
    {
        var on = Board(new AppearanceSettings());
        var off = Board(new AppearanceSettings { Hud = new HudSettings { Scanlines = false } });
        var wide = Board(new AppearanceSettings { Hud = new HudSettings { ScanlinePitch = 4, ScanlineOpacity = 10 } });
        foreach (var w in new[] { on, off, wide }) w.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.True(on.ScanlineLayer.IsVisible);
        Assert.Equal(0.25, on.ScanlineLayer.Opacity, 3);
        Assert.Equal(3, on.ScanlineLayer.Pitch);
        Assert.False(off.ScanlineLayer.IsVisible);
        Assert.Equal((4.0, 0.1), (wide.ScanlineLayer.Pitch, Math.Round(wide.ScanlineLayer.Opacity, 3)));
        // グリッドの上・タブと行の下
        var surface = (Grid)on.ScanlineLayer.Parent!;
        var gridIndex = surface.Children.IndexOf(surface.Children.OfType<GridLayer>().Single());
        var circuitIndex = surface.Children.IndexOf(surface.Children.OfType<CircuitLayer>().Single());
        var hologramIndex = surface.Children.IndexOf(surface.Children.OfType<HologramEdges>().Single());
        Assert.True(gridIndex < hologramIndex && hologramIndex < circuitIndex);
        Assert.True(circuitIndex < surface.Children.IndexOf(on.ScanlineLayer));
        Assert.True(surface.Children.IndexOf(on.ScanlineLayer) < surface.Children.IndexOf(surface.Children.OfType<DockPanel>().Single()));
        foreach (var w in new[] { on, off, wide }) { w.AllowClose = true; w.Close(); }
    }
}
