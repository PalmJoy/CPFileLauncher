using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CPClipboard.App;
using CPClipboard.Core.Model;

namespace CPClipboard.Desktop.Tests;

/// <summary>盤面（SPEC §3.1 / §3.2 / §6.1）: グループタブ・1 行表示・高さ・キー操作。</summary>
public sealed class BoardTests
{
    private static TemplateBook Book()
    {
        var book = TemplateBook.CreateEmpty();
        book[1].Name = "挨拶";
        foreach (var t in new[] { "いつもお世話になっております。\n株式会社〇〇の△△です。", "お疲れさまです。", "以上、よろしくお願いいたします。" })
            book[1].Snippets.Add(new Snippet { Text = t });
        book[4].Snippets.Add(new Snippet { Text = "住所\t〒100-0001" });
        return book;
    }

    private static BoardWindow Create(TemplateBook? book = null, AppearanceSettings? appearance = null, int group = 1)
    {
        appearance ??= new AppearanceSettings { Animation = false };
        var window = new BoardWindow();
        window.ApplyEffects(appearance);
        window.Render(book ?? Book(), appearance, group);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Close(BoardWindow w)
    {
        w.AllowClose = true;
        w.Close();
    }

    private static List<Button> Rows(BoardWindow w) =>
        w.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("row")).ToList();

    private static List<Avalonia.Controls.Primitives.ToggleButton> Tabs(BoardWindow w) =>
        w.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>().Where(b => b.Classes.Contains("tab")).ToList();

