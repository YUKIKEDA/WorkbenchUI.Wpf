using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WorkbenchUI.Wpf.Gallery.Infrastructure;
using WorkbenchUI.Wpf.Theming;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class TokensPage : UserControl
{
    private static readonly (string Group, string[] Keys)[] BrushGroups =
    [
        ("Surfaces", [
            "Wb.Brush.Surface.Window",
            "Wb.Brush.Surface.Panel",
            "Wb.Brush.Surface.Elevated",
            "Wb.Brush.Surface.Input",
        ]),
        ("Text", [
            "Wb.Brush.Text.Primary",
            "Wb.Brush.Text.Secondary",
            "Wb.Brush.Text.Disabled",
            "Wb.Brush.Text.OnAccent",
        ]),
        ("Controls & Borders", [
            "Wb.Brush.Control.Background",
            "Wb.Brush.Control.Hover",
            "Wb.Brush.Control.Pressed",
            "Wb.Brush.Border.Default",
            "Wb.Brush.Border.Strong",
            "Wb.Brush.Border.Focus",
        ]),
        ("Accent & Selection", [
            "Wb.Brush.Accent.Default",
            "Wb.Brush.Accent.Hover",
            "Wb.Brush.Accent.Pressed",
            "Wb.Brush.Accent.Muted",
            "Wb.Brush.State.Hover",
            "Wb.Brush.State.Selected",
        ]),
        ("Status & Danger", [
            "Wb.Brush.Info",
            "Wb.Brush.Success",
            "Wb.Brush.Warning",
            "Wb.Brush.Error",
            "Wb.Brush.Danger.Default",
            "Wb.Brush.Danger.Hover",
        ]),
    ];

    private readonly List<(string Key, TextBlock HexLabel)> _hexLabels = [];
    private bool _isHooked;

    public TokensPage()
    {
        InitializeComponent();
        PopulateAccentButtons();
        PopulateBrushGroups();
        PopulateSpacingBars();

        AccentExample.CSharp = """
            // Switch theme variant at runtime
            ThemeManager.SetTheme(WorkbenchThemeVariant.Light);

            // Override accent color (Hover / Pressed / Muted auto-derived)
            ThemeManager.SetAccent(Color.FromRgb(0xCA, 0x51, 0x00));

            // Restore default theme accent
            ThemeManager.ResetAccent();
            """;

        BrushesExample.Xaml = """
            <Border Background="{DynamicResource Wb.Brush.Surface.Panel}"
                    BorderBrush="{DynamicResource Wb.Brush.Border.Default}"
                    BorderThickness="1">
                <TextBlock Text="Token-aware text"
                           Foreground="{DynamicResource Wb.Brush.Text.Primary}" />
            </Border>
            """;

        MetricsExample.Xaml = """
            <TextBlock FontSize="{DynamicResource Wb.FontSize.L}"
                       FontFamily="{DynamicResource Wb.FontFamily.Default}"
                       Margin="0,0,0,8" />
            """;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isHooked)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            _isHooked = true;
        }

        RefreshResolvedHexValues();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_isHooked)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            _isHooked = false;
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(RefreshResolvedHexValues);

    private void PopulateAccentButtons()
    {
        AccentPanel.Children.Clear();
        foreach (var preset in GalleryAccentPresets.Presets)
        {
            var swatch = new Border
            {
                Width = 12,
                Height = 12,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(preset.PreviewColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            };
            swatch.SetResourceReference(Border.BorderBrushProperty, "Wb.Brush.Border.Default");
            swatch.BorderThickness = new Thickness(1);

            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(swatch);
            content.Children.Add(new TextBlock { Text = preset.Name, VerticalAlignment = VerticalAlignment.Center });

            var button = new Button
            {
                Content = content,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 8, 6),
            };
            button.Click += (_, _) => GalleryAccentPresets.Apply(preset);
            AccentPanel.Children.Add(button);
        }
    }

    private void PopulateBrushGroups()
    {
        BrushGroupsHost.Children.Clear();
        _hexLabels.Clear();

        foreach (var (groupName, keys) in BrushGroups)
        {
            var header = new TextBlock
            {
                Text = groupName,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 4, 0, 8),
            };
            BrushGroupsHost.Children.Add(header);

            var wrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
            foreach (var key in keys)
            {
                wrap.Children.Add(CreateSwatchCard(key));
            }

            BrushGroupsHost.Children.Add(wrap);
        }

        RefreshResolvedHexValues();
    }

    private Button CreateSwatchCard(string key)
    {
        var swatch = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 10, 0),
        };
        swatch.SetResourceReference(Border.BackgroundProperty, key);
        swatch.SetResourceReference(Border.BorderBrushProperty, "Wb.Brush.Border.Default");

        var nameText = new TextBlock
        {
            Text = key["Wb.Brush.".Length..],
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        nameText.SetResourceReference(TextBlock.FontSizeProperty, "Wb.FontSize.S");

        var hexText = new TextBlock
        {
            Text = "#------",
            Margin = new Thickness(0, 2, 0, 0),
        };
        hexText.SetResourceReference(TextBlock.FontFamilyProperty, "Wb.FontFamily.Monospace");
        hexText.SetResourceReference(TextBlock.FontSizeProperty, "Wb.FontSize.S");
        hexText.SetResourceReference(TextBlock.ForegroundProperty, "Wb.Brush.Text.Secondary");

        _hexLabels.Add((key, hexText));

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(nameText);
        info.Children.Add(hexText);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(swatch, 0);
        Grid.SetColumn(info, 1);
        grid.Children.Add(swatch);
        grid.Children.Add(info);

        var button = new Button
        {
            Width = 204,
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(10, 8, 10, 8),
            ToolTip = $"Click to copy {{DynamicResource {key}}}",
            Content = grid,
        };
        button.SetResourceReference(StyleProperty, "Gallery.TileCardButton");
        button.Click += (_, _) =>
        {
            var markup = $"{{DynamicResource {key}}}";
            try
            {
                Clipboard.SetText(markup);
                CopyStatusText.Text = $"Copied to clipboard: {markup}";
            }
            catch (ExternalException)
            {
                CopyStatusText.Text = $"Token: {markup}";
            }
        };

        return button;
    }

    private void RefreshResolvedHexValues()
    {
        foreach (var (key, hexLabel) in _hexLabels)
        {
            if (TryFindResource(key) is SolidColorBrush brush)
            {
                var c = brush.Color;
                hexLabel.Text = c.A == 255
                    ? $"#{c.R:X2}{c.G:X2}{c.B:X2}"
                    : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            else
            {
                hexLabel.Text = "(unresolved)";
            }
        }
    }

    private void PopulateSpacingBars()
    {
        SpacingPanel.Children.Clear();
        foreach (var name in new[] { "XS", "S", "M", "L" })
        {
            var key = $"Wb.Spacing.{name}";
            var size = (double)FindResource(key);

            var bar = new Border { Width = size * 10, Height = 12, CornerRadius = new CornerRadius(2) };
            bar.SetResourceReference(Border.BackgroundProperty, "Wb.Brush.Accent.Default");

            var label = new TextBlock
            {
                Text = $"{key} ({size} px)",
                Width = 148,
                VerticalAlignment = VerticalAlignment.Center,
            };
            label.SetResourceReference(TextBlock.FontSizeProperty, "Wb.FontSize.S");

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 3, 0, 3),
            };
            row.Children.Add(label);
            row.Children.Add(bar);
            SpacingPanel.Children.Add(row);
        }
    }
}
