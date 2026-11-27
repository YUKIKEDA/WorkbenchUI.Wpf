using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Base class for an explicit property row model displayed by <see cref="PropertyGrid"/>.
/// Applications construct collections of derived types and pass them to <see cref="PropertyGrid.ItemsSource"/>.
/// Editors are resolved automatically via implicit <see cref="System.Windows.DataTemplate"/> definitions matching each derived type,
/// allowing custom editors to be added by pairing a new subclass with a <see cref="System.Windows.DataTemplate"/>.
/// </summary>
public abstract class PropertyItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string? _category;
    private string? _description;
    private bool _isReadOnly;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets or sets the property name displayed in the row and matched by filtering.</summary>
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>Gets or sets the category name used to group rows into collapsible sections.</summary>
    public string? Category
    {
        get => _category;
        set => SetField(ref _category, value);
    }

    /// <summary>Gets or sets the description displayed as a tooltip when hovering over the row.</summary>
    public string? Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    /// <summary>Gets or sets a value indicating whether the property is read-only.</summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set
        {
            if (SetField(ref _isReadOnly, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEditable)));
            }
        }
    }

    /// <summary>Gets a value indicating whether the property is editable (the inverse of <see cref="IsReadOnly"/>, for editor <c>IsEnabled</c> bindings).</summary>
    public bool IsEditable => !_isReadOnly;

    /// <summary>
    /// Updates a backing field and raises <see cref="PropertyChanged"/> if the value changed.
    /// </summary>
    /// <typeparam name="T">Type of the backing field.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value to assign.</param>
    /// <param name="propertyName">Name of the property that changed.</param>
    /// <returns><see langword="true"/> if the field value changed; otherwise <see langword="false"/>.</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
