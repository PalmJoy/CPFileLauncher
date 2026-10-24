using CPClipboard.Core.Storage;

namespace CPClipboard.Tests;

public class DataPathsTests
{
    [Fact]
    public void portable_flagがあれば実行ファイル隣のdataを使う()
    {
        using var dir = new TempDir();
        File.WriteAllText(dir.File("portable.flag"), "");

        var paths = DataPaths.Resolve(dir.Path);

        Assert.True(paths.IsPortable);
        Assert.Equal(dir.File("data"), paths.Root);
        Assert.Equal(Path.Combine(dir.Path, "data", "templates.json"), paths.TemplatesFile);
    }

    [Fact]
    public void portable_flagが無ければユーザーデータ領域のCPClipboardフォルダ()
    {
        using var dir = new TempDir();
        using var userData = new TempDir(); // 本物のユーザーデータ領域には触れない（2026-10-04 にテストが本物の旧フォルダを改名した）

        var paths = DataPaths.Resolve(dir.Path, userData.Path);

        Assert.False(paths.IsPortable);
        Assert.Equal("CPClipboard", Path.GetFileName(paths.Root));
        Assert.Equal(userData.Path, Path.GetDirectoryName(paths.Root));
    }

    [Fact]
    public void portableflagは実行ファイルCPClipboardの隣を探し_dotnetで動かしたときはアプリのディレクトリを探す()
    {
        // Windows では GetDirectoryName が "/" を "\" に直すので、区切りは実行中の OS のものを使う
        string app = Path.Combine(Path.DirectorySeparatorChar + "Applications", "CPClipboard.app", "Contents", "MacOS");
        Assert.Equal(app, DataPaths.ExecutableDirectory(Path.Combine(app, "CPClipboard"), "/other"));
        Assert.Equal(@"C:\Tools", DataPaths.ExecutableDirectory(@"C:\Tools\CPClipboard.exe".Replace('\\', Path.DirectorySeparatorChar), @"C:\Tools").Replace(Path.DirectorySeparatorChar, '\\'));
        Assert.Equal("/repo/bin", DataPaths.ExecutableDirectory("/usr/local/share/dotnet/dotnet", "/repo/bin"));
        Assert.Equal("/repo/bin", DataPaths.ExecutableDirectory(null, "/repo/bin"));
    }

}
