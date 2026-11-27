using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class ProcessRailPage : UserControl
{
    public ProcessRailPage()
    {
        InitializeComponent();

        WorkflowExample.Xaml = """
            <wb:ProcessRail x:Name="WorkflowRail" SelectedIndex="2">
                <wb:ProcessRailItem Content="Geometry" State="Completed" />
                <wb:ProcessRailItem Content="Materials" State="Completed" />
                <wb:ProcessRailItem Content="Meshing" State="Current" />
                <wb:ProcessRailItem Content="Solve" State="Available" />
                <wb:ProcessRailItem Content="Post-Process" State="Locked" />
            </wb:ProcessRail>

            <wb:ChapterHost SelectedIndex="{Binding ElementName=WorkflowRail, Path=SelectedIndex, Mode=TwoWay}">
                <wb:ChapterHostItem Header="Geometry">...</wb:ChapterHostItem>
                <wb:ChapterHostItem Header="Materials">...</wb:ChapterHostItem>
                <wb:ChapterHostItem Header="Meshing">...</wb:ChapterHostItem>
                <wb:ChapterHostItem Header="Solve">...</wb:ChapterHostItem>
                <wb:ChapterHostItem Header="Post-Process">...</wb:ChapterHostItem>
            </wb:ChapterHost>
            """;

        WorkflowExample.CSharp = """
            // Mark downstream chapter stale after upstream edits
            ChapterMesh.State = ProcessChapterState.Stale;

            // Advance chapter progression
            ChapterSolve.State = ProcessChapterState.Completed;
            ChapterPost.State = ProcessChapterState.Current;
            WorkflowRail.SelectedIndex = 4;
            """;
    }

    private void CompleteMeshing_Click(object sender, RoutedEventArgs e)
    {
        ChapterMesh.State = ProcessChapterState.Completed;
        ChapterSolve.State = ProcessChapterState.Current;
        WorkflowRail.SelectedIndex = 3;
    }

    private void CompleteSolve_Click(object sender, RoutedEventArgs e)
    {
        ChapterSolve.State = ProcessChapterState.Completed;
        ChapterPost.State = ProcessChapterState.Current;
        WorkflowRail.SelectedIndex = 4;
    }

    private void MarkMeshStale_Click(object sender, RoutedEventArgs e)
    {
        ChapterMesh.State = ProcessChapterState.Stale;
        WorkflowRail.SelectedIndex = 2;
    }

    private void UnlockPost_Click(object sender, RoutedEventArgs e)
    {
        ChapterPost.State = ProcessChapterState.Available;
    }

    private void ResetWorkflow_Click(object sender, RoutedEventArgs e)
    {
        ChapterGeo.State = ProcessChapterState.Completed;
        ChapterMat.State = ProcessChapterState.Completed;
        ChapterMesh.State = ProcessChapterState.Current;
        ChapterSolve.State = ProcessChapterState.Available;
        ChapterPost.State = ProcessChapterState.Locked;
        WorkflowRail.SelectedIndex = 2;
    }
}
