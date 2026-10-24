using System.Diagnostics;

namespace FileLauncher.Core.Items;

/// <summary>Keep dropped file names out of shell source text.</summary>
public static class ShellCommand
{
    public static ProcessStartInfo Posix(string target, string args, IReadOnlyList<string>? paths)
    {
        var start = new ProcessStartInfo("/bin/zsh") { UseShellExecute = false };
        start.ArgumentList.Add("-lc");
        start.ArgumentList.Add(target + " " + Substitute(args, paths is { Count: > 0 } ? "\"$@\"" : ""));
        start.ArgumentList.Add("CPFileLauncher"); // $0, followed by the file names as positional parameters
        foreach (var path in paths ?? []) start.ArgumentList.Add(path);
        return start;
    }

    public static ProcessStartInfo Windows(string shell, string target, string args, IReadOnlyList<string>? paths)
    {
        var start = new ProcessStartInfo(shell) { UseShellExecute = false };
        var references = new List<string>();
        foreach (var path in paths ?? [])
        {
            // Windows file names cannot contain these. Do not allow a malformed imported path
            // to escape the quotes after cmd expands the environment variable.
            if (path.IndexOfAny(['"', '\r', '\n', '\0']) >= 0)
                throw new ArgumentException("Invalid dropped file name", nameof(paths));
            var name = "CPFL_DROP_" + Guid.NewGuid().ToString("N");
            start.Environment[name] = path;
            references.Add("\"%" + name + "%\"");
        }
        // Disable AutoRun and delayed expansion. Percent expansion does not recursively
        // expand percent signs contained in an environment variable's value.
        start.Arguments = "/d /v:off /s /c \"" + target + " " + Substitute(args, string.Join(" ", references)) + "\"";
        return start;
    }

    private static string Substitute(string args, string paths)
    {
        args ??= "";
        if (!args.Contains("%1", StringComparison.Ordinal) && !args.Contains("$1", StringComparison.Ordinal))
            return (args + " " + paths).Trim();
        // Replace in one pass, so a replacement containing $1 is never interpreted again.
        return System.Text.RegularExpressions.Regex.Replace(args, "\"[%$]1\"|'[%$]1'|[%$]1", _ => paths).Trim();
    }
}
