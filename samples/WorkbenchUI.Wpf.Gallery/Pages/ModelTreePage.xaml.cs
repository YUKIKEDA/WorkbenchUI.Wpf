using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages;

public partial class ModelTreePage : UserControl
{
    private static readonly Geometry PartIcon = Geometry.Parse(
        "M8,1.5 L14,4.75 L14,11.25 L8,14.5 L2,11.25 L2,4.75 Z M8,3.2 L4,5.4 L8,7.6 L12,5.4 Z");

    private readonly TreeNode[] _roots;

    public ModelTreePage()
    {
        InitializeComponent();

        var resultSets = new TreeNode { Name = "Result Sets" };
        for (var i = 1; i <= 2000; i++)
        {
            resultSets.Children.Add(new TreeNode { Name = $"Frame {i:D4}" });
        }

        var assembly = new TreeNode { Name = "Assembly", IsExpanded = true };
        assembly.Children.Add(MakePart("Bracket-01"));
        assembly.Children.Add(MakePart("Bracket-02"));
        assembly.Children.Add(MakePart("Base-Plate"));

        var analysis = new TreeNode { Name = "Analysis Cases", IsExpanded = true };
        analysis.Children.Add(new TreeNode { Name = "Static (Case 1)" });
        analysis.Children.Add(new TreeNode { Name = "Modal (Case 2)" });

        _roots = [assembly, analysis, resultSets];
        Tree.ItemsSource = _roots;

        ModelTreeExample.Xaml = """
            <wb:ModelTree x:Name="Tree"
                          Height="360"
                          ItemsSource="{Binding RootNodes}"
                          SelectionChanged="OnTreeSelectionChanged" />
            """;

        ModelTreeExample.CSharp = """
            var assembly = new TreeNode { Name = "Assembly", IsExpanded = true };
            var part = new TreeNode { Name = "Bracket-01", Icon = partGeometry };
            part.Children.Add(new TreeNode { Name = "Mesh" });
            assembly.Children.Add(part);

            Tree.ItemsSource = new[] { assembly };
            """;

        TreeViewExample.Xaml = """
            <TreeView Height="200" Width="340">
                <TreeViewItem Header="Assembly-1" IsExpanded="True">
                    <TreeViewItem Header="Part: Bracket" IsExpanded="True">
                        <TreeViewItem Header="Solid Body" />
                    </TreeViewItem>
                </TreeViewItem>
            </TreeView>
            """;
    }

    private void OnRenameClick(object sender, RoutedEventArgs e)
    {
        if (Tree.GetSelectedNodes() is [var node, ..])
        {
            Tree.BeginRename(node);
        }
    }

    private static TreeNode MakePart(string name)
    {
        var part = new TreeNode { Name = name, Icon = PartIcon };
        part.Children.Add(new TreeNode { Name = "Mesh" });
        part.Children.Add(new TreeNode { Name = "Material: SS400" });
        var loads = new TreeNode { Name = "Boundary Conditions" };
        loads.Children.Add(new TreeNode { Name = "Fixed Support" });
        loads.Children.Add(new TreeNode { Name = "Force 1000 N" });
        part.Children.Add(loads);
        return part;
    }

    private void OnTreeSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        SelectionInfo.Text = $"Selected: {Tree.GetSelectedNodes().Count}";

    private void OnExpandAll(object sender, RoutedEventArgs e) => SetExpandedAll(true);

    private void OnCollapseAll(object sender, RoutedEventArgs e) => SetExpandedAll(false);

    private void SetExpandedAll(bool isExpanded)
    {
        foreach (var root in _roots)
        {
            SetExpandedRecursive(root, isExpanded);
        }
    }

    private static void SetExpandedRecursive(ITreeNode node, bool isExpanded)
    {
        node.IsExpanded = isExpanded;
        foreach (var child in node.Children)
        {
            SetExpandedRecursive(child, isExpanded);
        }
    }
}
