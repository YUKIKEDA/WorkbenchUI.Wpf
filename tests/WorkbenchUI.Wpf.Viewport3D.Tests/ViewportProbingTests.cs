using System.Numerics;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Viewport3D.Tests;

public class ViewportProbingTests
{
    private const double Width = 800.0;
    private const double Height = 600.0;

    /// <summary>Test view-projection matrix looking near the origin from +Z (D3D depth convention).</summary>
    private static Matrix4x4 CreateViewProj(Vector3 eye, Vector3 target)
    {
        var view = Matrix4x4.CreateLookAt(eye, target, Vector3.UnitY);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(
            (float)(Math.PI / 4.0), (float)(Width / Height), 0.1f, 1000.0f);
        return view * proj;
    }

    // ================= ComputePickRay =================

    [Fact]
    public void ComputePickRay_PassesThroughProjectedPoint()
    {
        // Projection -> unprojection round-trip: project a 3D point to a pixel,
        // and verify the ray built from that pixel passes through the original point
        var viewProj = CreateViewProj(new Vector3(3.0f, 2.0f, 10.0f), Vector3.Zero);
        var point = new Vector3(0.7f, -0.4f, 0.2f);

        var pixel = ViewportPicking.ProjectToPixel(point, in viewProj, Width, Height);
        Assert.NotNull(pixel);

        var ray = ViewportProbing.ComputePickRay(pixel.Value, in viewProj, Width, Height);
        Assert.NotNull(ray);

        // Distance from point to ray line
        var toPoint = point - ray.Value.Origin;
        var distance = Vector3.Cross(toPoint, ray.Value.Direction).Length();
        Assert.True(distance < 1e-3f, $"Ray did not pass through point: distance={distance}");
    }

    [Fact]
    public void ComputePickRay_SingularMatrix_ReturnsNull()
    {
        var singular = default(Matrix4x4); // All zeros -> non-invertible
        Assert.Null(ViewportProbing.ComputePickRay(new Vector2(10.0f, 10.0f), in singular, Width, Height));
    }

    // ================= TryIntersectTriangle =================

    [Fact]
    public void TryIntersectTriangle_CentroidHit_ReturnsUniformBarycentric()
    {
        var v0 = new Vector3(0.0f, 0.0f, 0.0f);
        var v1 = new Vector3(3.0f, 0.0f, 0.0f);
        var v2 = new Vector3(0.0f, 3.0f, 0.0f);
        var centroid = (v0 + v1 + v2) / 3.0f;
        var origin = centroid + new Vector3(0.0f, 0.0f, 5.0f);

        var hit = ViewportProbing.TryIntersectTriangle(
            origin, new Vector3(0.0f, 0.0f, -1.0f), v0, v1, v2, out var t, out var bary);

        Assert.True(hit);
        Assert.Equal(5.0f, t, 4);
        Assert.Equal(1.0f / 3.0f, bary.X, 4);
        Assert.Equal(1.0f / 3.0f, bary.Y, 4);
        Assert.Equal(1.0f / 3.0f, bary.Z, 4);
    }

    [Fact]
    public void TryIntersectTriangle_BackFace_StillHits()
    {
        // Double-sided hit test (back-facing triangles also hit)
        var v0 = new Vector3(0.0f, 0.0f, 0.0f);
        var v1 = new Vector3(0.0f, 3.0f, 0.0f); // Reversed winding
        var v2 = new Vector3(3.0f, 0.0f, 0.0f);

        var hit = ViewportProbing.TryIntersectTriangle(
            new Vector3(1.0f, 1.0f, 5.0f), new Vector3(0.0f, 0.0f, -1.0f),
            v0, v1, v2, out _, out _);

        Assert.True(hit);
    }

