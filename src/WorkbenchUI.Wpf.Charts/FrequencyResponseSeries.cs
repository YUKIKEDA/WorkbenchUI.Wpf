using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkbenchUI.Wpf.Charts;

/// <summary>
/// Model representing a single frequency response series displayed in <see cref="FrequencyResponsePlot"/>.
/// </summary>
public class FrequencyResponseSeries : INotifyPropertyChanged
{
    private string _name = "";
    private double[]? _frequencies;
    private double[]? _magnitudes;
    private double[]? _phases;
    private System.Windows.Media.Color? _color;

    /// <summary>Series name displayed in the legend.</summary>
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>Frequency values in Hz. Must be positive because the frequency axis is logarithmic.</summary>
    public double[]? Frequencies
    {
        get => _frequencies;
        set => SetField(ref _frequencies, value);
    }

    /// <summary>Linear magnitude values. Conversion to dB is handled by the plot control.</summary>
    public double[]? Magnitudes
    {
        get => _magnitudes;
        set => SetField(ref _magnitudes, value);
    }

    /// <summary>Phase values in degrees. When <see langword="null"/>, this series is omitted from the phase panel.</summary>
    public double[]? Phases
    {
        get => _phases;
        set => SetField(ref _phases, value);
    }

    /// <summary>Line color. When <see langword="null"/>, a color is assigned automatically from the theme palette.</summary>
    public System.Windows.Media.Color? Color
    {
        get => _color;
        set => SetField(ref _color, value);
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
