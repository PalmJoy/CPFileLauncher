using System.Text.Json;
using CPClipboard.Core.Model;
using CPClipboard.Core.Storage;

namespace CPClipboard.Tests;

/// <summary>templates.json の正規化（SPEC §9.3 / §9.4）と 1 行表示（SPEC §3.1）。</summary>
public class TemplateBookTests
{
    private static TemplateBook Read(string json)
    {
        var book = JsonSerializer.Deserialize<TemplateBook>(json, JsonDefaults.Options)!;
        book.Normalize();
        return book;
    }

    [Fact]
    public void 長いクリップボード追加の拒否は変更として返さない()
    {
        var book = TemplateBook.CreateEmpty();
        var result = TemplateEditing.AddText(book, 1, null, new string('a', 10001));
        Assert.Equal(EditStatus.TooLong, result.Status);
        Assert.False(result.Changed);
        Assert.Empty(book[1].Snippets);
    }

    [Fact]
    public void 選択中の空欄だけは自動除去から保護できる()
    {
        var book = TemplateBook.CreateEmpty();
        var first = TemplateEditing.Add(book, 1, null).Select;
        var second = TemplateEditing.Add(book, 1, first).Select;
        Assert.Equal(1, TemplateEditing.RemoveEmpty(book, second));
        Assert.Equal(second, Assert.Single(book[1].Snippets).Id);
    }

    [Fact]
    public void Undoは空グループの選択も復元する()
    {
        var book = TemplateBook.CreateEmpty();
        var history = new TemplateHistory();
        history.Record(book, null, 7);
        TemplateEditing.AddText(book, 7, null, "added");
        Assert.True(history.Undo(book, out var id, out var group));
        Assert.Null(id); Assert.Equal(7, group); Assert.Empty(book[7].Snippets);
    }

    [Fact]
    public void 欠けたグループは空で補い_0から9の順に並べる()
    {
        var book = Read("""{ "groups": [ { "key": 7, "name": "七" }, { "key": 2 } ] }""");

        Assert.Equal(Enumerable.Range(0, 10), book.Groups.Select(g => g.Key));
        Assert.Equal("七", book[7].Name);
        Assert.All(book.Groups, g => Assert.NotNull(g.Snippets));
    }

    [Fact]
    public void 範囲外と重複のkeyは捨てる_重複は先のものを残す()
    {
        var book = Read("""
            { "groups": [ { "key": 3, "name": "先" }, { "key": 3, "name": "後" }, { "key": 10, "name": "外" }, { "key": -1 } ] }
            """);

        Assert.Equal(10, book.Groups.Count);
        Assert.Equal("先", book[3].Name);
        Assert.DoesNotContain(book.Groups, g => g.Name is "後" or "外");
    }

    [Fact]
    public void nullのグループや定型文や名前は読み飛ばす()
    {
        var book = Read("""{ "groups": [ null, { "key": 1, "name": null, "snippets": [ null, { "text": null } ] } ] }""");

        Assert.Equal("", book[1].Name);
        var snippet = Assert.Single(book[1].Snippets);
        Assert.Equal("", snippet.Text);
    }

    [Fact]
    public void Idが無いか重複していれば振り直す()
    {
        var dup = Guid.NewGuid();
        var book = Read($$"""
            { "groups": [
              { "key": 1, "snippets": [ { "id": "{{dup}}", "text": "a" }, { "text": "b" } ] },
              { "key": 2, "snippets": [ { "id": "{{dup}}", "text": "c" } ] } ] }
            """);

        var ids = book.Groups.SelectMany(g => g.Snippets).Select(s => s.Id).ToList();
        Assert.Equal(3, ids.Distinct().Count());
        Assert.DoesNotContain(Guid.Empty, ids);
        Assert.Equal(dup, book[1].Snippets[0].Id); // 先に出たものはそのまま
    }

    [Fact]
    public void 改行はLFにそろえる()
    {
        var book = Read("""{ "groups": [ { "key": 0, "snippets": [ { "text": "a\r\nb\rc\nd" } ] } ] }""");

        Assert.Equal("a\nb\nc\nd", book[0].Snippets[0].Text);
    }

