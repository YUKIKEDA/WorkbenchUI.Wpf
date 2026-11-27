using System.Windows.Controls;
using System.Windows.Markup;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Tests.Ribbon;

/// <summary>Verifies ribbon structural logic. Instantiates UI elements without displaying windows.</summary>
public class WorkbenchRibbonTests
{
    [Fact]
    public void RibbonTab_GroupsIsContentProperty()
    {
        var attribute = (ContentPropertyAttribute?)Attribute.GetCustomAttribute(
            typeof(WorkbenchRibbonTab), typeof(ContentPropertyAttribute));
        Assert.NotNull(attribute);
        Assert.Equal(nameof(WorkbenchRibbonTab.Groups), attribute.Name);
    }

    [Fact]
    public void RibbonTab_GroupsAreHostedInContentItemsControl()
    {
        StaTestRunner.Run(() =>
        {
            var tab = new WorkbenchRibbonTab();
            var group = new WorkbenchRibbonGroup { Header = "Test" };
            tab.Groups.Add(group);

            var host = Assert.IsAssignableFrom<ItemsControl>(tab.Content);
            Assert.Same(tab.Groups, host.ItemsSource);
            Assert.Single(tab.Groups);
        });
    }

    [Fact]
    public void Ribbon_GeneratesRibbonTabContainers()
    {
        StaTestRunner.Run(() =>
        {
            var ribbon = new WorkbenchRibbon();
            var tab = new WorkbenchRibbonTab { Header = "Model" };
            ribbon.Items.Add(tab);

            // WorkbenchRibbonTab is its own container (same convention as TabControl/TabItem)
            Assert.True(ribbon.Items.Count == 1);
            Assert.Same(tab, ribbon.Items[0]);
        });
    }

    [Fact]
    public void Ribbon_ApplicationLabelDefaultsToEnglish()
    {
        StaTestRunner.Run(() =>
        {
            // Library has no built-in localized resources (defaults to English)
            var ribbon = new WorkbenchRibbon();
            Assert.Equal("File", ribbon.ApplicationLabel);
        });
    }

    [Fact]
    public void ButtonAssist_IconRoundTrips()
    {
        StaTestRunner.Run(() =>
        {
            var button = new Button();
            Assert.Null(ButtonAssist.GetIcon(button));

            ButtonAssist.SetIcon(button, WorkbenchIcons.Play);
            Assert.Same(WorkbenchIcons.Play, ButtonAssist.GetIcon(button));

            Assert.Equal(16.0, ButtonAssist.GetIconSize(button));
            ButtonAssist.SetIconSize(button, 24.0);
            Assert.Equal(24.0, ButtonAssist.GetIconSize(button));
        });
    }
}
