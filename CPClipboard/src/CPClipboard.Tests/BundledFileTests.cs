using CPClipboard.Core.Storage;

namespace CPClipboard.Tests;

public sealed class BundledFileTests
{
    private const string Name = "THIRD-PARTY-NOTICES.txt";

    [Fact]
    public void 実行ファイルのフォルダにあればそれを返す()
    {
        using var dir = new TempDir();
        string exeDir = Directory.CreateDirectory(Path.Combine(dir.Path, "MacOS")).FullName;
        File.WriteAllText(Path.Combine(exeDir, Name), "x");
        Directory.CreateDirectory(Path.Combine(dir.Path, "Resources"));
        File.WriteAllText(Path.Combine(dir.Path, "Resources", Name), "y");

        Assert.Equal(Path.Combine(exeDir, Name), BundledFile.Find(exeDir, Name));
    }

    [Fact]
    public void 無ければ1つ上のResourcesを探す_macOSのappの形()
    {
        using var dir = new TempDir();
        string exeDir = Directory.CreateDirectory(Path.Combine(dir.Path, "MacOS")).FullName;
        Directory.CreateDirectory(Path.Combine(dir.Path, "Resources"));
        File.WriteAllText(Path.Combine(dir.Path, "Resources", Name), "y");

        Assert.Equal(Path.Combine(dir.Path, "Resources", Name), BundledFile.Find(exeDir, Name));
    }

    [Fact]
    public void どちらにも無ければnull()
    {
        using var dir = new TempDir();
        Assert.Null(BundledFile.Find(dir.Path, Name));
    }
}
