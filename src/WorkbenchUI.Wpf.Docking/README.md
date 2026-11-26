# WorkbenchUI.Wpf.Docking

AvalonDock integration for `WorkbenchUI.Wpf`, providing a token-driven dock theme and layout persistence helpers.

## Included Components

- `WorkbenchDockTheme`: AvalonDock `DictionaryTheme` using VS2013 control templates bound dynamically to `Wb.Color.*` tokens (automatically follows runtime Dark/Light theme and accent changes).
- `DockLayout`: Thin wrapper around `XmlLayoutSerializer` (`Save`, `SaveToString`, `Load`, `LoadFromString`) with `ContentId`-based content resolution.

## Usage

```xml
<ad:DockingManager>
    <ad:DockingManager.Theme>
        <wb:WorkbenchDockTheme />
    </ad:DockingManager.Theme>
</ad:DockingManager>
```

## Third-Party License Notice

`WorkbenchUI.Wpf.Docking` is licensed under the MIT License, and depends on **`Dirkster.AvalonDock`** and **`Dirkster.AvalonDock.Themes.VS2013`**, which are licensed under the **Microsoft Public License (Ms-PL)**. Please review the Ms-PL terms when redistributing applications that include this package.