    [Fact]
    public void TryIntersectTriangle_OutsideTriangle_Misses()
    {
        var v0 = new Vector3(0.0f, 0.0f, 0.0f);
        var v1 = new Vector3(3.0f, 0.0f, 0.0f);
        var v2 = new Vector3(0.0f, 3.0f, 0.0f);

        var hit = ViewportProbing.TryIntersectTriangle(
            new Vector3(5.0f, 5.0f, 5.0f), new Vector3(0.0f, 0.0f, -1.0f),
            v0, v1, v2, out _, out _);

        Assert.False(hit);
    }

    [Fact]
    public void TryIntersectTriangle_BehindRay_Misses()
    {
        var v0 = new Vector3(0.0f, 0.0f, 0.0f);
        var v1 = new Vector3(3.0f, 0.0f, 0.0f);
        var v2 = new Vector3(0.0f, 3.0f, 0.0f);

        // Triangle lies behind the ray origin (further in +Z)
        var hit = ViewportProbing.TryIntersectTriangle(
            new Vector3(1.0f, 1.0f, -5.0f), new Vector3(0.0f, 0.0f, -1.0f),
            v0, v1, v2, out _, out _);

        Assert.False(hit);
    }

    [Fact]
    public void TryIntersectTriangle_ParallelRay_Misses()
    {
        var v0 = new Vector3(0.0f, 0.0f, 0.0f);
        var v1 = new Vector3(3.0f, 0.0f, 0.0f);
        var v2 = new Vector3(0.0f, 3.0f, 0.0f);

        var hit = ViewportProbing.TryIntersectTriangle(
            new Vector3(1.0f, 1.0f, 5.0f), new Vector3(1.0f, 0.0f, 0.0f),
            v0, v1, v2, out _, out _);

        Assert.False(hit);
    }

    [Fact]
    public void TryIntersectTriangle_NearEdge_ClampsAndNormalizes()
    {
        var v0 = new Vector3(0.0f, 0.0f, 0.0f);
        var v1 = new Vector3(3.0f, 0.0f, 0.0f);
        var v2 = new Vector3(0.0f, 3.0f, 0.0f);

        // Slightly outside edge v0-v1 (within tolerance) -> clamped, weights sum to 1
        var hit = ViewportProbing.TryIntersectTriangle(
            new Vector3(1.5f, -0.01f, 5.0f), new Vector3(0.0f, 0.0f, -1.0f),
            v0, v1, v2, out _, out var bary);

        Assert.True(hit);
        Assert.True(bary.X >= 0.0f && bary.Y >= 0.0f && bary.Z >= 0.0f);
        Assert.Equal(1.0f, bary.X + bary.Y + bary.Z, 4);
    }

    // ================= Probe (Integration) =================

    private static ViewportMesh CreateUnitTriangleMesh() => new()
    {
        Name = "tri",
        Positions =
        [
            0.0, 0.0, 0.0,
            4.0, 0.0, 0.0,
            0.0, 4.0, 0.0,
        ],
        TriangleIndices = [0, 1, 2],
        ScalarValues = [10.0, 30.0, 50.0],
    };

    [Fact]
    public void Probe_InterpolatesScalarAtClickedPoint()
    {
        var mesh = CreateUnitTriangleMesh();
        var viewProj = CreateViewProj(new Vector3(1.0f, 1.0f, 12.0f), new Vector3(1.0f, 1.0f, 0.0f));

        // Project a known interior point (barycentric 0.5, 0.25, 0.25) to a pixel and probe it
        var target = new Vector3(
            0.25f * 4.0f, // x = w1 * 4
            0.25f * 4.0f, // y = w2 * 4
            0.0f);
        var pixel = ViewportPicking.ProjectToPixel(target, in viewProj, Width, Height)!.Value;

        var result = ViewportProbing.Probe(
            mesh, 0, pixel, in viewProj, Width, Height, 0.0, 0.0, 0.0, 0.0);

        Assert.NotNull(result);
        var expected = 0.5 * 10.0 + 0.25 * 30.0 + 0.25 * 50.0; // = 25
        Assert.Equal(expected, result.ScalarValue!.Value, 1);
        Assert.Equal(0, result.NodeIndex); // Vertex with largest barycentric weight = v0
        Assert.Equal(10.0, result.NodeScalarValue!.Value, 6);
        Assert.Equal(target.X, result.X, 2);
        Assert.Equal(target.Y, result.Y, 2);
        Assert.Same(mesh, result.Mesh);
        Assert.Equal(0, result.TriangleIndex);
    }

