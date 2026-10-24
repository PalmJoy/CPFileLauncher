using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CPClipboard.App;
using CPClipboard.Core.Model;
using CPClipboard.Core.Storage;

namespace CPClipboard.Desktop.Tests;

public sealed class TemplatesEditorTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CpcbTemplatesEditor", Guid.NewGuid().ToString("N"));
    private readonly AppDataStore _store;
    private readonly TemplatesHub _hub;
    private readonly FakePlatform _platform = new();
    private TemplatesWindow? _window;
    public TemplatesEditorTests()
    {
        _store = new(new(_dir, false));
        var loaded = _store.LoadAll();
        loaded.Templates.Value[1].Name = "Greetings";
        foreach (var text in new[] { "first", "second", "third" }) loaded.Templates.Value[1].Snippets.Add(new() { Text = text });
        _hub = new(_store, loaded.Templates.Value);
    }
    public void Dispose()
    {
        _window?.Close();
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }
    private TemplatesEditor Open(int group = 1, Guid? id = null)
    {
        var settings = new SettingsHub(_store, _store.LoadAll().Settings.Value);
        _window = new(_hub, _platform.Clipboard);
        _window.SetTemplateTarget(new(group, id)); _window.Show(); Dispatcher.UIThread.RunJobs();
        return _window.TemplatesEditor;
    }
    private static T Control<T>(TemplatesEditor editor, string name) where T : Control =>
        editor.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
    private static void Click(TemplatesEditor editor, string name)
    {
        var button = Control<Button>(editor, name);
        Assert.True(button.IsEnabled); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
    }
    private static ListBox List(TemplatesEditor editor) => Control<ListBox>(editor, "SnippetList");
    private static TextBox Text(TemplatesEditor editor) => Control<TextBox>(editor, "SnippetText");

    [AvaloniaFact]
    public void 対象指定で編集ウィンドウを開き本文と名前が即反映される()
    {
        Guid id = _hub.Current[1].Snippets[1].Id;
        var editor = Open(1, id);
        Assert.Equal(id, editor.SelectedSnippet);
        Assert.Equal("second", Text(editor).Text);
        Assert.Empty(_window!.GetVisualDescendants().OfType<TabControl>());
        Text(editor).Text = "edited\r\ntext";
        Control<TextBox>(editor, "GroupName").Text = "Renamed";
        Assert.Equal("edited\ntext", _hub.Current[1].Snippets[1].Text);
        Assert.Equal("Renamed", _hub.Current[1].Name);
        Assert.False(_hub.CanUndo);
        Assert.Contains("11", Control<TextBlock>(editor, "SnippetCount").Text);
        _window.Close();
        Assert.Equal("edited\ntext", new AppDataStore(_store.Paths).LoadAll().Templates.Value[1].Snippets[1].Text);
    }

    [AvaloniaFact]
    public void 追加直後は空欄を保持し選択移動で除き盤面には表示しない()
    {
        var editor = Open();
        Click(editor, "AddSnippet");
        Guid id = editor.SelectedSnippet!.Value;
        Assert.Equal("", _hub.Current[1].Snippets.Single(s => s.Id == id).Text);
        Assert.True(Text(editor).IsFocused);
        Assert.Equal(3, _hub.DisplayBook()[1].Snippets.Count);
        Assert.True(_hub.CanUndo);
        List(editor).SelectedIndex = 0;
        Assert.DoesNotContain(_hub.Current[1].Snippets, s => s.Id == id);
        Assert.True(_hub.CanUndo);
    }

    [AvaloniaFact]
    public void 複製削除並べ替えと一段階Undoを操作できる()
    {
        var editor = Open();
        Click(editor, "DuplicateSnippet");
        Assert.Equal(new[] { "first", "first", "second", "third" }, _hub.Current[1].Snippets.Select(s => s.Text));
        Click(editor, "MoveDown");
        Assert.Equal("first", _hub.Current[1].Snippets[2].Text);
        Click(editor, "DeleteSnippet");
        Assert.Equal(new[] { "first", "second", "third" }, _hub.Current[1].Snippets.Select(s => s.Text));
        Click(editor, "UndoSnippets");
        Assert.Equal(new[] { "first", "second", "first", "third" }, _hub.Current[1].Snippets.Select(s => s.Text));
        Assert.Equal(2, List(editor).SelectedIndex);
        Assert.False(Control<Button>(editor, "UndoSnippets").IsEnabled);
    }

    [AvaloniaFact]
    public void 本文編集後は一覧Undoを無効にし別IDへTextBoxUndoを持ち越さない()
    {
        var editor = Open();
        Click(editor, "DuplicateSnippet");
        Text(editor).Focus();
        _window!.KeyTextInput("x"); Dispatcher.UIThread.RunJobs();
        Assert.False(_hub.CanUndo);
        Assert.True(Text(editor).CanUndo);
        Guid before = editor.SelectedSnippet!.Value;
        List(editor).SelectedIndex = 0; Dispatcher.UIThread.RunJobs();
        Assert.False(Text(editor).CanUndo);
        Text(editor).Undo();
        Assert.Equal("first", Text(editor).Text);
        Assert.Equal("xfirst", _hub.Current[1].Snippets.Single(s => s.Id == before).Text);
    }

    [AvaloniaFact]
    public async Task クリップボードの空と上限拒否はUndo履歴を壊さない()
    {
        var editor = Open();
        Click(editor, "DuplicateSnippet");
        Guid selected = editor.SelectedSnippet!.Value;
        var clipboard = (FakeClipboard)_platform.Clipboard;
        await clipboard.SetTextAsync(" \n\t", false);
        await editor.RefreshClipboardAsync();
        await editor.AddClipboardAsync();
        Assert.Equal(Strings.Settings_Snippets_ClipboardEmpty, Control<TextBlock>(editor, "SnippetNote").Text);
        await clipboard.SetTextAsync(new string('a', 10001), false);
        await editor.AddClipboardAsync();
        Assert.Equal(Strings.Settings_Snippets_TooLong, Control<TextBlock>(editor, "SnippetNote").Text);
        Assert.Equal(selected, editor.SelectedSnippet);
        Assert.Equal(4, _hub.Current[1].Snippets.Count);
        Assert.True(_hub.CanUndo);
        await clipboard.SetTextAsync("clipboard\r\ntext", false);
        await editor.AddClipboardAsync();
        Assert.Equal("clipboard\ntext", Text(editor).Text);
        Assert.Equal(5, _hub.Current[1].Snippets.Count);
    }

    [AvaloniaFact]
    public async Task 空にする確認とUndoでグループと選択を復元する()
    {
        var editor = Open(); Guid id = editor.SelectedSnippet!.Value;
        _window!.Confirm = (_, _, _) => Task.FromResult(false);
        await editor.ClearGroupAsync(); Assert.Equal(3, _hub.Current[1].Snippets.Count);
        _window.Confirm = (_, _, _) => Task.FromResult(true);
        await editor.ClearGroupAsync(); Assert.Empty(_hub.Current[1].Snippets);
        editor.SelectTarget(new(7, null));
        Click(editor, "UndoSnippets");
        Assert.Equal(1, editor.SelectedGroup); Assert.Equal(id, editor.SelectedSnippet);
        editor.SelectTarget(new(7, null)); Click(editor, "AddSnippet");
        editor.SelectTarget(new(4, null)); Click(editor, "UndoSnippets");
        Assert.Equal(7, editor.SelectedGroup); Assert.Null(editor.SelectedSnippet);
    }

    [AvaloniaFact]
    public void ドロップで並べ替え移動複製でき外部や満杯は拒否する()
    {
        var editor = Open(); Guid id = editor.SelectedSnippet!.Value;
        var drag = new TemplatesEditor.SnippetDrag(editor.DragIdentity, id);
        editor.DropSnippet(drag, null, _hub.Current[1].Snippets[2].Id, true, false);
        Assert.Equal(new[] { "second", "third", "first" }, _hub.Current[1].Snippets.Select(s => s.Text));
        editor.DropSnippet(drag, 4, null, false, true);
        Assert.Equal("first", Assert.Single(_hub.Current[4].Snippets).Text);
        Assert.Equal(3, _hub.Current[1].Snippets.Count);
        editor.DropSnippet(drag, 5, null, false, false);
        Assert.Equal(id, Assert.Single(_hub.Current[5].Snippets).Id);
        Assert.Equal(2, _hub.Current[1].Snippets.Count);
        Assert.False(editor.CanDrop(new(Guid.NewGuid(), id), 8));
        for (int i = 0; i < 500; i++) _hub.Current[8].Snippets.Add(new() { Text = "full" });
        Assert.False(editor.CanDrop(new(editor.DragIdentity, id), 8));
    }

    [AvaloniaFact]
    public void 編集を閉じると空を除きCtrlTabは本文から一覧へ出る()
    {
        var editor = Open();
        Click(editor, "AddSnippet");
        _window!.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control); Dispatcher.UIThread.RunJobs();
        Assert.True(List(editor).IsKeyboardFocusWithin);
        _window.Close();
        Assert.Equal(3, _hub.Current[1].Snippets.Count);
    }

    [AvaloniaFact]
    public async Task 古いクリップボード追加は選択変更後や閉鎖後に反映しない()
    {
        var editor = Open();
        var clipboard = (FakeClipboard)_platform.Clipboard;
        var pending = new TaskCompletionSource<string?>();
        clipboard.ReadOverride = () => pending.Task;
        var add = editor.AddClipboardAsync();
        editor.SelectTarget(new(4, null));
        pending.SetResult("late"); await add;
        Assert.Empty(_hub.Current[4].Snippets);
        var closed = new TaskCompletionSource<string?>();
        clipboard.ReadOverride = () => closed.Task;
        add = editor.AddClipboardAsync();
        _window!.Close(); closed.SetResult("late again"); await add;
        Assert.Empty(_hub.Current[4].Snippets);
    }

    [AvaloniaFact]
    public void 上限では追加を止め本文の長さを制限する()
    {
        var editor = Open();
        Text(editor).Text = new string('a', 10001);
        Assert.Equal(10000, _hub.Current[1].Snippets[0].Text.Length);
        Assert.Equal(10000, Text(editor).Text!.Length);
        Assert.Equal(Strings.Settings_Snippets_TooLong, Control<TextBlock>(editor, "SnippetNote").Text);
        for (int i = _hub.Current[1].Snippets.Count; i < 500; i++) _hub.Edit(b => TemplateEditing.AddText(b, 1, null, "row"), null, true, 1);
        Assert.False(Control<Button>(editor, "AddSnippet").IsEnabled);
        Assert.False(Control<Button>(editor, "DuplicateSnippet").IsEnabled);
        Assert.True(Control<Button>(editor, "DeleteSnippet").IsEnabled);
    }

    [AvaloniaFact]
    public void 新しい版のデータは編集できるがディスクへ保存しない()
    {
        var future = _hub.Current.Clone(); future.SchemaVersion = TemplateBook.CurrentSchemaVersion + 1;
        string original = System.Text.Json.JsonSerializer.Serialize(future, JsonDefaults.Options);
        File.WriteAllText(_store.Paths.TemplatesFile, original);
        var store = new AppDataStore(_store.Paths);
        var loaded = store.LoadAll();
        var hub = new TemplatesHub(store, loaded.Templates.Value);
        var settings = new SettingsHub(store, loaded.Settings.Value);
        _window = new(hub, _platform.Clipboard);
        _window.SetTemplateTarget(new(1, null)); _window.Show(); Dispatcher.UIThread.RunJobs();
        var editor = _window.TemplatesEditor;
        Assert.Contains(editor.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == Strings.Settings_Snippets_ReadOnlyBanner && t.IsEffectivelyVisible);
        Click(editor, "AddSnippet");
        Text(editor).Text = "session only";
        Assert.Equal("session only", hub.Current[1].Snippets[0].Text);
        _window.Close();
        Assert.Equal(original, File.ReadAllText(_store.Paths.TemplatesFile));
    }

    [AvaloniaFact]
    public void 設定と編集は独立し編集の再表示は同じ窓を使い閉じると作り直す()
    {
        var settings = new SettingsHub(_store, _store.LoadAll().Settings.Value);
        var app = new CPClipboard.App.App();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        void Set(string name, object value) => typeof(CPClipboard.App.App).GetField(name, flags)!.SetValue(app, value);
        TemplatesWindow? Current() => (TemplatesWindow?)typeof(CPClipboard.App.App).GetField("_templatesWindow", flags)!.GetValue(app);
        void OpenEditor(TemplateEditTarget? target) => typeof(CPClipboard.App.App).GetMethod("OpenEditor", flags)!.Invoke(app, [target]);
        Set("_platform", _platform); Set("_templates", _hub);
        var board = new BoardWindow(); board.Render(_hub.Current, settings.Current.Appearance, 4); Set("_board", board);
        using var cleanup = new WindowCleanup(() => { Current()?.Close(); board.AllowClose = true; board.Close(); });
        OpenEditor(null);
        var first = Current()!; Assert.Equal(4, first.TemplatesEditor.SelectedGroup);
        OpenEditor(new(1, _hub.Current[1].Snippets[1].Id));
        Assert.Same(first, Current()); Assert.Equal(_hub.Current[1].Snippets[1].Id, first.TemplatesEditor.SelectedSnippet);
        OpenEditor(null); Assert.Equal(1, Current()!.TemplatesEditor.SelectedGroup);
        var window = new SettingsWindow(new(settings, _platform, _store.Paths, () => null, _ => { }, () => { }));
        try
        {
            window.Show(); Dispatcher.UIThread.RunJobs();
            Assert.Equal(7, window.GetVisualDescendants().OfType<TabControl>().Single().Items.Count);
            Assert.Empty(window.GetVisualDescendants().OfType<TemplatesEditor>());
            Text(first.TemplatesEditor).Text = "saved independently";
            window.Close(); Assert.True(first.IsVisible);
            first.Close(); Assert.Null(Current());
            Assert.Contains(new AppDataStore(_store.Paths).LoadAll().Templates.Value[1].Snippets, line => line.Text == "saved independently");
            OpenEditor(null); Assert.NotSame(first, Current());
        }
        finally { window.Close(); }
    }

    private sealed class WindowCleanup(Action cleanup) : IDisposable
    {
        public void Dispose() => cleanup();
    }

    [AvaloniaFact]
    public void 編集一覧は盤面と同じ行を使いグループ件数を表示せず英字は本文へ入力できる()
    {
        var editor = Open();
        editor.ApplyAppearance(new AppearanceSettings { FontSize = 17 });
        Dispatcher.UIThread.RunJobs();
        var groups = Control<ListBox>(editor, "SnippetGroups").Items.OfType<ListBoxItem>().ToArray();
        Assert.Equal("1  Greetings", ((TextBlock)groups[1].Content!).Text);
        var row = List(editor).Items.OfType<ListBoxItem>().First();
        var content = Assert.IsType<SnippetRowContent>(row.Content);
        Assert.Equal("a", content.Shortcut.Text); Assert.Equal(17, content.Text.FontSize); Assert.Equal(34, row.Height);
        Text(editor).Focus(); _window!.KeyTextInput("az"); Dispatcher.UIThread.RunJobs();
        Assert.Contains("az", Text(editor).Text);
        Assert.Equal(1, editor.SelectedGroup); Assert.Equal(3, _hub.Current[1].Snippets.Count);
    }

    [AvaloniaTheory]
    [InlineData("en")]
    [InlineData("ja")]
    public void Capture_templates_editor(string culture)
    {
        var capture = Environment.GetEnvironmentVariable("CPCB_CAPTURE_DIR");
        if (string.IsNullOrEmpty(capture)) return;
        var saved = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new(culture);
        try
        {
            var editor = Open();
            var text = Text(editor);
            text.Text = culture == "ja" ? "いつもお世話になっております。\n株式会社〇〇の△△です。" : "Thank you for your message.\nBest regards,";
            Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Directory.CreateDirectory(capture);
            using var frame = _window!.CaptureRenderedFrame();
            frame!.Save(Path.Combine(capture, "m5-snippets-" + culture + ".png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
        }
        finally { System.Globalization.CultureInfo.CurrentUICulture = saved; }
    }
}
