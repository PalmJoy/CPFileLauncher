using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using CPClipboard.Core.Model;

namespace CPClipboard.App;

internal sealed partial class TemplatesEditor
{
    private double _fontSize = 13;
    internal static void StyleList(ListBox list)
    {
        list.Background = Brushes.Transparent;
        list.BorderThickness = new Thickness(1);
        list[!ListBox.BorderBrushProperty] = Themed.Res("FlHeaderSeparator");
        var theme = new ControlTheme(typeof(ListBoxItem));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ListBoxItem>((_, _) => new Border
        {
            [!Border.BackgroundProperty] = new TemplateBinding(TemplatedControl.BackgroundProperty),
            [!Border.BorderBrushProperty] = new TemplateBinding(TemplatedControl.BorderBrushProperty),
            [!Border.BorderThicknessProperty] = new TemplateBinding(TemplatedControl.BorderThicknessProperty),
            [!Border.PaddingProperty] = new TemplateBinding(TemplatedControl.PaddingProperty),
            Child = new ContentPresenter
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                [!ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty),
            },
        })));
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent));
        theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)));
        theme.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, Themed.Res("FlRowText")));
        list.ItemContainerTheme = theme;
        foreach (string state in new[] { ":selected", ":pointerover", ":focus-visible" })
        {
            var style = new Style(x => x.OfType<ListBoxItem>().Class(state));
            style.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, Themed.Res("FlRowHoverBorder")));
            if (state == ":pointerover") style.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Themed.Res("FlRowHoverBackground")));
            list.Styles.Add(style);
        }
        var marker = new Style(x => x.OfType<ListBoxItem>().Descendant().OfType<TextBlock>().Class("marker"));
        marker.Setters.Add(new Setter(Visual.OpacityProperty, 0.0)); list.Styles.Add(marker);
        foreach (string state in new[] { ":selected", ":pointerover" })
        {
            var visible = new Style(x => x.OfType<ListBoxItem>().Class(state).Descendant().OfType<TextBlock>().Class("marker"));
            visible.Setters.Add(new Setter(Visual.OpacityProperty, 1.0)); list.Styles.Add(visible);
        }
    }
    internal void ApplyAppearance(AppearanceSettings appearance)
    {
        _fontSize = appearance.FontSize;
        _text.FontSize = _name.FontSize = _fontSize;
        Refresh();
    }
}
