using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CPClipboard.App;
using CPClipboard.Core.Model;

namespace CPClipboard.Desktop.Tests;

public class BoardInteractionTests
{
    private static BoardWindow Open(TemplateBook book, int group = 1, int maxRows = 4)
    {
        var a = new AppearanceSettings { Animation = false, MaxRows = maxRows };
        var w = new BoardWindow();
        w.ApplyEffects(a); w.Render(book, a, group); w.Show(); Dispatcher.UIThread.RunJobs();
        return w;
    }
    private static TemplateBook Book(string text = "first\nsecond")
    {
        var b = TemplateBook.CreateEmpty();
        b[1].Snippets.Add(new() { Text = text }); b[4].Snippets.Add(new() { Text = "short" });
        return b;
    }
    private static List<Button> Rows(BoardWindow w) => w.GetVisualDescendants().OfType<Button>().Where(c => c.Classes.Contains("row")).ToList();
    private static void Close(BoardWindow w) { w.AllowClose = true; w.Close(); }

    [AvaloniaFact]
    public void プレビューは対象だけに出て選択変更や無効化で消える()
    {
        var w = Open(Book());
        try
        {
            Assert.True(w.PreviewPending);
            w.ShowPreview();
            Assert.True(w.Preview?.IsVisible);
            Assert.False(w.Preview!.ShowActivated);
            Assert.False(w.Preview.IsHitTestVisible);
            Assert.True(w.Preview.Width <= 360 && w.Preview.Height <= 240);
            var texts = w.Preview.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
            Assert.Contains("first\nsecond", texts);
            Assert.Contains("LEN 12  /  LINES 2", texts);
            if (Environment.GetEnvironmentVariable("CPCB_CAPTURE_DIR") is { Length: > 0 } dir)
            {
                System.IO.Directory.CreateDirectory(dir);
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                using var board = w.CaptureRenderedFrame();
                using var preview = w.Preview.CaptureRenderedFrame();
                board!.Save(System.IO.Path.Combine(dir, "m3-board.png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
                preview!.Save(System.IO.Path.Combine(dir, "m3-preview.png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
            }
            w.KeyPressQwerty(PhysicalKey.Digit4, RawInputModifiers.None);
            Assert.False(w.Preview.IsVisible);
            w.ShowPreview();
            Assert.False(w.Preview.IsVisible); // 省略のない短い一行
            w.SwitchGroup(1);
            w.PreviewEnabled = false;
            w.ShowPreview();
            Assert.False(w.Preview.IsVisible);
            Assert.False(w.PreviewPending);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public async Task 待機中に選択が変わると古い全文を表示しない()
    {
        var w = Open(Book());
        try
        {
            Assert.False(w.Preview?.IsVisible == true);
            await Task.Delay(450);
            Dispatcher.UIThread.RunJobs();
            Assert.True(w.Preview?.IsVisible);
            w.SwitchGroup(1);
            w.SwitchGroup(4);
            await Task.Delay(450);
            Dispatcher.UIThread.RunJobs();
            Assert.False(w.Preview!.IsVisible);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 幅で省略された一行もプレビューし閉鎖で待機を破棄する()
    {
        var w = Open(Book(new string('W', 200)));
        try
        {
            w.ShowPreview();
            Assert.True(w.Preview?.IsVisible);
            w.Hide();
            Assert.False(w.Preview!.IsVisible);
            Assert.False(w.PreviewPending);
            w.ShowPreview();
            Assert.False(w.Preview.IsVisible);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void メニューは行の対象とcopyOnlyを保持する()
    {
        var book = Book(); var w = Open(book);
        try
        {
            Assert.All(Rows(w), row => Assert.Null(row.ContextMenu));
            Rows(w)[0].RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = InputElement.ContextRequestedEvent });
            Dispatcher.UIThread.RunJobs();
            var menu = w.OpenMenu!;
            Assert.True(w.ContextMenuOpen);
            var items = menu.Items.OfType<MenuItem>().ToList();
            Assert.Equal(new[] { Strings.Menu_Paste, Strings.Menu_CopyOnly, Strings.Menu_EditInSettings, Strings.Common_Settings, Strings.Update_Check, Strings.Menu_About }, items.Select(i => i.Header));
            var calls = new List<(Guid, bool)>();
            w.SnippetInvoked += (s, copy) => calls.Add((s.Id, copy));
            items[0].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            items[1].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(new[] { (book[1].Snippets[0].Id, false), (book[1].Snippets[0].Id, true) }, calls);
            (int, Guid?) target = (-1, null);
            w.EditRequested += (g, id) => target = (g, id);
            items[2].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal((1, (Guid?)book[1].Snippets[0].Id), target);
            menu.Open(Rows(w)[0]); Dispatcher.UIThread.RunJobs();
            Assert.True(w.ContextMenuOpen);
            Assert.False(w.PreviewPending);
            menu.Close(); Dispatcher.UIThread.RunJobs();
            Assert.False(w.ContextMenuOpen);
            var chrome = w.GetVisualDescendants().OfType<FrameChrome>().Single();
            Assert.Equal(3, chrome.ContextMenu!.Items.OfType<MenuItem>().Count());
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 狭いタブでも数字十個を残し広げると名前を戻す()
    {
        var book = Book();
        foreach (var g in book.Groups) g.Name = "長いグループ名";
        var w = Open(book);
        try
        {
            var a = new AppearanceSettings { Animation = false, BoardWidth = 280 };
            w.Render(book, a, 1); Dispatcher.UIThread.RunJobs();
            var tabs = w.GetVisualDescendants().OfType<ToggleButton>().Where(t => t.Classes.Contains("tab")).ToArray();
            Assert.Equal(10, tabs.Length);
            Assert.All(tabs, tab => Assert.Single(tab.GetVisualDescendants().OfType<TextBlock>()));
            a.BoardWidth = 800;
            foreach (var g in book.Groups) g.Name = "A";
            w.Render(book, a, 1); Dispatcher.UIThread.RunJobs();
            tabs = w.GetVisualDescendants().OfType<ToggleButton>().Where(t => t.Classes.Contains("tab")).ToArray();
            Assert.All(tabs, tab => Assert.Contains(tab.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "A"));
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void ホイールは一覧のあふれを優先しタブではグループを循環する()
    {
        var book = Book();
        for (int i = 0; i < 20; i++) book[1].Snippets.Add(new() { Text = "row" });
        var w = Open(book);
        try
        {
            var row = Rows(w)[0];
            var at = row.TranslatePoint(new Point(30, 10), w)!.Value;
            w.MouseMove(at); w.MouseWheel(at, new Vector(0, -1), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, w.GroupKey);
            var tab = w.GetVisualDescendants().OfType<ToggleButton>().First(t => t.Classes.Contains("tab"));
            at = tab.TranslatePoint(new Point(5, 5), w)!.Value;
            w.MouseMove(at); w.MouseWheel(at, new Vector(0, -1), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(4, w.GroupKey);
            w.WheelSwitchesGroup = false;
            w.MouseWheel(at, new Vector(0, -1), RawInputModifiers.None);
            Assert.Equal(4, w.GroupKey);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void グループが伸びると現在の左上を維持し下端だけ画面に収める()
    {
        var book = Book();
        for (int i = 0; i < 20; i++) book[4].Snippets.Add(new() { Text = "row" });
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CpcbM3", Guid.NewGuid().ToString("N"));
        BoardWindow? w = null;
        try
        {
            var store = new CPClipboard.Core.Storage.AppDataStore(new(dir, false));
            var settings = store.LoadAll().Settings.Value;
            settings.Appearance.Animation = false;
            settings.Appearance.MaxRows = 12;
            settings.Board.LastGroup = 1;
            var hub = new SettingsHub(store, settings);
            w = new BoardWindow(); w.ApplyEffects(settings.Appearance);
            var popup = new PopupController(w, new FakePlatform(), hub, () => book);
            popup.Attach(); popup.ShowFromExternal("test"); Dispatcher.UIThread.RunJobs();
            var screen = w.Screens.Primary!;
            var area = screen.WorkingArea;
            double scale = w.RenderScaling;
            w.Position = new PixelPoint(area.X + 100, area.Bottom - (int)Math.Ceiling(w.Height * scale));
            int originalX = w.Position.X;
            w.SwitchGroup(4); Dispatcher.UIThread.RunJobs();
            Assert.Equal(originalX, w.Position.X);
            Assert.True(w.Position.Y + Math.Ceiling(w.Height * scale) <= area.Bottom);
            var top = w.Position;
            w.SwitchGroup(1); Dispatcher.UIThread.RunJobs();
            Assert.Equal(top, w.Position);
        }
        finally
        {
            if (w is not null) Close(w);
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        }
    }

    [AvaloniaFact]
    public void 選択の角ブラケットが移り実行時に送信信号が一度動く()
    {
        var book = Book(); book[1].Snippets.Add(new() { Text = "next" });
        var w = Open(book);
        try
        {
            var appearance = new AppearanceSettings();
            w.ApplyEffects(appearance); w.Render(book, appearance, 1); Dispatcher.UIThread.RunJobs();
            var targets = w.GetVisualDescendants().OfType<SelectionLock>().ToArray();
            var target = Assert.Single(targets);
            Assert.True(target.Active);
            Assert.Contains(target, Rows(w)[0].GetVisualDescendants());
            w.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            Assert.Same(target, Assert.Single(w.GetVisualDescendants().OfType<SelectionLock>()));
            Assert.DoesNotContain(target, Rows(w)[0].GetVisualDescendants());
            Assert.Contains(target, Rows(w)[1].GetVisualDescendants());
            w.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.True(Rows(w)[1].GetVisualDescendants().OfType<SendSignal>().Single().IsRunning);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 演出OFFでは行の追加装飾を作らない()
    {
        var w = Open(Book());
        try { Assert.Empty(w.GetVisualDescendants().OfType<SelectionLock>()); Assert.Empty(Rows(w)[0].GetVisualDescendants().OfType<SignalPulse>()); }
        finally { Close(w); }
    }
    [AvaloniaFact]
    public void 五百件でも行生成数を制限し末尾へのキー移動と実行を保つ()
    {
        var book = TemplateBook.CreateEmpty();
        for (int i = 0; i < 500; i++) book[1].Snippets.Add(new() { Text = $"row {i}\nsecond" });
        var w = Open(book);
        try
        {
            Assert.InRange(Rows(w).Count, 4, 10);
            var scroller = w.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "RowsScroller");
            Assert.Equal(500 * 26, scroller.Extent.Height, 1);
            Guid? invoked = null;
            w.SnippetInvoked += (snippet, _) => invoked = snippet.Id;
            w.KeyPressQwerty(PhysicalKey.End, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.InRange(Rows(w).Count, 4, 10);
            Assert.Contains(Rows(w), r => r.Tag == book[1].Snippets[499] && r.Classes.Contains("selected"));
            Assert.True(scroller.Offset.Y > 12000);
            if (Environment.GetEnvironmentVariable("CPCB_CAPTURE_DIR") is { Length: > 0 } dir)
            {
                System.IO.Directory.CreateDirectory(dir);
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                using var bitmap = w.CaptureRenderedFrame();
                bitmap!.Save(System.IO.Path.Combine(dir, "virtualized-end.png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
            }
            w.ShowPreview();
            Assert.True(w.Preview?.IsVisible);
            w.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Equal(book[1].Snippets[499].Id, invoked);
            w.KeyPressQwerty(PhysicalKey.Home, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, scroller.Offset.Y);
            Assert.Contains(Rows(w), r => r.Tag == book[1].Snippets[0] && r.Classes.Contains("selected"));
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 画面外へスクロールした行の右クリックは対応する定型文を編集する()
    {
        var book = TemplateBook.CreateEmpty();
        for (int i = 0; i < 500; i++) book[1].Snippets.Add(new() { Text = $"row {i}" });
        var w = Open(book);
        try
        {
            var scroller = w.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "RowsScroller");
            scroller.Offset = new Vector(0, 2600.5);
            Dispatcher.UIThread.RunJobs();
            Assert.InRange(Rows(w).Count, 4, 10);
            Assert.Equal(2600.5, scroller.Offset.Y);
            var row = Rows(w).Single(r => ReferenceEquals(r.Tag, book[1].Snippets[101]));
            Guid? target = null;
            w.EditRequested += (_, id) => target = id;
            row.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = InputElement.ContextRequestedEvent });
            Dispatcher.UIThread.RunJobs();
            w.OpenMenu!.Items.OfType<MenuItem>().Single(item => Equals(item.Header, Strings.Menu_EditInSettings))
                .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal(book[1].Snippets[101].Id, target);
            w.OpenMenu?.Close();
            w.SwitchGroup(4);
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(Rows(w));
            w.SwitchGroup(1);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, scroller.Offset.Y);
            Assert.InRange(Rows(w).Count, 4, 10);
        }
        finally { Close(w); }
    }

    [AvaloniaFact]
    public void 余白用に保持した画面外の行にはプレビューを表示しない()
    {
        var book = Book();
        for (int i = 0; i < 100; i++) book[1].Snippets.Add(new() { Text = $"row {i}" });
        var w = Open(book);
        try
        {
            w.ShowPreview();
            Assert.True(w.Preview?.IsVisible);
            var scroller = w.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "RowsScroller");
            scroller.Offset = new Vector(0, 26);
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(Rows(w), r => ReferenceEquals(r.Tag, book[1].Snippets[0]));
            Assert.False(w.Preview!.IsVisible);
            Assert.False(w.PreviewPending);
            w.ShowPreview();
            Assert.False(w.Preview.IsVisible);
            scroller.Offset = default;
            Dispatcher.UIThread.RunJobs();
            w.ShowPreview();
            Assert.True(w.Preview.IsVisible);
        }
        finally { Close(w); }
    }

}
