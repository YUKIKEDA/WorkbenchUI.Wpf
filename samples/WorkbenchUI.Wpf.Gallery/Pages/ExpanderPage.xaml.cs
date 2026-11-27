using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class ExpanderPage : UserControl
{
    public ExpanderPage()
    {
        InitializeComponent();

        ExpanderExample.Xaml = """
            <Expander Header="Material Properties" IsExpanded="True">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="130" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    <TextBlock Text="Young's Modulus" VerticalAlignment="Center" />
                    <wb:NumericBox Grid.Column="1" Value="205" Unit="GPa" />
                </Grid>
            </Expander>
            """;

        GroupBoxExample.Xaml = """
            <GroupBox Header="Solver Termination">
                <StackPanel>
                    <CheckBox Content="Max iterations limit" IsChecked="True" />
                    <wb:NumericBox Value="500" Increment="50" Margin="0,6" />
                </StackPanel>
            </GroupBox>
            """;
    }
}
