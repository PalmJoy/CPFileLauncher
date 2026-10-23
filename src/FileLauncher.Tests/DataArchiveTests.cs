using System.IO.Compression;
using System.Text;
using FileLauncher.Core.Model;
using FileLauncher.Core.Storage;

namespace FileLauncher.Tests;

public sealed class DataArchiveTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "FileLauncherTests", Guid.NewGuid().ToString("N"));
    private readonly DataPaths _paths;

    public DataArchiveTests()
    {
        _paths = new DataPaths(Path.Combine(_dir, "data"), IsPortable: false);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private AppDataStore Store()
    {
        var store = new AppDataStore(_paths);
        store.LoadAll();
        return store;
    }

    private static MemoryStream Zip(params (string Name, string Text)[] entries)
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, text) in entries)
            {
                using var w = new StreamWriter(zip.CreateEntry(name).Open(), Encoding.UTF8);
                w.Write(text);
            }
        }
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void エクスポートしたzipを別のデータフォルダへインポートすると設定と盤面が移る()
    {
        var store = Store();
        var settings = new AppSettings();
        settings.Appearance.Opacity = 55;
        store.SaveSettings(settings);
        var board = Board.CreateDefault();
        board.Pages[0].Name = "仕事";
        store.SaveBoard(board);

        using var zip = new MemoryStream();
        DataArchive.Export(_paths, zip);
        zip.Position = 0;

        var otherPaths = new DataPaths(Path.Combine(_dir, "other"), IsPortable: false);
        var other = new AppDataStore(otherPaths);
        var (os, ob) = other.LoadAll();
        other.SaveSettings(os.Value); // 差し替え前のファイルがある状態（起動時に既定値を書く）
        other.SaveBoard(ob.Value);
        other.Import(DataArchive.Read(zip), new DateTime(2026, 10, 3, 12, 0, 0));

        var (s, b) = new AppDataStore(otherPaths).LoadAll();
        Assert.Equal(55, s.Value.Appearance.Opacity);
        Assert.Equal("仕事", b.Value.Pages[0].Name);
        Assert.True(File.Exists(Path.Combine(otherPaths.BackupDirectory, "import-before-settings-20261003-120000.json")));
        Assert.True(File.Exists(Path.Combine(otherPaths.BackupDirectory, "import-before-board-20261003-120000.json")));
    }

    [Fact]
    public void インポートの後はこのプロセスから保存しない()
    {
        var store = Store();
        using var zip = Zip((DataArchive.SettingsEntry, """{ "schemaVersion": 2, "appearance": { "opacity": 40 } }"""));
        store.Import(DataArchive.Read(zip), DateTime.Now);

        var stale = new AppSettings(); // 終了処理が古い設定を書こうとしても
        store.SaveSettings(stale);

        Assert.True(store.SavingSuspended);
        Assert.Equal(40, new AppDataStore(_paths).LoadAll().Settings.Value.Appearance.Opacity);
    }

    [Fact]
    public void 片方だけ入ったzipは入っているほうだけ差し替える()
    {
        var store = Store();
        var board = Board.CreateDefault();
        board.Pages[0].Name = "残る";
        store.SaveBoard(board);

        using var zip = Zip(("FileLauncher-backup/settings.json", """{ "schemaVersion": 2 }""")); // フォルダごと圧縮されていても見つける
        store.Import(DataArchive.Read(zip), DateTime.Now);

        Assert.Equal("残る", new AppDataStore(_paths).LoadAll().Board.Value.Pages[0].Name);
    }

    [Fact]
    public void 関係のないzipや壊れたJSONはインポートできない()
    {
        using (var empty = Zip(("readme.txt", "hello")))
            Assert.Equal(ArchiveError.NoKnownEntries, Assert.Throws<ArchiveException>(() => DataArchive.Read(empty)).Kind);
        using (var broken = Zip((DataArchive.BoardEntry, "{ not json")))
            Assert.Equal(ArchiveError.EntryCorrupt, Assert.Throws<ArchiveException>(() => DataArchive.Read(broken)).Kind);
        using (var array = Zip((DataArchive.SettingsEntry, "[1, 2]")))
            Assert.Equal(ArchiveError.EntryNotObject, Assert.Throws<ArchiveException>(() => DataArchive.Read(array)).Kind);
        using (var notZip = new MemoryStream(Encoding.UTF8.GetBytes("plain text")))
            Assert.Equal(ArchiveError.NotZip, Assert.Throws<ArchiveException>(() => DataArchive.Read(notZip)).Kind);
    }
    [Fact]
    public void Oversized_json_is_rejected_before_parsing()
    {
        using var zip = Zip((DataArchive.BoardEntry, "{}" + new string(' ', DataArchive.MaxJsonBytes)));
        Assert.Equal(ArchiveError.TooLarge, Assert.Throws<ArchiveException>(() => DataArchive.Read(zip)).Kind);
    }

    [Fact]
    public void Too_many_entries_are_rejected()
    {
        using var zip = Zip(Enumerable.Range(0, DataArchive.MaxEntries + 1).Select(i => ($"{i}.txt", "")).ToArray());
        Assert.Equal(ArchiveError.TooLarge, Assert.Throws<ArchiveException>(() => DataArchive.Read(zip)).Kind);
    }

    [Fact]
    public void Expanded_total_is_limited_even_for_highly_compressed_files()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            using (var writer = new StreamWriter(zip.CreateEntry("board.json").Open())) writer.Write("{}");
            byte[] chunk = new byte[1024 * 1024];
            for (int i = 0; i < 3; i++)
            {
                using var entry = zip.CreateEntry($"background/{i}.png", CompressionLevel.Optimal).Open();
                for (int j = 0; j < 24; j++) entry.Write(chunk);
            }
        }
        stream.Position = 0;
        Assert.Equal(ArchiveError.TooLarge, Assert.Throws<ArchiveException>(() => DataArchive.Read(stream)).Kind);
    }

    [Fact]
    public void Forged_directory_entry_count_is_rejected_before_zip_parsing()
    {
        using var zip = Zip(("board.json", "{}"), ("background/a.png", "image"));
        byte[] bytes = zip.ToArray();
        // EOCD is the last 22 bytes of this archive (no comment).
        int end = bytes.Length - 22;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(end + 8), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(end + 10), 1);
        using var forged = new MemoryStream(bytes);
        Assert.Equal(ArchiveError.NotZip, Assert.Throws<ArchiveException>(() => DataArchive.Read(forged)).Kind);
    }

    [Fact]
    public void Forged_uncompressed_size_is_bounded_or_rejected()
    {
        using var zip = Zip(("board.json", "{}" + new string(' ', DataArchive.MaxJsonBytes)));
        byte[] bytes = zip.ToArray();
        int end = bytes.Length - 22;
        int directory = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(end + 16));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(directory + 24), 2);
        using var forged = new MemoryStream(bytes);
        var exception = Assert.Throws<ArchiveException>(() => DataArchive.Read(forged));
        Assert.Contains(exception.Kind, new[] { ArchiveError.TooLarge, ArchiveError.NotZip, ArchiveError.EntryCorrupt });
    }

}
