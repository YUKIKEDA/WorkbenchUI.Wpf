using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using AvalonDock;
using AvalonDock.Layout;
using WorkbenchUI.Wpf.Docking;

namespace WorkbenchUI.Wpf.Docking.Tests;

public class DockLayoutAndThemeTests
{
    private static void EnsurePackSchemeRegistered()
    {
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                EnsurePackSchemeRegistered();
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
    public void Save_NullManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DockLayout.Save(null!, "layout.xml"));
    }

    [Fact]
    public void Load_MissingFile_ReturnsFalse()
    {
        RunSta(() =>
        {
            var manager = new DockingManager();
            var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.xml");

            Assert.False(DockLayout.Load(manager, missing, _ => null));
        });
    }

    [Fact]
    public void SaveToString_And_LoadFromString_RoundTripsKnownPanesAndDropsUnknown()
    {
        RunSta(() =>
        {
            var pane = new LayoutAnchorablePane();
            pane.Children.Add(new LayoutAnchorable
            {
                ContentId = "KnownTool",
                Title = "Inspector",
                Content = new TextBlock { Text = "A" },
            });
            pane.Children.Add(new LayoutAnchorable
            {
                ContentId = "ObsoleteTool",
                Title = "Old",
                Content = new TextBlock { Text = "B" },
            });

            var manager = new DockingManager
            {
                Layout = new LayoutRoot
                {
                    RootPanel = new LayoutPanel(pane),
                },
            };

            var xml = DockLayout.SaveToString(manager);
            Assert.Contains("KnownTool", xml);

            var restored = new DockingManager();
            var resolvedContent = new TextBlock { Text = "Restored" };
            DockLayout.LoadFromString(restored, xml, id => id == "KnownTool" ? resolvedContent : null);

            var anchorables = restored.Layout.Descendents().OfType<LayoutAnchorable>().ToList();
            var known = Assert.Single(anchorables);
            Assert.Equal("KnownTool", known.ContentId);
            Assert.Same(resolvedContent, known.Content);
        });
    }

    [Fact]
    public void WorkbenchDockTheme_LoadsResourceDictionary()
    {
        RunSta(() =>
        {
            var theme = new WorkbenchDockTheme();
            Assert.NotNull(theme.ThemeResourceDictionary);
            Assert.NotNull(theme.ThemeResourceDictionary.Source);
            Assert.Contains("WorkbenchDockResources.xaml", theme.ThemeResourceDictionary.Source.OriginalString);
            Assert.True(theme.ThemeResourceDictionary.Count > 0 || theme.ThemeResourceDictionary.MergedDictionaries.Count > 0);
        });
    }
}
