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
        new("basic-inputs", "Basic Inputs", "Core buttons, split actions, selection boxes, toggles, text entry, and sliders.", WorkbenchIcons.Edit, IsExpandedByDefault: true),
        new("engineering-inputs", "Engineering Inputs", "Precision numeric, vector, matrix, path, color, and shortcut capture controls.", WorkbenchIcons.Probe, IsExpandedByDefault: true),
        new("data-viz", "Data & Visualization", "Property inspectors, virtualized model trees, data grids, color maps, and diagnostic logs.", WorkbenchIcons.Grid, IsExpandedByDefault: false),
        new("nav-workflow", "Navigation & Workflow", "Shell navigation rails, command ribbons, tabs, expanders, process rails, wizards, and playback.", WorkbenchIcons.Menu, IsExpandedByDefault: true),
        new("feedback-windows", "Feedback & Windows", "Inline InfoBar banners, ToastHost notifications, progress indicators, windows, and shells.", WorkbenchIcons.Info, IsExpandedByDefault: false),
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
        // Foundation (2)
        new("tokens", "Design Tokens", "Semantic surface/text/accent brushes, resolved live hex values, typography, and spacing scale.", "foundation",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Theming", WorkbenchIcons.Sun,
            ["tokens", "colors", "brushes", "accent", "typography", "spacing", "theme"],
            _ => new TokensPage(), IsFeatured: true, IsFullBleed: true),
        new("icons", "Icons", "Interactive browser for all built-in 16×16 stroke vector icons and XAML resource snippets.", "foundation",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Grid,
            ["icons", "vector", "geometry", "WorkbenchIcon", "WorkbenchIcons"],
            _ => new IconsPage(), IsFullBleed: true),

        // Basic Inputs (6)
        new("buttons", "Button & SplitButton", "Accent, Standard, Subtle, Danger, Icon buttons, DropDownButton, and SplitButton.", "basic-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Play,
            ["button", "accent", "subtle", "danger", "icon", "ButtonAssist", "DropDownButton", "SplitButton"],
            _ => new ButtonsPage(), IsFeatured: true, Badge: "Updated", IsFullBleed: true),
        new("check-radio", "CheckBox & RadioButton", "Two-state and three-state CheckBoxes paired with grouped RadioButtons.", "basic-inputs",
            "WorkbenchUI.Wpf", "System.Windows.Controls", WorkbenchIcons.Check,
            ["CheckBox", "RadioButton", "indeterminate", "choice", "option"],
            _ => new CheckRadioPage(), IsFullBleed: true),
        new("toggle-switch", "ToggleSwitch", "Immediate ON/OFF toggle control deriving from ToggleButton for live settings.", "basic-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Edit,
            ["ToggleSwitch", "switch", "toggle", "boolean"],
            _ => new ToggleSwitchPage(), IsFullBleed: true),
        new("combo-box", "ComboBox & CheckComboBox", "Standard and editable ComboBox alongside multi-select CheckComboBox with Select All.", "basic-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.List,
            ["ComboBox", "CheckComboBox", "dropdown", "multiselect", "SelectAll"],
            _ => new ComboBoxPage(), IsFullBleed: true),
        new("text-inputs", "TextBox & SearchBox", "TextBox validation, debounced SearchBox filtering, PasswordBox, RichTextBox, Label, and Hyperlink.", "basic-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Search,
            ["TextBox", "SearchBox", "PasswordBox", "RichTextBox", "Label", "Hyperlink", "validation"],
            _ => new TextInputsPage(), IsFullBleed: true),
        new("sliders", "Slider & RangeSlider", "Single-value Slider with tick snapping and dual-thumb RangeSlider bound to ColorScale.", "basic-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Filter,
            ["Slider", "RangeSlider", "range", "bounds", "interval"],
            _ => new SlidersPage(), IsFullBleed: true),

        // Engineering Inputs (6)
        new("numeric-box", "NumericBox", "Unit-aware numeric editor with spin buttons, scientific notation, validation, and IUnitProvider.", "engineering-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Probe,
            ["NumericBox", "number", "unit", "IUnitProvider", "scientific", "validation"],
            _ => new NumericBoxPage(), IsFeatured: true, Badge: "Updated", IsFullBleed: true),
        new("vector3-box", "Vector3Box", "Composite nullable X/Y/Z vector editor with shared unit formatting and range limits.", "engineering-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Vector,
            ["Vector3Box", "vector", "xyz", "coordinates", "gravity"],
            _ => new Vector3BoxPage(), IsFullBleed: true),
        new("matrix-box", "MatrixBox", "Structured matrix cell grid supporting symmetric tensor mirroring and custom headers.", "engineering-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Grid,
            ["MatrixBox", "matrix", "tensor", "symmetric", "stiffness"],
            _ => new MatrixBoxPage(), IsFullBleed: true),
        new("path-box", "PathBox", "File and folder path input with OS dialogs (OpenFile, SaveFile, Folder) and BrowseRequested hook.", "engineering-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.FolderOpen,
            ["PathBox", "file", "folder", "browse", "dialog"],
            _ => new PathBoxPage(), IsFullBleed: true),
        new("color-picker", "ColorPicker & ColorEditor", "Dropdown color swatch picker and embeddable HSV/RGB/Hex color editor with alpha support.", "engineering-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Contour,
            ["ColorPicker", "ColorEditor", "color", "palette", "alpha"],
            _ => new ColorPickerPage(), IsFullBleed: true),
        new("key-gesture-box", "KeyGestureBox", "Keyboard shortcut capture input with validation, Esc revert, and KeyGestureConverter serialization.", "engineering-inputs",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Settings,
            ["KeyGestureBox", "shortcut", "hotkey", "KeyGesture"],
            _ => new KeyGestureBoxPage(), IsFullBleed: true),

        // Data & Visualization (5)
        new("property-grid", "PropertyGrid", "Typed property inspector with search filtering, categories, and dynamic items.", "data-viz",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.List,
            ["PropertyGrid", "inspector", "properties", "PropertyItem"],
            _ => new PropertyGridPage(), IsFeatured: true, IsFullBleed: true),
        new("model-tree", "ModelTree & TreeView", "Virtualized engineering ModelTree (2,000+ nodes, eye visibility, F2 rename) plus standard TreeView.", "data-viz",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Layers,
            ["ModelTree", "TreeView", "tree", "virtualized", "hierarchy", "TreeNode"],
            _ => new ModelTreePage(), IsFeatured: true, IsFullBleed: true),
        new("data-grid", "DataGrid & ListView", "High-density engineering table styling with grouping, RowDetails, IDataErrorInfo validation, and ListView.", "data-viz",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Grid,
            ["DataGrid", "ListView", "GridView", "table", "DataGridAssist", "validation"],
            _ => new DataGridPage(), IsFullBleed: true),
        new("color-map", "ColorMap & Scale", "Continuous/discrete LUT legends, live contour pixel preview, and paired ColorScaleEditor.", "data-viz",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Contour,
            ["ColorMap", "ColorMapLegend", "ColorScale", "ColorScaleEditor", "lut"],
            _ => new ColorMapPage(), IsFullBleed: true),
        new("log-console", "LogConsole", "Thread-safe 5,000-line diagnostic log console with timestamps, level filtering, and burst support.", "data-viz",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.File,
            ["LogConsole", "LogBuffer", "logging", "diagnostics"],
            _ => new LogConsolePage(), IsFullBleed: true),

        // Navigation & Workflow (7)
        new("navigation-view", "NavigationView", "Collapsible left navigation sidebar and compact icon rail with nested child flyouts.", "nav-workflow",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Layout,
            ["NavigationView", "NavigationViewItem", "sidebar", "rail", "flyout", "navigation"],
            _ => new NavigationViewPage(), IsFeatured: true, Badge: "New", IsFullBleed: true),
        new("ribbon", "Ribbon", "Compact contextual command ribbon with tabs, button groups, and quick actions.", "nav-workflow",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Layout,
            ["WorkbenchRibbon", "ribbon", "tabs", "commands"],
            _ => new RibbonPage(), IsFullBleed: true),
        new("navigation-menus", "TabControl & Menus", "Menu, ContextMenu, TabControl, ListBox, ScrollBar, and ToolTip styling.", "nav-workflow",
            "WorkbenchUI.Wpf", "System.Windows.Controls", WorkbenchIcons.Menu,
            ["Menu", "ContextMenu", "TabControl", "ListBox", "ScrollBar", "ToolTip"],
            _ => new NavigationPage(), IsFullBleed: true),
        new("expander-containers", "Expander & Containers", "Collapsible Expander sections, GroupBox framing, and layered panel surfaces.", "nav-workflow",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.ChevronDown,
            ["Expander", "GroupBox", "container", "accordion", "panel"],
            _ => new ExpanderPage(), IsFullBleed: true),
        new("process-rail", "ProcessRail & ChapterHost", "Guided sequential workflow stepper with Locked/Current/Completed/Stale states and chapter host.", "nav-workflow",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.ArrowRight,
            ["ProcessRail", "ProcessRailItem", "ChapterHost", "ChapterHostItem", "workflow"],
            _ => new ProcessRailPage(), Badge: "New", IsFullBleed: true),
        new("wizard", "Wizard & StepIndicator", "Multi-step guided configuration Wizard with CanGoNext gating and standalone StepIndicator.", "nav-workflow",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Check,
            ["Wizard", "WizardStep", "StepIndicator", "steps"],
            _ => new WizardPage(), IsFullBleed: true),
        new("playback-bar", "PlaybackBar", "Index-based simulation animation transport bar with FPS control, looping, and keyboard shortcuts.", "nav-workflow",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Play,
            ["PlaybackBar", "animation", "playback", "frames", "transient"],
            _ => new PlaybackBarPage(), IsFullBleed: true),

        // Feedback & Windows (5)
        new("info-bar", "InfoBar", "Persistent inline notification banner with Info/Success/Warning/Error levels and ActionContent.", "feedback-windows",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Info,
            ["InfoBar", "banner", "alert", "notification"],
            _ => new InfoBarPage(), IsFullBleed: true),
        new("toast-host", "ToastHost", "Transient bottom-right toast notification stack with severity stripes and auto-dismiss timers.", "feedback-windows",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Info,
            ["ToastHost", "toast", "notification", "alert"],
            _ => new ToastHostPage(), IsFullBleed: true),
        new("progress", "Progress & BusyOverlay", "ProgressBar, vector ProgressRing, cancellable ProgressDisplay, and BusyOverlay.", "feedback-windows",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Hourglass,
            ["ProgressBar", "ProgressRing", "ProgressDisplay", "BusyOverlay", "spinner", "loading"],
            _ => new ProgressPage(), IsFullBleed: true),
        new("windows-dialogs", "Windows & Dialogs", "Custom-chrome WorkbenchWindow, WorkbenchDialogWindow, and WorkbenchMessageBox.", "feedback-windows",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Layout,
            ["WorkbenchWindow", "WorkbenchDialogWindow", "WorkbenchMessageBox", "dialog", "modal"],
            _ => new WindowsPage(), IsFullBleed: true),
        new("shell", "Shell & ToolBars", "ToolBarTray overflow handling, StatusBar telemetry, and GridSplitter multi-pane layout.", "feedback-windows",
            "WorkbenchUI.Wpf", "WorkbenchUI.Wpf.Controls", WorkbenchIcons.Layout,
            ["ToolBar", "ToolBarTray", "StatusBar", "GridSplitter", "shell"],
            _ => new ShellPage(), IsFullBleed: true),

        // Docking & Charts (2)
        new("docking", "Docking", "AvalonDock integration styled with live semantic Dark/Light theme and accent tokens.", "docking-charts",
            "WorkbenchUI.Wpf.Docking", "WorkbenchUI.Wpf.Docking", WorkbenchIcons.Layout,
            ["Docking", "DockingManager", "AvalonDock", "panels", "tabs"],
            _ => new DockingPage(), IsFeatured: true, IsFullBleed: true),
        new("charts", "Charts", "ScottPlot 5 ConvergenceMonitor, HistoryChart, FrequencyResponsePlot, HistogramChart, and WorkbenchPlot.", "docking-charts",
            "WorkbenchUI.Wpf.Charts", "WorkbenchUI.Wpf.Charts", WorkbenchIcons.ChartLine,
            ["Charts", "ConvergenceMonitor", "HistoryChart", "FrequencyResponsePlot", "HistogramChart", "WorkbenchPlot"],
            _ => new ChartsPage(), IsFeatured: true, IsFullBleed: true),

        // 3D Viewport (7)
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
