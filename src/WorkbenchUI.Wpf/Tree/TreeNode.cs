using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Default observable implementation of <see cref="ITreeNode"/> and <see cref="IRenamableNode"/>.
/// Inherit from or instantiate this class to bind directly to <see cref="ModelTree"/> with built-in inline rename support.
/// </summary>
public class TreeNode : IRenamableNode
{
    private string _name = string.Empty;
    private bool _isExpanded;
    private bool? _isVisible = true;
    private bool _isSelected;
    private Geometry? _icon;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the mutable collection of child nodes.</summary>
    public ObservableCollection<ITreeNode> Children { get; } = [];

    IEnumerable<ITreeNode> ITreeNode.Children => Children;

    /// <inheritdoc />
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <inheritdoc />
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }

    /// <inheritdoc />
    public bool? IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    /// <inheritdoc />
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <inheritdoc />
    public Geometry? Icon
    {
        get => _icon;
        set => SetField(ref _icon, value);
    }

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
