using Avalonia.Controls;
using Avalonia.Input;
using CPClipboard.Core.Model;
using InputModifiers = Avalonia.Input.KeyModifiers;

namespace CPClipboard.App;

/// <summary>
/// 盤面のキーボード・ホイール操作（SPEC §6.1）。キーボードのフォーカスは盤面ウィンドウ自身が持ち、行のボタンはフォーカスを取らない。
/// 選択中の行は 1 つ（マウスを乗せた行もこれになる）。
/// </summary>
public partial class BoardWindow
{
    private int _selected = -1;

    /// <summary>選択中の行の番号（無ければ -1）。</summary>
    public int SelectedIndex => _selected;

    private void AttachKeyboard()
    {
        // 矢印・Tab は Avalonia のフォーカス移動が先に取るので、トンネルで先取りする
        AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Handled) return;
            if (HandleKey(e)) e.Handled = true;
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private bool HandleKey(KeyEventArgs e)
    {
        var mods = e.KeyModifiers;
        // 数字キー 0〜9（テンキーも）でグループへ（修飾キーなし。SPEC §3.2）
        if (mods == InputModifiers.None && DigitOf(e) is { } digit)
        {
            SwitchGroup(digit);
            return true;
        }
        // グループ全体で先頭26件に固定。画面外の行も論理indexで実行する。
        if ((mods == InputModifiers.None || mods == InputModifiers.Shift) &&
            e.PhysicalKey is >= PhysicalKey.A and <= PhysicalKey.Z)
        {
            int index = e.PhysicalKey - PhysicalKey.A;
            if (index >= SnippetCount) return false;
            _selected = index;
            RevealSelection();
            UpdateSelection();
            Invoke(_book[_groupKey].Snippets[index], copyOnly: mods == InputModifiers.Shift);
            return true;
        }
        switch (e.Key)
        {
            case Key.Up when mods == InputModifiers.None:
                MoveSelection(-1);
                return true;
            case Key.Down when mods == InputModifiers.None:
                MoveSelection(1);
                return true;
            case Key.PageUp when mods == InputModifiers.None:
                MoveSelection(-PageRows());
                return true;
            case Key.PageDown when mods == InputModifiers.None:
                MoveSelection(PageRows());
                return true;
            case Key.Home when mods == InputModifiers.None:
                MoveSelection(-SnippetCount);
                return true;
            case Key.End when mods == InputModifiers.None:
                MoveSelection(SnippetCount);
                return true;
            case Key.Left when mods == InputModifiers.None:
                SwitchGroupBy(-1);
                return true;
            case Key.Right when mods == InputModifiers.None:
                SwitchGroupBy(1);
                return true;
            case Key.Tab when mods == InputModifiers.Control:
                SwitchGroupBy(1);
                return true;
            case Key.Tab when mods == (InputModifiers.Control | InputModifiers.Shift):
                SwitchGroupBy(-1);
                return true;
            case Key.Enter when (mods == InputModifiers.None || mods == InputModifiers.Shift) && SelectedSnippet() is { } snippet:
                Invoke(snippet, copyOnly: mods == InputModifiers.Shift);
                return true;
        }
        return false;
    }

    private static int? DigitOf(KeyEventArgs e) => e.PhysicalKey switch
    {
        >= PhysicalKey.Digit0 and <= PhysicalKey.Digit9 => e.PhysicalKey - PhysicalKey.Digit0,
        >= PhysicalKey.NumPad0 and <= PhysicalKey.NumPad9 => e.PhysicalKey - PhysicalKey.NumPad0,
        _ => null,
    };

    private int PageRows() => Math.Max(1, (int)(RowsScroller.Viewport.Height / Math.Max(1, RowHeight)));

    private Snippet? SelectedSnippet() =>
        _selected >= 0 && _selected < SnippetCount ? _book[_groupKey].Snippets[_selected] : null;

    /// <summary>選択を動かす（端で止まる。循環しない）。見えない所へ行けばスクロール。</summary>
    private void MoveSelection(int delta)
    {
        if (SnippetCount == 0) return;
        _selected = Math.Clamp((_selected < 0 ? 0 : _selected) + delta, 0, SnippetCount - 1);
        RevealSelection();
        UpdateSelection();
    }

    /// <summary>選択中の行に .selected を付け直す。</summary>
    private void UpdateSelection()
    {
        SchedulePreview();
        foreach (var (i, row) in _rows)
        {
            if (i == _selected)
            {
                bool changed = !row.Classes.Contains("selected");
                row.Classes.Add("selected");
                if (changed && _hoverEffect.IsActive && row.Content is Grid grid)
                {
                    _selectionLock ??= new SelectionLock { Name = "SelectionLock", [!SignalPulse.StrokeProperty] = Themed.Res("FlAccent") };
                    MoveSignal(_selectionLock, grid);
                    _selectionLock.Easing = EffectRunner.ToEasing(_hoverEffect.Easing);
                    _selectionLock.Active = true;
                    _selectionLock.Play(_hoverEffect.DurationMs, clampDuration: false);
                }
            }
            else
            {
                row.Classes.Remove("selected");
                if (row.Content is Grid grid && grid.Children.OfType<SelectionLock>().FirstOrDefault() is { } target)
                { target.Active = false; target.Stop(); }
            }
        }
    }

    /// <summary>
    /// ホイール（SPEC §6.1）: 一覧があふれていれば一覧の上ではスクロール、そうでなければ前後の空でないグループへ（下 = 次）。
    /// タブの上では常にグループ。どちらも設定 board.wheelSwitchesGroup が OFF ならグループは切り替えない。
    /// </summary>
    private void AttachWheel()
    {
        Chrome.AddHandler(PointerWheelChangedEvent, (_, e) =>
        {
            if (e.Delta.Y == 0) return;
            bool overList = ListArea.IsPointerOver;
            bool overflow = RowsScroller.Extent.Height > RowsScroller.Viewport.Height + 0.5;
            if (overList && overflow) return; // ScrollViewer に任せる
            if (WheelSwitchesGroup) SwitchGroupBy(e.Delta.Y < 0 ? 1 : -1);
            e.Handled = true;
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    /// <summary>ホイールでグループを切り替えるか（設定 board.wheelSwitchesGroup。App が入れる）。</summary>
    public bool WheelSwitchesGroup { get; set; } = true;
}
