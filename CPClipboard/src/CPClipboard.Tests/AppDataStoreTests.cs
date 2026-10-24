using System.Text.Json.Nodes;
using CPClipboard.Core.Model;
using CPClipboard.Core.Storage;

namespace CPClipboard.Tests;

public class AppDataStoreTests : IDisposable
{
    private readonly TempDir _dir = new();
    private readonly FakeTime _time = new(new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero));

    public void Dispose() => _dir.Dispose();

    private AppDataStore NewStore() => new(new DataPaths(_dir.Path, IsPortable: false), _time);

    private static TemplateBook Book(string firstText)
    {
        var book = TemplateBook.CreateEmpty();
        book[1].Name = "仕事";
        book[1].Snippets.Add(new Snippet { Text = firstText });
        return book;
    }

    [Fact]
    public void ファイルが無ければ既定値を返し_まだ書き込まない()
    {
        var (settings, templates) = NewStore().LoadAll();

        Assert.Equal(LoadStatus.CreatedDefault, settings.Status);
        Assert.Equal(LoadStatus.CreatedDefault, templates.Status);
        Assert.Equal(10, templates.Value.Groups.Count);
        Assert.All(templates.Value.Groups, g => Assert.Empty(g.Snippets));
        Assert.False(File.Exists(_dir.File("templates.json")));
        Assert.False(File.Exists(_dir.File("settings.json")));
    }

    [Fact]
    public void 保存して読み直すと全属性が戻る()
    {
        var store = NewStore();
        store.LoadAll();
        var book = Book("いつもお世話になっております。\n株式会社〇〇です。");
        var id = book[1].Snippets[0].Id;
        store.SaveTemplates(book);

        var (_, loaded) = NewStore().LoadAll();

        Assert.Equal(LoadStatus.Loaded, loaded.Status);
        Assert.Equal("仕事", loaded.Value[1].Name);
        var got = Assert.Single(loaded.Value[1].Snippets);
        Assert.Equal(id, got.Id);
        Assert.Equal("いつもお世話になっております。\n株式会社〇〇です。", got.Text);
    }

    [Fact]
    public void JSONは人間可読_camelCase_日本語はそのまま()
    {
        var store = NewStore();
        store.LoadAll();
        store.SaveTemplates(Book("メモ"));

        string json = File.ReadAllText(_dir.File("templates.json"));

        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"snippets\":", json);
        Assert.Contains("\"key\": 9", json);
        Assert.Contains("メモ", json);
        Assert.Contains("\n", json); // インデントあり
    }

    [Fact]
    public void 保存後に一時ファイルが残らない()
    {
        var store = NewStore();
        store.LoadAll();
        store.SaveTemplates(TemplateBook.CreateEmpty());
        store.SaveTemplates(TemplateBook.CreateEmpty());

        Assert.Empty(Directory.GetFiles(_dir.Path, "*.tmp"));
    }

    [Fact]
    public void 保存のたびに直前の内容を世代バックアップし_設定の世代数を超えた分は消す()
    {
        var store = NewStore();
        var s = new AppSettings();
        s.Data.BackupGenerations = 3;
        store.SaveSettings(s);
        store.LoadAll();

        for (int i = 0; i < 6; i++)
        {
            store.SaveTemplates(Book($"v{i}"));
            _time.Advance(AppDataStore.TemplatesBackupInterval); // 定型文のバックアップは 10 分おき
        }

        var backups = Directory.GetFiles(_dir.File("backup"), "templates-*.json").OrderBy(f => f).ToList();
        Assert.Equal(3, backups.Count);
        // 初回保存時は本体が無いのでバックアップ無し → v0..v4 のうち新しい 3 つ（v2, v3, v4）が残る
        Assert.Contains("\"v2\"", File.ReadAllText(backups[0]));
        Assert.Contains("\"v4\"", File.ReadAllText(backups[2]));
    }

    [Fact]
    public void 同じミリ秒に2回保存してもバックアップが上書きされない()
    {
        // 時刻名の衝突を見るため、間隔を空けない（既定 0）ストアを直接使う
        var store = new VersionedJsonStore<TemplateBook>(_dir.File("templates.json"), TemplateBook.CurrentSchemaVersion, TemplateBook.CreateEmpty,
            backupDirectory: _dir.File("backup"), time: _time);
        store.Save(TemplateBook.CreateEmpty());
        store.Save(TemplateBook.CreateEmpty());
        store.Save(TemplateBook.CreateEmpty());

        Assert.Equal(2, Directory.GetFiles(_dir.File("backup"), "templates-*.json").Length);
    }

    [Fact]
    public void 壊れた本体は退避して最新の正常なバックアップから復旧する()
    {
        var store = NewStore();
        store.LoadAll();
        foreach (var text in new[] { "old", "good", "latest" })
        {
            store.SaveTemplates(Book(text));
            _time.Advance(AppDataStore.TemplatesBackupInterval);
        }
        // 最新バックアップ（= "good"）も壊す → その前の "old" から復旧されるはず
        var newestBackup = Directory.GetFiles(_dir.File("backup")).OrderBy(f => f).Last();
        File.WriteAllText(newestBackup, "{ broken");
        File.WriteAllText(_dir.File("templates.json"), "{ \"groups\": [ 壊れた");

        var (_, templates) = NewStore().LoadAll();

        Assert.Equal(LoadStatus.RecoveredFromBackup, templates.Status);
        Assert.Equal("old", templates.Value[1].Snippets[0].Text);
        Assert.Single(Directory.GetFiles(_dir.Path, "templates.json.broken-*"));
        // 本体も復旧内容で書き直されている
        Assert.Equal(LoadStatus.Loaded, NewStore().LoadAll().Templates.Status);
    }

    [Fact]
    public void 壊れていてバックアップも無ければ既定値_壊れたファイルは残す()
    {
        File.WriteAllText(_dir.File("templates.json"), "not json");

        var (_, templates) = NewStore().LoadAll();

        Assert.Equal(LoadStatus.CorruptReplacedWithDefault, templates.Status);
        Assert.Equal(10, templates.Value.Groups.Count);
        var broken = Assert.Single(Directory.GetFiles(_dir.Path, "templates.json.broken-*"));
        Assert.Equal("not json", File.ReadAllText(broken));
    }

    [Fact]
    public void 新しい版のデータは読み取り専用にして上書きしない()
    {
        const string newer = """{ "schemaVersion": 99, "groups": [], "futureField": true }""";
        File.WriteAllText(_dir.File("templates.json"), newer);
        var store = NewStore();

        var (_, templates) = store.LoadAll();

        Assert.Equal(LoadStatus.NewerSchemaReadOnly, templates.Status);
        Assert.True(store.TemplatesStore.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => store.SaveTemplates(templates.Value));
        Assert.Equal(newer, File.ReadAllText(_dir.File("templates.json")));
    }

    [Fact]
    public void schemaVersionが無い手書きJSONはv1として読む_コメントと末尾カンマも許す()
    {
        File.WriteAllText(_dir.File("templates.json"), """
            {
              // 手編集
              "groups": [ { "key": 3, "name": "手書き", "snippets": [ { "text": "a\r\nb" }, ], }, ],
            }
            """);

        var (_, templates) = NewStore().LoadAll();

        Assert.Equal(LoadStatus.Loaded, templates.Status);
        Assert.Equal(10, templates.Value.Groups.Count);
        Assert.Equal("手書き", templates.Value[3].Name);
        var snippet = Assert.Single(templates.Value[3].Snippets);
        Assert.NotEqual(Guid.Empty, snippet.Id);
        Assert.Equal("a\nb", snippet.Text);
    }

    [Fact]
    public void 設定の既定値はSPECどおり()
    {
        var s = NewStore().LoadAll().Settings.Value;

        Assert.Equal(1, s.SchemaVersion);
        Assert.True(s.Triggers.DoubleTap.Enabled);
        Assert.Equal((DoubleTapModifier.Ctrl, 300), (s.Triggers.DoubleTap.Modifier, s.Triggers.DoubleTap.IntervalMs));
        Assert.False(s.Triggers.Hotkey.Enabled);
        Assert.Equal(KeyModifiers.Ctrl | KeyModifiers.Alt, s.Triggers.Hotkey.Modifiers);
        Assert.Equal("V", s.Triggers.Hotkey.Key);
        var mouse = Assert.Single(s.Triggers.Mouse);
        Assert.Equal((MouseButtonKind.Middle, KeyModifiers.Alt, true),
            (mouse.Button, mouse.Modifiers, mouse.Enabled));
        Assert.Equal((420, 12, 13), (s.Appearance.BoardWidth, s.Appearance.MaxRows, s.Appearance.FontSize));
        Assert.True(s.Board.Preview && s.Board.WheelSwitchesGroup);
        Assert.Equal(1, s.Board.LastGroup);
        Assert.True(s.Paste.AutoPaste);
        Assert.False(s.Paste.RestoreClipboard);
        Assert.Equal(500, s.Paste.RestoreDelayMs);
        Assert.Equal((PopupPosition.Cursor, PopupPosition.Cursor), (s.Popup.Keyboard.Position, s.Popup.Mouse.Position));
        Assert.Null(s.Popup.Keyboard.X);
        Assert.True(s.Popup.CloseOnOutsideClick && s.Popup.ToggleOnTrigger);
        Assert.False(s.Popup.CloseOnMouseLeave);
        Assert.Equal(10, s.Data.BackupGenerations);
    }

    [Fact]
    public void 修飾キーのフラグはJSONで読める形になる()
    {
        var store = NewStore();
        store.LoadAll();
        store.SaveSettings(new AppSettings());

        var node = JsonNode.Parse(File.ReadAllText(_dir.File("settings.json")))!;

        Assert.Equal("ctrl, alt", node["triggers"]!["hotkey"]!["modifiers"]!.GetValue<string>());
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void 明示保存したクリップボード復元設定は既定変更後も維持する(bool restore)
    {
        var store = NewStore();
        var settings = store.LoadAll().Settings.Value;
        settings.Paste.RestoreClipboard = restore;
        store.SaveSettings(settings);
        Assert.Equal(restore, NewStore().LoadAll().Settings.Value.Paste.RestoreClipboard);
    }

}

