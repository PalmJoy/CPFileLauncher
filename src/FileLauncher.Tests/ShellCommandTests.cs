using System.Diagnostics;
using FileLauncher.Core.Items;

namespace FileLauncher.Tests;

public sealed class ShellCommandTests
{
    [Theory]
    [InlineData("")]
    [InlineData("%1")]
    [InlineData("\"%1\"")]
    [InlineData("$1")]
    [InlineData("\"$1\"")]
    [InlineData("'%1'")]
    public async Task Posix_dropped_names_are_literal_arguments(string template)
    {
        if (!OperatingSystem.IsMacOS() && !File.Exists("/bin/zsh")) return;
        using var dir = new TempDir();
        string marker = dir.File("injected");
        string[] paths = ["/tmp/$(touch '" + marker + "').txt", "`touch injected`", "a\";touch injected;#", "a'b\\c $1 %1", "a\nb"];
        var start = ShellCommand.Posix("/usr/bin/printf '%s\\0'", template, paths);
        start.WorkingDirectory = dir.Path;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        using var process = Process.Start(start)!;
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(0, process.ExitCode);
        Assert.Equal(string.Join('\0', paths) + '\0', output);
        Assert.False(File.Exists(marker));
    }

    [Fact]
    public async Task Windows_dropped_names_do_not_expand_variables_or_commands()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var dir = new TempDir();
        string path = dir.File("a%CPFL_ATTACK% & b!.txt");
        File.WriteAllText(path, "literal file contents");
        var start = ShellCommand.Windows(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), "type", "\"%1\"", [path]);
        start.Environment["CPFL_ATTACK"] = "EXPANDED";
        start.RedirectStandardOutput = true;
        using var process = Process.Start(start)!;
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(0, process.ExitCode);
        Assert.Equal("literal file contents", output);
    }

    [Fact]
    public void Windows_rejects_paths_that_could_break_quoting()
    {
        Assert.Throws<ArgumentException>(() => ShellCommand.Windows("cmd.exe", "echo", "", ["x\" & echo injected"]));
        Assert.Throws<ArgumentException>(() => ShellCommand.Windows("cmd.exe", "echo", "", ["x\ny"]));
    }

    [Fact]
    public async Task Direct_arguments_preserve_quotes_backslashes_and_placeholder_text()
    {
        if (!OperatingSystem.IsMacOS() && !File.Exists("/usr/bin/printf")) return;
        string[] paths = ["a\"b", "a\\", "a\\\"b", "$1 %1"];
        var start = new ProcessStartInfo("/usr/bin/printf")
        {
            Arguments = "%s\\0 " + LaunchArgs.Build("%1", paths),
            RedirectStandardOutput = true,
        };
        using var process = Process.Start(start)!;
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.Equal(string.Join('\0', paths) + '\0', output);
    }
}
