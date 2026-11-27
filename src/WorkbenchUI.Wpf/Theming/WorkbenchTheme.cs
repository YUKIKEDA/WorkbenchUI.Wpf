using System.Windows;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Theming;

/// <summary>
/// A <see cref="ResourceDictionary"/> that merges all WorkbenchUI.Wpf design tokens
/// and control styles in the required order.
/// Add <c>&lt;wb:WorkbenchTheme Theme="Dark" /&gt;</c> to <c>App.xaml</c>.
/// </summary>
public class WorkbenchTheme : ResourceDictionary
{
    private const string ThemesBaseUri = "pack://application:,,,/WorkbenchUI.Wpf;component/Themes/";

    private static readonly string[] ControlDictionaries =
    [
        "Controls/Shared.xaml",
        "Controls/Button.xaml",
        "Controls/TextBox.xaml",
        "Controls/CheckBox.xaml",
        "Controls/ComboBox.xaml",
        "Controls/ListBox.xaml",
        "Controls/ListView.xaml",
        "Controls/TreeView.xaml",
        "Controls/Text.xaml",
        "Controls/ScrollBar.xaml",
        "Controls/TabControl.xaml",
        "Controls/Menu.xaml",
        "Controls/ToolTip.xaml",
        "Controls/Slider.xaml",
        "Controls/ProgressBar.xaml",
        "Controls/Expander.xaml",
        "Controls/GridSplitter.xaml",
        "Controls/GroupBox.xaml",
        "Controls/Separator.xaml",
        "Controls/ToolBar.xaml",
        "Controls/StatusBar.xaml",
        "Controls/DataGrid.xaml",
        // Merged at application level so implicit DataTemplates for PropertyGrid resolve reliably.
        "Controls/PropertyGrid.xaml",
        // Merged at application level so keyed Ribbon and DropDownButton/SplitButton styles can be referenced via StaticResource.
        "Controls/Ribbon.xaml",
        "Controls/DropDownButton.xaml",
    ];

    private WorkbenchThemeVariant _theme;
    private bool _applied;

    /// <summary>Initializes a new instance of <see cref="WorkbenchTheme"/> with the default <see cref="WorkbenchThemeVariant.Dark"/> variant.</summary>
    public WorkbenchTheme()
    {
        ThemeManager.Register(this);
        Apply(WorkbenchThemeVariant.Dark, notifyManager: true);
    }

    /// <summary>Gets or sets the active theme variant. Changing this property replaces the merged token dictionary.</summary>
    public WorkbenchThemeVariant Theme
    {
        get => _theme;
        set => Apply(value, notifyManager: true);
    }

    internal void Apply(WorkbenchThemeVariant variant, bool notifyManager = true)
    {
        if (_applied && variant == _theme)
        {
            return;
        }

        var isInitialApply = !_applied;
        _theme = variant;
        _applied = true;

        if (isInitialApply)
        {
            // Accent overrides live directly on this dictionary, so clearing MergedDictionaries preserves them.
            MergedDictionaries.Clear();
            MergedDictionaries.Add(Load("Tokens.Core.xaml"));
            MergedDictionaries.Add(Load($"Tokens.{variant}.xaml"));

            // Built-in vector icons (Wb.Icon.* keys, theme-independent frozen Geometries).
            MergedDictionaries.Add(WorkbenchIcons.CreateResourceDictionary());

            // Control styles reference tokens via DynamicResource and stay constant across variants.
            foreach (var dictionary in ControlDictionaries)
            {
                MergedDictionaries.Add(Load(dictionary));
            }
        }
        else if (MergedDictionaries.Count > 1)
        {
            MergedDictionaries[1] = Load($"Tokens.{variant}.xaml");
        }

        if (notifyManager)
        {
            ThemeManager.SyncFromTheme(this, variant, raiseThemeChanged: !isInitialApply);
        }
    }

    private static ResourceDictionary Load(string fileName) =>
        new() { Source = new Uri(ThemesBaseUri + fileName, UriKind.Absolute) };
}
