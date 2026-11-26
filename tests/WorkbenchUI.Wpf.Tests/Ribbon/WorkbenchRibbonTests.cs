using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using System.Windows.Markup;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Tests.Ribbon;

/// <summary>Verifies ribbon structural logic. Instantiates UI elements without displaying windows.</summary>
public class WorkbenchRibbonTests
{
    /// <summary>Creates WPF elements on an STA thread.</summary>
    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

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
        RunSta(() =>
        {
            var tab = new WorkbenchRibbonTab();
            var group = new WorkbenchRibbonGroup { Header = "テスト" };
            tab.Groups.Add(group);

            var host = Assert.IsAssignableFrom<ItemsControl>(tab.Content);
            Assert.Same(tab.Groups, host.ItemsSource);
            Assert.Single(tab.Groups);
        });
    }

    [Fact]
    public void Ribbon_GeneratesRibbonTabContainers()
    {
        RunSta(() =>
        {
            var ribbon = new WorkbenchRibbon();
            var tab = new WorkbenchRibbonTab { Header = "モデル" };
            ribbon.Items.Add(tab);

            // WorkbenchRibbonTab is its own container (same convention as TabControl/TabItem)
            Assert.True(ribbon.Items.Count == 1);
            Assert.Same(tab, ribbon.Items[0]);
        });
    }

    [Fact]
    public void Ribbon_ApplicationLabelDefaultsToEnglish()
    {
        RunSta(() =>
        {
            // Library has no built-in localized resources (defaults to English)
            var ribbon = new WorkbenchRibbon();
            Assert.Equal("File", ribbon.ApplicationLabel);
        });
    }

    [Fact]
    public void ButtonAssist_IconRoundTrips()
    {
        RunSta(() =>
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
