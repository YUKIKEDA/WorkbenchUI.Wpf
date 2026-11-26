namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Selection state model for <see cref="WorkbenchViewport"/>.
/// <para>
/// Maintains selected sets of parts, faces (triangle indices), and nodes (vertex indices),
/// raising <see cref="Changed"/> when modified. <see cref="WorkbenchViewport"/> subscribes to this model
/// to render highlights automatically even when selection is mutated programmatically (e.g., from a model tree).
/// Mapping render-level indices back to solver entities is the application's responsibility.
/// </para>
/// <para>
/// Wrap batch mutations in <see cref="BeginUpdate"/> / <see cref="EndUpdate"/> to coalesce multiple changes
/// into a single <see cref="Changed"/> notification. UI-thread only.
/// </para>
/// </summary>
public sealed class ViewportSelection
{
    private readonly HashSet<ViewportMesh> _parts = [];
    private readonly Dictionary<ViewportMesh, HashSet<int>> _faces = [];
    private readonly Dictionary<ViewportMesh, HashSet<int>> _nodes = [];

    private int _updateDepth;
    private bool _changedDuringUpdate;

    /// <summary>Raised when the selection changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Collection of currently selected parts.</summary>
    public IReadOnlyCollection<ViewportMesh> SelectedParts => _parts;

    /// <summary>Meshes that currently have one or more selected faces.</summary>
    public IReadOnlyCollection<ViewportMesh> MeshesWithFaceSelection => _faces.Keys;

    /// <summary>Meshes that currently have one or more selected nodes.</summary>
    public IReadOnlyCollection<ViewportMesh> MeshesWithNodeSelection => _nodes.Keys;

    /// <summary>Number of selected parts.</summary>
    public int PartCount => _parts.Count;

    /// <summary>Total number of selected faces (triangles).</summary>
    public int FaceCount => _faces.Values.Sum(s => s.Count);

    /// <summary>Total number of selected nodes.</summary>
    public int NodeCount => _nodes.Values.Sum(s => s.Count);

    /// <summary>Gets whether no parts, faces, or nodes are currently selected.</summary>
    public bool IsEmpty => _parts.Count == 0 && _faces.Count == 0 && _nodes.Count == 0;

    /// <summary>Returns the selected triangle indices for the specified mesh.</summary>
    public IReadOnlyCollection<int> GetSelectedFaces(ViewportMesh mesh) =>
        _faces.TryGetValue(mesh, out var set) ? set : [];

    /// <summary>Returns the selected node indices for the specified mesh.</summary>
    public IReadOnlyCollection<int> GetSelectedNodes(ViewportMesh mesh) =>
        _nodes.TryGetValue(mesh, out var set) ? set : [];

    /// <summary>Returns whether <paramref name="mesh"/> is selected as a part.</summary>
    public bool IsPartSelected(ViewportMesh mesh) => _parts.Contains(mesh);

    /// <summary>Returns whether <paramref name="triangleIndex"/> on <paramref name="mesh"/> is selected.</summary>
    public bool IsFaceSelected(ViewportMesh mesh, int triangleIndex) =>
        _faces.TryGetValue(mesh, out var set) && set.Contains(triangleIndex);

    /// <summary>Returns whether <paramref name="nodeIndex"/> on <paramref name="mesh"/> is selected.</summary>
    public bool IsNodeSelected(ViewportMesh mesh, int nodeIndex) =>
        _nodes.TryGetValue(mesh, out var set) && set.Contains(nodeIndex);

    // ================= Mutators =================

    /// <summary>Begins a batch update, suppressing <see cref="Changed"/> notifications until <see cref="EndUpdate"/> (nestable).</summary>
    public void BeginUpdate() => _updateDepth++;

