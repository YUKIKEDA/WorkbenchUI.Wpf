using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace WorkbenchUI.Wpf.Viewport3D.Rendering;

/// <summary>
/// GPU resources for selection highlights on a single mesh part.
/// <para>
/// - Selected faces: standalone vertex buffer of selected triangle vertices (position + displacement, 24 B),
///   independent of the chunked mesh vertex buffers.
/// - Selected nodes: 6-vertex screen-space quads per node (position + corner + displacement) rendered as circles.
/// Rebuilt on selection changes (negligible cost at interactive rates).
/// </para>
/// </summary>
internal sealed unsafe class GpuSelectionMesh : IDisposable
{
    /// <summary>Point vertex stride: position (12 B) + corner (8 B) + displacement (12 B) = 32 B.</summary>
    public const uint PointVertexStride = 32;

    /// <summary>Selected face vertex stride: position (12 B) + displacement (12 B) = 24 B.</summary>
    public const uint FaceVertexStride = 24;

    private ComPtr<ID3D11Buffer> _faceVertexBuffer;
    private ComPtr<ID3D11Buffer> _nodeVertexBuffer;

    private GpuSelectionMesh()
    {
    }

    public uint FaceVertexCount { get; private set; }

    public uint NodeVertexCount { get; private set; }

    public ID3D11Buffer* FaceVertexBufferHandle => _faceVertexBuffer.Handle;

    public ID3D11Buffer* NodeVertexBufferHandle => _nodeVertexBuffer.Handle;

    /// <summary>Builds selection GPU buffers; returns null if the selection is empty or out of range.</summary>
    public static GpuSelectionMesh? Create(
        ComPtr<ID3D11Device> device,
        ViewportMesh source,
        IReadOnlyCollection<int> selectedFaces,
        IReadOnlyCollection<int> selectedNodes,
        double originX, double originY, double originZ)
    {
        var triangles = source.TriangleIndices;
        var positions = source.Positions;
        var triangleCount = triangles.Length / 3;
        var vertexCount = positions.Length / 3;

        // Pack displacements into both face and node vertices to follow active deformation;
        // WorkbenchViewport rebuilds selection buffers when Displacements is replaced.
        var displacements = ViewportDeformation.ToDisplacementArray(source.Displacements, vertexCount);

        // Gather selected face vertices (silently ignore indices invalidated by geometry replacement)
        var faceVertices = new List<float>(selectedFaces.Count * 3 * 6);
        foreach (var face in selectedFaces)
        {
            if (face < 0 || face >= triangleCount)
            {
                continue;
            }

            for (var corner = 0; corner < 3; corner++)
            {
                var v = triangles[face * 3 + corner];
                faceVertices.Add((float)(positions[v * 3] - originX));
                faceVertices.Add((float)(positions[v * 3 + 1] - originY));
                faceVertices.Add((float)(positions[v * 3 + 2] - originZ));
                faceVertices.Add(displacements[v * 3]);
                faceVertices.Add(displacements[v * 3 + 1]);
                faceVertices.Add(displacements[v * 3 + 2]);
            }
        }
        var validNodes = selectedNodes.Where(n => n >= 0 && n < vertexCount).ToList();
        var nodeVertices = new float[validNodes.Count * 6 * 8];
        ReadOnlySpan<(float X, float Y)> corners =
        [
            (-0.5f, -0.5f), (0.5f, -0.5f), (0.5f, 0.5f),
            (-0.5f, -0.5f), (0.5f, 0.5f), (-0.5f, 0.5f),
        ];

        for (var i = 0; i < validNodes.Count; i++)
        {
            var node = validNodes[i];
            var px = (float)(positions[node * 3] - originX);
            var py = (float)(positions[node * 3 + 1] - originY);
            var pz = (float)(positions[node * 3 + 2] - originZ);

            for (var c = 0; c < 6; c++)
            {
                var dst = (i * 6 + c) * 8;
                nodeVertices[dst] = px;
                nodeVertices[dst + 1] = py;
                nodeVertices[dst + 2] = pz;
                nodeVertices[dst + 3] = corners[c].X;
                nodeVertices[dst + 4] = corners[c].Y;
                nodeVertices[dst + 5] = displacements[node * 3];
                nodeVertices[dst + 6] = displacements[node * 3 + 1];
                nodeVertices[dst + 7] = displacements[node * 3 + 2];
            }
        }

        if (faceVertices.Count == 0 && nodeVertices.Length == 0)
        {
            return null;
        }

        var result = new GpuSelectionMesh
        {
            FaceVertexCount = (uint)(faceVertices.Count / 6),
            NodeVertexCount = (uint)(validNodes.Count * 6),
        };

        if (faceVertices.Count > 0)
        {
            var array = faceVertices.ToArray();
            fixed (float* pVertices = array)
            {
                result._faceVertexBuffer = CreateImmutableBuffer(
                    device, pVertices, (uint)(array.Length * sizeof(float)), BindFlag.VertexBuffer);
            }
        }

        if (nodeVertices.Length > 0)
        {
            fixed (float* pVertices = nodeVertices)
            {
                result._nodeVertexBuffer = CreateImmutableBuffer(
                    device, pVertices, (uint)(nodeVertices.Length * sizeof(float)), BindFlag.VertexBuffer);
            }
        }

        return result;
    }

    private static ComPtr<ID3D11Buffer> CreateImmutableBuffer(
        ComPtr<ID3D11Device> device, void* data, uint byteWidth, BindFlag bindFlag)
    {
        var desc = new BufferDesc
        {
            ByteWidth = byteWidth,
            Usage = Usage.Immutable,
            BindFlags = (uint)bindFlag,
        };
        var subresource = new SubresourceData { PSysMem = data };

        ComPtr<ID3D11Buffer> buffer = default;
        SilkMarshal.ThrowHResult(device.CreateBuffer(in desc, in subresource, ref buffer));
        return buffer;
    }

    public void Dispose()
    {
        _faceVertexBuffer.Dispose();
        _nodeVertexBuffer.Dispose();
        _faceVertexBuffer = default;
        _nodeVertexBuffer = default;
    }
}