    [Fact]
    public void Probe_WithDeformation_HitsDeformedTriangle_ReturnsUndeformedCoords()
    {
        // Probe a deformed mesh shifted by +2 in X (effective +1 at scale 0.5)
        var mesh = CreateUnitTriangleMesh();
        mesh.Displacements =
        [
            2.0, 0.0, 0.0,
            2.0, 0.0, 0.0,
            2.0, 0.0, 0.0,
        ];
        const double scale = 0.5;
        var viewProj = CreateViewProj(new Vector3(2.0f, 1.0f, 12.0f), new Vector3(2.0f, 1.0f, 0.0f));

        // Click the deformed centroid (model centroid + (1,0,0))
        var deformedCentroid = new Vector3(4.0f / 3.0f + 1.0f, 4.0f / 3.0f, 0.0f);
        var pixel = ViewportPicking.ProjectToPixel(deformedCentroid, in viewProj, Width, Height)!.Value;

        var result = ViewportProbing.Probe(
            mesh, 0, pixel, in viewProj, Width, Height, 0.0, 0.0, 0.0, scale);

        Assert.NotNull(result);
        // Hit point model coordinates lie on the undeformed mesh (centroid ~= (4/3, 4/3))
        Assert.Equal(4.0 / 3.0, result.X, 2);
        Assert.Equal(4.0 / 3.0, result.Y, 2);
        Assert.Equal(30.0, result.ScalarValue!.Value, 0); // Near centroid -> average 30
    }

    [Fact]
    public void Probe_MissedRay_FallsBackToNearestNode()
    {
        // Even for a pixel slightly outside the triangle (simulating GPU ID pass subpixel offset),
        // snaps to the nearest vertex and returns a result
        var mesh = CreateUnitTriangleMesh();
        var viewProj = CreateViewProj(new Vector3(1.0f, 1.0f, 12.0f), new Vector3(1.0f, 1.0f, 0.0f));

        // Slightly outside v1 (4,0,0)
        var outside = new Vector3(4.5f, -0.5f, 0.0f);
        var pixel = ViewportPicking.ProjectToPixel(outside, in viewProj, Width, Height)!.Value;

        var result = ViewportProbing.Probe(
            mesh, 0, pixel, in viewProj, Width, Height, 0.0, 0.0, 0.0, 0.0);

        Assert.NotNull(result);
        Assert.Equal(1, result.NodeIndex);
        Assert.Equal(30.0, result.ScalarValue!.Value, 6); // Exact vertex value
        Assert.Equal(4.0, result.X, 4);
    }

    [Fact]
    public void Probe_InvalidTriangleIndex_ReturnsNull()
    {
        var mesh = CreateUnitTriangleMesh();
        var viewProj = CreateViewProj(new Vector3(0.0f, 0.0f, 10.0f), Vector3.Zero);

        Assert.Null(ViewportProbing.Probe(
            mesh, 5, new Vector2(400.0f, 300.0f), in viewProj, Width, Height, 0.0, 0.0, 0.0, 0.0));
        Assert.Null(ViewportProbing.Probe(
            mesh, -1, new Vector2(400.0f, 300.0f), in viewProj, Width, Height, 0.0, 0.0, 0.0, 0.0));
    }

