using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using CPClipboard.App;
using CPClipboard.Core.Storage;
using CPClipboard.Core.Model;

namespace CPClipboard.Desktop.Tests;

public sealed class StartupGuideTests
{
    [Fact]
    public void InvocationMethodsIncludeOnlyEnabledShortcuts()
    {
        var triggers = new TriggerSettings
        {
            DoubleTap = new() { Enabled = true, Modifier = DoubleTapModifier.Shift },
            Hotkey = new() { Enabled = true, Modifiers = KeyModifiers.Alt, Key = "F8" },
            Mouse =
            [
                new() { Button = MouseButtonKind.X1, Modifiers = KeyModifiers.Ctrl },
                new() { Button = MouseButtonKind.Right, Modifiers = KeyModifiers.Alt },
                new() { Enabled = false, Button = MouseButtonKind.X2 },
            ],
        };
        var text = StartupGuideWindow.InvocationText(triggers);
        Assert.Contains(Strings.FormatStartupGuide_DoubleTap(Loc.Modifier(DoubleTapModifier.Shift)), text);
        Assert.Contains(Loc.Shortcut(KeyModifiers.Alt, "F8"), text);
        Assert.Contains(Loc.Shortcut(KeyModifiers.Ctrl,
            Strings.FormatStartupGuide_MouseButton(EnumNames.Of(MouseButtonKind.X1))), text);
        Assert.Contains(EnumNames.Of(MouseButtonKind.Right), text);
        Assert.DoesNotContain(EnumNames.Of(MouseButtonKind.X2), text);
        triggers.Hotkey.Enabled = false;
        Assert.DoesNotContain("F8", StartupGuideWindow.InvocationText(triggers));
        triggers.Mouse.Clear();
        triggers.DoubleTap.Enabled = false;
        Assert.Equal(Strings.StartupGuide_ShortcutDisabled, StartupGuideWindow.InvocationText(triggers));
    }

    [AvaloniaFact]
    public void SuppressionIsSavedAndCanBeUnchecked()
    {
        var dir = Path.Combine(Path.GetTempPath(), "StartupGuideTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        StartupGuideWindow? window = null;
        try
        {
            var paths = new DataPaths(dir, IsPortable: false);
            var store = new AppDataStore(paths);
            var hub = new SettingsHub(store, store.LoadAll().Settings.Value);
            hub.Current.Appearance.Animation = false;
            Assert.False(hub.Current.General.HideStartupGuide);
            window = new StartupGuideWindow(hub);
            window.Show();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Assert.Equal(hub.Current.Appearance.Opacity / 100.0, window.Opacity);
            Assert.Contains(window.GetVisualDescendants(), c => c is BackdropLayer);
            var checkbox = window.GetVisualDescendants().OfType<CheckBox>().Single();
            checkbox.IsChecked = true;
            Assert.True(new AppDataStore(paths).LoadAll().Settings.Value.General.HideStartupGuide);
            checkbox.IsChecked = false;
            window.Close();
            Assert.False(new AppDataStore(paths).LoadAll().Settings.Value.General.HideStartupGuide);
        }
        finally
        {
            window?.Close();
            Directory.Delete(dir, recursive: true);
        }
    }
}
