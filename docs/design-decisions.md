# Architecture & Design Decisions

This document records the architectural, naming, packaging, and API decisions made when redefining and extracting the internal prototype (`WpfCustomUI`) into the public open-source library **WorkbenchUI.Wpf**.

---

## ADR-01: Positioning & Package Modularity

- **Status**: Accepted
- **Context**: Engineering and scientific desktop applications (CAD/CAE pre/post-processors, simulation workbenches, instrument consoles) need high-density, low-chroma UI chrome that does not compete visually with color-mapped results, alongside domain-specific controls (unit-aware numeric boxes, property grids, virtualized model trees, high-throughput log consoles, charting, docking, and 3D viewports).
- **Decision**:
  1. Keep **`WorkbenchUI.Wpf`** (the core package) **zero-dependency** (WPF framework assemblies only).
  2. Isolate third-party library integrations into dedicated optional extension packages:
     - `WorkbenchUI.Wpf.Charts` (`ScottPlot.WPF`)
     - `WorkbenchUI.Wpf.Docking` (`Dirkster.AvalonDock`, `Dirkster.AvalonDock.Themes.VS2013`)
     - `WorkbenchUI.Wpf.Viewport3D` (`Silk.NET` Direct3D 11 / D3D9Ex / DXGI)
  3. Ship `WorkbenchUI.Wpf.Gallery` as the interactive showcase sample. Domain-specific internal applications (`CaeStudio`, `CastingStudio`) are excluded from this repository.
- **Consequences**: Consumers who only need themes and workbench controls do not pull SkiaSharp, AvalonDock, or Silk.NET native/managed bindings.

---

## ADR-02: Target Framework

- **Status**: Accepted
- **Context**: Choosing between `net8.0-windows`, multi-targeting (`net8.0-windows;net10.0-windows`), and `net10.0-windows`.
- **Decision**: Target **`net10.0-windows`** exclusively.
- **Rationale**: .NET 8 LTS reached end-of-life in November 2026, making .NET 10 the active LTS target. A single TFM avoids conditional compilation and simplifies CI, packaging, and maintenance.

---

## ADR-03: Naming, Rebranding & XAML Namespace

- **Status**: Accepted
- **Context**: The internal codebase used `WpfCustomUI.*` packages, `Wcu*` type prefixes, `Wcu.*` resource keys, and `https://schemas.wpfcustomui.dev/xaml`.
- **Decision**:
  1. **Packages & Root Namespaces**:
     - Core: `WorkbenchUI.Wpf` (public CLR namespaces: `WorkbenchUI.Wpf.Controls`, `WorkbenchUI.Wpf.Theming`)
     - Charts: `WorkbenchUI.Wpf.Charts`
     - Docking: `WorkbenchUI.Wpf.Docking`
     - Viewport3D: `WorkbenchUI.Wpf.Viewport3D` (`WorkbenchUI.Wpf.Viewport3D.Rendering` is `internal`)
     - Gallery: `WorkbenchUI.Wpf.Gallery`
  2. **Prefixed Public Types (`Wcu*` → `Workbench*`)**:
     We evaluated three options: full `Workbench*` prefix on all 14 prefixed types, short `Wb*` prefix, and minimal prefixing. We chose **`Workbench*`** across all 14 prefixed types for API consistency, adherence to .NET naming guidelines (avoiding cryptic abbreviations in public type names), and alignment with established WPF libraries (e.g., `MetroWindow`, `FluentWindow`), while keeping domain controls (`NumericBox`, `ModelTree`, `PropertyGrid`, `LogConsole`, etc.) unprefixed.
  3. **Resource Keys (`Wcu.*` → `Wb.*`)**:
     Resource dictionary keys are written frequently in XAML (`{DynamicResource Wb.Brush.Surface.Panel}`, `{StaticResource Wb.Button.Accent}`, `Wb.Icon.*`), so `Wb.*` provides collision safety and brevity.
  4. **XAML Namespace URI**:
     `https://schemas.workbenchui.dev/wpf` with recommended prefix `wb` (`XmlnsPrefix`), aggregating all four libraries into a single XML namespace declaration.

### Mapping Table

| Category | Legacy (`WpfCustomUI`) | Public (`WorkbenchUI.Wpf`) |
| --- | --- | --- |
| Core Package | `WpfCustomUI.Controls` | `WorkbenchUI.Wpf` |
| Extension Packages | `WpfCustomUI.{Charts,Docking,Viewport3D}` | `WorkbenchUI.Wpf.{Charts,Docking,Viewport3D}` |
| XAML Namespace URI | `https://schemas.wpfcustomui.dev/xaml` (`ui`) | `https://schemas.workbenchui.dev/wpf` (`wb`) |
| Resource Keys | `Wcu.*` | `Wb.*` |
| Theme / Variant | `WcuTheme`, `WcuThemeVariant` | `WorkbenchTheme`, `WorkbenchThemeVariant` |
| Windows / Dialogs | `WcuWindow`, `WcuDialogWindow`, `WcuMessageBox` | `WorkbenchWindow`, `WorkbenchDialogWindow`, `WorkbenchMessageBox` |
| Ribbon | `WcuRibbon`, `WcuRibbonTab`, `WcuRibbonGroup` | `WorkbenchRibbon`, `WorkbenchRibbonTab`, `WorkbenchRibbonGroup` |
| Icons | `WcuIcon`, `WcuIcons` | `WorkbenchIcon`, `WorkbenchIcons` |
| Charts | `WcuPlot`, `WcuChartTheme` | `WorkbenchPlot`, `WorkbenchChartTheme` |
| Docking | `WcuDockTheme` | `WorkbenchDockTheme` |
| Viewport3D | `WcuViewport` | `WorkbenchViewport` |

