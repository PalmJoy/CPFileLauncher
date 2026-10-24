using Avalonia.Controls;
using CPClipboard.Core.Model;

namespace CPClipboard.App;

public partial class BoardWindow
{
    public event Action<int, Guid?>? EditRequested;
    public event Action? UpdateRequested;
    public event Action? AboutRequested;
    internal bool ContextMenuOpen { get; private set; }

    private ContextMenu? _openMenu;
    internal ContextMenu? OpenMenu => _openMenu;
    internal void DismissTransientUi()
    {
        _openMenu?.Close();
        DismissPreview();
    }

    private ContextMenu BuildMenu(Snippet? snippet)
    {
        int group = _groupKey;
        var menu = new ContextMenu();
        var items = new List<Control>();
        void Add(string title, Action action)
        {
            var item = new MenuItem { Header = title };
            item.Click += (_, _) => action();
            items.Add(item);
        }
        if (snippet is not null)
        {
            Add(Strings.Menu_Paste, () => Invoke(snippet, false));
            Add(Strings.Menu_CopyOnly, () => Invoke(snippet, true));
            items.Add(new Separator());
            Add(Strings.Menu_EditInSettings, () => EditRequested?.Invoke(group, snippet.Id));
            items.Add(new Separator());
        }
        Add(Strings.Common_Settings, () => SettingsRequested?.Invoke());
        Add(Strings.Update_Check, () => UpdateRequested?.Invoke());
        Add(Strings.Menu_About, () => AboutRequested?.Invoke());
        menu.ItemsSource = items;
        menu.Opened += (_, _) => { _openMenu = menu; ContextMenuOpen = true; DismissPreview(); };
        menu.Closed += (_, _) => { if (ReferenceEquals(_openMenu, menu)) { _openMenu = null; ContextMenuOpen = false; } SchedulePreview(); };
        return menu;
    }
}
