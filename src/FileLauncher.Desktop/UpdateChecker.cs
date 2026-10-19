using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace FileLauncher.App;

internal static class UpdateChecker
{
    internal const string Website = "https://palmjoy.github.io/CPFileLauncher/";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };

    internal static string CurrentVersion => typeof(App).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? typeof(App).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    internal static bool IsNewer(string tag, string current)
    {
        // Only stable numeric release tags are supported. Ignore preview tags and build metadata.
        static Version? Parse(string value) => Version.TryParse(value.TrimStart('v', 'V').Split('+')[0], out var v)
            ? new Version(v.Major, v.Minor, Math.Max(0, v.Build), Math.Max(0, v.Revision)) : null;
        return Parse(tag) is { } latest && Parse(current) is { } installed && latest > installed;
    }

    internal static async Task<string?> CheckAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "https://api.github.com/repos/PalmJoy/CPFileLauncher/releases/latest");
        request.Headers.UserAgent.ParseAdd("CPFileLauncher/" + CurrentVersion);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await Client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = json.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) return null;
        var tag = root.GetProperty("tag_name").GetString() ?? throw new JsonException("Missing release tag");
        return IsNewer(tag, CurrentVersion) ? tag : null;
    }
}
