namespace CPClipboard.Core.Model;

/// <summary>
/// 定型文の一覧操作の 1 段階の元に戻す（SPEC §6.3）。操作の直前に全体の控えを取り、戻すときは控えの中身を書き戻す。
/// 本文・名前を編集したら控えを捨てる（戻すとその編集まで消えるため。呼び出し側が Discard する）。
/// </summary>
public sealed class TemplateHistory
{
    private TemplateBook? _snapshot;
    private Guid? _selected;
    private int? _group;

    public bool CanUndo => _snapshot is not null;

    /// <summary>一覧操作の直前に呼ぶ。selected = そのとき選んでいた定型文（戻したあとに選び直す）。</summary>
    public void Record(TemplateBook book, Guid? selected, int? selectedGroup = null)
    {
        _snapshot = book.Clone();
        _selected = selected;
        _group = selectedGroup ?? (selected is { } id ? TemplateEditing.Find(book, id)?.Group.Key : null);
    }

    public void Discard()
    {
        _snapshot = null;
        _selected = null;
        _group = null;
    }

    /// <summary>控えを book に書き戻して控えを消す。戻したら true（選び直す定型文は selected）。</summary>
    public bool Undo(TemplateBook book, out Guid? selected) => Undo(book, out selected, out _);

    public bool Undo(TemplateBook book, out Guid? selected, out int? selectedGroup)
    {
        selectedGroup = _group;
        selected = _selected;
        if (_snapshot is null) return false;
        book.Groups = _snapshot.Groups;
        Discard();
        return true;
    }
}
