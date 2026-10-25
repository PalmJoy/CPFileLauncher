using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using CPClipboard.Core.Model;
namespace CPClipboard.App;

internal static class BoardWindowSurface
{
    internal static void Attach(ChromeWindow window, SettingsHub? settings, Func<Bitmap?>? background, Action<AppearanceSettings>? apply = null)
    {
        var _backdrop = new BackdropLayer();
        var _overlay = new Border { IsHitTestVisible = false, [!Border.BackgroundProperty] = Themed.Res("FlBoardBackground") };
        var _grid = new GridLayer { [!GridLayer.StrokeProperty] = Themed.Res("FlHudGrid") };
        var _scanlines = new ScanlineLayer { [!ScanlineLayer.StrokeProperty] = Themed.Res("FlHudScanline") };
        var chrome = (FrameChrome)window.Content!;
        var layout = chrome.Body!;
        chrome.Body = null;
        chrome.BackgroundKey = "FlBoardBackground";
        chrome.Body = new Grid { Children = { _backdrop, _overlay, _grid, _scanlines, layout } };
        var button = new Style(x => x.OfType<Button>());
        button.Setters.Add(new Setter(Button.BackgroundProperty, Avalonia.Media.Brushes.Transparent));
        button.Setters.Add(new Setter(Button.BorderBrushProperty, Themed.Res("FlTabOutline")));
        button.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(1)));
        window.Styles.Add(button);
        void ApplyAppearance()
        {
            var appearance = settings?.Current.Appearance ?? new AppearanceSettings();
            apply?.Invoke(appearance);
            window.Opacity = appearance.Opacity / 100.0;
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
        window.Closed += (_, _) =>
        {
            if (settings is not null) { settings.Changed -= OnSettingsChanged; settings.BackgroundLoaded -= ApplyAppearance; }
            _backdrop.Source = null;
        };
    }
}
