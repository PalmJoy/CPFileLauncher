using KeyModifiers = Avalonia.Input.KeyModifiers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CPClipboard.Core.Model;
using CPClipboard.Platform;

namespace CPClipboard.App;

/// <summary>定型文の編集画面。変更・保存・一覧 Undo は TemplatesHub にまとめる。</summary>
internal sealed partial class TemplatesEditor : Grid, IDisposable
{
    private readonly TemplatesHub _hub;
    private readonly IClipboardService _clipboard;
    private readonly Func<string, string, string, Task<bool>> _confirm;
    private readonly ListBox _groups = new() { Name = "SnippetGroups" };
    private readonly ListBox _snippets = new() { Name = "SnippetList" };
    private readonly TextBox _name = new() { Name = "GroupName", MaxLength = TemplateBook.MaxGroupNameLength };
    private readonly TextBox _text = new()
    {
        Name = "SnippetText", AcceptsReturn = true, AcceptsTab = true,
        MaxLength = TemplateBook.MaxSnippetLength, TextWrapping = TextWrapping.Wrap,
        VerticalContentAlignment = VerticalAlignment.Top,
    };
    private readonly TextBlock _count = new() { Name = "SnippetCount", HorizontalAlignment = HorizontalAlignment.Right, FontSize = 11 };
    private readonly TextBlock _note = new() { Name = "SnippetNote", TextWrapping = TextWrapping.Wrap, FontSize = 11, [!TextBlock.ForegroundProperty] = Themed.Res("FlWarning") };
    private readonly Button _add, _fromClipboard, _duplicate, _delete, _up, _down, _move, _undo, _clear;
    private readonly DispatcherTimer _clipboardTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private int _group = 1;
    private Guid? _selected;
    private bool _syncing, _editing, _active, _disposed, _reading;
    private int _generation;
    private string? _clipboardText;

