using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Gallery.Pages;

namespace WorkbenchUI.Wpf.Gallery.Infrastructure;

/// <summary>Represents a collapsible navigation category in the Gallery catalog.</summary>
public sealed record GalleryCategory(
    string Id,
    string Title,
    string Description,
    Geometry Icon,
    bool IsExpandedByDefault = false);

/// <summary>Describes a navigable page in the Gallery application.</summary>
public sealed record GalleryPageDescriptor(
    string Id,
    string Title,
    string Summary,
    string CategoryId,
    string PackageName,
    string Namespace,
    Geometry Icon,
    string[] Keywords,
    Func<IGalleryHost, object> CreatePage,
    bool IsFeatured = false,
    string? Badge = null,
    bool IsFullBleed = false);

/// <summary>Central registry of categories, pages, and keyword search across the Gallery.</summary>
public static class GalleryCatalog
{
    public const string HomePageId = "home";
    public const string SettingsPageId = "settings";

    public static IReadOnlyList<GalleryCategory> Categories { get; } =
    [
        new("foundation", "Foundation", "Design tokens, semantic brushes, typography, spacing, and built-in vector icons.", WorkbenchIcons.Layers, IsExpandedByDefault: true),
        new("basic-inputs", "Basic Inputs", "Core interactive buttons, split actions, text inputs, selectors, and sliders.", WorkbenchIcons.Edit, IsExpandedByDefault: true),
        new("engineering-inputs", "Engineering Inputs", "Precision numeric, vector, matrix, path, color, and unit-aware input controls.", WorkbenchIcons.Probe, IsExpandedByDefault: true),
        new("data-viz", "Data & Visualization", "Property inspectors, virtualized model trees, data grids, color maps, and consoles.", WorkbenchIcons.Grid, IsExpandedByDefault: false),
        new("nav-workflow", "Navigation & Workflow", "Shell navigation rails, command ribbons, tabs, menus, and expandable containers.", WorkbenchIcons.Menu, IsExpandedByDefault: true),
        new("feedback-windows", "Feedback & Windows", "Custom window chrome, dialogs, notifications, progress indicators, and shell toolbars.", WorkbenchIcons.Info, IsExpandedByDefault: false),
        new("docking-charts", "Docking & Charts", "Dockable workspace panes and high-performance engineering charts.", WorkbenchIcons.ChartLine, IsExpandedByDefault: false),
        new("viewport-3d", "3D Viewport", "Hardware-accelerated FEM/CAE 3D mesh rendering, contours, slices, probes, and benchmarks.", WorkbenchIcons.Cube, IsExpandedByDefault: false),
    ];

    public static GalleryPageDescriptor HomeDescriptor { get; } = new(
        Id: HomePageId,
        Title: "Home",
        Summary: "Overview of all WorkbenchUI.Wpf controls, packages, and interactive examples.",
        CategoryId: "gallery",
        PackageName: "WorkbenchUI.Wpf",
        Namespace: "WorkbenchUI.Wpf.Controls",
        Icon: WorkbenchIcons.Home,
        Keywords: ["home", "overview", "catalog", "components"],
        CreatePage: host => new HomePage(host),
        IsFullBleed: true);

    public static GalleryPageDescriptor SettingsDescriptor { get; } = new(
        Id: SettingsPageId,
        Title: "Settings",
        Summary: "Configure Dark/Light theme, dynamic accent colors, navigation pane mode, and keyboard shortcuts.",
        CategoryId: "gallery",
        PackageName: "WorkbenchUI.Wpf",
        Namespace: "WorkbenchUI.Wpf.Theming",
        Icon: WorkbenchIcons.Settings,
        Keywords: ["settings", "theme", "dark", "light", "accent", "preferences"],
        CreatePage: host => new SettingsPage(host),
        IsFullBleed: true);

