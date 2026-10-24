namespace CPClipboard.Core.Model;

/// <summary>グループと定型文（SPEC §2 / §9.3）。templates.json のルート。グループは 0〜9 の 10 個で固定。</summary>
public sealed class TemplateBook
{
    public const int CurrentSchemaVersion = 1;
    public const int GroupCount = 10;

    /// <summary>1 件の文字数の上限（SPEC §9.4）。</summary>
    public const int MaxSnippetLength = 10_000;

    /// <summary>1 グループの件数の上限（SPEC §9.4）。</summary>
    public const int MaxSnippetsPerGroup = 500;

    /// <summary>グループ名の上限（SPEC §9.4）。</summary>
    public const int MaxGroupNameLength = 20;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public List<SnippetGroup> Groups { get; set; } = new();

    public SnippetGroup this[int key] => Groups[key];

    /// <summary>空のグループ 10 個。初回の例文は App が言語に合わせて入れる（Core は文言を持たない）。</summary>
    public static TemplateBook CreateEmpty()
    {
        var book = new TemplateBook();
        book.Normalize();
        return book;
    }

    /// <summary>
    /// 読み込み後の正規化（SPEC §9.3 / §9.4）: 欠けた key は空のグループで補い、範囲外・重複の key は捨て、key 順に並べる。
    /// id の無い・重複した定型文には振り直し、改行を \n にそろえ、上限を超えた分は切り詰める。
    /// </summary>
    public void Normalize()
    {
        Groups ??= new();
        var byKey = new SnippetGroup?[GroupCount];
        foreach (var g in Groups)
        {
            if (g is null || g.Key is < 0 or >= GroupCount || byKey[g.Key] is not null) continue;
            byKey[g.Key] = g;
        }
        var ids = new HashSet<Guid>();
        Groups = new List<SnippetGroup>(GroupCount);
        for (int key = 0; key < GroupCount; key++)
        {
            var g = byKey[key] ?? new SnippetGroup { Key = key };
            g.Normalize(ids);
            Groups.Add(g);
        }
    }

    /// <summary>深いコピー（Id も同じ）。元に戻すの控え（TemplateHistory）に使う。</summary>
    public TemplateBook Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        Groups = Groups.Select(g => new SnippetGroup
        {
            Key = g.Key,
            Name = g.Name,
            Snippets = g.Snippets.Select(x => new Snippet { Id = x.Id, Text = x.Text }).ToList(),
        }).ToList(),
    };

    /// <summary>改行を \n にそろえる（保存は \n。貼り付け時に OS の流儀へ変える。SPEC §5.1）。</summary>
    public static string NormalizeNewlines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}

/// <summary>グループ（SPEC §2）。Key は 0〜9。</summary>
public sealed class SnippetGroup
{
    public int Key { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Snippet> Snippets { get; set; } = new();

    internal void Normalize(HashSet<Guid> ids)
    {
        Name ??= string.Empty;
        if (Name.Length > TemplateBook.MaxGroupNameLength) Name = Name[..TemplateBook.MaxGroupNameLength];
        Snippets ??= new();
        Snippets.RemoveAll(s => s is null);
        if (Snippets.Count > TemplateBook.MaxSnippetsPerGroup) Snippets.RemoveRange(TemplateBook.MaxSnippetsPerGroup, Snippets.Count - TemplateBook.MaxSnippetsPerGroup);
        foreach (var s in Snippets)
        {
            if (s.Id == Guid.Empty || !ids.Add(s.Id))
            {
                s.Id = Guid.NewGuid();
                ids.Add(s.Id);
            }
            s.Text = TemplateBook.NormalizeNewlines(s.Text ?? string.Empty);
            if (s.Text.Length > TemplateBook.MaxSnippetLength) s.Text = s.Text[..TemplateBook.MaxSnippetLength];
        }
    }
}

/// <summary>定型文 1 件（SPEC §2）。Text の改行は \n。</summary>
public sealed class Snippet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
}
