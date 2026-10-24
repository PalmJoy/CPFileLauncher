using Avalonia;
using Avalonia.Controls;

namespace CPClipboard.App;

public partial class BoardWindow
{
    private const int RowBuffer = 2;
    private int _rowStart = -1, _rowEnd = -1;
    private bool _realizingRows;
    private bool _ignoreRowHover;
    private int SnippetCount => _book[_groupKey].Snippets.Count;

    // 固定行高なので画面外は同じ高さの余白に置き換え、ScrollViewerの範囲を保つ。
    private void RealizeRows(double offset, bool force = false)
    {
        if (_realizingRows) return;
        int height = Math.Max(1, RowHeight), count = SnippetCount;
        double viewport = RowsScroller.Viewport.Height > 0 ? RowsScroller.Viewport.Height : _appearance.MaxRows * height;
        offset = Math.Clamp(offset, 0, Math.Max(0, count * height - viewport));
        int start = Math.Max(0, (int)Math.Floor(offset / height) - RowBuffer);
        int end = Math.Min(count, (int)Math.Ceiling((offset + viewport) / height) + RowBuffer);
        if (!force && start == _rowStart && end == _rowEnd) return;
        _realizingRows = true;
        try
        {
            DismissPreview();
            _ignoreRowHover = true;
            foreach (int index in _rows.Keys.Where(i => i < start || i >= end).ToArray()) _rows.Remove(index);
            RowsPanel.Children.Clear();
            if (start > 0) RowsPanel.Children.Add(new Border { Height = start * height, IsHitTestVisible = false });
            for (int index = start; index < end; index++)
            {
                if (!_rows.TryGetValue(index, out var row))
                    _rows[index] = row = RowButton(_book[_groupKey].Snippets[index], height, index);
                RowsPanel.Children.Add(row);
            }
            if (end < count) RowsPanel.Children.Add(new Border { Height = (count - end) * height, IsHitTestVisible = false });
            _rowStart = start;
            _rowEnd = end;
            UpdateSelection();
        }
        finally { _realizingRows = false; }
    }

    private void RevealSelection()
    {
        double viewport = RowsScroller.Viewport.Height > 0 ? RowsScroller.Viewport.Height : _appearance.MaxRows * RowHeight;
        double offset = RowsScroller.Offset.Y;
        double top = _selected * RowHeight, bottom = top + RowHeight;
        if (top < offset) offset = top;
        else if (bottom > offset + viewport) offset = bottom - viewport;
        RowsScroller.Offset = new Vector(0, offset);
        RealizeRows(offset);
    }
}