    /// <summary>Ends a batch update and raises <see cref="Changed"/> once if any modifications occurred.</summary>
    public void EndUpdate()
    {
        if (_updateDepth == 0)
        {
            return;
        }

        if (--_updateDepth == 0 && _changedDuringUpdate)
        {
            _changedDuringUpdate = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Clears all selected parts, faces, and nodes.</summary>
    public void Clear()
    {
        if (IsEmpty)
        {
            return;
        }

        _parts.Clear();
        _faces.Clear();
        _nodes.Clear();
        RaiseChanged();
    }

    /// <summary>Adds a part to the selection.</summary>
    public void AddPart(ViewportMesh mesh)
    {
        if (_parts.Add(mesh))
        {
            RaiseChanged();
        }
    }

    /// <summary>Removes a part from the selection.</summary>
    public void RemovePart(ViewportMesh mesh)
    {
        if (_parts.Remove(mesh))
        {
            RaiseChanged();
        }
    }

    /// <summary>Toggles selection state for a part.</summary>
    public void TogglePart(ViewportMesh mesh)
    {
        if (!_parts.Remove(mesh))
        {
            _parts.Add(mesh);
        }

        RaiseChanged();
    }

    /// <summary>Adds a triangle face on <paramref name="mesh"/> to the selection.</summary>
    public void AddFace(ViewportMesh mesh, int triangleIndex) => AddFaces(mesh, [triangleIndex]);

    /// <summary>Adds multiple triangle faces on <paramref name="mesh"/> to the selection.</summary>
    public void AddFaces(ViewportMesh mesh, IEnumerable<int> triangleIndices)
    {
        if (!_faces.TryGetValue(mesh, out var set))
        {
            set = [];
            _faces[mesh] = set;
        }

        var changed = false;
        foreach (var index in triangleIndices)
        {
            changed |= set.Add(index);
        }

        if (set.Count == 0)
        {
            _faces.Remove(mesh);
        }

        if (changed)
        {
            RaiseChanged();
        }
    }

    /// <summary>Removes a triangle face on <paramref name="mesh"/> from the selection.</summary>
    public void RemoveFace(ViewportMesh mesh, int triangleIndex)
    {
        if (_faces.TryGetValue(mesh, out var set) && set.Remove(triangleIndex))
        {
            if (set.Count == 0)
            {
                _faces.Remove(mesh);
            }

            RaiseChanged();
        }
    }

    /// <summary>Toggles selection state for a triangle face on <paramref name="mesh"/>.</summary>
    public void ToggleFace(ViewportMesh mesh, int triangleIndex)
    {
        if (IsFaceSelected(mesh, triangleIndex))
        {
            RemoveFace(mesh, triangleIndex);
        }
        else
        {
            AddFace(mesh, triangleIndex);
        }
    }

    /// <summary>Adds a node on <paramref name="mesh"/> to the selection.</summary>
    public void AddNode(ViewportMesh mesh, int nodeIndex) => AddNodes(mesh, [nodeIndex]);

    /// <summary>Adds multiple nodes on <paramref name="mesh"/> to the selection.</summary>
    public void AddNodes(ViewportMesh mesh, IEnumerable<int> nodeIndices)
    {
        if (!_nodes.TryGetValue(mesh, out var set))
        {
            set = [];
            _nodes[mesh] = set;
        }

        var changed = false;
        foreach (var index in nodeIndices)
        {
            changed |= set.Add(index);
        }

        if (set.Count == 0)
        {
            _nodes.Remove(mesh);
        }

        if (changed)
        {
            RaiseChanged();
        }
    }

    /// <summary>Removes a node on <paramref name="mesh"/> from the selection.</summary>
    public void RemoveNode(ViewportMesh mesh, int nodeIndex)
    {
        if (_nodes.TryGetValue(mesh, out var set) && set.Remove(nodeIndex))
        {
            if (set.Count == 0)
            {
                _nodes.Remove(mesh);
            }

            RaiseChanged();
        }
    }

    /// <summary>Toggles selection state for a node on <paramref name="mesh"/>.</summary>
    public void ToggleNode(ViewportMesh mesh, int nodeIndex)
    {
        if (IsNodeSelected(mesh, nodeIndex))
        {
            RemoveNode(mesh, nodeIndex);
        }
        else
        {
            AddNode(mesh, nodeIndex);
        }
    }

    /// <summary>
    /// Removes selection entries for meshes no longer present in <paramref name="existingMeshes"/>
    /// (keeps selection synchronized when parts are removed from MeshSource).
    /// </summary>
    internal void PruneTo(IReadOnlyCollection<ViewportMesh> existingMeshes)
    {
        var changed = _parts.RemoveWhere(m => !existingMeshes.Contains(m)) > 0;

        foreach (var stale in _faces.Keys.Where(m => !existingMeshes.Contains(m)).ToList())
        {
            _faces.Remove(stale);
            changed = true;
        }

        foreach (var stale in _nodes.Keys.Where(m => !existingMeshes.Contains(m)).ToList())
        {
            _nodes.Remove(stale);
            changed = true;
        }

        if (changed)
        {
            RaiseChanged();
        }
    }

    private void RaiseChanged()
    {
        if (_updateDepth > 0)
        {
            _changedDuringUpdate = true;
        }
        else
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