    public static IReadOnlyList<GalleryPageDescriptor> Pages { get; } =
    [
        // Foundation
        new("tokens", "Design Tokens", "Semantic surface/text/accent brushes, resolved live hex values, typography, and spacing scale.", "foundation",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Theming", WorkbenchIcons.Sun,
            ["tokens", "colors", "brushes", "accent", "typography", "spacing", "theme"],
            _ => new TokensPage(), IsFeatured: true, IsFullBleed: true),
        new("icons", "Icons", "Interactive browser for all built-in 16×16 stroke vector icons and XAML resource snippets.", "foundation",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Grid,
            ["icons", "vector", "geometry", "WorkbenchIcon", "WorkbenchIcons"],
            _ => new IconsPage(), IsFullBleed: true),

        // Basic Inputs
        new("buttons", "Button & SplitButton", "Accent, Standard, Subtle, Danger, Icon buttons, DropDownButton, and SplitButton.", "basic-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Play,
            ["button", "accent", "subtle", "danger", "icon", "ButtonAssist", "DropDownButton", "SplitButton"],
            _ => new ButtonsPage(), IsFeatured: true, Badge: "Updated", IsFullBleed: true),
        new("inputs", "Form Inputs", "Standard WPF CheckBox, RadioButton, TextBox validation, ComboBox, Slider, and ProgressBar styles.", "basic-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Edit,
            ["checkbox", "radiobutton", "textbox", "validation", "combobox", "slider", "progressbar"],
            _ => new InputsPage()),

        // Engineering Inputs
        new("numeric-box", "NumericBox", "Unit-aware numeric editor with spin buttons, scientific notation, validation, and IUnitProvider.", "engineering-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Probe,
            ["NumericBox", "number", "unit", "IUnitProvider", "scientific", "validation"],
            _ => new NumericBoxPage(), IsFeatured: true, Badge: "Updated", IsFullBleed: true),
        new("pickers", "Pickers & Range", "PathBox file/folder picker, Vector3Box, RangeSlider, ColorPicker, and ColorEditor.", "engineering-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Filter,
            ["PathBox", "Vector3Box", "RangeSlider", "ColorPicker", "ColorEditor", "DropDownButton", "SplitButton"],
            _ => new PickersPage()),
        new("misc-inputs", "CheckComboBox, Matrix & Wizard", "Multi-select CheckComboBox, MatrixBox tensor editor, KeyGestureBox, and Wizard flows.", "engineering-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Grid,
            ["CheckComboBox", "MatrixBox", "KeyGestureBox", "Wizard", "StepIndicator"],
            _ => new MiscInputsPage()),

        // Data & Visualization
        new("property-grid", "PropertyGrid", "Typed property inspector with search filtering, categories, and expandable sub-properties.", "data-viz",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.List,
            ["PropertyGrid", "inspector", "properties", "PropertyItem"],
            _ => new PropertyGridPage(), IsFeatured: true),
        new("model-tree", "ModelTree", "Virtualized hierarchy tree supporting thousands of CAE nodes, inline rename, and visibility toggles.", "data-viz",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Layers,
            ["ModelTree", "tree", "virtualized", "hierarchy", "TreeNode"],
            _ => new ModelTreePage(), IsFeatured: true),
        new("data-grid", "DataGrid", "High-density engineering table styling with column assist, selection states, and alternating rows.", "data-viz",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Grid,
            ["DataGrid", "table", "DataGridAssist", "rows"],
            _ => new DataGridPage()),
        new("color-map", "ColorMap & Scale", "Continuous/discrete LUT legends and ColorScaleEditor for scalar field post-processing.", "data-viz",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Contour,
            ["ColorMap", "ColorMapLegend", "ColorScale", "ColorScaleEditor", "lut"],
            _ => new ColorMapPage()),
        new("post-processing", "Post-Processing", "ProcessRail chapter stepper, ChapterHost container, and time-step PlaybackBar.", "data-viz",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Play,
            ["ProcessRail", "ChapterHost", "PlaybackBar", "workflow", "playback"],
            _ => new PostProcessingPage()),
        new("log-console", "LogConsole & Progress", "Filtered diagnostic log buffer console and determinate ProgressDisplay with elapsed/cancel.", "data-viz",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.File,
            ["LogConsole", "LogBuffer", "ProgressDisplay", "logging"],
            _ => new LogConsolePage()),

        // Navigation & Workflow
        new("navigation-view", "NavigationView", "Collapsible left navigation sidebar and compact icon rail with nested child flyouts.", "nav-workflow",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Layout,
            ["NavigationView", "NavigationViewItem", "sidebar", "rail", "flyout", "navigation"],
            _ => new NavigationViewPage(), IsFeatured: true, Badge: "New", IsFullBleed: true),
        new("ribbon", "Ribbon", "Compact contextual command ribbon with tabs, button groups, and quick actions.", "nav-workflow",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Layout,
            ["WorkbenchRibbon", "ribbon", "tabs", "commands"],
            _ => new RibbonPage()),
        new("navigation-menus", "TabControl & Menus", "Menu, ContextMenu, TabControl, ListBox, ScrollBar, and ToolTip styling.", "nav-workflow",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Menu,
            ["Menu", "ContextMenu", "TabControl", "ListBox", "ScrollBar", "ToolTip"],
            _ => new NavigationPage()),
        new("expander-containers", "Expander & Containers", "Collapsible Expander sections, GroupBox framing, and layered panel surfaces.", "nav-workflow",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.ChevronDown,
            ["Expander", "GroupBox", "container", "accordion", "panel"],
            _ => new ExpanderPage(), Badge: "New", IsFullBleed: true),

        // Feedback & Windows
        new("windows-dialogs", "Windows & Dialogs", "Custom-chrome WorkbenchWindow, WorkbenchDialogWindow, MessageBox, ToastHost, and BusyOverlay.", "feedback-windows",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Layout,
            ["WorkbenchWindow", "WorkbenchDialogWindow", "WorkbenchMessageBox", "ToastHost", "BusyOverlay", "dialog"],
            _ => new WindowsPage()),
        new("more-controls", "InfoBar, Toggle & Standard", "Inline InfoBar alerts, ToggleSwitch, ProgressRing, TreeView, ListView, PasswordBox, and RichTextBox.", "feedback-windows",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Info,
            ["InfoBar", "ToggleSwitch", "ProgressRing", "TreeView", "ListView", "PasswordBox", "RichTextBox", "Label", "Hyperlink"],
            _ => new MoreControlsPage()),
        new("shell", "Shell & ToolBars", "ToolBarTray overflow handling, StatusBar, GridSplitter panes, and debounced SearchBox.", "feedback-windows",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Layout,
            ["ToolBar", "StatusBar", "GridSplitter", "SearchBox"],
            _ => new ShellPage()),

        // Docking & Charts
        new("docking", "Docking", "AvalonDock integration styled with live semantic Dark/Light theme and accent tokens.", "docking-charts",
            "WorkbenchUI.Wpf.Docking", "WorkbenchUI.Wpf.Docking", WorkbenchIcons.Layout,
            ["Docking", "DockingManager", "AvalonDock", "panels", "tabs"],
            _ => new DockingPage(), IsFeatured: true, IsFullBleed: true),
        new("charts", "Charts", "ScottPlot-backed LineChart, HistogramChart, ScatterChart, and HeatmapChart synced with ThemeManager.", "docking-charts",
            "WorkbenchUI.Wpf.Charts", "WorkbenchUI.Wpf.Charts", WorkbenchIcons.ChartLine,
            ["Charts", "LineChart", "HistogramChart", "ScatterChart", "HeatmapChart", "ScottPlot"],
            _ => new ChartsPage(), IsFeatured: true),

        // 3D Viewport
        new("viewport-3d", "Overview & Contours", "Hardware-accelerated FEM mesh viewer with scalar contour shading and camera presets.", "viewport-3d",
            "WorkbenchUI.Wpf.Viewport3D", "WorkbenchUI.Wpf.Viewport3D", WorkbenchIcons.Cube,
            ["3D", "Viewport", "FEM", "contour", "mesh", "camera"],
            _ => new Viewport3DPage(), IsFeatured: true, IsFullBleed: true),
        new("viewport-deformation", "Deformation", "Modal/static displacement amplification with undeformed wireframe reference overlay.", "viewport-3d",
            "WorkbenchUI.Wpf.Viewport3D", "WorkbenchUI.Wpf.Viewport3D", WorkbenchIcons.Mesh,
            ["3D", "deformation", "displacement", "mode", "warp"],
            _ => new ViewportDeformationPage(), IsFullBleed: true),
        new("viewport-section", "Section Cut", "Interactive clipping planes with capped interior scalar visualization.", "viewport-3d",
            "WorkbenchUI.Wpf.Viewport3D", "WorkbenchUI.Wpf.Viewport3D", WorkbenchIcons.Section,
            ["3D", "section", "clip", "slice", "plane"],
            _ => new ViewportSectionPage(), IsFullBleed: true),
        new("viewport-probe", "Probing & Annotations", "Raycast node/element picking, callout annotations, and min/max markers.", "viewport-3d",
            "WorkbenchUI.Wpf.Viewport3D", "WorkbenchUI.Wpf.Viewport3D", WorkbenchIcons.Probe,
            ["3D", "probe", "pick", "annotation", "callout"],
            _ => new ViewportProbePage(), IsFullBleed: true),
        new("viewport-glyphs", "Vector Glyphs", "Instanced 3D arrow fields for reaction forces, velocity vectors, and flux direction.", "viewport-3d",
            "WorkbenchUI.Wpf.Viewport3D", "WorkbenchUI.Wpf.Viewport3D", WorkbenchIcons.Vector,
            ["3D", "glyphs", "vectors", "arrows", "instancing"],
            _ => new ViewportGlyphsPage(), IsFullBleed: true),
        new("viewport-voxels", "Sparse Voxels", "Sparse voxel octree / volume grid rendering for topology optimization and CT data.", "viewport-3d",
            "WorkbenchUI.Wpf.Viewport3D", "WorkbenchUI.Wpf.Viewport3D", WorkbenchIcons.Cube,
            ["3D", "voxels", "topology", "sparse", "grid"],
            _ => new ViewportVoxelsPage(), IsFullBleed: true),
        new("viewport-benchmark", "Large-Mesh Benchmark", "Million-triangle GPU throughput benchmark with frame-time diagnostics.", "viewport-3d",
            "WorkbenchUI.Wpf.Viewport3D", "WorkbenchUI.Wpf.Viewport3D", WorkbenchIcons.ChartBar,
            ["3D", "benchmark", "performance", "fps", "throughput"],
            _ => new ViewportBenchmarkPage(), IsFullBleed: true),
    ];

