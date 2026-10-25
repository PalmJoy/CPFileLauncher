using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using CPClipboard.Core.Model;
using CPClipboard.Core.Effects;
using CPClipboard.Platform;
using Avalonia.Threading;
namespace CPClipboard.App;

internal static class BoardWindowSurface
{
    internal static void Attach(ChromeWindow window, SettingsHub? settings, Func<Bitmap?>? background, Action<AppearanceSettings>? apply = null, IAmbientLayerService? ambientLayers = null, Func<bool>? reducedMotion = null)
    {
        var _backdrop = new BackdropLayer();
        var _overlay = new Border { IsHitTestVisible = false, [!Border.BackgroundProperty] = Themed.Res("FlBoardBackground") };
        var _grid = new GridLayer { [!GridLayer.StrokeProperty] = Themed.Res("FlHudGrid") };
        var _scanlines = new ScanlineLayer { [!ScanlineLayer.StrokeProperty] = Themed.Res("FlHudScanline") };
        var hologram = new HologramEdges { [!HologramEdges.PrimaryProperty] = Themed.Res("FlAccent"), [!HologramEdges.SecondaryProperty] = Themed.Res("FlAccent2"), Opacity = .4 };
        var circuits = new CircuitLayer { [!CircuitLayer.StrokeProperty] = Themed.Res("FlAccent") };
        var chrome = (FrameChrome)window.Content!;
        var layout = chrome.Body!;
        chrome.Body = null;
        chrome.BackgroundKey = "FlBoardBackground";
        chrome.ShadowKey = "FlBoardGlow";
        chrome.BakeGlow = true;
        chrome.Body = new Grid { Children = { _backdrop, _overlay, _grid, hologram, circuits, _scanlines, layout } };
        var pulse = new PulseLayer { Chamfer = chrome.Chamfer };
        var orbs = new OrbLayer { Chamfer = chrome.Chamfer };
        var beam = new ScanBeamLayer { Clip = chrome.FaceClip };
        chrome.FaceClipChanged += (_, _) => beam.Clip = chrome.FaceClip;
        var ambientRoot = new Grid { IsHitTestVisible = false, IsVisible = false, Children = { pulse, beam, orbs } };
        chrome.Overlays.Children.Insert(0, ambientRoot);
        var animator = new AmbientAnimator(window, orbs, pulse, beam);
        NativeAmbient? native = null;
        bool closed = false, nativeFailed = false;
        void UpdateAmbient()
        {
            if (closed) return;
            var appearance = settings?.Current.Appearance ?? new AppearanceSettings();
            var orbSpec = EffectCatalog.Resolve(appearance, EffectCatalog.FrameOrb);
            var pulseSpec = EffectCatalog.Resolve(appearance, EffectCatalog.GlowPulse);
            var beamSpec = EffectCatalog.Resolve(appearance, EffectCatalog.ScanBeam);
            bool run = window.IsVisible && window.IsHitTestVisible && reducedMotion?.Invoke() != true
                && (orbSpec.IsActive || pulseSpec.IsActive || beamSpec.IsActive);
            if (window.IsVisible && native is null && !nativeFailed && ambientLayers is { IsSupported: true })
            {
                var candidate = new NativeAmbient(window, chrome, ambientLayers, pulse);
                if (candidate.TryAttach()) native = candidate;
                else { candidate.Dispose(); nativeFailed = true; }
            }
            animator.Stop();
            if (native is not null)
            {
                ambientRoot.IsVisible = false;
                native.Apply(orbSpec, pulseSpec, beamSpec);
                native.SetOpacity(window.Opacity);
                native.SetRunning(run);
            }
            else
            {
                orbs.Spec = orbSpec; beam.Spec = beamSpec; animator.PulseSpec = pulseSpec;
                animator.Vsync = appearance.Vsync;
                ambientRoot.IsVisible = run && (orbSpec.IsActive || pulseSpec.IsActive || beamSpec.IsActive);
                pulse.IsVisible = pulseSpec.IsActive;
                if (ambientRoot.IsVisible) { pulse.Refresh(); animator.Start(); }
            }
        }
        pulse.SizeChanged += (_, _) => { if (animator.IsRunning) pulse.Refresh(); };
        window.Opened += (_, _) => UpdateAmbient();
        window.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.IsVisibleProperty || e.Property == InputElement.IsHitTestVisibleProperty) UpdateAmbient();
        };
        window.ActualThemeVariantChanged += (_, _) => Dispatcher.UIThread.Post(UpdateAmbient, DispatcherPriority.Loaded);
        var button = new Style(x => x.OfType<Button>());
        button.Setters.Add(new Setter(Button.BackgroundProperty, Avalonia.Media.Brushes.Transparent));
        button.Setters.Add(new Setter(Button.BorderBrushProperty, Themed.Res("FlTabOutline")));
        button.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(1)));
        window.Styles.Add(button);
        void ApplyAppearance()
        {
            var appearance = settings?.Current.Appearance ?? new AppearanceSettings();
            apply?.Invoke(appearance);
            window.WindowShowEffect = settings is null ? EffectSpec.None : EffectCatalog.Resolve(appearance, EffectCatalog.BoardShow);
            window.WindowHideEffect = settings is null ? EffectSpec.None : EffectCatalog.Resolve(appearance, EffectCatalog.BoardHide);
            hologram.IsVisible = circuits.IsVisible = appearance.Hud.Grid;
            circuits.Opacity = Math.Min(.25, appearance.Hud.GridOpacity / 100.0 * 4);
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
            UpdateAmbient();
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
            closed = true;
            animator.Stop();
            native?.Dispose();
            _backdrop.Source = null;
        };
    }
}
