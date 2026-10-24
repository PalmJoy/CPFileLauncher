using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using FileLauncher.App;
using FileLauncher.Core.Model;
using FileLauncher.Core.Storage;

namespace FileLauncher.Desktop.Tests;

public class DoubleTapPopupTests
{
    [AvaloniaFact]
    public void 二回押し通知はポップアップの既存キーボード表示経路に届く()
    {
        string dir = Path.Combine(Path.GetTempPath(), "FileLauncherDoubleTapTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var board = new BoardWindow();
        var platform = new FakePlatform();
        try
        {
            var store = new AppDataStore(new DataPaths(dir, IsPortable: false));
            var hub = new SettingsHub(store, store.LoadAll().Settings.Value);
            hub.Current.Appearance.Animation = false;
            var data = Board.CreateDefault();
            board.ApplyEffects(hub.Current.Appearance); board.Render(data, hub.Current.Appearance, 0);
            var popup = new PopupController(board, platform, hub, data);
            popup.Attach(); popup.Activate();
            platform.RaiseDoubleTap(System.Diagnostics.Stopwatch.GetTimestamp());
            Dispatcher.UIThread.RunJobs();
            Assert.True(popup.IsShown);
        }
        finally { board.AllowClose = true; board.Close(); Directory.Delete(dir, true); }
    }
}
