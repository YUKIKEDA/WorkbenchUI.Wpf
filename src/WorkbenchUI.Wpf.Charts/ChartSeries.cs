using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkbenchUI.Wpf.Charts;

/// <summary>
/// Explicit item model representing a single line series displayed in <see cref="HistoryChart"/>.
/// Data updates are notified by replacing the <see cref="X"/> and <see cref="Y"/> array instances.
/// </summary>
public class ChartSeries : INotifyPropertyChanged
{
    private string _name = "";
    private double[]? _x;
    private double[]? _y;
    private System.Windows.Media.Color? _color;
    private double _lineWidth = 2;

    /// <summary>Series name displayed in the legend.</summary>
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>Array of X values. Must have the same length as <see cref="Y"/>.</summary>
    public double[]? X
    {
        get => _x;
        set => SetField(ref _x, value);
    }

    /// <summary>Array of Y values. Must have the same length as <see cref="X"/>.</summary>
    public double[]? Y
    {
        get => _y;
        set => SetField(ref _y, value);
    }

    /// <summary>Line color. When <see langword="null"/>, a color is assigned automatically from the theme palette.</summary>
    public System.Windows.Media.Color? Color
    {
        get => _color;
        set => SetField(ref _color, value);
    }

    /// <summary>Line thickness (default is 2).</summary>
    public double LineWidth
    {
        get => _lineWidth;
        set => SetField(ref _lineWidth, value);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Updates a backing field and raises <see cref="PropertyChanged"/> if the value changed.
    /// </summary>
    /// <typeparam name="T">Type of the backing field.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">New value to assign.</param>
    /// <param name="propertyName">Name of the changed property.</param>
    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
