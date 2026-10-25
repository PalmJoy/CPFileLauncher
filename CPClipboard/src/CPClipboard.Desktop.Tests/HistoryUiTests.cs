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
using CPClipboard.Core.Paste;
using CPClipboard.Core.Storage;
using CPClipboard.Platform;
namespace CPClipboard.Desktop.Tests;

public sealed class HistoryUiTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "HistoryUi", Guid.NewGuid().ToString());
    private readonly SettingsHub _settings;
    private readonly DataPaths _paths;
    public HistoryUiTests()
    {
        _paths = new(_dir, false); var store = new AppDataStore(_paths);
        _settings = new(store, store.LoadAll().Settings.Value);
    }
    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }
    private sealed class Clipboard : IClipboardService
    {
        public long ChangeToken { get; set; } = 1;
        public string? Text = "first";
        public TaskCompletionSource<string?>? Pending;
        public Task<string?> GetTextAsync() => Pending?.Task ?? Task.FromResult(Text);
        public Task<ClipboardSnapshot?> SnapshotAsync() => Task.FromResult<ClipboardSnapshot?>(null);
        public Task<long?> SetTextAsync(string text, bool transient, long? expectedToken = null) => Task.FromResult<long?>(++ChangeToken);
        public Task<bool> RestoreAsync(ClipboardSnapshot s, long token) => Task.FromResult(false);
    }
    [AvaloniaFact]
    public async Task MonitorDoesNotReimportAfterClearOrAcceptRacedReads()
    {
        var clipboard = new Clipboard();
        using var hub = new HistoryHub(_paths, _settings, clipboard);
        await hub.PollAsync(); Assert.Single(hub.Book.Entries);
        hub.Clear(); await hub.PollAsync(); Assert.Empty(hub.Book.Entries);
        clipboard.ChangeToken++; clipboard.Pending = new();
        var pending = hub.PollAsync(); hub.Clear(); clipboard.Pending.SetResult("stale"); await pending;
        Assert.Empty(hub.Book.Entries);
        clipboard.Pending = new(); clipboard.ChangeToken++;
        pending = hub.PollAsync(); clipboard.ChangeToken++; clipboard.Pending.SetResult("raced"); await pending;
        Assert.Empty(hub.Book.Entries);
        clipboard.Pending = null; clipboard.Text = "latest"; await hub.PollAsync();
        Assert.Equal("latest", Assert.Single(hub.Book.Entries).Text);
        hub.Dispose(); using var restored = new HistoryHub(_paths, _settings, clipboard);
        Assert.Equal("latest", Assert.Single(restored.Book.Entries).Text);
    }
    [AvaloniaFact]
    public void HistoryFilteringAndShortcutsDoNotStealSearchTyping()
    {
        using var history = new HistoryHub(_paths, _settings, new Clipboard());
        history.Book.Add("alpha", DateTimeOffset.UtcNow, _settings.Current.History);
        history.Book.Add("bravo", DateTimeOffset.UtcNow.AddSeconds(1), _settings.Current.History);
        var window = new HistoryWindow(history, _settings); string? chosen = null; bool copy = false;
        window.Chosen += (text, only) => { chosen = text; copy = only; };
        window.Show(); Dispatcher.UIThread.RunJobs();
        try
        {
            Assert.Equal(2, window.HistoryList.Items.Count);
            window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None);
            Assert.Equal("bravo", chosen);
            chosen = null; window.Filter.Focus(); window.Filter.Text = "ALP"; Dispatcher.UIThread.RunJobs();
            Assert.Single(window.HistoryList.Items);
            window.KeyPressQwerty(PhysicalKey.B, RawInputModifiers.None); Assert.Null(chosen);
            window.Filter.Text = "alpha"; window.HistoryList.Focus(); Dispatcher.UIThread.RunJobs();
            window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Shift);
            Assert.Equal("alpha", chosen); Assert.True(copy);
            window.Filter.Text = "missing"; Dispatcher.UIThread.RunJobs(); Assert.Empty(window.HistoryList.Items);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData("ja")]
    [InlineData("en")]
    public void HistoryWindowUsesBoardSurfaceAndCanRender(string culture)
    {
        var saved = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new(culture);
        using var history = new HistoryHub(_paths, _settings, new Clipboard());
        history.Book.Add("こんにちは、", DateTimeOffset.UtcNow, _settings.Current.History);
        var window = new HistoryWindow(history, _settings); window.Show(); Dispatcher.UIThread.RunJobs();
        try
        {
            Assert.Single(window.GetVisualDescendants().OfType<BackdropLayer>());
            Assert.Single(window.GetVisualDescendants().OfType<GridLayer>());
            Assert.Equal("a", ((SnippetRowContent)((ListBoxItem)window.HistoryList.Items[0]!).Content!).Shortcut.Text);
            Assert.True(window.Filter.Bounds.Width > 200);
            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            if (Environment.GetEnvironmentVariable("CPTOOLS_CAPTURE_DIR") is { } dir)
            { Directory.CreateDirectory(dir); frame.Save(Path.Combine(dir, $"history-{culture}.png")); }
        }
        finally { window.Close(); System.Globalization.CultureInfo.CurrentUICulture = saved; }
    }
    [AvaloniaFact]
    public void HistorySettingsUseFreeInputsClampAndClearAction()
    {
        bool cleared = false;
        var window = new SettingsWindow(new(_settings, new FakePlatform(), _paths, () => null, _ => {}, () => {}, ClearHistory: () => cleared = true));
        window.Confirm = (_, _, _) => Task.FromResult(true); window.Show();
        var tabs = window.GetVisualDescendants().OfType<TabControl>().Single();
        tabs.SelectedItem = tabs.Items.OfType<TabItem>().Single(t => (t.Header as TextBlock)?.Text == Strings.Settings_Tab_History);
        Dispatcher.UIThread.RunJobs();
        try
        {
            var numbers = window.GetVisualDescendants().OfType<NumericUpDown>().Where(n => n.IsEffectivelyVisible).ToArray();
            Assert.Equal(2, numbers.Length);
            Assert.False(numbers[0].IsEnabled);
            var auto = window.GetVisualDescendants().OfType<ToggleSwitch>().Single(t => t.IsEffectivelyVisible);
            auto.IsChecked = true; Assert.True(numbers[0].IsEnabled);
            numbers[0].Value = 17; numbers[1].Value = 2000;
            Assert.Equal(17, _settings.Current.History.AutoClearMinutes); Assert.Equal(1000, _settings.Current.History.MaxItems);
            var clear = window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == Strings.History_Clear);
            clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs(); Assert.True(cleared);
        }
        finally { window.Close(); }
    }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 25, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    [AvaloniaFact]
    public async Task RetentionExpiresWithoutANewCopyAndPersistsTheDeletion()
    {
        var clock = new Clock(); var clipboard = new Clipboard();
        _settings.Update(s => { s.History.AutoClear = true; s.History.AutoClearMinutes = 2; }, SettingsChange.History);
        using var history = new HistoryHub(_paths, _settings, clipboard, clock);
        await history.PollAsync(); Assert.Single(history.Book.Entries);
        clock.Now = clock.Now.AddMinutes(2); await history.PollAsync();
        Assert.Empty(history.Book.Entries);
        using var reloaded = new HistoryHub(_paths, _settings, clipboard, clock); Assert.Empty(reloaded.Book.Entries);
        clock.Now = clock.Now.AddMinutes(1); await history.PollAsync(); Assert.Empty(history.Book.Entries);
    }
    [AvaloniaFact]
    public void LowerTriggerSectionDoesNotChangeUpperSectionAndRecordingCancelsOnTabChange()
    {
        var platform = new FakePlatform { HookRunning = true };
        var window = new SettingsWindow(new(_settings, platform, _paths, () => null, _ => {}, () => {}));
        window.Show();
        var tabs = window.GetVisualDescendants().OfType<TabControl>().Single();
        tabs.SelectedItem = tabs.Items.OfType<TabItem>().Single(t => (t.Header as TextBlock)?.Text == Strings.Settings_Tab_Triggers);
        Dispatcher.UIThread.RunJobs();
        try
        {
            var lists = window.GetVisualDescendants().OfType<MouseTriggerList>().ToArray(); Assert.Equal(2, lists.Length);
            var add = lists[1].GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == Strings.Common_Add);
            add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs();
            Assert.Single(_settings.Current.Triggers.Mouse); Assert.Single(_settings.Current.HistoryTriggers.Mouse);
            var recorders = window.GetVisualDescendants().OfType<HotkeyRecorder>().ToArray(); Assert.Equal(2, recorders.Length);
            recorders[1].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            platform.Capture!("H", CPClipboard.Core.Model.KeyModifiers.Ctrl); Dispatcher.UIThread.RunJobs();
            Assert.Equal("H", _settings.Current.HistoryTriggers.Hotkey.Key);
            Assert.Equal("V", _settings.Current.Triggers.Hotkey.Key);
            recorders[1].RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Assert.NotNull(platform.Capture);
            tabs.SelectedIndex = 0; Dispatcher.UIThread.RunJobs(); Assert.Null(platform.Capture);
            Assert.False(recorders[1].IsRecording);
            if (Environment.GetEnvironmentVariable("CPTOOLS_CAPTURE_DIR") is { } dir)
            {
                tabs.SelectedItem = tabs.Items.OfType<TabItem>().Single(t => (t.Header as TextBlock)?.Text == Strings.Settings_Tab_History);
                Dispatcher.UIThread.RunJobs(); using var frame = window.CaptureRenderedFrame();
                frame!.Save(Path.Combine(dir, "history-settings.png"));
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task HistorySelectionClosesWindowAndPastesBackIntoCapturedApp()
    {
        var platform = new FakePlatform { HookRunning = true, Foreground = 42 };
        using var history = new HistoryHub(_paths, _settings, platform.Clipboard);
        history.Book.Add("history paste", DateTimeOffset.UtcNow, _settings.Current.History);
        var app = new CPClipboard.App.App();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        void Set(string name, object value) => typeof(CPClipboard.App.App).GetField(name, flags)!.SetValue(app, value);
        HistoryWindow? Window() => (HistoryWindow?)typeof(CPClipboard.App.App).GetField("_historyWindow", flags)!.GetValue(app);
        var paste = new PasteController(platform, () => Window() is null);
        Set("_hub", _settings); Set("_platform", platform); Set("_history", history); Set("_historyPaste", paste);
        typeof(CPClipboard.App.App).GetMethod("OpenHistory", flags)!.Invoke(app, [false]);
        Dispatcher.UIThread.RunJobs();
        try
        {
            Assert.NotNull(Window());
            Window()!.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None);
            for (int i = 0; i < 40 && ((FakeKeys)platform.KeySender).Sent == 0; i++)
            { await Task.Delay(25); Dispatcher.UIThread.RunJobs(); }
            Assert.Null(Window());
            Assert.Equal("history paste", ((FakeClipboard)platform.Clipboard).Text);
            Assert.Equal(1, ((FakeKeys)platform.KeySender).Sent);
            Assert.Contains((nint)42, platform.RestoredTargets);
        }
        finally { Window()?.Close(); await paste.StopAsync(); }
    }

}
