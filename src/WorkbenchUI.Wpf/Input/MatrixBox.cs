using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Numeric matrix input control for anisotropic material properties, coordinate transforms, and inertia tensors.
/// <list type="bullet">
/// <item>Values are stored in <see cref="Values"/> (<c>double[,]</c>). Each committed cell edit assigns a new array instance so TwoWay bindings notify the ViewModel.</item>
/// <item>When <see cref="IsSymmetric"/> is <see langword="true"/>, editing cell (i, j) automatically mirrors to (j, i).</item>
/// <item>Units, formatting, and range limits are forwarded uniformly to every cell's <see cref="NumericBox"/> (matching <see cref="Vector3Box"/>).</item>
/// </list>
/// </summary>
[TemplatePart(Name = PartGrid, Type = typeof(Grid))]
public class MatrixBox : Control
{
    private const string PartGrid = "PART_Grid";

    private Grid? _grid;
    private MatrixCell[,]? _cells;
    private bool _updating;

    static MatrixBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MatrixBox), new FrameworkPropertyMetadata(typeof(MatrixBox)));
    }

    // Control has no AutomationPeer by default; expose AutomationId and child NumericBoxes
    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new FrameworkElementAutomationPeer(this);

    #region Dependency properties

    /// <summary>Identifies the <see cref="Values"/> dependency property.</summary>
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(double[,]), typeof(MatrixBox),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValuesChanged));

    /// <summary>
    /// Gets or sets the matrix values in base units. Replaced with a new array instance on each committed edit.
    /// When <see langword="null"/> or smaller than <see cref="Rows"/> × <see cref="Columns"/>, missing cells appear blank.
    /// </summary>
    public double[,]? Values
    {
        get => (double[,]?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Identifies the <see cref="Rows"/> dependency property.</summary>
    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
        nameof(Rows), typeof(int), typeof(MatrixBox),
        new PropertyMetadata(3, OnStructureChanged), v => (int)v >= 1);

    /// <summary>Gets or sets the number of rows (default 3). Takes precedence over <see cref="Values"/> dimensions if they differ.</summary>
    public int Rows
    {
        get => (int)GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    /// <summary>Identifies the <see cref="Columns"/> dependency property.</summary>
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
        nameof(Columns), typeof(int), typeof(MatrixBox),
        new PropertyMetadata(3, OnStructureChanged), v => (int)v >= 1);

    /// <summary>Gets or sets the number of columns (default 3).</summary>
    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSymmetric"/> dependency property.</summary>
    public static readonly DependencyProperty IsSymmetricProperty = DependencyProperty.Register(
        nameof(IsSymmetric), typeof(bool), typeof(MatrixBox), new PropertyMetadata(false));

    /// <summary>Gets or sets whether editing cell (i, j) mirrors the value to (j, i) (for stiffness/compliance matrices).</summary>
    public bool IsSymmetric
    {
        get => (bool)GetValue(IsSymmetricProperty);
        set => SetValue(IsSymmetricProperty, value);
    }

    /// <summary>Identifies the <see cref="RowHeaders"/> dependency property.</summary>
    public static readonly DependencyProperty RowHeadersProperty = DependencyProperty.Register(
        nameof(RowHeaders), typeof(IEnumerable), typeof(MatrixBox),
        new PropertyMetadata(null, OnStructureChanged));

    /// <summary>Gets or sets the row header labels (e.g., X/Y/Z). Hidden when <see langword="null"/>.</summary>
    public IEnumerable? RowHeaders
    {
        get => (IEnumerable?)GetValue(RowHeadersProperty);
        set => SetValue(RowHeadersProperty, value);
    }

    /// <summary>Identifies the <see cref="ColumnHeaders"/> dependency property.</summary>
    public static readonly DependencyProperty ColumnHeadersProperty = DependencyProperty.Register(
        nameof(ColumnHeaders), typeof(IEnumerable), typeof(MatrixBox),
        new PropertyMetadata(null, OnStructureChanged));

    /// <summary>Gets or sets the column header labels. Hidden when <see langword="null"/>.</summary>
    public IEnumerable? ColumnHeaders
    {
        get => (IEnumerable?)GetValue(ColumnHeadersProperty);
        set => SetValue(ColumnHeadersProperty, value);
    }

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(MatrixBox), new PropertyMetadata(double.NegativeInfinity));

    /// <summary>Gets or sets the shared minimum value (in base units) for all cells.</summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(MatrixBox), new PropertyMetadata(double.PositiveInfinity));

    /// <summary>Gets or sets the shared maximum value (in base units) for all cells.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Identifies the <see cref="Increment"/> dependency property.</summary>
    public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
        nameof(Increment), typeof(double), typeof(MatrixBox), new PropertyMetadata(1.0));

    /// <summary>Gets or sets the shared step increment for all cells.</summary>
    public double Increment
    {
        get => (double)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    /// <summary>Identifies the <see cref="UnitProvider"/> dependency property.</summary>
    public static readonly DependencyProperty UnitProviderProperty = DependencyProperty.Register(
        nameof(UnitProvider), typeof(IUnitProvider), typeof(MatrixBox), new PropertyMetadata(null));

    /// <summary>Gets or sets the shared unit provider for all cells.</summary>
    public IUnitProvider? UnitProvider
    {
        get => (IUnitProvider?)GetValue(UnitProviderProperty);
        set => SetValue(UnitProviderProperty, value);
    }

    /// <summary>Identifies the <see cref="Format"/> dependency property.</summary>
    public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
        nameof(Format), typeof(string), typeof(MatrixBox), new PropertyMetadata("G"));

    /// <summary>Gets or sets the shared numeric format string for all cells.</summary>
    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    #endregion

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _grid = GetTemplateChild(PartGrid) as Grid;
        RebuildGrid();
    }

    private static void OnStructureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((MatrixBox)d).RebuildGrid();

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (MatrixBox)d;
        if (!box._updating)
        {
            box.SyncCellsFromValues();
        }
    }

    /// <summary>Rebuilds the grid rows, columns, and headers.</summary>
    private void RebuildGrid()
    {
        if (_grid is null)
        {
            return;
        }

        _grid.Children.Clear();
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();

        var rows = Rows;
        var cols = Columns;
        var rowHeaders = RowHeaders?.Cast<object>().ToArray();
        var colHeaders = ColumnHeaders?.Cast<object>().ToArray();
        var headerCol = rowHeaders is not null ? 1 : 0;
        var headerRow = colHeaders is not null ? 1 : 0;

        if (headerCol > 0)
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        for (var c = 0; c < cols; c++)
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
                MinWidth = 56,
            });
        }

        if (headerRow > 0)
        {
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (var r = 0; r < rows; r++)
        {
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (var c = 0; c < cols && colHeaders is not null; c++)
        {
            _grid.Children.Add(CreateHeader(colHeaders.ElementAtOrDefault(c), 0, c + headerCol));
        }

        for (var r = 0; r < rows && rowHeaders is not null; r++)
        {
            _grid.Children.Add(CreateHeader(rowHeaders.ElementAtOrDefault(r), r + headerRow, 0));
        }

        _cells = new MatrixCell[rows, cols];
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var cell = new MatrixCell(this, r, c);
                _cells[r, c] = cell;

                var numericBox = new NumericBox
                {
                    DataContext = cell,
                    Margin = new Thickness(c == 0 ? 0 : 2, r == 0 ? 0 : 2, 0, 0),
                };
                numericBox.SetBinding(NumericBox.ValueProperty, new System.Windows.Data.Binding(nameof(MatrixCell.Value))
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay,
                });
                numericBox.SetBinding(NumericBox.MinimumProperty, BindSelf(nameof(Minimum)));
                numericBox.SetBinding(NumericBox.MaximumProperty, BindSelf(nameof(Maximum)));
                numericBox.SetBinding(NumericBox.IncrementProperty, BindSelf(nameof(Increment)));
                numericBox.SetBinding(NumericBox.UnitProviderProperty, BindSelf(nameof(UnitProvider)));
                numericBox.SetBinding(NumericBox.FormatProperty, BindSelf(nameof(Format)));

                Grid.SetRow(numericBox, r + headerRow);
                Grid.SetColumn(numericBox, c + headerCol);
                _grid.Children.Add(numericBox);
            }
        }

        SyncCellsFromValues();
        return;

        System.Windows.Data.Binding BindSelf(string path) => new(path) { Source = this };
    }

    private static TextBlock CreateHeader(object? content, int row, int col)
    {
        var text = new TextBlock
        {
            Text = content?.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 4, 2),
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "Wb.Brush.Text.Secondary");
        Grid.SetRow(text, row);
        Grid.SetColumn(text, col);
        return text;
    }

    /// <summary>Updates cell display values from the <see cref="Values"/> array (out-of-bounds or null becomes blank).</summary>
    private void SyncCellsFromValues()
    {
        if (_cells is null)
        {
            return;
        }

        _updating = true;
        try
        {
            var values = Values;
            for (var r = 0; r < _cells.GetLength(0); r++)
            {
                for (var c = 0; c < _cells.GetLength(1); c++)
                {
                    _cells[r, c].SetSilently(
                        values is not null && r < values.GetLength(0) && c < values.GetLength(1)
                            ? values[r, c]
                            : null);
                }
            }
        }
        finally
        {
            _updating = false;
        }
    }

    /// <summary>Commits a cell edit by creating a new array instance and replacing <see cref="Values"/>.</summary>
    internal void OnCellEdited(int row, int col, double? newValue)
    {
        if (_updating || _cells is null)
        {
            return;
        }

        // Empty/null commit is invalid; revert to the previous value
        if (newValue is not double value)
        {
            SyncCellsFromValues();
            return;
        }

        var rows = _cells.GetLength(0);
        var cols = _cells.GetLength(1);
        var source = Values;
        var next = new double[rows, cols];
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                next[r, c] = source is not null && r < source.GetLength(0) && c < source.GetLength(1)
                    ? source[r, c]
                    : 0.0;
            }
        }

        next[row, col] = value;
        if (IsSymmetric && row != col && col < rows && row < cols)
        {
            next[col, row] = value;
        }

        _updating = true;
        try
        {
            SetCurrentValue(ValuesProperty, next);

            // Update the mirrored cell and any cells zero-filled when initializing from null
            if (IsSymmetric && row != col && col < rows && row < cols)
            {
                _cells[col, row].SetSilently(value);
            }

            if (source is null)
            {
                for (var r = 0; r < rows; r++)
                {
                    for (var c = 0; c < cols; c++)
                    {
                        _cells[r, c].SetSilently(next[r, c]);
                    }
                }
            }
        }
        finally
        {
            _updating = false;
        }
    }

    /// <summary>Internal ViewModel for a single cell (bound two-way by its <see cref="NumericBox"/>).</summary>
    internal sealed class MatrixCell(MatrixBox owner, int row, int col) : INotifyPropertyChanged
    {
        private double? _value;

        public event PropertyChangedEventHandler? PropertyChanged;

        public double? Value
        {
            get => _value;
            set
            {
                if (!Nullable.Equals(_value, value))
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                    owner.OnCellEdited(row, col, value);
                }
            }
        }

        /// <summary>Updates the displayed value without notifying the owner (for model-to-view sync).</summary>
        public void SetSilently(double? value)
        {
            if (!Nullable.Equals(_value, value))
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }
}
