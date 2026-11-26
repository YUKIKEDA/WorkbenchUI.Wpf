using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>Specifies the kind of dialog opened by <see cref="PathBox"/>.</summary>
public enum PathBoxMode
{
    /// <summary>Opens a file selection dialog.</summary>
    OpenFile,

    /// <summary>Opens a save-file dialog.</summary>
    SaveFile,

    /// <summary>Opens a folder selection dialog.</summary>
    Folder,
}

/// <summary>
/// File or folder path input control with a browse button.
/// Uses built-in <c>Microsoft.Win32</c> dialogs by default to stay dependency-free.
/// To replace with a custom dialog, subscribe to <see cref="BrowseRequested"/> and set <c>Handled = true</c>.
/// </summary>
[TemplatePart(Name = PartBrowseButton, Type = typeof(ButtonBase))]
public class PathBox : Control
{
    private const string PartBrowseButton = "PART_BrowseButton";

    private ButtonBase? _browseButton;

    static PathBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(PathBox), new FrameworkPropertyMetadata(typeof(PathBox)));
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FrameworkElementAutomationPeer(this);

    /// <summary>Identifies the <see cref="SelectedPath"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedPathProperty = DependencyProperty.Register(
        nameof(SelectedPath), typeof(string), typeof(PathBox),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Gets or sets the selected path. Updated by both text editing and dialog selection.</summary>
    public string SelectedPath
    {
        get => (string)GetValue(SelectedPathProperty);
        set => SetValue(SelectedPathProperty, value);
    }

    /// <summary>Identifies the <see cref="Mode"/> dependency property.</summary>
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(PathBoxMode), typeof(PathBox), new PropertyMetadata(PathBoxMode.OpenFile));

    /// <summary>Gets or sets the kind of dialog opened by the browse button.</summary>
    public PathBoxMode Mode
    {
        get => (PathBoxMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    /// <summary>Identifies the <see cref="Filter"/> dependency property.</summary>
    public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
        nameof(Filter), typeof(string), typeof(PathBox), new PropertyMetadata(null));

    /// <summary>Gets or sets the file dialog filter string (e.g., "Mesh files (*.msh)|*.msh|All files (*.*)|*.*").</summary>
    public string? Filter
    {
        get => (string?)GetValue(FilterProperty);
        set => SetValue(FilterProperty, value);
    }

    /// <summary>Identifies the <see cref="DialogTitle"/> dependency property.</summary>
    public static readonly DependencyProperty DialogTitleProperty = DependencyProperty.Register(
        nameof(DialogTitle), typeof(string), typeof(PathBox), new PropertyMetadata(null));

    /// <summary>Gets or sets the dialog title, or <see langword="null"/> to use the OS default.</summary>
    public string? DialogTitle
    {
        get => (string?)GetValue(DialogTitleProperty);
        set => SetValue(DialogTitleProperty, value);
    }

    /// <summary>Identifies the <see cref="IsReadOnly"/> dependency property.</summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(PathBox), new PropertyMetadata(false));

    /// <summary>Gets or sets whether both text editing and the browse button are disabled.</summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>Identifies the <see cref="BrowseRequested"/> routed event.</summary>
    public static readonly RoutedEvent BrowseRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(BrowseRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PathBox));

    /// <summary>
    /// Raised before opening the built-in dialog when the browse button is clicked.
    /// Set <c>Handled = true</c> to suppress the built-in dialog and show a custom one instead.
    /// </summary>
    public event RoutedEventHandler BrowseRequested
    {
        add => AddHandler(BrowseRequestedEvent, value);
        remove => RemoveHandler(BrowseRequestedEvent, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_browseButton is not null)
        {
            _browseButton.Click -= OnBrowseClick;
        }

        base.OnApplyTemplate();

        _browseButton = GetTemplateChild(PartBrowseButton) as ButtonBase;

        if (_browseButton is not null)
        {
            _browseButton.Click += OnBrowseClick;
        }
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e) => Browse();

    /// <summary>Opens the browse dialog and updates <see cref="SelectedPath"/> if confirmed.</summary>
    public void Browse()
    {
        var args = new RoutedEventArgs(BrowseRequestedEvent, this);
        RaiseEvent(args);
        if (args.Handled)
        {
            return;
        }

        var owner = Window.GetWindow(this);
        var current = SelectedPath;

        switch (Mode)
        {
            case PathBoxMode.OpenFile:
            {
                var dialog = new OpenFileDialog { Filter = Filter ?? string.Empty };
                InitializeFileDialog(dialog, current);
                if (dialog.ShowDialog(owner) == true)
                {
                    SetCurrentValue(SelectedPathProperty, dialog.FileName);
                }

                break;
            }

            case PathBoxMode.SaveFile:
            {
                var dialog = new SaveFileDialog { Filter = Filter ?? string.Empty };
                InitializeFileDialog(dialog, current);
                if (dialog.ShowDialog(owner) == true)
                {
                    SetCurrentValue(SelectedPathProperty, dialog.FileName);
                }

                break;
            }

            case PathBoxMode.Folder:
            {
                var dialog = new OpenFolderDialog();
                if (DialogTitle is { } title)
                {
                    dialog.Title = title;
                }

                if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
                {
                    dialog.InitialDirectory = current;
                }

                if (dialog.ShowDialog(owner) == true)
                {
                    SetCurrentValue(SelectedPathProperty, dialog.FolderName);
                }

                break;
            }
        }
    }

    /// <summary>Initializes a file dialog with the file name and directory from the current path.</summary>
    private void InitializeFileDialog(FileDialog dialog, string? current)
    {
        if (DialogTitle is { } title)
        {
            dialog.Title = title;
        }

        if (string.IsNullOrWhiteSpace(current))
        {
            return;
        }

        try
        {
            dialog.FileName = Path.GetFileName(current);
            if (Path.GetDirectoryName(current) is { Length: > 0 } directory && Directory.Exists(directory))
            {
                dialog.InitialDirectory = directory;
            }
        }
        catch (ArgumentException)
        {
            // Ignore malformed paths while typing and fall back to default location
        }
    }
}
