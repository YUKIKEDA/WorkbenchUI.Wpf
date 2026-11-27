using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Threading;
using WorkbenchUI.Wpf.Controls;
using WorkbenchUI.Wpf.Theming;

namespace WorkbenchUI.Wpf.Gallery.Controls;

/// <summary>
/// Gallery component showcase card with a live preview stage, optional right-hand Options panel,
/// and collapsible XAML / C# syntax-highlighted source viewer with one-click clipboard copy.
/// </summary>
[ContentProperty(nameof(ExampleContent))]
[TemplatePart(Name = PartCodeToggleButton, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartXamlTabButton, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartCSharpTabButton, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartCopyButton, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartCopyIcon, Type = typeof(WorkbenchIcon))]
[TemplatePart(Name = PartCopyText, Type = typeof(TextBlock))]
[TemplatePart(Name = PartCodeViewer, Type = typeof(RichTextBox))]
public class ControlExample : Control
{
    private const string PartCodeToggleButton = "PART_CodeToggleButton";
    private const string PartXamlTabButton = "PART_XamlTabButton";
    private const string PartCSharpTabButton = "PART_CSharpTabButton";
    private const string PartCopyButton = "PART_CopyButton";
    private const string PartCopyIcon = "PART_CopyIcon";
    private const string PartCopyText = "PART_CopyText";
    private const string PartCodeViewer = "PART_CodeViewer";

    private readonly DispatcherTimer _copyResetTimer;
    private ButtonBase? _codeToggleButton;
    private ButtonBase? _xamlTabButton;
    private ButtonBase? _csharpTabButton;
    private ButtonBase? _copyButton;
    private WorkbenchIcon? _copyIcon;
    private TextBlock? _copyText;
    private RichTextBox? _codeViewer;
    private bool _isThemeHooked;

