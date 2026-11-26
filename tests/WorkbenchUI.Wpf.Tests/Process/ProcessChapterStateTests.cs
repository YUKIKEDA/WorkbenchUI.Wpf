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
}