---

## ADR-04: Theme System Architecture

- **Status**: Accepted
- **Decision**:
  - Maintain a **custom lookless theme system** independent of WPF's built-in Fluent `ThemeMode`, preserving high-density sizing (`ControlHeight` 24px, 4px spacing grid, 2px corner radius) and neutral-gray surfaces.
  - Use a **two-layer semantic token model**:
    1. Semantic `Wb.Color.*` primitives (consumed by non-WPF-brush renderers such as `WorkbenchChartTheme` and `WorkbenchDockTheme`).
    2. Semantic `Wb.Brush.*` resources referencing the color layer.
  - All color/brush references in control styles use **`DynamicResource`** for runtime switching between `Dark` and `Light` and dynamic accent overrides via `ThemeManager`.
  - Expose `ThemeManager.CurrentTheme` and `ThemeManager.CurrentAccent` alongside `SetTheme`, `SetAccent`, `ResetAccent`, `GetSystemTheme`, and `ThemeChanged`.

---

## ADR-05: Public API Surface & Visibility Policy

- **Status**: Accepted
- **Context**: Internal utility and math classes were previously marked `public` for convenience across internal apps and test projects.
- **Decision**: Restrict internal implementation helpers to **`internal`** visibility and expose them to unit test assemblies via `InternalsVisibleTo`, keeping the public API surface clean and semver-maintainable:
  - **Made `internal` in `WorkbenchUI.Wpf`**: `ColorMath`, `AccentPalette`, `NumericText`, `FlatTreeSource`.
  - **Kept `public` in `WorkbenchUI.Wpf`**: `FlatTreeItem` (element type of `ModelTree.FlatItems`), `ColorMapToBrushConverter`, `TreeIndentConverter`, `VisibilityToBooleanConverter` (used in control templates and custom style overrides).
  - **Made `internal` in `WorkbenchUI.Wpf.Viewport3D`**: `ViewportCulling`, `ViewportDeformation`, `ViewportGeometry`, `ViewportSection`, `ViewportVoxelLod`, `ViewportVoxelPicking` (static algorithm class only), `ViewportVoxelSlice`, `ViewportVoxelSurface`.
  - **Kept `public` in `WorkbenchUI.Wpf.Viewport3D`**: `ViewportVoxelIndexing` (used by callers to build `ViewportVoxelGrid.ActiveCellIndices`), `VoxelProbeResult`, `VoxelProbePickedEventArgs`, `WorkbenchViewport`, data models (`ViewportMesh`, `ViewportVoxelGrid`, `ViewportCamera`, `ViewportSelection`, `ViewportStatistics`, `Bounds3D`, `SectionPlane`, `ProbeResult`, `ViewportAnnotation`, `ViewportHoverInfo`), and enums.

---

## ADR-06: Licensing & Third-Party Notices

- **Status**: Accepted
- **Decision**:
  - Release all original code in `WorkbenchUI.Wpf` under the **MIT License**.
  - `WorkbenchUI.Wpf.Charts` depends on `ScottPlot.WPF` (MIT).
  - `WorkbenchUI.Wpf.Viewport3D` depends on `Silk.NET` (MIT).
  - `WorkbenchUI.Wpf.Docking` depends on `Dirkster.AvalonDock` and `Dirkster.AvalonDock.Themes.VS2013`, which are licensed under the **Microsoft Public License (Ms-PL)**. Document this clearly in `src/WorkbenchUI.Wpf.Docking/README.md` so downstream consumers can evaluate Ms-PL obligations separately from the MIT core.

---

## ADR-07: Repository Layout, Testing & OSS Standards

- **Status**: Accepted
- **Decision**:
  - Migrate into a clean repository history in `WorkbenchUI.Wpf` (`src/`, `tests/`, `samples/`, `docs/`) without carrying over internal `.dev` planning logs or private sample apps.
  - Enable **Central Package Management** (`Directory.Packages.props`), deterministic builds, built-in .NET 10 SDK **SourceLink**, symbol packages (`.snupkg`), XML doc generation (`GenerateDocumentationFile=true`), and GitHub Actions CI (`windows-latest`).
  - Standardize on **English** for all source comments, XML docs, Gallery UI, and primary READMEs, with a `README.ja.md` overview for Japanese-speaking users.
  - Port `WorkbenchUI.Wpf.Tests` and `WorkbenchUI.Wpf.Viewport3D.Tests`, and add `WorkbenchUI.Wpf.Charts.Tests` and `WorkbenchUI.Wpf.Docking.Tests` so all four packages have automated test coverage.

---

## ADR-08: User Documentation (Diátaxis + Docusaurus 3 Bilingual EN/JA)

- **Status**: Accepted
- **Context**: Public library adopters need task-oriented guides, exact token/control references, and architectural context in addition to XML API comments. Both English and Japanese documentation are desired, hosted cleanly on GitHub Pages.
- **Decision**:
  - Structure all user documentation according to the **Diátaxis** framework (`Tutorials`, `How-to Guides`, `Reference`, `Explanation`).
  - Author English source pages under `docs/` (so they remain directly readable in the Git repository) and host a bilingual **Docusaurus 3** site (`en` + `ja`) under `website/`, with Japanese translations under `website/i18n/ja/docusaurus-plugin-content-docs/current/`.
  - Keep `docs/design-decisions.md` in the repository as maintainer-facing documentation and intentionally **exclude** it from the public Docusaurus user site (`exclude: ['design-decisions.md']` in `website/docusaurus.config.ts`) so the published site focuses strictly on library consumers.
  - Automate strict link validation and GitHub Pages deployment via `.github/workflows/docs.yml`.
