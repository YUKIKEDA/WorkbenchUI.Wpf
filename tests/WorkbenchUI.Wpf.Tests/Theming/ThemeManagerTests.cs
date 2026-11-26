using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using WorkbenchUI.Wpf.Theming;

namespace WorkbenchUI.Wpf.Tests.Theming;

public class ThemeManagerTests
{
    private static void EnsurePackSchemeRegistered()
    {
        // Creating Application registers the pack:// URI scheme and WPF resource loading in headless xUnit runs.
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
    public void WorkbenchTheme_DefaultsToDarkAndExposesExpectedTokens()
    {
        RunSta(() =>
        {
            ThemeManager.ResetAccent();
            var theme = new WorkbenchTheme();

            Assert.Equal(WorkbenchThemeVariant.Dark, theme.Theme);
            Assert.Equal(WorkbenchThemeVariant.Dark, ThemeManager.CurrentTheme);
            Assert.Null(ThemeManager.CurrentAccent);
            Assert.IsType<Color>(theme["Wb.Color.Surface.Window"]);
            Assert.IsAssignableFrom<Brush>(theme["Wb.Brush.Surface.Window"]);
            Assert.IsAssignableFrom<Geometry>(theme["Wb.Icon.Save"]);
        });
    }

    [Fact]
    public void SetTheme_SwitchesVariantAndRaisesThemeChanged()
    {
        RunSta(() =>
        {
            ThemeManager.ResetAccent();
            var theme = new WorkbenchTheme();
            var raisedCount = 0;
            void Handler(object? s, EventArgs e) => raisedCount++;
            ThemeManager.ThemeChanged += Handler;

            try
            {
                ThemeManager.SetTheme(WorkbenchThemeVariant.Light);
                Assert.Equal(WorkbenchThemeVariant.Light, theme.Theme);
                Assert.Equal(WorkbenchThemeVariant.Light, ThemeManager.CurrentTheme);
                Assert.Equal(1, raisedCount);

                ThemeManager.SetTheme(WorkbenchThemeVariant.Dark);
                Assert.Equal(WorkbenchThemeVariant.Dark, theme.Theme);
                Assert.Equal(WorkbenchThemeVariant.Dark, ThemeManager.CurrentTheme);
                Assert.Equal(2, raisedCount);
            }
            finally
            {
                ThemeManager.ThemeChanged -= Handler;
            }
        });
    }

    [Fact]
    public void SetAccent_And_ResetAccent_UpdateTokensAndCurrentAccent()
    {
        RunSta(() =>
        {
            ThemeManager.ResetAccent();
            var theme = new WorkbenchTheme();
            var customAccent = Color.FromRgb(0x2E, 0xA0, 0x43);

            ThemeManager.SetAccent(customAccent);
            Assert.Equal(customAccent, ThemeManager.CurrentAccent);
            Assert.Equal(customAccent, Assert.IsType<Color>(theme["Wb.Color.Accent.Default"]));

            var brush = Assert.IsType<SolidColorBrush>(theme["Wb.Brush.Accent.Default"]);
            Assert.Equal(customAccent, brush.Color);
            Assert.True(brush.IsFrozen);

            ThemeManager.ResetAccent();
            Assert.Null(ThemeManager.CurrentAccent);
        });
    }

    [Fact]
    public void GetSystemTheme_ReturnsValidVariant()
    {
        var variant = ThemeManager.GetSystemTheme();
        Assert.True(Enum.IsDefined(variant));
    }
}
