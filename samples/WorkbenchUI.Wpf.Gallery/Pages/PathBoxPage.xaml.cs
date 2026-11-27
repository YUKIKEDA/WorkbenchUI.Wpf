using System.Windows;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class PathBoxPage : UserControl
{
    private int _browseCount;

    public PathBoxPage()
    {
        InitializeComponent();

        ModesExample.Xaml = """
            <wb:PathBox Mode="OpenFile"
                        DialogTitle="Open Mesh File"
                        Filter="Mesh files (*.msh;*.inp)|*.msh;*.inp|All files (*.*)|*.*"
                        SelectedPath="{Binding MeshFilePath, Mode=TwoWay}" />

            <wb:PathBox Mode="SaveFile" Filter="Result files (*.vtk)|*.vtk" />
            <wb:PathBox Mode="Folder" SelectedPath="{Binding WorkingDirectory, Mode=TwoWay}" />
            """;

        CustomBrowseExample.Xaml = """
            <wb:PathBox SelectedPath="{Binding VaultUri, Mode=TwoWay}"
                        BrowseRequested="OnVaultBrowseRequested" />
            """;

        CustomBrowseExample.CSharp = """
            private void OnVaultBrowseRequested(object sender, RoutedEventArgs e)
            {
                e.Handled = true; // Suppress built-in OS dialog
                CustomPathBox.SelectedPath = "vault://projects/wing-rib/mesh-v5.msh";
            }
            """;
    }

    private void CustomPathBox_BrowseRequested(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        _browseCount++;
        CustomPathBox.SelectedPath = $"vault://projects/wing-rib/mesh-v{4 + _browseCount}.msh";
        CustomBrowseStatus.Text = $"BrowseRequested handled cleanly (#{_browseCount}) -> {CustomPathBox.SelectedPath}";
    }
}
