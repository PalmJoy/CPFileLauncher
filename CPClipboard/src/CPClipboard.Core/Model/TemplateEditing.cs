namespace CPClipboard.Core.Model;

/// <summary>編集操作の結果（文言は返さない。注記は App が組む）。</summary>
public enum EditStatus
{
    Ok,
    /// <summary>何も変えていない（同じ位置・同じ値・同じグループ・空のグループ）。</summary>
    NoChange,
    NotFound,
    /// <summary>グループの件数の上限（500）。</summary>
    GroupFull,
    /// <summary>本文の上限（10,000 文字）。UpdateText は切り詰めて反映し、AddText は足さない。</summary>
    TooLong,
    /// <summary>空白・改行だけ（AddText は足さない）。</summary>
    Empty,
}

/// <summary>操作の結果と、そのあと設定画面が選ぶ定型文（null = 選択なし）。</summary>
public readonly record struct EditResult(EditStatus Status, Guid? Select = null, bool? WasChanged = null)
{
    /// <summary>TemplateBook が変わったか。</summary>
    public bool Changed => WasChanged ?? (Status is EditStatus.Ok or EditStatus.TooLong);
}

/// <summary>
/// 定型文タブの編集操作（SPEC §6.3）。TemplateBook を直接書き換える。上限は SPEC §9.4。
/// 変更は App の TemplatesHub.Edit を通す（保存・通知・元に戻すの控え）。
/// </summary>
public static class TemplateEditing
{
    public static (SnippetGroup Group, int Index)? Find(TemplateBook book, Guid id)
    {
        foreach (var g in book.Groups)
        {
            int i = g.Snippets.FindIndex(s => s.Id == id);
            if (i >= 0) return (g, i);
        }
        return null;
    }

    /// <summary>空の定型文を afterId の直後（null・見つからなければ末尾）に足す。</summary>
    public static EditResult Add(TemplateBook book, int key, Guid? afterId) => Insert(book, key, afterId, string.Empty);

    /// <summary>文字（クリップボードの中身）を新しい定型文として足す。空白だけ・長すぎるものは足さない。</summary>
    public static EditResult AddText(TemplateBook book, int key, Guid? afterId, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new(EditStatus.Empty);
        text = TemplateBook.NormalizeNewlines(text);
        if (text.Length > TemplateBook.MaxSnippetLength) return new(EditStatus.TooLong, WasChanged: false);
        return Insert(book, key, afterId, text);
    }

    private static EditResult Insert(TemplateBook book, int key, Guid? afterId, string text)
    {
        var group = book[key];
        if (group.Snippets.Count >= TemplateBook.MaxSnippetsPerGroup) return new(EditStatus.GroupFull);
        int at = afterId is { } after && group.Snippets.FindIndex(s => s.Id == after) is var i and >= 0 ? i + 1 : group.Snippets.Count;
        var snippet = new Snippet { Text = text };
        group.Snippets.Insert(at, snippet);
        return new(EditStatus.Ok, snippet.Id);
    }

    /// <summary>直後に同じ本文の定型文を足す。</summary>
    public static EditResult Duplicate(TemplateBook book, Guid id)
    {
        if (Find(book, id) is not var (group, index)) return new(EditStatus.NotFound);
        if (group.Snippets.Count >= TemplateBook.MaxSnippetsPerGroup) return new(EditStatus.GroupFull, id);
        var copy = new Snippet { Text = group.Snippets[index].Text };
        group.Snippets.Insert(index + 1, copy);
        return new(EditStatus.Ok, copy.Id);
    }

    /// <summary>消す（確認なし。元に戻すで戻せる）。選択は同じ位置の次、無ければ前。</summary>
    public static EditResult Delete(TemplateBook book, Guid id)
    {
        if (Find(book, id) is not var (group, index)) return new(EditStatus.NotFound);
        group.Snippets.RemoveAt(index);
        return new(EditStatus.Ok, Neighbor(group, index));
    }

