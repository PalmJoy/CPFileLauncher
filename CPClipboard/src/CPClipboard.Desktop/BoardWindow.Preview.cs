using Avalonia.Controls;
using Avalonia.Threading;
using CPClipboard.Core.Effects;
using CPClipboard.Core.Model;
using CPClipboard.Core.Popup;
using CPClipboard.Platform;

namespace CPClipboard.App;

public partial class BoardWindow
{
    private readonly DispatcherTimer _previewTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private PreviewWindow? _preview;
    private Snippet? _previewCandidate;
    private bool _previewEnabled = true;
    internal IWindowService? WindowService { get; set; }
    internal PreviewWindow? Preview => _preview;
    internal bool PreviewPending => _previewTimer.IsEnabled;
    public bool PreviewEnabled
    {
        get => _previewEnabled;
        set { if (_previewEnabled == value) return; _previewEnabled = value; if (!value) DismissPreview(); else SchedulePreview(); }
    }

    private void AttachPreview()
    {
        _previewTimer.Tick += (_, _) => ShowPreview();
        PositionChanged += (_, _) => SchedulePreview();
        Deactivated += (_, _) => DismissPreview();
        Closed += (_, _) => { DismissPreview(); _preview?.Close(); _preview = null; };
    }
    private void SchedulePreview()
    {
        DismissPreview();
        if (!_previewEnabled || !IsVisible || !IsHitTestVisible || ContextMenuOpen || !IsSelectedRowVisible()
            || SelectedSnippet() is not { } snippet) return;
        _previewCandidate = snippet;
        _previewTimer.Start();
    }
    private bool IsSelectedRowVisible() => _selected >= 0 && _rows.ContainsKey(_selected)
        && _selected * RowHeight < RowsScroller.Offset.Y + RowsScroller.Viewport.Height
        && (_selected + 1) * RowHeight > RowsScroller.Offset.Y;

    internal void ShowPreview()
    {
        _previewTimer.Stop();
        if (!_previewEnabled || !IsVisible || !IsHitTestVisible || ContextMenuOpen || !IsSelectedRowVisible() || _previewCandidate is not { } snippet
            || !ReferenceEquals(snippet, SelectedSnippet())) return;
        if (!_rows.TryGetValue(_selected, out var row)) return;
        var text = ((Avalonia.Controls.DockPanel)((Avalonia.Controls.Grid)row.Content!).Children[0]).Children.OfType<TextBlock>().Last();
        if (!SnippetPreview.NeedsPreview(snippet.Text, text.TextLayout.TextLines.Any(line => line.HasCollapsed))) return;
        _preview ??= new PreviewWindow();
        _preview.Display(snippet, this, WindowService, EffectCatalog.Resolve(_appearance, EffectCatalog.PreviewShow));
    }
    internal bool IsOverPreview(CPClipboard.Core.Input.ScreenPoint point)
    {
        if (_preview?.IsVisible != true) return false;
        double scale = WindowService?.UsesLogicalScreenCoordinates == true ? 1 : _preview.RenderScaling;
        return point.X >= _preview.Position.X && point.Y >= _preview.Position.Y
            && point.X < _preview.Position.X + _preview.Width * scale && point.Y < _preview.Position.Y + _preview.Height * scale;
    }

    internal void DismissPreview()
    {
        _previewTimer.Stop();
        _previewCandidate = null;
        _preview?.Dismiss();
    }
}
