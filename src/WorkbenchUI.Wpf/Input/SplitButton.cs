using System.Windows;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Split button whose main body acts as a regular button (executing its command)
/// and whose trailing arrow opens <see cref="DropDownButton.DropDownMenu"/>.
/// </summary>
public class SplitButton : DropDownButton
{
    static SplitButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SplitButton), new FrameworkPropertyMetadata(typeof(SplitButton)));
    }

    /// <summary>Gets whether clicking the main button body opens the menu; returns <see langword="false"/> so only <c>PART_DropDownButton</c> opens it.</summary>
    protected override bool OpensOnClick => false;
}