    /// <summary>同じグループの中で位置を変える（範囲に丸める）。↑↓ は index ∓ 1。</summary>
    public static EditResult Move(TemplateBook book, Guid id, int newIndex)
    {
        if (Find(book, id) is not var (group, index)) return new(EditStatus.NotFound);
        newIndex = Math.Clamp(newIndex, 0, group.Snippets.Count - 1);
        if (newIndex == index) return new(EditStatus.NoChange, id);
        var snippet = group.Snippets[index];
        group.Snippets.RemoveAt(index);
        group.Snippets.Insert(newIndex, snippet);
        return new(EditStatus.Ok, id);
    }

    /// <summary>
    /// 他のグループの末尾へ移す（copy なら複製）。移したあとは元のグループの同じ位置を選ぶ。複製なら元を選んだまま。
    /// </summary>
    public static EditResult MoveToGroup(TemplateBook book, Guid id, int targetKey, bool copy)
    {
        if (Find(book, id) is not var (group, index)) return new(EditStatus.NotFound);
        if (group.Key == targetKey) return new(EditStatus.NoChange, id);
        var target = book[targetKey];
        if (target.Snippets.Count >= TemplateBook.MaxSnippetsPerGroup) return new(EditStatus.GroupFull, id);
        var snippet = group.Snippets[index];
        if (copy)
        {
            target.Snippets.Add(new Snippet { Text = snippet.Text });
            return new(EditStatus.Ok, id);
        }
        group.Snippets.RemoveAt(index);
        target.Snippets.Add(snippet);
        return new(EditStatus.Ok, Neighbor(group, index));
    }

    /// <summary>グループの定型文をすべて消す（名前は残す）。</summary>
    public static EditResult ClearGroup(TemplateBook book, int key)
    {
        var group = book[key];
        if (group.Snippets.Count == 0) return new(EditStatus.NoChange);
        group.Snippets.Clear();
        return new(EditStatus.Ok);
    }

    /// <summary>グループ名を変える（20 文字に切り詰める）。</summary>
    public static EditResult Rename(TemplateBook book, int key, string? name)
    {
        name ??= string.Empty;
        if (name.Length > TemplateBook.MaxGroupNameLength) name = name[..TemplateBook.MaxGroupNameLength];
        var group = book[key];
        if (group.Name == name) return new(EditStatus.NoChange);
        group.Name = name;
        return new(EditStatus.Ok);
    }

    /// <summary>本文を変える。改行は \n に、10,000 文字を超えたら切り詰めて反映し TooLong を返す。</summary>
    public static EditResult UpdateText(TemplateBook book, Guid id, string? text)
    {
        if (Find(book, id) is not var (group, index)) return new(EditStatus.NotFound);
        text = TemplateBook.NormalizeNewlines(text ?? string.Empty);
        var status = EditStatus.Ok;
        if (text.Length > TemplateBook.MaxSnippetLength)
        {
            text = text[..TemplateBook.MaxSnippetLength];
            status = EditStatus.TooLong;
        }
        var snippet = group.Snippets[index];
        if (snippet.Text == text) return new(EditStatus.NoChange, id);
        snippet.Text = text;
        return new(status, id);
    }

    /// <summary>空白・改行だけの定型文を取り除く（SPEC §6.3。選択が移ったとき・タブを離れたとき・閉じたとき）。除いた件数。</summary>
    public static int RemoveEmpty(TemplateBook book, Guid? keep = null) =>
        book.Groups.Sum(g => g.Snippets.RemoveAll(s => s.Id != keep && string.IsNullOrWhiteSpace(s.Text)));

    private static Guid? Neighbor(SnippetGroup group, int removedIndex) =>
        group.Snippets.Count == 0 ? null : group.Snippets[Math.Min(removedIndex, group.Snippets.Count - 1)].Id;
}