public class AppSettingsTests
{
    [Fact]
    public void 盤面の不透明度の既定は80パーセント()
    {
        Assert.Equal(80, new AppSettings().Appearance.Opacity);
    }

    [Theory]
    [InlineData(100, 280, 2, 4, 5, 11)]
    [InlineData(431, 440, 13, 13, 30, 20)]
    [InlineData(9999, 800, 99, 30, 99, 20)]
    public void 盤面の大きさと行は範囲に丸める(int width, int expectedWidth, int rows, int expectedRows, int font, int expectedFont)
    {
        var s = new AppSettings();
        s.Appearance.BoardWidth = width;
        s.Appearance.MaxRows = rows;
        s.Appearance.FontSize = font;

        s.Normalize();

        Assert.Equal((expectedWidth, expectedRows, expectedFont), (s.Appearance.BoardWidth, s.Appearance.MaxRows, s.Appearance.FontSize));
    }

    [Theory]
    [InlineData(-3, 0)]
    [InlineData(12, 9)]
    public void 前回のグループは0から9に丸める(int saved, int expected)
    {
        var s = new AppSettings();
        s.Board.LastGroup = saved;
        s.Normalize();
        Assert.Equal(expected, s.Board.LastGroup);
    }

    [Theory]
    [InlineData(10, 30)]
    [InlineData(30, 30)]
    [InlineData(62, 60)]
    [InlineData(63, 65)]
    [InlineData(100, 100)]
    [InlineData(150, 100)]
    public void 不透明度は30から100の5刻みに丸める(int saved, int expected)
    {
        var s = new AppSettings();
        s.Appearance.Opacity = saved;

        s.Normalize();

        Assert.Equal(expected, s.Appearance.Opacity);
    }


}
