namespace CPClipboard.Core.Model;

public sealed record HistoryEntry(Guid Id, string Text, DateTimeOffset CopiedAt);

/// <summary>新しい順のテキスト履歴。同じ文章は最新位置へ移し、保存上限と経過時間を守る。</summary>
public sealed class HistoryBook
{
    public int SchemaVersion { get; set; } = 1;
    public List<HistoryEntry> Entries { get; set; } = [];
    public const int MaxTextLength = 1024 * 1024;
    private const long MaxTotalCharacters = 16 * 1024 * 1024;

    public bool Add(string? text, DateTimeOffset now, HistorySettings settings)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxTextLength) return Prune(now, settings);
        text = TemplateBook.NormalizeNewlines(text);
        var existing = Entries.FirstOrDefault(e => e.Text == text);
        Entries.RemoveAll(e => e.Text == text);
        Entries.Insert(0, new(existing?.Id ?? Guid.NewGuid(), text, now));
        Prune(now, settings);
        return true;
    }
    public bool Prune(DateTimeOffset now, HistorySettings settings)
    {
        settings.Normalize();
        Entries ??= [];
        int before = Entries.Count;
        Entries.RemoveAll(e => e is null || string.IsNullOrWhiteSpace(e.Text) || e.Text.Length > MaxTextLength
            || (settings.AutoClear && (now - e.CopiedAt).TotalMinutes >= settings.AutoClearMinutes));
        Entries = Entries.OrderByDescending(e => e.CopiedAt).DistinctBy(e => e.Text).ToList();
        long characters = 0;
        int keep = 0;
        foreach (var entry in Entries.Take(settings.MaxItems))
        {
            if (characters + entry.Text.Length > MaxTotalCharacters) break;
            characters += entry.Text.Length; keep++;
        }
        if (Entries.Count > keep) Entries.RemoveRange(keep, Entries.Count - keep);
        return before != Entries.Count;
    }
    public IReadOnlyList<HistoryEntry> Filter(string? query) => string.IsNullOrWhiteSpace(query)
        ? Entries.ToArray()
        : Entries.Where(e => e.Text.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
}
