using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class ButtonsPage : UserControl
{
    private int _clickCount;

    public ButtonsPage()
    {
        InitializeComponent();

        PlaygroundExample.Xaml = """
            <Button Content="Run Solver"
                    Style="{DynamicResource Wb.Button.Accent}"
                    wb:ButtonAssist.Icon="{x:Static wb:WorkbenchIcons.Play}"
                    Click="RunButton_Click" />
            """;

        VariantsExample.Xaml = """
            <!-- Button hierarchy -->
            <Button Content="Accent" Style="{DynamicResource Wb.Button.Accent}" />
            <Button Content="Standard" />
            <Button Content="Subtle" Style="{DynamicResource Wb.Button.Subtle}" />
            <Button Content="Danger" Style="{DynamicResource Wb.Button.Danger}" />

            <!-- Icon attachment & icon-only toolbar button -->
            <Button Content="Save" wb:ButtonAssist.Icon="{x:Static wb:WorkbenchIcons.Save}" />
            <Button Style="{DynamicResource Wb.Button.Icon}"
                    wb:ButtonAssist.Icon="{x:Static wb:WorkbenchIcons.Settings}"
                    ToolTip="Settings" />
            """;

        DropDownExample.Xaml = """
            <wb:DropDownButton Content="View Presets"
                               DropDownMenu="{StaticResource ViewPresetMenu}" />

            <wb:SplitButton Content="Run Analysis"
                            Click="Solve_Click"
                            DropDownMenu="{StaticResource SolveOptionsMenu}" />
            """;

        DropDownExample.CSharp = """
            private void Solve_Click(object sender, RoutedEventArgs e)
            {
                // Primary button area invoked directly
                RunAnalysis(remesh: false);
            }
            """;
    }

    private void DemoButton_Click(object sender, RoutedEventArgs e)
    {
        _clickCount++;
        ClickCounterText.Text = $"Clicks: {_clickCount}";
    }

    private void VariantCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DemoButton is null)
        {
            return;
        }

        if (VariantCombo.SelectedItem is ComboBoxItem { Tag: string key })
        {
            DemoButton.SetResourceReference(StyleProperty, key);
        }
    }

    private void ButtonLabelBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DemoButton is not null)
        {
            DemoButton.Content = ButtonLabelBox.Text;
        }
    }

    private void ShowIconSwitch_Changed(object sender, RoutedEventArgs e)
    {
        if (DemoButton is null)
        {
            return;
        }

        var icon = ShowIconSwitch.IsChecked == true ? WorkbenchIcons.Play : null;
        ButtonAssist.SetIcon(DemoButton, icon);
    }

    private void SplitPrimary_Click(object sender, RoutedEventArgs e) =>
        DropDownLogText.Text = "Invoked: SplitButton primary action (Run Analysis)";

    private void MenuAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            DropDownLogText.Text = $"Invoked menu item: {item.Header}";
        }
    }
}
