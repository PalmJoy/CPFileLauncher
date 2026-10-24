using Avalonia.Controls;
using Avalonia.Input;
using CPClipboard.Platform;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using CPClipboard.Core.Model;

namespace CPClipboard.App;

/// <summary>定型文の編集。設定とは独立して開き、閉じたら部品と購読を破棄する。</summary>
internal sealed class TemplatesWindow : ChromeWindow
{
    internal TemplatesEditor TemplatesEditor { get; }
    internal TemplateEditTarget? TemplateTarget { get; private set; }
    internal Func<string, string, string, Task<bool>> Confirm { get; set; }

    private readonly BackdropLayer _backdrop = new();
    private readonly Border _overlay = new() { IsHitTestVisible = false, [!Border.BackgroundProperty] = Themed.Res("FlBoardBackground") };
    private readonly GridLayer _grid = new() { [!GridLayer.StrokeProperty] = Themed.Res("FlHudGrid") };
    private readonly ScanlineLayer _scanlines = new() { [!ScanlineLayer.StrokeProperty] = Themed.Res("FlHudScanline") };

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
        var chrome = (FrameChrome)Content!;
        var layout = chrome.Body!;
        chrome.Body = null;
        chrome.BackgroundKey = "FlBoardBackground";
        chrome.Body = new Grid { Children = { _backdrop, _overlay, _grid, _scanlines, layout } };
        var button = new Style(x => x.OfType<Button>());
        button.Setters.Add(new Setter(Button.BackgroundProperty, Avalonia.Media.Brushes.Transparent));
        button.Setters.Add(new Setter(Button.BorderBrushProperty, Themed.Res("FlTabOutline")));
        button.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(1)));
        Styles.Add(button);
        void ApplyAppearance()
        {
            var appearance = settings?.Current.Appearance ?? new AppearanceSettings();
            TemplatesEditor.ApplyAppearance(appearance);
            Opacity = appearance.Opacity / 100.0;
            _grid.IsVisible = appearance.Hud.Grid;
            _grid.Opacity = appearance.Hud.GridOpacity / 100.0;
            _scanlines.IsVisible = appearance.Hud.Scanlines;
            _scanlines.Opacity = appearance.Hud.ScanlineOpacity / 100.0;
            _scanlines.Pitch = appearance.Hud.ScanlinePitch;
            _backdrop.Source = background?.Invoke();
            _backdrop.Fit = appearance.Background.Fit;
            _backdrop.ImageOpacity = appearance.Background.ImageOpacity / 100.0;
            bool hasImage = _backdrop.Source is not null;
            chrome.SetSurfaceVisible(!hasImage);
            _overlay.IsVisible = hasImage;
            _overlay.Opacity = appearance.Background.Overlay / 100.0;
        }
        void OnSettingsChanged(SettingsChange _) => ApplyAppearance();
        if (settings is not null)
        {
            settings.Changed += OnSettingsChanged;
            settings.BackgroundLoaded += ApplyAppearance;
        }
        ApplyAppearance();
        Closed += (_, _) =>
        {
            if (settings is not null) { settings.Changed -= OnSettingsChanged; settings.BackgroundLoaded -= ApplyAppearance; }
            _backdrop.Source = null;
        };
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
