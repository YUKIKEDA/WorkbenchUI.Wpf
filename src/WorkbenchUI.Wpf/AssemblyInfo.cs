using System.Windows;
using System.Windows.Markup;

[assembly: ThemeInfo(
    // Theme dictionaries are managed explicitly by WorkbenchTheme + token dictionaries.
    ResourceDictionaryLocation.None,
    // Default control styles are resolved from Themes/Generic.xaml.
    ResourceDictionaryLocation.SourceAssembly
)]

// Consumers can access all namespaces with a single XAML declaration:
// xmlns:wb="https://schemas.workbenchui.dev/wpf"
[assembly: XmlnsPrefix("https://schemas.workbenchui.dev/wpf", "wb")]
[assembly: XmlnsDefinition("https://schemas.workbenchui.dev/wpf", "WorkbenchUI.Wpf.Controls")]
[assembly: XmlnsDefinition("https://schemas.workbenchui.dev/wpf", "WorkbenchUI.Wpf.Theming")]