    public TemplatesEditor(TemplatesHub hub, IClipboardService clipboard, Func<string, string, string, Task<bool>> confirm)
    {
        _hub = hub; _clipboard = clipboard; _confirm = confirm;
        StyleList(_groups); StyleList(_snippets);
        foreach (var input in new[] { _name, _text })
        {
            input[!TextBox.BackgroundProperty] = Themed.Res("FlBoardBackground");
            input[!TextBox.ForegroundProperty] = Themed.Res("FlRowText");
            input[!TextBox.BorderBrushProperty] = Themed.Res("FlTabOutline");
        }
        Margin = new Thickness(12); RowDefinitions = new("Auto,*"); ColumnDefinitions = new("180,*"); ColumnSpacing = 12; RowSpacing = 8;
        var banner = new TextBlock { Text = Strings.Settings_Snippets_ReadOnlyBanner, IsVisible = hub.IsReadOnly, TextWrapping = TextWrapping.Wrap, [!TextBlock.ForegroundProperty] = Themed.Res("FlWarning") };
        SetColumnSpan(banner, 2); Children.Add(banner);
        var left = new Grid { RowDefinitions = new("*,Auto,Auto,Auto"), RowSpacing = 6 };
        _clear = ActionButton("ClearGroup", Strings.Settings_Snippets_ClearGroup, async () => await ClearGroupAsync());
        left.Children.Add(_groups);
        var label = new TextBlock { Text = Strings.Settings_Snippets_GroupName, FontSize = 12 };
        AddAt(left, label, 1); AddAt(left, _name, 2); AddAt(left, _clear, 3);
        AddAt(this, left, 1);
        var right = new Grid { RowDefinitions = new("*,Auto,Auto,180,Auto"), RowSpacing = 6 };
        AddAt(this, right, 1, 1);
        right.Children.Add(_snippets);
        _add = ActionButton("AddSnippet", Strings.Common_Add, Add);
        _fromClipboard = ActionButton("AddClipboard", Strings.Settings_Snippets_AddClipboard, async () => await AddClipboardAsync());
        _duplicate = ActionButton("DuplicateSnippet", Strings.Menu_Duplicate, () => Apply(b => TemplateEditing.Duplicate(b, _selected!.Value)));
        _delete = ActionButton("DeleteSnippet", Strings.Menu_Delete, () => Apply(b => TemplateEditing.Delete(b, _selected!.Value)));
        _up = ActionButton("MoveUp", "↑", () => MoveBy(-1));
        _down = ActionButton("MoveDown", "↓", () => MoveBy(1));
        _move = ActionButton("MoveGroup", Strings.Settings_Snippets_Move, OpenMoveMenu);
        _undo = ActionButton("UndoSnippets", Strings.Menu_Undo, Undo);
        var actions = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 5, LineSpacing = 5 };
        foreach (var button in new[] { _add, _fromClipboard, _duplicate, _delete, _up, _down, _move, _undo }) actions.Children.Add(button);
        AddAt(right, actions, 1); AddAt(right, _note, 2); AddAt(right, _text, 3); AddAt(right, _count, 4);
        _groups.SelectionChanged += (_, _) =>
        {
            if (_syncing || _groups.SelectedItem is not ListBoxItem { Tag: int key }) return;
            SelectTarget(new(key, null));
        };
        _snippets.SelectionChanged += (_, _) =>
        {
            if (_syncing) return;
            var id = (_snippets.SelectedItem as ListBoxItem)?.Tag as Guid?;
            if (id == _selected) return;
            _note.Text = ""; Cleanup(id); _selected = id; Refresh();
        };
        _text.TextChanging += (_, _) => ChangeText();
        _name.TextChanging += (_, _) => ChangeName();
        foreach (var list in new[] { _groups, _snippets })
            list.AddHandler(KeyDownEvent, (_, e) =>
            {
                if (e.Key == Key.Z && e.KeyModifiers == (OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control))
                { Undo(); e.Handled = true; }
            }, RoutingStrategies.Tunnel);
        _hub.Changed += OnChanged;
        _clipboardTimer.Tick += (_, _) => _ = RefreshClipboardAsync();
        AttachDrag();
        Refresh();
    }
    internal bool HandleNavigation(KeyEventArgs e)
    {
        if (e.Key != Key.Tab || e.KeyModifiers != KeyModifiers.Control || !_text.IsKeyboardFocusWithin) return false;
        if (_snippets.SelectedItem is ListBoxItem item) item.Focus(); else _snippets.Focus();
        return true;
    }
    internal int SelectedGroup => _group;
    internal Guid? SelectedSnippet => _selected;
    private static void AddAt(Grid parent, Control child, int row, int column = 0)
    { SetRow(child, row); SetColumn(child, column); parent.Children.Add(child); }
    private static Button ActionButton(string name, string text, Action action)
    {
        var b = new Button { Name = name, Content = text, Padding = new Thickness(7, 4), MinHeight = 26, FontSize = 12 };
        b.Click += (_, _) => action(); return b;
    }
    public void SelectTarget(TemplateEditTarget target)
    {
        _note.Text = "";
        Cleanup(target.SnippetId);
        _group = Math.Clamp(target.Group, 0, 9);
        _selected = target.SnippetId;
        Refresh();
        if (_selected is { }) _snippets.ScrollIntoView(_snippets.SelectedItem!);
    }
    public void SetActive(bool active)
    {
        if (_disposed) return;
        _active = active; _generation++;
        if (active) { _clipboardTimer.Start(); _ = RefreshClipboardAsync(); }
        else { _clipboardTimer.Stop(); Cleanup(); Refresh(); _hub.Flush(); }
    }
    private void OnChanged() { if (!_editing && !_disposed) Refresh(); }
    private void Cleanup(Guid? keep = null)
    {
        _editing = true;
        try { _hub.RemoveEmpty(keep); }
        finally { _editing = false; }
    }
    private void Refresh()
    {
        if (_disposed) return;
        _syncing = true;
        try
        {
            var group = _hub.Current[_group];
            if (!group.Snippets.Any(s => s.Id == _selected)) _selected = group.Snippets.FirstOrDefault()?.Id;
            var groups = _groups.Items.OfType<ListBoxItem>().ToArray();
            if (groups.Length != TemplateBook.GroupCount)
            {
                groups = _hub.Current.Groups.Select(g =>
                {
                    var item = new ListBoxItem { Tag = g.Key, Content = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis }, Padding = new Thickness(7, 5) };
                    AttachGroupDrop(item, g.Key); return item;
                }).ToArray();
                _groups.ItemsSource = groups;
            }
            foreach (var g in _hub.Current.Groups)
            {
                var text = (TextBlock)groups[g.Key].Content!;
                text.Text = $"{g.Key}  {g.Name}"; text.FontSize = _fontSize;
                groups[g.Key].Height = _fontSize * 2;
                groups[g.Key].Padding = new Thickness(7, 0);
            }
            _groups.SelectedItem = groups[_group];
            var rows = _snippets.Items.OfType<ListBoxItem>().ToArray();
            if (!rows.Select(r => (Guid)r.Tag!).SequenceEqual(group.Snippets.Select(s => s.Id)))
            {
                rows = group.Snippets.Select(s =>
                {
                    var row = new ListBoxItem { Tag = s.Id, Padding = new Thickness(0), Content = new SnippetRowContent() };
                    AttachRowDrag(row, s.Id); return row;
                }).ToArray();
                _snippets.ItemsSource = rows;
            }
            int shortcut = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                bool empty = string.IsNullOrWhiteSpace(group.Snippets[i].Text);
                ((SnippetRowContent)rows[i].Content!).Update(group.Snippets[i].Text, empty ? -1 : shortcut++, _fontSize);
                rows[i].Height = _fontSize * 2;
            }
            _snippets.SelectedItem = rows.FirstOrDefault(r => (Guid)r.Tag! == _selected);
            var snippet = group.Snippets.FirstOrDefault(s => s.Id == _selected);
            bool changedTarget = _text.Tag is not Guid previousId || previousId != _selected;
            if (changedTarget) _text.IsUndoEnabled = false;
            if (_text.Text != (snippet?.Text ?? "")) _text.Text = snippet?.Text ?? "";
            _text.Tag = _selected;
            if (changedTarget) { _text.IsUndoEnabled = true; _text.CaretIndex = 0; }
            _text.IsEnabled = snippet is not null;
            bool changedGroup = _name.Tag is not int oldGroup || oldGroup != _group;
            if (changedGroup) _name.IsUndoEnabled = false;
            if (_name.Text != group.Name) _name.Text = group.Name;
            _name.Tag = _group;
            if (changedGroup) _name.IsUndoEnabled = true;
            _count.Text = Strings.FormatSettings_Snippets_CharacterCount(snippet?.Text.Length ?? 0);
            UpdateButtons();
        }
        finally { _syncing = false; }
    }
    private void UpdateButtons()
    {
        var group = _hub.Current[_group]; bool full = group.Snippets.Count >= TemplateBook.MaxSnippetsPerGroup;
        int index = group.Snippets.FindIndex(s => s.Id == _selected);
        _add.IsEnabled = !full; _fromClipboard.IsEnabled = _active && !full && !string.IsNullOrEmpty(_clipboardText);
        _duplicate.IsEnabled = index >= 0 && !full; _delete.IsEnabled = _move.IsEnabled = index >= 0;
        _up.IsEnabled = index > 0; _down.IsEnabled = index >= 0 && index < group.Snippets.Count - 1;
        _clear.IsEnabled = group.Snippets.Count > 0; _undo.IsEnabled = _hub.CanUndo;
        if (full) _note.Text = Strings.Settings_Snippets_GroupFull;
    }
    private void ChangeText()
    {
        if (_syncing || _selected is not { } id) return;
        _editing = true;
        try { _hub.Edit(b => TemplateEditing.UpdateText(b, id, _text.Text), id, false, _group); }
        finally { _editing = false; }
        _note.Text = (_text.Text?.Length ?? 0) >= TemplateBook.MaxSnippetLength ? Strings.Settings_Snippets_TooLong : "";
        Refresh();
    }
    private void ChangeName()
    {
        if (_syncing) return;
        _editing = true;
        try { _hub.Edit(b => TemplateEditing.Rename(b, _group, _name.Text), _selected, false, _group); }
        finally { _editing = false; }
        Refresh();
    }
    private void Apply(Func<TemplateBook, EditResult> operation, bool focus = false)
    {
        _editing = true;
        EditResult result;
        try { result = _hub.Edit(operation, _selected, true, _group); }
        finally { _editing = false; }
        _note.Text = result.Status switch
        {
            EditStatus.GroupFull => Strings.Settings_Snippets_GroupFull,
            EditStatus.TooLong => Strings.Settings_Snippets_TooLong,
            EditStatus.Empty => Strings.Settings_Snippets_ClipboardEmpty,
            _ => "",
        };
        if (result.Changed) { _selected = result.Select; Cleanup(_selected); }
        Refresh();
        if (focus && result.Changed) { _text.Focus(); _text.CaretIndex = _text.Text?.Length ?? 0; }
    }
    private void Add() => Apply(b => TemplateEditing.Add(b, _group, _selected), true);
    private void MoveBy(int delta)
    {
        if (_selected is not { } id || TemplateEditing.Find(_hub.Current, id) is not { } found) return;
        Apply(b => TemplateEditing.Move(b, id, found.Index + delta));
    }
    private void Undo()
    {
        _editing = true;
        try
        {
            if (!_hub.Undo(out var id, out var group)) return;
            _group = group ?? (id is { } selected ? TemplateEditing.Find(_hub.Current, selected)?.Group.Key : null) ?? _group;
            _selected = id;
        }
        finally { _editing = false; }
        _note.Text = ""; Refresh();
    }
    internal async Task ClearGroupAsync()
    {
        int group = _group, count = _hub.Current[group].Snippets.Count;
        if (count == 0 || !await _confirm(Strings.Settings_Snippets_ClearGroup, Strings.FormatSettings_Snippets_ClearConfirm(group, count), Strings.Menu_Delete)) return;
        if (_disposed || _group != group) return;
        Apply(b => TemplateEditing.ClearGroup(b, group));
    }
    private void OpenMoveMenu()
    {
        if (_selected is not { } id) return;
        var menu = new ContextMenu();
        menu.ItemsSource = _hub.Current.Groups.Where(g => g.Key != _group).Select(g =>
        {
            int key = g.Key;
            var item = new MenuItem { Header = $"{key}  {g.Name}", IsEnabled = g.Snippets.Count < TemplateBook.MaxSnippetsPerGroup };
            item.Click += (_, _) => Apply(b => TemplateEditing.MoveToGroup(b, id, key, false)); return item;
        }).ToArray();
        _move.ContextMenu = menu; menu.Open(_move);
    }
    internal async Task RefreshClipboardAsync()
    {
        if (_reading || !_active || _disposed) return;
        int generation = _generation;
        _reading = true;
        try
        {
            string? text = await _clipboard.GetTextAsync();
            if (_disposed || !_active || generation != _generation) return;
            _clipboardText = text; UpdateButtons();
        }
        catch (Exception) { if (!_disposed && _active && generation == _generation) { _clipboardText = null; UpdateButtons(); } }
        finally { _reading = false; }
    }
    internal async Task AddClipboardAsync()
    {
        int generation = _generation, group = _group; Guid? selected = _selected;
        try
        {
            string? text = await _clipboard.GetTextAsync();
            if (_disposed || !_active || generation != _generation || group != _group || selected != _selected) return;
            Apply(b => TemplateEditing.AddText(b, group, selected, text), true);
        }
        catch (Exception) { if (!_disposed) _note.Text = Strings.Toast_ClipboardBusy; }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _active = false; _generation++; _clipboardTimer.Stop();
        _hub.Changed -= OnChanged; Cleanup(); _hub.Flush(); _disposed = true;
    }
}
