using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FileLauncher.App;
using FileLauncher.Core.Storage;
using FileLauncher.Core.Model;

namespace FileLauncher.Desktop.Tests;

public sealed class StartupGuideTests
{
    [Fact]
    public void InvocationMethodsIncludeOnlyEnabledShortcutsAndDesktopRestriction()
    {
        var triggers = new TriggerSettings
        {
            Hotkey = new() { Enabled = true, Modifiers = KeyModifiers.Alt, Key = "F8" },
            DesktopOnly = true,
            Mouse =
            [
                new() { Gesture = MouseGesture.LongPress, Button = MouseButtonKind.X1, Modifiers = KeyModifiers.Ctrl },
                new() { Gesture = MouseGesture.LeftRightTogether },
                new() { Gesture = MouseGesture.WheelClickRotate },
                new() { Enabled = false, Button = MouseButtonKind.X2 },
            ],
        };
        var text = StartupGuideWindow.InvocationText(triggers);
        Assert.Contains(Loc.Shortcut(KeyModifiers.Alt, "F8"), text);
        Assert.Contains(Strings.FormatStartupGuide_DesktopOnly(Loc.Shortcut(KeyModifiers.Ctrl,
            Strings.FormatStartupGuide_MouseButton(EnumNames.Of(MouseButtonKind.X1), EnumNames.Of(MouseGesture.LongPress)))), text);
        Assert.Contains(EnumNames.Of(MouseGesture.LeftRightTogether), text);
        Assert.Contains(EnumNames.Of(MouseGesture.WheelClickRotate), text);
        Assert.DoesNotContain(EnumNames.Of(MouseButtonKind.X2), text);
        triggers.Hotkey.Enabled = false;
        Assert.DoesNotContain("F8", StartupGuideWindow.InvocationText(triggers));
        triggers.Mouse.Clear();
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
    [Fact]
    public void ガイドは有効な二回押しだけを表示する()
    {
        var settings = new TriggerSettings { Hotkey = new() { Enabled = false }, Mouse = [] };
        Assert.Equal(Strings.StartupGuide_ShortcutDisabled, StartupGuideWindow.InvocationText(settings));
        settings.DoubleTap = new() { Enabled = true, Modifier = DoubleTapModifier.Alt };
        Assert.Contains(Strings.FormatStartupGuide_DoubleTap(Loc.Modifier(DoubleTapModifier.Alt)), StartupGuideWindow.InvocationText(settings));
    }

}
