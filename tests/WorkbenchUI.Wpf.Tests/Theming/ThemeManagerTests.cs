using System.Windows.Media;
using WorkbenchUI.Wpf.Theming;

namespace WorkbenchUI.Wpf.Tests.Theming;

public class ThemeManagerTests
{
    [Fact]
    public void WorkbenchTheme_DefaultsToDarkAndExposesExpectedTokens()
    {
        StaTestRunner.Run(() =>
        {
            try
            {
                ThemeManager.ResetAccent();
                var theme = new WorkbenchTheme();

                Assert.Equal(WorkbenchThemeVariant.Dark, theme.Theme);
                Assert.Equal(WorkbenchThemeVariant.Dark, ThemeManager.CurrentTheme);
                Assert.Null(ThemeManager.CurrentAccent);
                Assert.IsType<Color>(theme["Wb.Color.Surface.Window"]);
                Assert.IsAssignableFrom<Brush>(theme["Wb.Brush.Surface.Window"]);
                Assert.IsAssignableFrom<Geometry>(theme["Wb.Icon.Save"]);
            }
            finally
            {
                ThemeManager.ResetAccent();
                ThemeManager.SetTheme(WorkbenchThemeVariant.Dark);
            }
        });
    }

    [Fact]
    public void SetTheme_SwitchesVariantAndRaisesThemeChanged()
    {
        StaTestRunner.Run(() =>
        {
            ThemeManager.ResetAccent();
            var theme = new WorkbenchTheme();
            var initialDictionaryCount = theme.MergedDictionaries.Count;
            var raisedCount = 0;
            void Handler(object? s, EventArgs e) => raisedCount++;
            ThemeManager.ThemeChanged += Handler;

            try
            {
                ThemeManager.SetTheme(WorkbenchThemeVariant.Light);
                Assert.Equal(WorkbenchThemeVariant.Light, theme.Theme);
                Assert.Equal(WorkbenchThemeVariant.Light, ThemeManager.CurrentTheme);
                Assert.Equal(initialDictionaryCount, theme.MergedDictionaries.Count);
                Assert.Equal(1, raisedCount);

                ThemeManager.SetTheme(WorkbenchThemeVariant.Dark);
                Assert.Equal(WorkbenchThemeVariant.Dark, theme.Theme);
                Assert.Equal(WorkbenchThemeVariant.Dark, ThemeManager.CurrentTheme);
                Assert.Equal(initialDictionaryCount, theme.MergedDictionaries.Count);
                Assert.Equal(2, raisedCount);
            }
            finally
            {
                ThemeManager.ThemeChanged -= Handler;
                ThemeManager.ResetAccent();
                ThemeManager.SetTheme(WorkbenchThemeVariant.Dark);
            }
        });
    }

    [Fact]
    public void WorkbenchTheme_ThemePropertySetter_UpdatesManagerAndRaisesThemeChanged()
    {
        StaTestRunner.Run(() =>
        {
            ThemeManager.ResetAccent();
            var theme = new WorkbenchTheme();
            var initialDictionaryCount = theme.MergedDictionaries.Count;
            var raisedCount = 0;
            void Handler(object? s, EventArgs e) => raisedCount++;
            ThemeManager.ThemeChanged += Handler;

            try
            {
                theme.Theme = WorkbenchThemeVariant.Light;

                Assert.Equal(WorkbenchThemeVariant.Light, ThemeManager.CurrentTheme);
                Assert.Equal(initialDictionaryCount, theme.MergedDictionaries.Count);
                Assert.Equal(1, raisedCount);
            }
            finally
            {
                ThemeManager.ThemeChanged -= Handler;
                ThemeManager.ResetAccent();
                ThemeManager.SetTheme(WorkbenchThemeVariant.Dark);
            }
        });
    }

    [Fact]
    public void SetAccent_And_ResetAccent_UpdateTokensAndCurrentAccent()
    {
        StaTestRunner.Run(() =>
        {
            try
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
            }
            finally
            {
                ThemeManager.ResetAccent();
                ThemeManager.SetTheme(WorkbenchThemeVariant.Dark);
            }
        });
    }

    [Fact]
    public void GetSystemTheme_ReturnsValidVariant()
    {
        var variant = ThemeManager.GetSystemTheme();
        Assert.True(Enum.IsDefined(variant));
    }
}
