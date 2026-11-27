namespace WorkbenchUI.Wpf.Controls;

/// <summary>
/// Opt-in contract for tree nodes that support inline renaming via <c>F2</c> or <see cref="ModelTree.BeginRename"/>.
/// <see cref="ITreeNode"/> remains read-only for names; implementing this interface enables editing on participating node types.
/// The new name is written directly to the model, and the UI updates via <see cref="System.ComponentModel.INotifyPropertyChanged"/>.
/// </summary>
public interface IRenamableNode : ITreeNode
{
    /// <summary>Gets or sets the writable display name shown in the tree.</summary>
    new string Name { get; set; }
}
