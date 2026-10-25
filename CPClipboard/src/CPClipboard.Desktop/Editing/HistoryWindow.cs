using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using CPClipboard.Core.Model;
using CPClipboard.Core.Popup;
using CPClipboard.Platform;
using Avalonia.Threading;
using KeyModifiers = Avalonia.Input.KeyModifiers;

namespace CPClipboard.App;

internal sealed class HistoryWindow : ChromeWindow
{
    private readonly HistoryHub _history;
    private readonly SettingsHub _settings;
    private readonly IWindowService? _windowService;
    private readonly MouseLeaveTracker _leave = new();
    private readonly DispatcherTimer _leaveTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private bool _pointerHeld;
    private readonly ListBox _list = new() { Name = "HistoryList", Focusable = true };
    private readonly TextBox _filter = new() { Name = "HistoryFilter", PlaceholderText = Strings.History_Filter };
    private readonly TextBlock _empty = Themed.Note(new TextBlock { Text = Strings.History_Empty, Margin = new Thickness(12) });
    private IReadOnlyList<HistoryEntry> _visible = [];
    private double _fontSize = 13;
    public event Action<string, bool>? Chosen;
    internal ListBox HistoryList => _list;
    internal TextBox Filter => _filter;

    public HistoryWindow(HistoryHub history, SettingsHub settings, Func<Bitmap?>? background = null, IWindowService? windowService = null)
    {
        _history = history;
        _settings = settings;
        _windowService = windowService;
        Title = AppInfo.TitlePrefix + Strings.History_Title;
        TagCode = "HIST";
        BodyWidth = 620; BodyHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = OperatingSystem.IsWindows();
        TemplatesEditor.StyleList(_list);
        var layout = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 12 };
        var rows = new Grid { Children = { _list, _empty } };
        layout.Children.Add(rows);
        var search = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 12 };
        search.Children.Add(new TextBlock { Text = Strings.History_Search, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(_filter, 1); search.Children.Add(_filter);
        Grid.SetRow(search, 1); layout.Children.Add(search);
        Body = layout;
        BoardWindowSurface.Attach(this, settings, background, a => { _fontSize = a.FontSize; Refresh(); });
        _filter.TextChanged += (_, _) => Refresh();
        _list.AddHandler(PointerReleasedEvent, (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left && _list.SelectedItem is ListBoxItem row && row.IsPointerOver)
                Choose(_list.SelectedIndex, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
        }, RoutingStrategies.Bubble);
        AddHandler(KeyDownEvent, OnNavigation, RoutingStrategies.Tunnel);
        history.Changed += Refresh;
        Closed += (_, _) => history.Changed -= Refresh;
        Opened += (_, _) => { _list.Focus(); UpdateDistanceMonitoring(); };
        _leaveTimer.Tick += (_, _) => CheckMouseDistance();
        AddHandler(PointerPressedEvent, (_, _) => _pointerHeld = true, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, (_, _) => _pointerHeld = false, RoutingStrategies.Tunnel, handledEventsToo: true);
        PointerCaptureLost += (_, _) => _pointerHeld = false;
        void OnSettingsChanged(SettingsChange change)
        {
            if (change.HasFlag(SettingsChange.Popup)) UpdateDistanceMonitoring();
        }
        settings.Changed += OnSettingsChanged;
        Closed += (_, _) => { _leaveTimer.Stop(); settings.Changed -= OnSettingsChanged; };
        Refresh();
    }
    private void UpdateDistanceMonitoring()
    {
        _leave.Reset();
        if (IsVisible && _windowService is not null && _settings.Current.Popup.CloseOnMouseLeave) _leaveTimer.Start();
        else _leaveTimer.Stop();
    }
    internal void CheckMouseDistance()
    {
        if (!IsVisible || !IsHitTestVisible || _windowService is null || _pointerHeld || !_settings.Current.Popup.CloseOnMouseLeave) return;
        double scaling = _windowService.UsesLogicalScreenCoordinates ? 1.0 : RenderScaling;
        var bounds = new CPClipboard.Core.Input.ScreenRect(Position.X, Position.Y,
            (int)Math.Ceiling(Bounds.Width * scaling), (int)Math.Ceiling(Bounds.Height * scaling));
        if (_leave.Update(_windowService.GetCursorPosition(), bounds, _settings.Current.Popup.MouseLeaveDistance)) Close();
    }

    private void Refresh()
    {
        var selected = (_list.SelectedItem as ListBoxItem)?.Tag as HistoryEntry;
        _visible = _history.Book.Filter(_filter.Text);
        _list.Items.Clear();
        for (int i = 0; i < _visible.Count; i++)
        {
            var content = new SnippetRowContent(); content.Update(_visible[i].Text, i, _fontSize);
            _list.Items.Add(new ListBoxItem { Tag = _visible[i], Padding = new Thickness(0), Height = _fontSize * 2, Content = content });
        }
        _empty.IsVisible = _visible.Count == 0;
        _list.SelectedIndex = _visible.Count == 0 ? -1 : Math.Max(0, _visible.ToList().FindIndex(e => e.Id == selected?.Id));
    }
    private void OnNavigation(object? sender, KeyEventArgs e)
    {
        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0 && e.Key == Key.F)
        { _filter.Focus(); e.Handled = true; return; }
        if (_filter.IsKeyboardFocusWithin)
        {
            if (e.Key is Key.Down or Key.Enter) { _list.Focus(); e.Handled = true; }
            return;
        }
        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta | KeyModifiers.Alt)) != 0) return;
        if (e.Key is >= Key.A and <= Key.Z)
        { Choose((int)e.Key - (int)Key.A, e.KeyModifiers.HasFlag(KeyModifiers.Shift)); e.Handled = true; }
        else if (e.Key == Key.Enter)
        { Choose(_list.SelectedIndex, e.KeyModifiers.HasFlag(KeyModifiers.Shift)); e.Handled = true; }
    }
    private void Choose(int index, bool copyOnly)
    {
        if (index < 0 || index >= _visible.Count) return;
        Chosen?.Invoke(_visible[index].Text, copyOnly);
    }
}