    [Fact]
    public void 上限を超えたものは切り詰める()
    {
        var book = TemplateBook.CreateEmpty();
        book[0].Name = new string('名', 30);
        book[0].Snippets.Add(new Snippet { Text = new string('x', TemplateBook.MaxSnippetLength + 5) });
        for (int i = 0; i < TemplateBook.MaxSnippetsPerGroup + 3; i++) book[1].Snippets.Add(new Snippet { Text = "s" + i });

        book.Normalize();

        Assert.Equal(TemplateBook.MaxGroupNameLength, book[0].Name.Length);
        Assert.Equal(TemplateBook.MaxSnippetLength, book[0].Snippets[0].Text.Length);
        Assert.Equal(TemplateBook.MaxSnippetsPerGroup, book[1].Snippets.Count);
        Assert.Equal("s0", book[1].Snippets[0].Text); // 先頭から残す
    }

    [Fact]
    public void Cloneは別物でIdは同じ()
    {
        var book = TemplateBook.CreateEmpty();
        book[1].Name = "元";
        book[1].Snippets.Add(new Snippet { Text = "a" });

        var copy = book.Clone();
        copy[1].Name = "写";
        copy[1].Snippets[0].Text = "b";
        copy[1].Snippets.Add(new Snippet());

        Assert.Equal("元", book[1].Name);
        Assert.Equal("a", Assert.Single(book[1].Snippets).Text);
        Assert.Equal(book[1].Snippets[0].Id, copy[1].Snippets[0].Id);
    }

    [Theory]
    [InlineData("a\nb", "a⏎b")]
    [InlineData("a\r\nb", "a⏎b")]
    [InlineData("a\rb", "a⏎b")]
    [InlineData("a\tb", "a b")]
    [InlineData("a  b", "a  b")] // 連続する空白はまとめない
    [InlineData("", "")]
    [InlineData("\n\n", "⏎⏎")]
    public void 一行表示は改行を記号に_タブを空白にする(string text, string expected)
    {
        Assert.Equal(expected, SnippetLine.Format(text));
    }

    [Theory]
    [InlineData("one line", true)]
    [InlineData("a\nb", false)]
    [InlineData("a\tb", false)]
    [InlineData("a\rb", false)]
    public void 改行もタブも無ければ一行(string text, bool expected)
    {
        Assert.Equal(expected, SnippetLine.IsSingleLine(text));
    }
}

/// <summary>定型文タブの編集操作（SPEC §6.3）。</summary>
public class TemplateEditingTests
{
    private static TemplateBook Book(params string[] texts)
    {
        var book = TemplateBook.CreateEmpty();
        foreach (var t in texts) book[1].Snippets.Add(new Snippet { Text = t });
        return book;
    }

    private static string[] Texts(TemplateBook book, int key = 1) => book[key].Snippets.Select(s => s.Text).ToArray();

    private static Guid Id(TemplateBook book, int index, int key = 1) => book[key].Snippets[index].Id;

    [Fact]
    public void 追加は選択の直後に空の定型文を入れて選ぶ_無ければ末尾()
    {
        var book = Book("a", "b");

        var r = TemplateEditing.Add(book, 1, Id(book, 0));
        Assert.Equal(EditStatus.Ok, r.Status);
        Assert.Equal(["a", "", "b"], Texts(book));
        Assert.Equal(Id(book, 1), r.Select);

        TemplateEditing.Add(book, 1, null);
        Assert.Equal(["a", "", "b", ""], Texts(book));
        TemplateEditing.Add(book, 1, Guid.NewGuid()); // 見つからなければ末尾
        Assert.Equal(5, book[1].Snippets.Count);
        TemplateEditing.Add(book, 0, null); // 空のグループ
        Assert.Single(book[0].Snippets);
    }

    [Fact]
    public void 満杯のグループには足さない()
    {
        var book = TemplateBook.CreateEmpty();
        for (int i = 0; i < TemplateBook.MaxSnippetsPerGroup; i++) book[1].Snippets.Add(new Snippet { Text = "x" });

        Assert.Equal(EditStatus.GroupFull, TemplateEditing.Add(book, 1, null).Status);
        Assert.Equal(EditStatus.GroupFull, TemplateEditing.AddText(book, 1, null, "y").Status);
        Assert.Equal(EditStatus.GroupFull, TemplateEditing.Duplicate(book, Id(book, 0)).Status);
        Assert.Equal(TemplateBook.MaxSnippetsPerGroup, book[1].Snippets.Count);
    }

    [Fact]
    public void クリップボードから追加は空白だけや長すぎる文字を足さない_改行はLFにする()
    {
        var book = Book("a");

        Assert.Equal(EditStatus.Empty, TemplateEditing.AddText(book, 1, null, " \r\n\t ").Status);
        Assert.Equal(EditStatus.Empty, TemplateEditing.AddText(book, 1, null, null).Status);
        Assert.Equal(EditStatus.TooLong, TemplateEditing.AddText(book, 1, null, new string('x', TemplateBook.MaxSnippetLength + 1)).Status);
        Assert.Single(book[1].Snippets);

        var r = TemplateEditing.AddText(book, 1, Id(book, 0), "x\r\ny");
        Assert.Equal(EditStatus.Ok, r.Status);
        Assert.Equal(["a", "x\ny"], Texts(book));
        Assert.Equal(Id(book, 1), r.Select);
    }

