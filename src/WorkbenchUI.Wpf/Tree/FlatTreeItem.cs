using System.ComponentModel;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Represents a single row in a flattened tree list, associating an <see cref="ITreeNode"/> with its depth and parent.
/// </summary>
public sealed class FlatTreeItem : INotifyPropertyChanged
{
    private bool _hasChildren;
    private bool _isRenaming;

    internal FlatTreeItem(ITreeNode node, ITreeNode? parent, int level)
    {
        Node = node;
        Parent = parent;
        Level = level;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the underlying tree node.</summary>
    public ITreeNode Node { get; }

    /// <summary>Gets the parent node, or <see langword="null"/> for root items.</summary>
    public ITreeNode? Parent { get; }

    /// <summary>Gets the zero-based nesting depth used to compute row indentation.</summary>
    public int Level { get; }

    /// <summary>Gets a value indicating whether the item is currently in inline rename mode (managed by <see cref="ModelTree"/>).</summary>
    public bool IsRenaming
    {
        get => _isRenaming;
        internal set
        {
            if (_isRenaming != value)
            {
                _isRenaming = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRenaming)));
            }
        }
    }

    /// <summary>Gets a value indicating whether the node currently has child nodes (controls expander visibility).</summary>
    public bool HasChildren
    {
        get => _hasChildren;
        internal set
        {
            if (_hasChildren != value)
            {
                _hasChildren = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasChildren)));
            }
        }
    }
}
