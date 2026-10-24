using System.Text;

namespace CPClipboard.Core.Model;

/// <summary>盤面の 1 行表示（SPEC §3.1）。改行は ⏎、タブは空白 1 つにして続けて並べる（連続する空白はまとめない）。</summary>
public static class SnippetLine
{
    public const char NewlineMark = '⏎';

    /// <summary>1 行表示の文字列。末尾の切り詰め（…）は表示側（TextTrimming）が行う。</summary>
    public static string Format(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                sb.Append(NewlineMark);
            }
            else if (c == '\n') sb.Append(NewlineMark);
            else if (c == '\t') sb.Append(' ');
            else sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>改行もタブも含まない（1 行に収まれば全文のプレビューは要らない。SPEC §3.4）。</summary>
    public static bool IsSingleLine(string text) => text.IndexOfAny(['\r', '\n', '\t']) < 0;
}
