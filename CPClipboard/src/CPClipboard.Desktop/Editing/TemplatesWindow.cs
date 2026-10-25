using Avalonia.Controls;
using Avalonia.Input;
using CPClipboard.Platform;
using Avalonia.Media.Imaging;

namespace CPClipboard.App;

/// <summary>定型文の編集。設定とは独立して開き、閉じたら部品と購読を破棄する。</summary>
internal sealed class TemplatesWindow : ChromeWindow
{
    internal TemplatesEditor TemplatesEditor { get; }
    internal TemplateEditTarget? TemplateTarget { get; private set; }
    internal Func<string, string, string, Task<bool>> Confirm { get; set; }

    public TemplatesWindow(TemplatesHub templates, IClipboardService clipboard, SettingsHub? settings = null, Func<Bitmap?>? background = null)
    {
        Title = AppInfo.TitlePrefix + Strings.Editor_Title;
        TagCode = "EDIT";
        BodyWidth = 820;
        BodyHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = OperatingSystem.IsWindows();
        Confirm = (title, message, ok) => Dialogs.ConfirmAsync(this, title, message, ok);
        TemplatesEditor = new TemplatesEditor(templates, clipboard, (title, text, ok) => Confirm(title, text, ok));
        Body = TemplatesEditor;
        BoardWindowSurface.Attach(this, settings, background, TemplatesEditor.ApplyAppearance);
        AddHandler(KeyDownEvent, (_, e) =>
        {
            if (TemplatesEditor.HandleNavigation(e)) e.Handled = true;
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        Opened += (_, _) => TemplatesEditor.SetActive(true);
        Activated += (_, _) => _ = TemplatesEditor.RefreshClipboardAsync();
        Closed += (_, _) => TemplatesEditor.Dispose();
    }

    internal void SetTemplateTarget(TemplateEditTarget target)
    {
        TemplateTarget = target;
        TemplatesEditor.SelectTarget(target);
    }
}
