using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class WizardPage : UserControl
{
    public WizardPage()
    {
        InitializeComponent();

        SetupWizard.CurrentIndex = 0;
        UpdateWizardCanGoNext();

        WizardExample.Xaml = """
            <wb:Wizard x:Name="SetupWizard"
                       Navigating="SetupWizard_Navigating"
                       Finished="SetupWizard_Finished"
                       Cancelled="SetupWizard_Cancelled">
                <wb:WizardStep Header="Analysis Type">...</wb:WizardStep>
                <wb:WizardStep Header="Conditions">...</wb:WizardStep>
                <wb:WizardStep Header="Review">...</wb:WizardStep>
            </wb:Wizard>
            """;

        WizardExample.CSharp = """
            private void UpdateCanGoNext()
            {
                SetupWizard.CanGoNext = SetupWizard.CurrentIndex != 1 || ConditionCheck.IsChecked == true;
            }
            """;

        IndicatorExample.Xaml = """
            <wb:StepIndicator CurrentIndex="1">
                <sys:String>Import CAD</sys:String>
                <sys:String>Clean Geometry</sys:String>
                <sys:String>Generate Mesh</sys:String>
                <sys:String>Run Solver</sys:String>
            </wb:StepIndicator>
            """;
    }

    private void ConditionCheck_Changed(object sender, RoutedEventArgs e) => UpdateWizardCanGoNext();

    private void UpdateWizardCanGoNext()
    {
        SetupWizard.CanGoNext = SetupWizard.CurrentIndex != 1 || ConditionCheck?.IsChecked == true;
    }

    private void SetupWizard_Navigating(object sender, WizardNavigatingEventArgs e) =>
        Dispatcher.BeginInvoke(UpdateWizardCanGoNext);

    private void SetupWizard_Finished(object sender, RoutedEventArgs e)
    {
        WizardInfo.Text = "Finished event raised (the host dialog would close here).";
        SetupWizard.CurrentIndex = 0;
        UpdateWizardCanGoNext();
    }

    private void SetupWizard_Cancelled(object sender, RoutedEventArgs e)
    {
        WizardInfo.Text = "Cancelled event raised.";
        SetupWizard.CurrentIndex = 0;
        UpdateWizardCanGoNext();
    }

    private void PrevStep_Click(object sender, RoutedEventArgs e) =>
        StandaloneIndicator.CurrentIndex = Math.Max(0, StandaloneIndicator.CurrentIndex - 1);

    private void NextStep_Click(object sender, RoutedEventArgs e) =>
        StandaloneIndicator.CurrentIndex = Math.Min(StandaloneIndicator.Items.Count - 1, StandaloneIndicator.CurrentIndex + 1);
}
