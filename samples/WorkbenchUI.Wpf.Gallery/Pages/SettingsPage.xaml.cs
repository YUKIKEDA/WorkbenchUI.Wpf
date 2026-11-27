using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Gallery.Infrastructure;
using WorkbenchUI.Wpf.Theming;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class SettingsPage : UserControl
{
    private readonly IGalleryHost _host;
    private bool _isSyncing;
    private bool _isHooked;

    public SettingsPage(IGalleryHost host)
    {
        _host = host;
        InitializeComponent();

        PaneModeCombo.ItemsSource = Enum.GetValues<NavigationViewPaneDisplayMode>();
        BuildAccentPresetButtons();
        SyncFromState();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isHooked)
        {
            ThemeManager.ThemeChanged += OnThemeOrShellChanged;
            _host.ShellStateChanged += OnThemeOrShellChanged;
            _isHooked = true;
        }

        SyncFromState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_isHooked)
        {
            ThemeManager.ThemeChanged -= OnThemeOrShellChanged;
            _host.ShellStateChanged -= OnThemeOrShellChanged;
            _isHooked = false;
        }
    }

    private void OnThemeOrShellChanged(object? sender, EventArgs e) => SyncFromState();

    private void BuildAccentPresetButtons()
    {
        AccentPresetsPanel.Children.Clear();
        foreach (var preset in GalleryAccentPresets.Presets)
        {
            var swatch = new Border
            {
                Width = 14,
                Height = 14,
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
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(10, 4, 10, 4),
            };
            button.Click += (_, _) => GalleryAccentPresets.Apply(preset);
            AccentPresetsPanel.Children.Add(button);
        }
    }

    private void SyncFromState()
    {
        _isSyncing = true;
        try
        {
            DarkThemeRadio.IsChecked = ThemeManager.CurrentTheme == WorkbenchThemeVariant.Dark;
            LightThemeRadio.IsChecked = ThemeManager.CurrentTheme == WorkbenchThemeVariant.Light;
            CustomAccentPicker.SelectedColor = ThemeManager.CurrentAccent ?? Color.FromRgb(0x00, 0x7A, 0xCC);
            PaneModeCombo.SelectedItem = _host.PaneDisplayMode;
            PaneOpenSwitch.IsChecked = _host.IsPaneOpen;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void DarkThemeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isSyncing)
        {
            ThemeManager.SetTheme(WorkbenchThemeVariant.Dark);
        }
    }

    private void LightThemeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isSyncing)
        {
            ThemeManager.SetTheme(WorkbenchThemeVariant.Light);
        }
    }

    private void MatchSystemTheme_Click(object sender, RoutedEventArgs e)
    {
        var osTheme = ThemeManager.GetSystemTheme();
        ThemeManager.SetTheme(osTheme);
        SystemThemeLabel.Text = $"Windows theme detected: {osTheme}";
    }

    private void CustomAccentPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
    {
        if (!_isSyncing)
        {
            ThemeManager.SetAccent(e.NewValue);
        }
    }

    private void ResetAccent_Click(object sender, RoutedEventArgs e) =>
        ThemeManager.ResetAccent();

    private void PaneModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isSyncing && PaneModeCombo.SelectedItem is NavigationViewPaneDisplayMode mode)
        {
            _host.PaneDisplayMode = mode;
        }
    }

    private void PaneOpenSwitch_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isSyncing)
        {
            _host.IsPaneOpen = PaneOpenSwitch.IsChecked == true;
        }
    }
}
