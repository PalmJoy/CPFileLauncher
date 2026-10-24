using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CPClipboard.Tests;

/// <summary>
/// 配布物に同梱する THIRD-PARTY-NOTICES.txt が依存と食い違っていないこと（SPEC §10.4「ライセンス文の同梱」）。
/// 依存の版を上げたら notices とワークフローの libuiohook のコミットも直す。
/// </summary>
public sealed partial class ThirdPartyNoticesTests
{
    private static readonly string[] ProductProjects =
    [
        "src/CPClipboard.Core/CPClipboard.Core.csproj",
        "src/CPClipboard.Platform/CPClipboard.Platform.csproj",
        "src/CPClipboard.Platform.Windows/CPClipboard.Platform.Windows.csproj",
        "src/CPClipboard.Platform.MacOS/CPClipboard.Platform.MacOS.csproj",
        "src/CPClipboard.Desktop/CPClipboard.Desktop.csproj",
    ];

    [GeneratedRegex(@"^\s*LIBUIOHOOK_COMMIT:\s*([0-9a-f]{40})\s*$", RegexOptions.Multiline)]
    private static partial Regex WorkflowCommit();

    [GeneratedRegex(@"^\s*Commit:\s*([0-9a-f]{40})\s*$", RegexOptions.Multiline)]
    private static partial Regex NoticesCommit();

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "src", "CPClipboard.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static string Notices() => File.ReadAllText(Path.Combine(RepoRoot(), "THIRD-PARTY-NOTICES.txt"));

    [Fact]
    public void 配布物に入るパッケージの名前と版がすべて載っている()
    {
        string root = RepoRoot();
        string notices = Notices();
        var missing = new List<string>();
        foreach (string rel in ProductProjects)
        {
            string path = Path.Combine(root, rel);
            if (!File.Exists(path)) continue;
            foreach (var pr in XDocument.Load(path).Descendants("PackageReference"))
            {
                // Debug だけのもの・ビルド時だけのもの（PrivateAssets="all"）は配布物に入らない
                if (((string?)pr.Attribute("Condition"))?.Contains("Debug", StringComparison.Ordinal) == true) continue;
                if (string.Equals((string?)pr.Attribute("PrivateAssets"), "all", StringComparison.OrdinalIgnoreCase)) continue;
                string name = (string)pr.Attribute("Include")!;
                string version = (string?)pr.Attribute("Version") ?? "";
                if (!notices.Contains(name, StringComparison.Ordinal) || !notices.Contains(version, StringComparison.Ordinal))
                    missing.Add($"{name} {version}（{rel}）");
            }
        }
        Assert.True(missing.Count == 0, "THIRD-PARTY-NOTICES.txt に載っていません:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void libuiohookのコミットがリリースのワークフローと同じ()
    {
        string workflow = File.ReadAllText(Path.Combine(RepoRoot(), "..", ".github", "workflows", "release-clipboard.yml"));
        var inWorkflow = WorkflowCommit().Match(workflow);
        var inNotices = NoticesCommit().Match(Notices());
        Assert.True(inWorkflow.Success, "release.yml に LIBUIOHOOK_COMMIT がありません");
        Assert.True(inNotices.Success, "THIRD-PARTY-NOTICES.txt に libuiohook の Commit がありません");
        Assert.Equal(inNotices.Groups[1].Value, inWorkflow.Groups[1].Value);
    }

    [Fact]
    public void LGPLの全文と差し替え方が載っている()
    {
        string notices = Notices();
        Assert.Contains("GNU LESSER GENERAL PUBLIC LICENSE", notices);
        Assert.Contains("uiohook.dll", notices);
        Assert.Contains("libuiohook.dylib", notices);
        Assert.Contains("libuiohook-source.zip", notices);
    }
}