    [Fact]
    public void 複製は直後に新しいIdで入れて複製を選ぶ()
    {
        var book = Book("a", "b");
        var original = Id(book, 0);

        var r = TemplateEditing.Duplicate(book, original);

        Assert.Equal(["a", "a", "b"], Texts(book));
        Assert.NotEqual(original, Id(book, 1));
        Assert.Equal(Id(book, 1), r.Select);
        Assert.Equal(EditStatus.NotFound, TemplateEditing.Duplicate(book, Guid.NewGuid()).Status);
    }

    [Fact]
    public void 削除のあとは同じ位置の次_末尾なら前_空なら選択なし()
    {
        var book = Book("a", "b", "c");

        var r = TemplateEditing.Delete(book, Id(book, 1));
        Assert.Equal(["a", "c"], Texts(book));
        Assert.Equal(Id(book, 1), r.Select); // c

        r = TemplateEditing.Delete(book, Id(book, 1));
        Assert.Equal(Id(book, 0), r.Select); // a
        r = TemplateEditing.Delete(book, Id(book, 0));
        Assert.Null(r.Select);
        Assert.Equal(EditStatus.NotFound, TemplateEditing.Delete(book, Guid.NewGuid()).Status);
    }

    [Fact]
    public void 並べ替えは範囲に丸め_同じ位置なら変えない()
    {
        var book = Book("a", "b", "c");
        var a = Id(book, 0);

        Assert.Equal(EditStatus.Ok, TemplateEditing.Move(book, a, 2).Status);
        Assert.Equal(["b", "c", "a"], Texts(book));
        Assert.Equal(EditStatus.NoChange, TemplateEditing.Move(book, a, 99).Status); // 末尾のまま
        Assert.Equal(EditStatus.Ok, TemplateEditing.Move(book, a, -5).Status);
        Assert.Equal(["a", "b", "c"], Texts(book));
        Assert.Equal(a, TemplateEditing.Move(book, a, 1).Select);
    }

    [Fact]
    public void 他のグループへの移動は末尾へ入り_元のグループの同じ位置を選ぶ()
    {
        var book = Book("a", "b", "c");
        book[4].Snippets.Add(new Snippet { Text = "x" });
        var b = Id(book, 1);

        var r = TemplateEditing.MoveToGroup(book, b, 4, copy: false);

        Assert.Equal(["a", "c"], Texts(book));
        Assert.Equal(["x", "b"], Texts(book, 4));
        Assert.Equal(b, book[4].Snippets[1].Id); // 移動は Id を保つ
        Assert.Equal(Id(book, 1), r.Select); // c
    }

    [Fact]
    public void 他のグループへの複製は新しいIdで_元を選んだまま()
    {
        var book = Book("a");
        var a = Id(book, 0);

        var r = TemplateEditing.MoveToGroup(book, a, 2, copy: true);

        Assert.Equal(["a"], Texts(book));
        Assert.Equal(["a"], Texts(book, 2));
        Assert.NotEqual(a, Id(book, 0, key: 2));
        Assert.Equal(a, r.Select);
    }

    [Fact]
    public void 同じグループや満杯のグループへは移さない()
    {
        var book = Book("a");
        for (int i = 0; i < TemplateBook.MaxSnippetsPerGroup; i++) book[2].Snippets.Add(new Snippet { Text = "x" });

        Assert.Equal(EditStatus.NoChange, TemplateEditing.MoveToGroup(book, Id(book, 0), 1, copy: false).Status);
        Assert.Equal(EditStatus.GroupFull, TemplateEditing.MoveToGroup(book, Id(book, 0), 2, copy: false).Status);
        Assert.Equal(["a"], Texts(book));
    }

    [Fact]
    public void グループを空にすると名前は残る()
    {
        var book = Book("a", "b");
        book[1].Name = "挨拶";

        Assert.Equal(EditStatus.Ok, TemplateEditing.ClearGroup(book, 1).Status);
        Assert.Empty(book[1].Snippets);
        Assert.Equal("挨拶", book[1].Name);
        Assert.Equal(EditStatus.NoChange, TemplateEditing.ClearGroup(book, 1).Status);
    }

