using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FileLauncher.Core.Model;
using FileLauncher.Core.Storage;
using FileLauncher.Platform;

namespace FileLauncher.App;

/// <summary>「CPFileLauncher について」（SPEC §6.3）。バージョン・ライセンス（GPL-3.0-only・第三者）・データフォルダ・同梱フォントのライセンス。</summary>
internal static class AboutWindow
{
    private static Window? _open; // 開いていれば前に出すだけ

    internal static Window Show(IShellService shell, string dataRoot, FolderOpenTarget folderTarget = FolderOpenTarget.System,
        AppearanceSettings? appearance = null, DataPaths? paths = null, IPlatformServices? platform = null)
    {
        if (_open is { } existing) { existing.Activate(); return existing; }

        var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "?";
        int plus = version.IndexOf('+'); // ビルドメタデータ（コミットハッシュ）は省く
        if (plus >= 0) version = version[..plus];
#if DEBUG
        version += " (Debug)";
#endif

        var openData = new Button { Content = Strings.Common_Open };
        openData.Click += (_, _) =>
        {
            var r = shell.Launch(new LauncherItem { Name = "data", Target = dataRoot, Kind = ItemKind.Folder }, null, folderTarget);
            if (!r.Success) Toast.Show(Strings.FormatAbout_OpenDataFailed(Loc.LaunchError(r)));
        };
        var source = Link(Strings.About_Source, () => Open(shell, ItemKind.Url, AppInfo.SourceUrl));
        ToolTip.SetTip(source, AppInfo.SourceUrl);
        var thirdParty = Link(Strings.About_ThirdParty, () =>
        {
            // 同梱の notices を既定のアプリで開く。見つからなければリポジトリの同じファイル
            string? path = BundledFile.Find(AppContext.BaseDirectory, AppInfo.ThirdPartyNoticesFileName);
            if (path is not null) Open(shell, ItemKind.File, path);
            else Open(shell, ItemKind.Url, AppInfo.ThirdPartyNoticesUrl);
        });
        var ok = Dialogs.ActionButton(Strings.Common_OK, isDefault: true);
        ok.HorizontalAlignment = HorizontalAlignment.Right;

        var window = new ChromeWindow
        {
            Title = Strings.About_Title,
            TagCode = "INFO",
            BodyWidth = 520,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true,
            Body = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    Themed.Glow(new TextBlock { Text = AppInfo.ProductName, FontWeight = FontWeight.Bold, FontSize = 18 }),
                    Themed.Note(new TextBlock { Text = AppInfo.Subtitle }),
                    new TextBlock { Text = Strings.FormatAbout_Version(version) },
                    new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock { Text = AppInfo.Copyright },
                            new TextBlock { Text = Strings.About_License, TextWrapping = TextWrapping.Wrap },
                            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Children = { source, thirdParty } },
                        },
                    },
                    new TextBlock { Text = Strings.About_DataFolder, FontWeight = FontWeight.SemiBold },
                    new DockPanel
                    {
                        Children =
                        {
                            Dock(openData),
                            new TextBox { Text = dataRoot, IsReadOnly = true, Margin = new Thickness(0, 0, 8, 0) },
                        },
                    },
                    new Expander
                    {
                        Header = Strings.About_FontLicense,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Content = new ScrollViewer
                        {
                            MaxHeight = 260,
                            Content = new SelectableTextBlock { Text = ReadLicense(), TextWrapping = TextWrapping.Wrap, FontSize = 12 },
                        },
                    },
                    ok,
                },
            },
        };
        if (appearance is not null) _ = new BoardWindowAppearance(window, appearance, paths, platform);
        ok.Click += (_, _) => window.Close();
        window.Closed += (_, _) => _open = null;
        _open = window;
        if (platform is not null) WindowActivation.ShowAndActivate(window, platform);
        else window.Show();
        return window;
    }

    private static HyperlinkButton Link(string text, Action onClick)
    {
        var link = new HyperlinkButton { Content = text, Padding = new Thickness(0) };
        link.Click += (_, _) => onClick();
        return link;
    }

    private static void Open(IShellService shell, ItemKind kind, string target)
    {
        var r = shell.Launch(new LauncherItem { Name = Path.GetFileName(target), Target = target, Kind = kind }, null);
        if (!r.Success) Toast.Show(Strings.FormatAbout_OpenFailed(target, Loc.LaunchError(r)));
    }

    private static Control Dock(Control c)
    {
        DockPanel.SetDock(c, Avalonia.Controls.Dock.Right);
        return c;
    }

    private static string ReadLicense()
    {
        try
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts");
            return string.Join("\n\n―――\n\n", new[] { "OFL-ChakraPetch.txt", "OFL-ShareTechMono.txt" }.Select(f => File.ReadAllText(Path.Combine(dir, f))));
        }
        catch (IOException) { return Strings.About_LicenseMissing; }
        catch (UnauthorizedAccessException) { return Strings.About_LicenseUnreadable; }
    }
}