    static ControlExample()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ControlExample),
            new FrameworkPropertyMetadata(typeof(ControlExample)));
    }

    /// <summary>Initializes a new instance of the <see cref="ControlExample"/> class.</summary>
    public ControlExample()
    {
        _copyResetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1600) };
        _copyResetTimer.Tick += OnCopyResetTick;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region Dependency properties

    /// <summary>Identifies the <see cref="HeaderText"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
        nameof(HeaderText), typeof(string), typeof(ControlExample), new PropertyMetadata(null));

    /// <summary>Gets or sets the section heading displayed above the card.</summary>
    public string? HeaderText
    {
        get => (string?)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(ControlExample), new PropertyMetadata(null));

    /// <summary>Gets or sets optional descriptive text displayed beneath <see cref="HeaderText"/>.</summary>
    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Identifies the <see cref="ExampleContent"/> dependency property.</summary>
    public static readonly DependencyProperty ExampleContentProperty = DependencyProperty.Register(
        nameof(ExampleContent), typeof(object), typeof(ControlExample), new PropertyMetadata(null));

    /// <summary>Gets or sets the interactive sample UI rendered in the preview stage.</summary>
    public object? ExampleContent
    {
        get => GetValue(ExampleContentProperty);
        set => SetValue(ExampleContentProperty, value);
    }

    /// <summary>Identifies the <see cref="Options"/> dependency property.</summary>
    public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(
        nameof(Options), typeof(object), typeof(ControlExample), new PropertyMetadata(null));

    /// <summary>Gets or sets optional live configuration controls rendered in the right-hand Options column.</summary>
    public object? Options
    {
        get => GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    /// <summary>Identifies the <see cref="OptionsWidth"/> dependency property.</summary>
    public static readonly DependencyProperty OptionsWidthProperty = DependencyProperty.Register(
        nameof(OptionsWidth), typeof(double), typeof(ControlExample), new PropertyMetadata(240.0));

    /// <summary>Gets or sets the width of the right-hand Options panel when <see cref="Options"/> is non-null.</summary>
    public double OptionsWidth
    {
        get => (double)GetValue(OptionsWidthProperty);
        set => SetValue(OptionsWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="Xaml"/> dependency property.</summary>
    public static readonly DependencyProperty XamlProperty = DependencyProperty.Register(
        nameof(Xaml), typeof(string), typeof(ControlExample), new PropertyMetadata(null, OnCodeSourceChanged));

    /// <summary>Gets or sets the XAML code snippet shown in the expandable code panel.</summary>
    public string? Xaml
    {
        get => (string?)GetValue(XamlProperty);
        set => SetValue(XamlProperty, value);
    }

    /// <summary>Identifies the <see cref="CSharp"/> dependency property.</summary>
    public static readonly DependencyProperty CSharpProperty = DependencyProperty.Register(
        nameof(CSharp), typeof(string), typeof(ControlExample), new PropertyMetadata(null, OnCodeSourceChanged));

    /// <summary>Gets or sets the C# code snippet shown in the expandable code panel.</summary>
    public string? CSharp
    {
        get => (string?)GetValue(CSharpProperty);
        set => SetValue(CSharpProperty, value);
    }

    /// <summary>Identifies the <see cref="IsCodeExpanded"/> dependency property.</summary>
    public static readonly DependencyProperty IsCodeExpandedProperty = DependencyProperty.Register(
        nameof(IsCodeExpanded), typeof(bool), typeof(ControlExample),
        new PropertyMetadata(false, OnCodePresentationChanged));

    /// <summary>Gets or sets whether the syntax-highlighted code pane is expanded.</summary>
    public bool IsCodeExpanded
    {
        get => (bool)GetValue(IsCodeExpandedProperty);
        set => SetValue(IsCodeExpandedProperty, value);
    }

    private static readonly DependencyPropertyKey HasCodePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasCode), typeof(bool), typeof(ControlExample), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasCode"/> dependency property.</summary>
    public static readonly DependencyProperty HasCodeProperty = HasCodePropertyKey.DependencyProperty;

    /// <summary>Gets whether XAML or C# source code is provided.</summary>
    public bool HasCode => (bool)GetValue(HasCodeProperty);

    private static readonly DependencyPropertyKey HasXamlPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasXaml), typeof(bool), typeof(ControlExample), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasXaml"/> dependency property.</summary>
    public static readonly DependencyProperty HasXamlProperty = HasXamlPropertyKey.DependencyProperty;

    /// <summary>Gets whether XAML source code is provided.</summary>
    public bool HasXaml => (bool)GetValue(HasXamlProperty);

    private static readonly DependencyPropertyKey HasCSharpPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasCSharp), typeof(bool), typeof(ControlExample), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasCSharp"/> dependency property.</summary>
    public static readonly DependencyProperty HasCSharpProperty = HasCSharpPropertyKey.DependencyProperty;

    /// <summary>Gets whether C# source code is provided.</summary>
    public bool HasCSharp => (bool)GetValue(HasCSharpProperty);

    private static readonly DependencyPropertyKey HasBothLanguagesPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasBothLanguages), typeof(bool), typeof(ControlExample), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasBothLanguages"/> dependency property.</summary>
    public static readonly DependencyProperty HasBothLanguagesProperty = HasBothLanguagesPropertyKey.DependencyProperty;

    /// <summary>Gets whether both XAML and C# snippets are provided.</summary>
    public bool HasBothLanguages => (bool)GetValue(HasBothLanguagesProperty);

    private static readonly DependencyPropertyKey IsCSharpActivePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsCSharpActive), typeof(bool), typeof(ControlExample),
        new PropertyMetadata(false, OnCodePresentationChanged));

    /// <summary>Identifies the <see cref="IsCSharpActive"/> dependency property.</summary>
    public static readonly DependencyProperty IsCSharpActiveProperty = IsCSharpActivePropertyKey.DependencyProperty;

    /// <summary>Gets whether the C# tab is currently selected.</summary>
    public bool IsCSharpActive => (bool)GetValue(IsCSharpActiveProperty);

    #endregion

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        UnhookTemplateParts();
        base.OnApplyTemplate();

        _codeToggleButton = GetTemplateChild(PartCodeToggleButton) as ButtonBase;
        _xamlTabButton = GetTemplateChild(PartXamlTabButton) as ButtonBase;
        _csharpTabButton = GetTemplateChild(PartCSharpTabButton) as ButtonBase;
        _copyButton = GetTemplateChild(PartCopyButton) as ButtonBase;
        _copyIcon = GetTemplateChild(PartCopyIcon) as WorkbenchIcon;
        _copyText = GetTemplateChild(PartCopyText) as TextBlock;
        _codeViewer = GetTemplateChild(PartCodeViewer) as RichTextBox;

        if (_codeToggleButton is not null)
        {
            _codeToggleButton.Click += OnCodeToggleClick;
        }

        if (_xamlTabButton is not null)
        {
            _xamlTabButton.Click += OnXamlTabClick;
        }

        if (_csharpTabButton is not null)
        {
            _csharpTabButton.Click += OnCSharpTabClick;
        }

        if (_copyButton is not null)
        {
            _copyButton.Click += OnCopyClick;
        }

        UpdateCodeMetadata();
        RefreshCodeDocument();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isThemeHooked)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            _isThemeHooked = true;
        }

        if (IsCodeExpanded)
        {
            RefreshCodeDocument();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_isThemeHooked)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            _isThemeHooked = false;
        }

        _copyResetTimer.Stop();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsCodeExpanded)
        {
            RefreshCodeDocument();
        }
    }

    private static void OnCodeSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var example = (ControlExample)d;
        example.UpdateCodeMetadata();
        example.RefreshCodeDocument();
    }

    private static void OnCodePresentationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ControlExample)d).RefreshCodeDocument();

    private void UpdateCodeMetadata()
    {
        var hasXaml = !string.IsNullOrWhiteSpace(Xaml);
        var hasCSharp = !string.IsNullOrWhiteSpace(CSharp);

        SetValue(HasXamlPropertyKey, hasXaml);
        SetValue(HasCSharpPropertyKey, hasCSharp);
        SetValue(HasCodePropertyKey, hasXaml || hasCSharp);
        SetValue(HasBothLanguagesPropertyKey, hasXaml && hasCSharp);

        if (!hasXaml && hasCSharp)
        {
            SetValue(IsCSharpActivePropertyKey, true);
        }
        else if (hasXaml && !hasCSharp)
        {
            SetValue(IsCSharpActivePropertyKey, false);
        }
    }

    private void RefreshCodeDocument()
    {
        if (_codeViewer is null || !HasCode || !IsCodeExpanded)
        {
            return;
        }

        var useCSharp = IsCSharpActive && HasCSharp || !HasXaml;
        var raw = useCSharp ? CSharp : Xaml;
        var lang = useCSharp ? CodeLanguage.CSharp : CodeLanguage.Xaml;

        _codeViewer.Document = CodeSyntaxHighlighter.Highlight(raw ?? string.Empty, lang, ThemeManager.CurrentTheme);
    }

    private void OnCodeToggleClick(object sender, RoutedEventArgs e) =>
        SetCurrentValue(IsCodeExpandedProperty, !IsCodeExpanded);

    private void OnXamlTabClick(object sender, RoutedEventArgs e)
    {
        if (HasXaml)
        {
            SetValue(IsCSharpActivePropertyKey, false);
        }
    }

    private void OnCSharpTabClick(object sender, RoutedEventArgs e)
    {
        if (HasCSharp)
        {
            SetValue(IsCSharpActivePropertyKey, true);
        }
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        var useCSharp = IsCSharpActive && HasCSharp || !HasXaml;
        var text = CodeSyntaxHighlighter.NormalizeCode(useCSharp ? CSharp : Xaml);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            Clipboard.SetText(text);
        }
        catch (ExternalException)
        {
            return;
        }

        if (_copyIcon is not null)
        {
            _copyIcon.Icon = WorkbenchIcons.Check;
        }

        if (_copyText is not null)
        {
            _copyText.Text = "Copied";
        }

        _copyResetTimer.Stop();
        _copyResetTimer.Start();
    }

    private void OnCopyResetTick(object? sender, EventArgs e)
    {
        _copyResetTimer.Stop();
        if (_copyIcon is not null)
        {
            _copyIcon.Icon = WorkbenchIcons.Copy;
        }

        if (_copyText is not null)
        {
            _copyText.Text = "Copy";
        }
    }

    private void UnhookTemplateParts()
    {
        if (_codeToggleButton is not null)
        {
            _codeToggleButton.Click -= OnCodeToggleClick;
        }

        if (_xamlTabButton is not null)
        {
            _xamlTabButton.Click -= OnXamlTabClick;
        }

        if (_csharpTabButton is not null)
        {
            _csharpTabButton.Click -= OnCSharpTabClick;
        }

        if (_copyButton is not null)
        {
            _copyButton.Click -= OnCopyClick;
        }
    }
}
