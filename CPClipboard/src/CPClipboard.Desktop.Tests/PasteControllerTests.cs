using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using CPClipboard.App;
using CPClipboard.Core.Model;
using CPClipboard.Core.Paste;
using CPClipboard.Core.Storage;

namespace CPClipboard.Desktop.Tests;

public class PasteControllerTests
{
    private static PasteRequest Request => new("text", 42, false, true, true, 500);

    [Fact]
    public void トーストは貼り付け成功や設定OFFで出さずコピーだけと失敗理由を知らせる()
    {
        Assert.Null(PasteController.Message(PasteResult.Pasted, Request));
        Assert.Null(PasteController.Message(PasteResult.Copied, Request with { AutoPaste = false }));
        Assert.Equal(Strings.Toast_Copied, PasteController.Message(PasteResult.Copied, Request with { CopyOnly = true }));
        Assert.Equal(Strings.Toast_Copied, PasteController.Message(PasteResult.NoTarget, Request));
        Assert.Equal(Strings.Toast_CopiedOnly_ModifierHeld, PasteController.Message(PasteResult.ModifierHeld, Request));
        Assert.Equal(Strings.Toast_ClipboardChanged, PasteController.Message(PasteResult.ClipboardChanged, Request));
        Assert.Equal(Strings.Toast_CopiedOnly_CheckFailed, PasteController.Message(PasteResult.CheckFailed, Request));
    }

    [AvaloniaFact]
    public async Task 盤面を隠す前の対象を使い非表示演出を待たずに送信する()
    {
        string dir = Path.Combine(Path.GetTempPath(), "CpcbPasteUi", Guid.NewGuid().ToString("N"));
        BoardWindow? window = null; PopupController? popup = null;
        try
        {
            var store = new AppDataStore(new(dir, false));
            var settings = store.LoadAll().Settings.Value;
            settings.Paste.RestoreClipboard = false;
            var hub = new SettingsHub(store, settings);
            var platform = new FakePlatform { HookRunning = true, Foreground = 42 };
            var book = TemplateBook.CreateEmpty(); book[1].Snippets.Add(new() { Text = "chosen\ntext" });
            window = new BoardWindow(); window.ApplyEffects(settings.Appearance);
            popup = new PopupController(window, platform, hub, () => book);
            popup.Attach(); popup.ShowFromExternal("test"); Dispatcher.UIThread.RunJobs();
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.False(popup.IsShown);
            Assert.Equal("chosen\ntext", ((FakeClipboard)platform.Clipboard).Text);
            Assert.True(((FakeClipboard)platform.Clipboard).Transient);
            Assert.Equal(1, ((FakeKeys)platform.KeySender).Sent);
            Assert.Equal(new nint[] { 42 }, platform.RestoredTargets);
            Assert.True(window.IsVisible); // 非表示演出の完了は待たない
            popup.ShowFromExternal("new board");
            Assert.True(popup.IsShown);
            await popup.StopPastingAsync();
            Assert.True(popup.IsShown); // 旧処理の完了で新しい盤面を閉じない
        }
        finally
        {
            if (popup is not null) await popup.StopPastingAsync();
            if (window is not null) { window.AllowClose = true; window.Close(); }
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task 新しい盤面が開いている間は古い要求で前面化や送信をしない()
    {
        var platform = new FakePlatform { HookRunning = true, Foreground = 42 };
        var paste = new PasteController(platform, () => false) { Notify = (_, _) => { } };
        Assert.Equal(PasteResult.FocusFailed, await paste.ExecuteAsync(Request));
        Assert.Empty(platform.RestoredTargets);
        Assert.Equal(0, ((FakeKeys)platform.KeySender).Sent);
        Assert.False(((FakeClipboard)platform.Clipboard).Transient);
    }
}