    [Fact]
    public void 名前は20文字に切り詰め_同じなら変えない()
    {
        var book = Book();

        Assert.Equal(EditStatus.Ok, TemplateEditing.Rename(book, 3, new string('a', 25)).Status);
        Assert.Equal(TemplateBook.MaxGroupNameLength, book[3].Name.Length);
        Assert.Equal(EditStatus.NoChange, TemplateEditing.Rename(book, 3, new string('a', 20)).Status);
        Assert.Equal(EditStatus.Ok, TemplateEditing.Rename(book, 3, null).Status);
        Assert.Equal("", book[3].Name);
    }

    [Fact]
    public void 本文の更新は改行をそろえ_長すぎれば切り詰めて反映する()
    {
        var book = Book("a");
        var id = Id(book, 0);

        Assert.Equal(EditStatus.Ok, TemplateEditing.UpdateText(book, id, "x\r\ny").Status);
        Assert.Equal("x\ny", book[1].Snippets[0].Text);
        Assert.Equal(EditStatus.NoChange, TemplateEditing.UpdateText(book, id, "x\ny").Status);

        var r = TemplateEditing.UpdateText(book, id, new string('z', TemplateBook.MaxSnippetLength + 10));
        Assert.Equal(EditStatus.TooLong, r.Status);
        Assert.True(r.Changed);
        Assert.Equal(TemplateBook.MaxSnippetLength, book[1].Snippets[0].Text.Length);
        Assert.Equal(EditStatus.NotFound, TemplateEditing.UpdateText(book, Guid.NewGuid(), "q").Status);
    }

    [Fact]
    public void 空白と改行だけの定型文をすべてのグループから取り除く()
    {
        var book = Book("a", "", " \n\t");
        book[5].Snippets.Add(new Snippet { Text = "  " });

        Assert.Equal(3, TemplateEditing.RemoveEmpty(book));
        Assert.Equal(["a"], Texts(book));
        Assert.Empty(book[5].Snippets);
        Assert.Equal(0, TemplateEditing.RemoveEmpty(book));
    }

    [Fact]
    public void Findはグループと位置を返す()
    {
        var book = Book("a", "b");

        var found = TemplateEditing.Find(book, Id(book, 1));
        Assert.NotNull(found);
        Assert.Equal((1, 1), (found!.Value.Group.Key, found.Value.Index));
        Assert.Null(TemplateEditing.Find(book, Guid.NewGuid()));
    }
}

/// <summary>1 段階の元に戻す（SPEC §6.3）。</summary>
public class TemplateHistoryTests
{
    [Fact]
    public void 控えてから操作し_戻すと順もIdも名前も元どおりで選択も戻る()
    {
        var book = TemplateBook.CreateEmpty();
        book[1].Name = "挨拶";
        foreach (var t in new[] { "a", "b", "c" }) book[1].Snippets.Add(new Snippet { Text = t });
        var before = book[1].Snippets.Select(s => s.Id).ToArray();
        var selected = before[1];
        var history = new TemplateHistory();

        history.Record(book, selected);
        TemplateEditing.Delete(book, selected);
        TemplateEditing.ClearGroup(book, 1); // 1 段階なので、ここで控えを取り直さなければ Delete の前まで戻る

        Assert.True(history.CanUndo);
        Assert.True(history.Undo(book, out var reselect));
        Assert.Equal(before, book[1].Snippets.Select(s => s.Id));
        Assert.Equal("挨拶", book[1].Name);
        Assert.Equal(selected, reselect);
    }

    [Fact]
    public void 戻すのは1回だけ_捨てた後は戻せない()
    {
        var book = TemplateBook.CreateEmpty();
        book[1].Snippets.Add(new Snippet { Text = "a" });
        var history = new TemplateHistory();

        Assert.False(history.Undo(book, out _));
        history.Record(book, null);
        TemplateEditing.ClearGroup(book, 1);
        Assert.True(history.Undo(book, out _));
        Assert.False(history.Undo(book, out _));
        Assert.Single(book[1].Snippets);

        history.Record(book, null);
        history.Discard();
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void 戻したあとの本は控えと別物()
    {
        var book = TemplateBook.CreateEmpty();
        book[1].Snippets.Add(new Snippet { Text = "a" });
        var history = new TemplateHistory();
        history.Record(book, null);
        history.Undo(book, out _);
        history.Record(book, null);

        book[1].Snippets[0].Text = "changed";
        history.Undo(book, out _);

        Assert.Equal("a", book[1].Snippets[0].Text);
    }
}
