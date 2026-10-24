using KeyModifiers = Avalonia.Input.KeyModifiers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CPClipboard.Core.Model;

namespace CPClipboard.App;

internal sealed partial class TemplatesEditor
{
    internal sealed record SnippetDrag(Guid Editor, Guid Id);
    internal static readonly DataFormat<SnippetDrag> DragFormat = DataFormat.CreateInProcessFormat<SnippetDrag>("CPClipboard.Snippet");
    internal readonly Guid DragIdentity = Guid.NewGuid();
    private PointerPressedEventArgs? _dragPress;
    private Point _dragStart;
    private Guid _dragSnippet;

    private void AttachDrag()
    {
        _snippets.AddHandler(PointerMovedEvent, async (_, e) =>
        {
            if (_dragPress is not { } press || !e.GetCurrentPoint(_snippets).Properties.IsLeftButtonPressed) return;
            var point = e.GetPosition(_snippets);
            if (Math.Abs(point.X - _dragStart.X) < 4 && Math.Abs(point.Y - _dragStart.Y) < 4) return;
            _dragPress = null;
            var transfer = new DataTransfer(); var item = new DataTransferItem();
            item.Set(DragFormat, new SnippetDrag(DragIdentity, _dragSnippet)); transfer.Add(item);
            try { await DragDrop.DoDragDropAsync(press, transfer, DragDropEffects.Move | DragDropEffects.Copy); }
            catch (Exception ex) { AppLog.Info($"定型文のドラッグ: {ex.Message}"); }
        }, RoutingStrategies.Tunnel, handledEventsToo: true);
        _snippets.AddHandler(PointerReleasedEvent, (_, _) => _dragPress = null, RoutingStrategies.Tunnel, handledEventsToo: true);
        AttachDrop(_snippets, null, null);
    }
    private void AttachRowDrag(ListBoxItem row, Guid id)
    {
        row.AddHandler(PointerPressedEvent, (_, e) =>
        {
            if (!e.GetCurrentPoint(row).Properties.IsLeftButtonPressed) return;
            _dragPress = e; _dragStart = e.GetPosition(_snippets); _dragSnippet = id;
        }, RoutingStrategies.Tunnel, handledEventsToo: true);
        AttachDrop(row, null, id);
    }
    private void AttachGroupDrop(ListBoxItem row, int key) => AttachDrop(row, key, null);
    private void AttachDrop(Control control, int? group, Guid? row)
    {
        DragDrop.SetAllowDrop(control, true);
        control.AddHandler(DragDrop.DragOverEvent, (_, e) =>
        {
            var drag = e.DataTransfer.TryGetValue(DragFormat);
            bool copy = CopyModifier(e.KeyModifiers);
            bool valid = CanDrop(drag, group);
            e.DragEffects = valid ? group is not null && copy ? DragDropEffects.Copy : DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        });
        control.AddHandler(DragDrop.DropEvent, (_, e) =>
        {
            var drag = e.DataTransfer.TryGetValue(DragFormat);
            if (!CanDrop(drag, group)) { e.DragEffects = DragDropEffects.None; e.Handled = true; return; }
            bool after = row is not null && e.GetPosition(control).Y >= control.Bounds.Height / 2;
            DropSnippet(drag!, group, row, after, CopyModifier(e.KeyModifiers));
            e.Handled = true;
        });
    }
    private static bool CopyModifier(KeyModifiers modifiers) => modifiers.HasFlag(OperatingSystem.IsMacOS() ? KeyModifiers.Alt : KeyModifiers.Control);
    internal bool CanDrop(SnippetDrag? drag, int? targetGroup)
    {
        if (_disposed || drag?.Editor != DragIdentity || TemplateEditing.Find(_hub.Current, drag.Id) is not { } source) return false;
        return targetGroup is { } group
            ? group is >= 0 and < 10 && group != source.Group.Key && _hub.Current[group].Snippets.Count < TemplateBook.MaxSnippetsPerGroup
            : source.Group.Key == _group;
    }
    internal void DropSnippet(SnippetDrag drag, int? targetGroup, Guid? targetRow, bool after, bool copy)
    {
        if (!CanDrop(drag, targetGroup)) return;
        if (targetGroup is { } group) { Apply(b => TemplateEditing.MoveToGroup(b, drag.Id, group, copy)); return; }
        var list = _hub.Current[_group].Snippets;
        int source = list.FindIndex(s => s.Id == drag.Id);
        int at = targetRow is { } id ? list.FindIndex(s => s.Id == id) : list.Count;
        if (at < 0) return;
        if (targetRow is not null && after) at++;
        if (source < at) at--;
        Apply(b => TemplateEditing.Move(b, drag.Id, at));
    }
}
