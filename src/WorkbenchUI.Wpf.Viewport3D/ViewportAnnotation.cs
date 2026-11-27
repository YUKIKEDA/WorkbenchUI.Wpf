using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// A single viewport annotation. Adding an instance to <see cref="WorkbenchViewport.Annotations"/>
/// renders it as an overlay label chip with a leader line.
/// <para>
/// Two anchor modes are supported:
/// - **Node-bound** (set <see cref="Mesh"/> and <see cref="NodeIndex"/>): follows deformation and frame
///   playback on every frame (default for probing). Automatically hides when the mesh is hidden or the
///   node is clipped by a section plane.
/// - **Free 3D point** (<see cref="Mesh"/> is null; set <see cref="X"/>/<see cref="Y"/>/<see cref="Z"/>):
///   fixed in model space without deformation tracking or automatic hiding (e.g., for dimension callouts).
/// </para>
/// </summary>
public sealed class ViewportAnnotation : INotifyPropertyChanged
{
    private ViewportMesh? _mesh;
    private int _nodeIndex = -1;
    private double _x;
    private double _y;
    private double _z;
    private string _text = "";
    private object? _tag;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Target mesh for node-bound anchors, or null for a free 3D point anchor.</summary>
    public ViewportMesh? Mesh
    {
        get => _mesh;
        set => SetField(ref _mesh, value);
    }

    /// <summary>Node index for node-bound anchors (used when <see cref="Mesh"/> is set).</summary>
    public int NodeIndex
    {
        get => _nodeIndex;
        set => SetField(ref _nodeIndex, value);
    }

    /// <summary>Model-space X coordinate for free 3D point anchors (used when <see cref="Mesh"/> is null).</summary>
    public double X
    {
        get => _x;
        set => SetField(ref _x, value);
    }

    /// <summary>Model-space Y coordinate for free 3D point anchors (used when <see cref="Mesh"/> is null).</summary>
    public double Y
    {
        get => _y;
        set => SetField(ref _y, value);
    }

    /// <summary>Model-space Z coordinate for free 3D point anchors (used when <see cref="Mesh"/> is null).</summary>
    public double Z
    {
        get => _z;
        set => SetField(ref _z, value);
    }

    /// <summary>Label text (supports multiple lines).</summary>
    public string Text
    {
        get => _text;
        set => SetField(ref _text, value ?? "");
    }

    /// <summary>Arbitrary application data (holds <see cref="ProbeResult"/> for probe-created annotations).</summary>
    public object? Tag
    {
        get => _tag;
        set => SetField(ref _tag, value);
    }

    /// <summary>
    /// Returns the label text so bound controls (e.g., ListBox), UI Automation names,
    /// and debugger displays show the annotation content directly.
    /// </summary>
    public override string ToString() => _text;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
