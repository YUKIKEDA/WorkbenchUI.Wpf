using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Represents a single step page in a <see cref="Wizard"/>.
/// <see cref="HeaderedContentControl.Header"/> supplies the step indicator title, and <see cref="ContentControl.Content"/> supplies the page body.
/// </summary>
public class WizardStep : HeaderedContentControl
{
    static WizardStep()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(WizardStep), new FrameworkPropertyMetadata(typeof(WizardStep)));
    }
}
