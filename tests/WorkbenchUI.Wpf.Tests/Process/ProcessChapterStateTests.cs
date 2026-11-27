using System.Windows;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Tests.Process;

public class ProcessChapterStateTests
{
    [Fact]
    public void ProcessChapterState_HasExpectedMembers()
    {
        Assert.Equal(0, (int)ProcessChapterState.Locked);
        Assert.True(Enum.IsDefined(ProcessChapterState.Available));
        Assert.True(Enum.IsDefined(ProcessChapterState.Current));
        Assert.True(Enum.IsDefined(ProcessChapterState.Completed));
        Assert.True(Enum.IsDefined(ProcessChapterState.Stale));
    }

    [Fact]
    public void ProcessRail_SelectionUpdatesVisualStateWithoutOverwritingState()
    {
        StaTestRunner.Run(() =>
        {
            var step1 = new ProcessRailItem { Content = "Geometry", State = ProcessChapterState.Completed };
            var step2 = new ProcessRailItem { Content = "Mesh", State = ProcessChapterState.Stale };
            var rail = new ProcessRail();
            rail.Items.Add(step1);
            rail.Items.Add(step2);

            rail.Measure(new Size(400, 40));
            rail.Arrange(new Rect(0, 0, 400, 40));
            rail.UpdateLayout();

            rail.SelectedIndex = 0;
            Assert.Equal(ProcessChapterState.Completed, step1.State);
            Assert.Equal(ProcessChapterState.Current, step1.VisualState);
            Assert.Equal(ProcessChapterState.Stale, step2.VisualState);

            rail.SelectedIndex = 1;
            Assert.Equal(ProcessChapterState.Completed, step1.State);
            Assert.Equal(ProcessChapterState.Completed, step1.VisualState);
            Assert.Equal(ProcessChapterState.Stale, step2.State);
            Assert.Equal(ProcessChapterState.Current, step2.VisualState);
        });
    }
}