    [AvaloniaFact]
    public void グループタブは0から9の10個で_空のグループは暗くし_選択中のグループに印が付く()
    {
        var w = Create();
        try
        {
            var tabs = Tabs(w);
            Assert.Equal(10, tabs.Count);
            Assert.True(tabs[1].IsChecked);
            Assert.Contains("empty", tabs[0].Classes);
            Assert.DoesNotContain("empty", tabs[1].Classes);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 定型文は1件1行で改行はリターン記号_タブは空白で並ぶ()
    {
        var w = Create();
        try
        {
            var rows = Rows(w);
            Assert.Equal(3, rows.Count);
            var text = rows[0].GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("snippet-text"));
            Assert.Equal("いつもお世話になっております。⏎株式会社〇〇の△△です。", text.Text);
            Assert.Equal(Avalonia.Media.TextTrimming.CharacterEllipsis, text.TextTrimming);
            Assert.Contains("selected", rows[0].Classes); // 開いたときは先頭の行
            w.SwitchGroup(4);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("住所 〒100-0001", Rows(w)[0].GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("snippet-text")).Text);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 盤面の高さは件数に合わせ_最小3行_最大は最大行数()
    {
        var book = Book();
        for (int i = 0; i < 20; i++) book[2].Snippets.Add(new Snippet { Text = "x" + i });
        var appearance = new AppearanceSettings { Animation = false, MaxRows = 5 };
        var w = Create(book, appearance, group: 0);
        try
        {
            double empty = w.Height;
            w.SwitchGroup(1); // 3 件
            Assert.Equal(empty, w.Height, 3);
            w.SwitchGroup(2); // 20 件 → 5 行
            Assert.Equal(empty + 2 * 26, w.Height, 3);
            Assert.Equal(appearance.BoardWidth + 80, w.Width, 3);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 行が最大行数以下ならスクロールしない()
    {
        var w = Create();
        try
        {
            var scroller = w.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "RowsScroller");
            Assert.True(scroller.Extent.Height <= scroller.Viewport.Height + 0.5, $"{scroller.Extent.Height} > {scroller.Viewport.Height}");
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 空のグループは案内を出す()
    {
        var w = Create(group: 0);
        try
        {
            Assert.Empty(Rows(w));
            var notice = w.GetVisualDescendants().OfType<StackPanel>().Single(p => p.Name == "EmptyNotice");
            Assert.True(notice.IsVisible);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 数字キーでグループへ_CtrlTabと矢印は空のグループを飛ばす()
    {
        var w = Create();
        try
        {
            w.KeyPressQwerty(PhysicalKey.Digit0, RawInputModifiers.None);
            Assert.Equal(0, w.GroupKey); // 数字キーは空のグループにも移る
            w.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);
            Assert.Equal(1, w.GroupKey);
            w.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            Assert.Equal(4, w.GroupKey);
            w.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            Assert.Equal(1, w.GroupKey); // 循環
            w.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control | RawInputModifiers.Shift);
            Assert.Equal(4, w.GroupKey);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 矢印で選びEnterで貼り付け_ShiftEnterはコピーだけ()
    {
        var w = Create();
        var invoked = new List<(string Text, bool CopyOnly)>();
        w.SnippetInvoked += (s, copyOnly) => invoked.Add((s.Text, copyOnly));
        try
        {
            w.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            w.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            w.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            w.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None); // 端で止まる
            w.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.Shift);
            Assert.Equal([("お疲れさまです。", false), ("以上、よろしくお願いいたします。", true)], invoked);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void グループの切替で走査が切替方向に流れる()
    {
        var w = Create(appearance: new AppearanceSettings());
        try
        {
            var sweep = w.GetVisualDescendants().OfType<DataSweep>().Single();
            w.SwitchGroup(4);
            Assert.True(sweep.IsRunning);
            Assert.False(sweep.Reverse);
            w.SwitchGroup(1);
            Assert.True(sweep.Reverse);
            w.Render(Book(), new AppearanceSettings(), 1);
            Assert.False(sweep.IsRunning);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 回路パターンはグリッドの表示に従う()
    {
        var w = Create(appearance: new AppearanceSettings());
        try
        {
            var circuit = w.GetVisualDescendants().OfType<CircuitLayer>().Single();
            Assert.True(circuit.IsVisible);
            Assert.Equal(0.12, circuit.Opacity, 3);
            var appearance = new AppearanceSettings();
            appearance.Hud.Grid = false;
            w.Render(Book(), appearance, 1);
            Assert.False(circuit.IsVisible);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 開いたときのグループは前回_空なら最初の空でないグループ()
    {
        var book = Book();
        Assert.Equal(4, PopupController.StartGroup(book, 4));
        Assert.Equal(1, PopupController.StartGroup(book, 7));
        Assert.Equal(7, PopupController.StartGroup(TemplateBook.CreateEmpty(), 7));
    }

    [AvaloniaFact]
    public void Capture_board()
    {
        var dir = Environment.GetEnvironmentVariable("CPCB_CAPTURE_DIR");
        if (string.IsNullOrEmpty(dir)) return;
        var appearance = new AppearanceSettings();
        appearance.Hud.Clock = ClockMode.Off;
        appearance.Hud.Date = false;
        var w = Create(appearance: appearance);
        try
        {
            Directory.CreateDirectory(dir);
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            using var frame = w.CaptureRenderedFrame();
            Assert.NotNull(frame);
            frame.Save(Path.Combine(dir, "board.png"));
        }
        finally { Close(w); }
    }
    [AvaloniaFact]
    public void 英字キーは論理順の先頭26件だけを実行しスクロールしても割当が変わらない()
    {
        var book = TemplateBook.CreateEmpty();
        for (int i = 0; i < 30; i++) book[1].Snippets.Add(new() { Text = $"line {i}" });
        book[4].Snippets.Add(new() { Text = "other group" });
        var w = Create(book, new AppearanceSettings { Animation = false, MaxRows = 4 });
        var invoked = new List<(string, bool)>();
        w.SnippetInvoked += (snippet, copy) => invoked.Add((snippet.Text, copy));
        try
        {
            w.KeyPressQwerty(PhysicalKey.End, RawInputModifiers.None); Dispatcher.UIThread.RunJobs();
            Assert.All(Rows(w), row =>
            {
                var snippet = (Snippet)row.Tag!;
                int index = book[1].Snippets.IndexOf(snippet);
                Assert.Equal(index < 26 ? ((char)('a'+index)).ToString() : "", ((SnippetRowContent)row.Content!).Shortcut.Text);
            });
            w.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.None);
            Assert.Equal(25, w.SelectedIndex);
            w.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None);
            w.KeyPressQwerty(PhysicalKey.B, RawInputModifiers.Shift);
            foreach(var mods in new[] { RawInputModifiers.Control, RawInputModifiers.Alt, RawInputModifiers.Meta })
                w.KeyPressQwerty(PhysicalKey.C, mods);
            Assert.Equal(new[] { ("line 25", false), ("line 0", false), ("line 1", true) }, invoked);
            w.SwitchGroup(4);
            w.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.None); Assert.Equal(3, invoked.Count);
            w.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None); Assert.Equal(("other group", false), invoked[^1]);
            w.SwitchGroup(0); w.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None); Assert.Equal(4, invoked.Count);
        }
        finally { Close(w); }
    }

}
