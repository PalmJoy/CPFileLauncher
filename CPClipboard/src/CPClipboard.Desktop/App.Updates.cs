using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CPClipboard.Core.Model;

namespace CPClipboard.App;

public partial class App
{
    private bool _checkingUpdates;
    private Window? _updateWindow;
    private readonly CancellationTokenSource _updateCts = new();

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (_updateWindow is { } existing)
        {
            if (manual && _platform is { } platform) WindowActivation.ShowAndActivate(existing, platform);
            return;
        }
        if (_checkingUpdates) return;
        _checkingUpdates = true;
        try
        {
            var latest = await UpdateChecker.CheckAsync(_updateCts.Token);
            if (_updateCts.IsCancellationRequested) return;
            if (latest is null)
            {
                if (manual) Notice.Show(Strings.Update_Check, string.Format(Strings.Update_Latest, UpdateChecker.CurrentVersion));
                return;
            }
            var window = CreateUpdateWindow(latest);
            _updateWindow = window;
            WindowActivation.ShowAndActivate(window, _platform!);
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or OperationCanceledException
            or System.Text.Json.JsonException or InvalidOperationException or KeyNotFoundException)
        {
            if (_updateCts.IsCancellationRequested) return;
            AppLog.Info($"Update check failed: {ex.Message}");
            if (manual) Notice.Show(Strings.Update_Check, Strings.Update_Failed);
        }
        finally { _checkingUpdates = false; }
    }

    internal ChromeWindow CreateUpdateWindow(string latest)
    {
        var open = Dialogs.ActionButton(Strings.Update_OpenWebsite, isDefault: true);
        var cancel = Dialogs.ActionButton(Strings.Common_Cancel, isCancel: true);
        var window = new ChromeWindow
        {
            Title = AppInfo.TitlePrefix + Strings.Update_Check,
            TagCode = "UPDATE",
            BodyWidth = 440,
            ShowInTaskbar = false,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Body = new StackPanel
            {
                Margin = new Thickness(24), Spacing = 18,
                Children =
                {
                    new TextBlock { Text = string.Format(Strings.Update_Available, UpdateChecker.CurrentVersion, latest), TextWrapping = TextWrapping.Wrap },
                    new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8, Children = { open, cancel } },
                },
            },
        };
        if (_hub is not null) _ = new BoardWindowAppearance(window, _hub.Current.Appearance, _paths, _platform);
        open.Click += (_, _) =>
        {
            var result = _platform!.Shell.Open(UpdateChecker.Website);
            if (result.Success) window.Close();
            else Toast.Show(Strings.FormatAbout_OpenFailed(UpdateChecker.Website, Loc.LaunchError(result)));
        };
        cancel.Click += (_, _) => window.Close();
        window.Closed += (_, _) => _updateWindow = null;
        return window;
    }
}
