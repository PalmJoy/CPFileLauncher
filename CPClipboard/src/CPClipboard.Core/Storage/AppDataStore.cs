using System.Text.Json.Nodes;
using CPClipboard.Core.Model;

namespace CPClipboard.Core.Storage;

/// <summary>settings.json と templates.json をまとめて扱う入口。</summary>
public sealed class AppDataStore
{
    public DataPaths Paths { get; }
    public VersionedJsonStore<AppSettings> SettingsStore { get; }
    public VersionedJsonStore<TemplateBook> TemplatesStore { get; }

    /// <summary>templates.json のバックアップ世代数は設定値に従うため、読み込んだ設定を参照する。</summary>
    private AppSettings? _settings;

    public AppDataStore(DataPaths paths, TimeProvider? time = null)
    {
        Paths = paths;
        SettingsStore = new VersionedJsonStore<AppSettings>(
            paths.SettingsFile,
            AppSettings.CurrentSchemaVersion,
            createDefault: () => new AppSettings(),
            migrateOneStep: MigrateSettings,
            normalize: s => s.Normalize(),
            time: time);

        TemplatesStore = new VersionedJsonStore<TemplateBook>(
            paths.TemplatesFile,
            TemplateBook.CurrentSchemaVersion,
            createDefault: TemplateBook.CreateEmpty,
            backupDirectory: paths.BackupDirectory,
            backupGenerations: () => _settings?.Data.BackupGenerations ?? 10,
            migrateOneStep: MigrateTemplates,
            normalize: b => b.Normalize(),
            time: time,
            backupMinInterval: TemplatesBackupInterval);
    }

    /// <summary>定型文の世代バックアップの最短間隔（編集のたびに保存されるため。SPEC §6.4）。</summary>
    public static readonly TimeSpan TemplatesBackupInterval = TimeSpan.FromMinutes(10);

    /// <summary>設定 → 定型文の順に読む（バックアップ世代数が設定に依存するため）。</summary>
    public (LoadResult<AppSettings> Settings, LoadResult<TemplateBook> Templates) LoadAll()
    {
        Paths.EnsureCreated();
        var settings = SettingsStore.Load();
        _settings = settings.Value;
        var templates = TemplatesStore.Load();
        return (settings, templates);
    }

    public void SaveSettings(AppSettings settings)
    {
        if (SavingSuspended) return;
        _settings = settings;
        SettingsStore.Save(settings);
    }

    public void SaveTemplates(TemplateBook templates)
    {
        if (SavingSuspended) return;
        TemplatesStore.Save(templates);
    }

    /// <summary>インポートで差し替えた後は、再起動までこのプロセスから保存しない（終了処理の保存で差し替えを上書きしないため）。</summary>
    public bool SavingSuspended { get; private set; }

    /// <summary>
    /// エクスポートした zip の中身で settings.json / templates.json を差し替える（入っているほうだけ）。
    /// 差し替え前のファイルは backup/ に import-before-*.json として残す（定型文の世代バックアップとは別名）。
    /// 呼んだ後は保存を止めるので、呼び出し側はすぐ再起動すること。
    /// </summary>
    public void Import(ArchiveContents contents, DateTime localNow)
    {
        Paths.EnsureCreated();
        Directory.CreateDirectory(Paths.BackupDirectory);
        string stamp = localNow.ToString("yyyyMMdd-HHmmss");
        foreach (var (text, file, name) in new[] { (contents.Settings, Paths.SettingsFile, "settings"), (contents.Templates, Paths.TemplatesFile, "templates") })
        {
            if (text is null) continue;
            if (File.Exists(file)) File.Copy(file, Path.Combine(Paths.BackupDirectory, $"import-before-{name}-{stamp}.json"), overwrite: true);
            string tmp = file + ".tmp";
            File.WriteAllText(tmp, text);
            File.Move(tmp, file, overwrite: true);
        }
        if (contents.Files is { Count: > 0 } files)
        {
            // 背景画像: background/ は 1 枚だけなので、入れ替える前に今の中身を消す
            if (Directory.Exists(Paths.BackgroundDirectory))
                foreach (var old in Directory.GetFiles(Paths.BackgroundDirectory)) File.Delete(old);
            Directory.CreateDirectory(Paths.BackgroundDirectory);
            foreach (var (key, bytes) in files)
                File.WriteAllBytes(Path.Combine(Paths.BackgroundDirectory, Path.GetFileName(key)), bytes);
        }
        SavingSuspended = true;
    }

    // schemaVersion を上げるときはここに 1 段ずつ追加する
    private static JsonObject MigrateSettings(JsonObject data, int fromVersion) =>
        throw new InvalidDataException($"settings.json: no migration from schemaVersion {fromVersion}");

    private static JsonObject MigrateTemplates(JsonObject data, int fromVersion) =>
        throw new InvalidDataException($"templates.json: no migration from schemaVersion {fromVersion}");
}
