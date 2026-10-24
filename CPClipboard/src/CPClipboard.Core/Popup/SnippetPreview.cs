using CPClipboard.Core.Input;

namespace CPClipboard.Core.Popup;

/// <summary>全文プレビューの内容と画面座標。OS や画面部品には依存しない。</summary>
public static class SnippetPreview
{
    public static bool NeedsPreview(string text, bool ellipsized) => ellipsized || text.IndexOfAny(['\n', '\r', '\t']) >= 0;
    public static int LineCount(string text) => text.Count(c => c == '\n') + 1;
    public static ScreenPoint Place(ScreenRect board, int width, int height, ScreenRect area, int gap = 8)
    {
        int right = board.X + board.Width + gap;
        int x = right + width <= area.X + area.Width ? right : board.X - width - gap;
        return PopupPlacement.ClampInto(new ScreenPoint(x, board.Y), width, height, area);
    }
}
