using System.ComponentModel;
using System.Windows.Media;

namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Minimal node contract observed by <see cref="ModelTree"/>.
/// Implement this interface on existing view-model hierarchies, or inherit from <see cref="TreeNode"/> for a ready-to-use base implementation.
/// </summary>
public interface ITreeNode : INotifyPropertyChanged
{
    /// <summary>Gets the display name shown in the tree.</summary>
    string Name { get; }

    /// <summary>
    /// Gets the child nodes. To support dynamic updates, consistently return the same
    /// <see cref="System.Collections.Specialized.INotifyCollectionChanged"/> instance (such as an <c>ObservableCollection</c>).
    /// </summary>
    IEnumerable<ITreeNode> Children { get; }

    /// <summary>Gets or sets a value indicating whether the node is expanded. Change notifications keep <see cref="ModelTree"/> in sync.</summary>
    bool IsExpanded { get; set; }

    /// <summary>
    /// Gets or sets the tri-state visibility (<see langword="true"/> = visible, <see langword="false"/> = hidden, <see langword="null"/> = mixed descendant states).
    /// Propagation is managed by <see cref="ModelTree"/>.
    /// </summary>
    bool? IsVisible { get; set; }

    /// <summary>Gets or sets a value indicating whether the node is selected. Synchronized two-way with <see cref="ModelTree"/> multi-selection.</summary>
    bool IsSelected { get; set; }

    /// <summary>Gets the optional icon <see cref="Geometry"/> for the node, or <see langword="null"/> to hide the icon.</summary>
    Geometry? Icon { get; }
}