    /// <summary>Gets all navigable pages including Home, catalog pages, and Settings.</summary>
    public static IEnumerable<GalleryPageDescriptor> AllNavigablePages
    {
        get
        {
            yield return HomeDescriptor;
            foreach (var page in Pages)
            {
                yield return page;
            }

            yield return SettingsDescriptor;
        }
    }

    /// <summary>Finds a descriptor by identifier, returning null if unknown.</summary>
    public static GalleryPageDescriptor? FindById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (string.Equals(id, HomePageId, StringComparison.OrdinalIgnoreCase))
        {
            return HomeDescriptor;
        }

        if (string.Equals(id, SettingsPageId, StringComparison.OrdinalIgnoreCase))
        {
            return SettingsDescriptor;
        }

        return Pages.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Looks up a category by identifier.</summary>
    public static GalleryCategory? GetCategory(string categoryId) =>
        Categories.FirstOrDefault(c => string.Equals(c.Id, categoryId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Searches catalog pages (plus Settings) by title, summary, package, category, or keywords,
    /// returning up to 12 ranked matches.
    /// </summary>
    public static IReadOnlyList<GalleryPageDescriptor> Search(string? query)
    {
        var q = query?.Trim();
        if (string.IsNullOrEmpty(q))
        {
            return [];
        }

        var scored = new List<(int Score, GalleryPageDescriptor Descriptor)>();
        foreach (var page in AllNavigablePages)
        {
            var categoryTitle = GetCategory(page.CategoryId)?.Title ?? page.CategoryId;
            var score = 0;

            if (page.Title.StartsWith(q, StringComparison.OrdinalIgnoreCase))
            {
                score = 100;
            }
            else if (page.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                score = 80;
            }
            else if (page.Keywords.Any(k => k.StartsWith(q, StringComparison.OrdinalIgnoreCase)))
            {
                score = 65;
            }
            else if (page.Keywords.Any(k => k.Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                score = 50;
            }
            else if (categoryTitle.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                     page.PackageName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                     page.Summary.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                score = 30;
            }

            if (score > 0)
            {
                scored.Add((score, page));
            }
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Descriptor.Title, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .Select(x => x.Descriptor)
            .ToList();
    }
}
