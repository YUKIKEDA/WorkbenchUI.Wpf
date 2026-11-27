using System.Windows;
using System.Windows.Controls.Primitives;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// ON/OFF toggle switch derived from <see cref="ToggleButton"/>, so <see cref="ToggleButton.IsChecked"/>,
/// <see cref="ButtonBase.Command"/>, and related members work out of the box.
/// Specify a label via <see cref="System.Windows.Controls.ContentControl.Content"/> on the right side (no built-in On/Off strings).
/// Sized to fit compact UI layouts and does not support tri-state mode.
/// </summary>
public class ToggleSwitch : ToggleButton
{
    static ToggleSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ToggleSwitch), new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
    }

    /// <summary>Toggles between checked and unchecked; tri-state is unsupported, so <see langword="null"/> is treated as <see langword="false"/>.</summary>
    protected override void OnToggle()
    {
        SetCurrentValue(IsCheckedProperty, IsChecked != true);
    }
}
