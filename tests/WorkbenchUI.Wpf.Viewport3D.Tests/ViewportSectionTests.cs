using System.Numerics;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Viewport3D.Tests;

public class ViewportSectionTests
{
    [Fact]
    public void ComputeClipCoefficients_NullPlane_ReturnsNull()
    {
        Assert.Null(ViewportSection.ComputeClipCoefficients(null, 0.0, 0.0, 0.0));
    }

    [Fact]
    public void ComputeClipCoefficients_ZeroNormal_ReturnsNull()
    {
        var plane = new SectionPlane(0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
        Assert.Null(ViewportSection.ComputeClipCoefficients(plane, 0.0, 0.0, 0.0));
    }

    [Fact]
    public void ComputeClipCoefficients_NormalizesNormal()
    {
        // Normal (0,0,2) normalizes to (0,0,1), and plane through z=5 has constant term -5
        var plane = new SectionPlane(0.0, 0.0, 5.0, 0.0, 0.0, 2.0);
        var clip = ViewportSection.ComputeClipCoefficients(plane, 0.0, 0.0, 0.0);

        Assert.NotNull(clip);
        Assert.Equal(0.0f, clip.Value.X, 6);
        Assert.Equal(0.0f, clip.Value.Y, 6);
        Assert.Equal(1.0f, clip.Value.Z, 6);
        Assert.Equal(-5.0f, clip.Value.W, 4);
    }

    [Fact]
    public void ComputeClipCoefficients_SignedDistanceMatchesHalfSpace()
    {
        // Plane x=3, normal +X -> x>3 is positive (visible), x<3 is negative (clipped)
        var plane = new SectionPlane(3.0, 0.0, 0.0, 1.0, 0.0, 0.0);
        var clip = ViewportSection.ComputeClipCoefficients(plane, 0.0, 0.0, 0.0)!.Value;

        float Distance(float x) => clip.X * x + clip.W;
        Assert.True(Distance(10.0f) > 0.0f);
        Assert.True(Distance(-10.0f) < 0.0f);
        Assert.Equal(0.0f, Distance(3.0f), 4);
    }

    [Fact]
    public void ComputeClipCoefficients_RecentersWithSceneOrigin()
    {
        // Large-coordinate support: even near scene origin 1e7, localized constant term stays small to preserve precision
        const double bigOrigin = 1.0e7;
        var plane = new SectionPlane(bigOrigin + 5.0, 0.0, 0.0, 1.0, 0.0, 0.0);
        var clip = ViewportSection.ComputeClipCoefficients(plane, bigOrigin, 0.0, 0.0)!.Value;

        Assert.Equal(1.0f, clip.X, 6);
        Assert.Equal(-5.0f, clip.W, 3);
    }

    [Fact]
    public void ComputeClipCoefficients_FlippedNormal_InvertsHalfSpace()
    {
        var plane = new SectionPlane(0.0, 0.0, 0.0, 0.0, 1.0, 0.0);
        var flipped = new SectionPlane(0.0, 0.0, 0.0, 0.0, -1.0, 0.0);

        var clip = ViewportSection.ComputeClipCoefficients(plane, 0.0, 0.0, 0.0)!.Value;
        var clipFlipped = ViewportSection.ComputeClipCoefficients(flipped, 0.0, 0.0, 0.0)!.Value;

        // Sign flips at the same point y=2
        var d = clip.Y * 2.0f + clip.W;
        var dFlipped = clipFlipped.Y * 2.0f + clipFlipped.W;
        Assert.True(d > 0.0f);
        Assert.True(dFlipped < 0.0f);
    }

    [Fact]
    public void BuildIndicatorVertices_Returns14VerticesOnPlane()
    {
        var plane = new SectionPlane(1.0, 2.0, 3.0, 0.3, -0.5, 0.8);
        var clip = ViewportSection.ComputeClipCoefficients(plane, 0.0, 0.0, 0.0)!.Value;

        var vertices = ViewportSection.BuildIndicatorVertices(clip, sceneRadius: 10.0f);

        Assert.Equal(14 * 6, vertices.Length);

        // All vertices lie on the plane (signed distance ~= 0) + displacement components zero-padded
        for (var i = 0; i < 14; i++)
        {
            var p = new Vector3(vertices[i * 6], vertices[i * 6 + 1], vertices[i * 6 + 2]);
            var distance = Vector3.Dot(p, new Vector3(clip.X, clip.Y, clip.Z)) + clip.W;
            Assert.Equal(0.0f, distance, 3);

            Assert.Equal(0.0f, vertices[i * 6 + 3]);
            Assert.Equal(0.0f, vertices[i * 6 + 4]);
            Assert.Equal(0.0f, vertices[i * 6 + 5]);
        }
    }

    [Fact]
    public void BuildIndicatorVertices_QuadSizeFollowsSceneRadius()
    {
        var clip = new Vector4(0.0f, 0.0f, 1.0f, 0.0f); // z=0 plane
        const float radius = 20.0f;

        var vertices = ViewportSection.BuildIndicatorVertices(clip, radius);

        // Quad corners (4 unique vertices among the first 6) sit at distance radius * 1.15 * sqrt(2) from center
        var expected = radius * 1.15f * MathF.Sqrt(2.0f);
        var corner = new Vector3(vertices[0], vertices[1], vertices[2]);
        Assert.Equal(expected, corner.Length(), 2);
    }

    [Fact]
    public void BuildIndicatorVertices_DegenerateNormal_ReturnsEmpty()
    {
        var vertices = ViewportSection.BuildIndicatorVertices(Vector4.Zero, 10.0f);
        Assert.Empty(vertices);
    }

    [Fact]
    public void SectionPlane_RaisesPropertyChanged()
    {
        var plane = new SectionPlane();
        var raised = new List<string?>();
        plane.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        plane.OriginX = 5.0;
        plane.NormalY = -1.0;
        plane.NormalZ = 1.0; // Same as default -> no notification

        Assert.Equal([nameof(SectionPlane.OriginX), nameof(SectionPlane.NormalY)], raised);
    }

    [Fact]
    public void ViewportMesh_IsClippable_DefaultTrueAndNotifies()
    {
        var mesh = new ViewportMesh();
        Assert.True(mesh.IsClippable);

        string? raised = null;
        mesh.PropertyChanged += (_, e) => raised = e.PropertyName;

        mesh.IsClippable = false;
        Assert.False(mesh.IsClippable);
        Assert.Equal(nameof(ViewportMesh.IsClippable), raised);
    }
}