    [Fact]
    public void Probe_MeshWithoutScalars_ReturnsNullScalar()
    {
        var mesh = CreateUnitTriangleMesh();
        mesh.ScalarValues = null;
        var viewProj = CreateViewProj(new Vector3(1.0f, 1.0f, 12.0f), new Vector3(1.0f, 1.0f, 0.0f));
        var pixel = ViewportPicking.ProjectToPixel(
            new Vector3(1.0f, 1.0f, 0.0f), in viewProj, Width, Height)!.Value;

        var result = ViewportProbing.Probe(
            mesh, 0, pixel, in viewProj, Width, Height, 0.0, 0.0, 0.0, 0.0);

        Assert.NotNull(result);
        Assert.Null(result.ScalarValue);
        Assert.Null(result.NodeScalarValue);
    }

    // ================= Default Label Format =================

    [Fact]
    public void FormatDefaultProbeLabel_WithScalar_UsesInvariantCulture()
    {
        var mesh = CreateUnitTriangleMesh();
        var result = new ProbeResult(mesh, 0, 12, 1.0, 2.0, 3.0, 123.456, 120.0);

        Assert.Equal("N12: 123.5", WorkbenchViewport.FormatDefaultProbeLabel(result));
    }

    [Fact]
    public void FormatDefaultProbeLabel_WithoutScalar_ShowsCoordinates()
    {
        var mesh = CreateUnitTriangleMesh();
        var result = new ProbeResult(mesh, 0, 3, 1.5, -2.0, 0.25, null, null);

        Assert.Equal("N3 (1.5, -2, 0.25)", WorkbenchViewport.FormatDefaultProbeLabel(result));
    }

    // ================= Clip Visibility Check =================

    [Fact]
    public void IsClipped_DisabledClip_NeverClips()
    {
        Assert.False(ViewportSection.IsClipped(new Vector3(100.0f, -50.0f, 3.0f), ViewportSection.DisabledClip));
        Assert.False(ViewportSection.IsClipped(Vector3.Zero, ViewportSection.DisabledClip));
    }

    [Fact]
    public void IsClipped_MatchesSignedDistance()
    {
        // Plane z=1, normal +Z -> z<1 is clipped
        var clip = ViewportSection.ComputeClipCoefficients(
            new SectionPlane(0.0, 0.0, 1.0, 0.0, 0.0, 1.0), 0.0, 0.0, 0.0)!.Value;

        Assert.True(ViewportSection.IsClipped(new Vector3(0.0f, 0.0f, 0.0f), clip));
        Assert.False(ViewportSection.IsClipped(new Vector3(0.0f, 0.0f, 2.0f), clip));
    }

    // ================= ViewportAnnotation =================

    [Fact]
    public void ViewportAnnotation_RaisesPropertyChanged()
    {
        var annotation = new ViewportAnnotation();
        var changed = new List<string?>();
        annotation.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        var mesh = CreateUnitTriangleMesh();
        annotation.Mesh = mesh;
        annotation.NodeIndex = 7;
        annotation.X = 1.0;
        annotation.Y = 2.0;
        annotation.Z = 3.0;
        annotation.Text = "hello";
        annotation.Tag = "tag";

        Assert.Equal(
            [
                nameof(ViewportAnnotation.Mesh),
                nameof(ViewportAnnotation.NodeIndex),
                nameof(ViewportAnnotation.X),
                nameof(ViewportAnnotation.Y),
                nameof(ViewportAnnotation.Z),
                nameof(ViewportAnnotation.Text),
                nameof(ViewportAnnotation.Tag),
            ],
            changed);

        // Reassigning the same value does not raise notification
        changed.Clear();
        annotation.NodeIndex = 7;
        annotation.Text = "hello";
        Assert.Empty(changed);
    }

    [Fact]
    public void ViewportAnnotation_Defaults_AreFreePointAnchor()
    {
        var annotation = new ViewportAnnotation();
        Assert.Null(annotation.Mesh);
        Assert.Equal(-1, annotation.NodeIndex);
        Assert.Equal("", annotation.Text);
    }
}
