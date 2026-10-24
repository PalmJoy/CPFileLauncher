using Avalonia.Headless.XUnit;
using CPClipboard.App;
using CPClipboard.Core.Model;
using CPClipboard.Core.Storage;

namespace CPClipboard.Desktop.Tests;

/// <summary>定型文の変更の入口（SPEC §6.3 / §6.4）: 元に戻すの控え・通知・保存。</summary>
public sealed class TemplatesHubTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CPClipboardUiTests", Guid.NewGuid().ToString("N"));
    private readonly DataPaths _paths;
    private readonly TemplatesHub _hub;
    private int _changed;

    public TemplatesHubTests()
    {
        Directory.CreateDirectory(_dir);
        _paths = new DataPaths(_dir, IsPortable: false);
        var store = new AppDataStore(_paths);
        var (_, templates) = store.LoadAll();
        var book = templates.Value;
        foreach (var t in new[] { "a", "b" }) book[1].Snippets.Add(new Snippet { Text = t });
        _hub = new TemplatesHub(store, book);
        _hub.Changed += () => _changed++;
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private Guid Id(int index) => _hub.Current[1].Snippets[index].Id;

    [AvaloniaFact]
    public void 一覧操作は元に戻せて_戻すと選択も返る()
    {
        var a = Id(0);
        var r = _hub.Edit(b => TemplateEditing.Delete(b, a), selected: a, undoable: true);

        Assert.Equal(EditStatus.Ok, r.Status);
        Assert.True(_hub.CanUndo);
        Assert.Equal(1, _changed);
        Assert.True(_hub.Undo(out var selected));
        Assert.Equal(a, selected);
        Assert.Equal(["a", "b"], _hub.Current[1].Snippets.Select(s => s.Text));
        Assert.Equal(2, _changed);
    }

    [AvaloniaFact]
    public void 本文や名前を編集すると元に戻すは使えなくなる()
    {
        var a = Id(0);
        _hub.Edit(b => TemplateEditing.Duplicate(b, a), a, undoable: true);
        _hub.Edit(b => TemplateEditing.UpdateText(b, a, "edited"), a, undoable: false);

        Assert.False(_hub.CanUndo);
        Assert.False(_hub.Undo(out _));
        Assert.Equal("edited", _hub.Current[1].Snippets[0].Text);
    }

    [AvaloniaFact]
    public void 変わらなかった操作は控えも通知も保存もしない()
    {
        var a = Id(0);
        _hub.Edit(b => TemplateEditing.Delete(b, a), a, undoable: true);
        _hub.Edit(b => TemplateEditing.Move(b, Id(0), 0), Id(0), undoable: true); // NoChange
        _hub.Edit(b => TemplateEditing.UpdateText(b, Id(0), "b"), Id(0), undoable: false); // NoChange

        Assert.True(_hub.CanUndo); // Delete の控えのまま
        Assert.Equal(1, _changed);
    }

    [AvaloniaFact]
    public void 空の定型文の除去は控えを消さない_保存すると書かれる()
    {
        var a = Id(0);
        _hub.Edit(b => TemplateEditing.Add(b, 1, a), a, undoable: true);
        Assert.Equal(1, _hub.RemoveEmpty());
        Assert.True(_hub.CanUndo);

        _hub.Flush();
        var saved = new AppDataStore(_paths).LoadAll().Templates.Value;
        Assert.Equal(["a", "b"], saved[1].Snippets.Select(s => s.Text));
    }
}
