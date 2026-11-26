using System.Numerics;

namespace WorkbenchUI.Wpf.Viewport3D.Tests;

/// <summary>
/// Tests through-rectangle selection math, hover ID resolution,
/// and generation tracking for asynchronous geometry builds.
/// </summary>
public class ViewportRectSelectTests
{
    private const double PixelWidth = 100.0;
    private const double PixelHeight = 100.0;

    /// <summary>Camera (0,0,10) -> origin, orthographic (10x10). Origin projects to pixel (50,50).</summary>
    private static Matrix4x4 CreateOrthoViewProj() =>
        Matrix4x4.CreateLookAt(new Vector3(0, 0, 10), Vector3.Zero, Vector3.UnitY)
        * Matrix4x4.CreateOrthographic(10, 10, 0.1f, 100.0f);

    /// <summary>Camera (0,0,10) -> origin, perspective (for behind-camera tests).</summary>
    private static Matrix4x4 CreatePerspectiveViewProj() =>
        Matrix4x4.CreateLookAt(new Vector3(0, 0, 10), Vector3.Zero, Vector3.UnitY)
        * Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3, 1.0f, 0.1f, 100.0f);

    private static readonly Vector2 CenterRectMin = new(30, 30);
    private static readonly Vector2 CenterRectMax = new(70, 70);

    // ================= IsNodeInRect =================

    [Fact]
    public void IsNodeInRect_CenterNode_Inside()
    {
        var viewProj = CreateOrthoViewProj();
        double[] positions = [0.0, 0.0, 0.0];

        Assert.True(ViewportRectSelect.IsNodeInRect(
            positions, null, 0.0, 0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, ViewportSection.DisabledClip));
    }

    [Fact]
    public void IsNodeInRect_OffsetNode_Outside()
    {
        var viewProj = CreateOrthoViewProj();
        // x=4 -> NDC 0.8 -> pixel 90 (outside rect 30-70)
        double[] positions = [4.0, 0.0, 0.0];

        Assert.False(ViewportRectSelect.IsNodeInRect(
            positions, null, 0.0, 0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, ViewportSection.DisabledClip));
    }

    [Fact]
    public void IsNodeInRect_BehindCamera_Excluded()
    {
        var viewProj = CreatePerspectiveViewProj();
        double[] positions = [0.0, 0.0, 20.0]; // Behind camera (z=10)

        Assert.False(ViewportRectSelect.IsNodeInRect(
            positions, null, 0.0, 0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, ViewportSection.DisabledClip));
    }

    [Fact]
    public void IsNodeInRect_ClippedNode_Excluded()
    {
        var viewProj = CreateOrthoViewProj();
        double[] positions = [-1.0, 0.0, 0.0]; // x<0 is on the clipped side

        var clip = new Vector4(1, 0, 0, 0); // Only x >= 0 visible
        Assert.False(ViewportRectSelect.IsNodeInRect(
            positions, null, 0.0, 0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, clip));

        // Same vertex is inside rect when clipping is disabled (x=-1 -> pixel 40)
        Assert.True(ViewportRectSelect.IsNodeInRect(
            positions, null, 0.0, 0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, ViewportSection.DisabledClip));
    }

    [Fact]
    public void IsNodeInRect_UsesDeformedCoordinates()
    {
        var viewProj = CreateOrthoViewProj();
        double[] positions = [100.0, 0.0, 0.0];      // Undeformed position is off-screen
        double[] displacements = [-100.0, 0.0, 0.0]; // Deformed position is at origin

        Assert.False(ViewportRectSelect.IsNodeInRect(
            positions, displacements, 0.0, 0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, ViewportSection.DisabledClip));

        Assert.True(ViewportRectSelect.IsNodeInRect(
            positions, displacements, 1.0, 0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, ViewportSection.DisabledClip));
    }

    [Fact]
    public void IsNodeInRect_InvalidIndex_False()
    {
        var viewProj = CreateOrthoViewProj();
        double[] positions = [0.0, 0.0, 0.0];

        Assert.False(ViewportRectSelect.IsNodeInRect(
            positions, null, 0.0, 5, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, ViewportSection.DisabledClip));
    }

    // ================= IsTriangleInRect =================

    /// <summary>Small triangle near the origin (all vertices project inside the center rectangle).</summary>
    private static (double[] Positions, int[] Triangles) CreateCenterTriangle() =>
        ([0.0, 0.0, 0.0, 0.5, 0.0, 0.0, 0.0, 0.5, 0.0], [0, 1, 2]);

    [Fact]
    public void IsTriangleInRect_AllVerticesInside_True()
    {
        var viewProj = CreateOrthoViewProj();
        var (positions, triangles) = CreateCenterTriangle();

        Assert.True(ViewportRectSelect.IsTriangleInRect(
            positions, triangles, null, 0.0, 0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, ViewportSection.DisabledClip));
    }

    [Fact]
    public void IsTriangleInRect_OneVertexOutside_False()
    {
        var viewProj = CreateOrthoViewProj();
        // Vertex 1 at x=4 (pixel 90, outside rect) -> fails strict containment
        double[] positions = [0.0, 0.0, 0.0, 4.0, 0.0, 0.0, 0.0, 0.5, 0.0];
        int[] triangles = [0, 1, 2];

        Assert.False(ViewportRectSelect.IsTriangleInRect(
            positions, triangles, null, 0.0, 0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, ViewportSection.DisabledClip));
    }

    [Fact]
    public void IsTriangleInRect_FullyClipped_Excluded()
    {
        var viewProj = CreateOrthoViewProj();
        // All vertices have x<0 (clipped side), though their projection falls inside the rect
        double[] positions = [-0.5, 0.0, 0.0, -0.2, 0.0, 0.0, -0.5, 0.5, 0.0];
        int[] triangles = [0, 1, 2];
        var clip = new Vector4(1, 0, 0, 0);

        Assert.False(ViewportRectSelect.IsTriangleInRect(
            positions, triangles, null, 0.0, 0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, clip));
    }

    [Fact]
    public void IsTriangleInRect_PartiallyClipped_StillSelected()
    {
        var viewProj = CreateOrthoViewProj();
        // Only 1 vertex is clipped -> triangle remains partially visible and selectable
        double[] positions = [-0.5, 0.0, 0.0, 0.5, 0.0, 0.0, 0.0, 0.5, 0.0];
        int[] triangles = [0, 1, 2];
        var clip = new Vector4(1, 0, 0, 0);

        Assert.True(ViewportRectSelect.IsTriangleInRect(
            positions, triangles, null, 0.0, 0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, clip));
    }

    // ================= ScreenBoundsMayOverlapRect (Chunk AABB Coarse Culling) =================

    [Fact]
    public void ScreenBounds_OverlappingBox_True()
    {
        var viewProj = CreateOrthoViewProj();

        Assert.True(ViewportRectSelect.ScreenBoundsMayOverlapRect(
            new Vector3(-0.5f), new Vector3(0.5f), 0.0f,
            in viewProj, PixelWidth, PixelHeight, CenterRectMin, CenterRectMax));
    }

    [Fact]
    public void ScreenBounds_DisjointBox_False()
    {
        var viewProj = CreateOrthoViewProj();

        // x=3..4 -> pixels 80..90 (does not overlap rect 30..70)
        Assert.False(ViewportRectSelect.ScreenBoundsMayOverlapRect(
            new Vector3(3.0f, -0.5f, -0.5f), new Vector3(4.0f, 0.5f, 0.5f), 0.0f,
            in viewProj, PixelWidth, PixelHeight, CenterRectMin, CenterRectMax));
    }

    [Fact]
    public void ScreenBounds_ExpandBringsIntoRect()
    {
        var viewProj = CreateOrthoViewProj();
        var min = new Vector3(3.0f, -0.5f, -0.5f);
        var max = new Vector3(4.0f, 0.5f, 0.5f);

        // With deformation padding 2.5 (expands to x=0.5), overlaps the rect
        Assert.True(ViewportRectSelect.ScreenBoundsMayOverlapRect(
            min, max, 2.5f, in viewProj, PixelWidth, PixelHeight, CenterRectMin, CenterRectMax));
    }

    [Fact]
    public void ScreenBounds_SpansBehindCamera_ConservativeTrue()
    {
        var viewProj = CreatePerspectiveViewProj();

        // An AABB straddling the camera plane (z=10) has undefined screen bounds -> conservatively true
        Assert.True(ViewportRectSelect.ScreenBoundsMayOverlapRect(
            new Vector3(50.0f, 50.0f, 5.0f), new Vector3(51.0f, 51.0f, 15.0f), 0.0f,
            in viewProj, PixelWidth, PixelHeight, CenterRectMin, CenterRectMax));
    }

    // ================= CollectTrianglesInRect =================

    [Fact]
    public void CollectTriangles_RangeRespected()
    {
        var viewProj = CreateOrthoViewProj();

        // Triangle 0: center (hit), Triangle 1: near x=4 (outside rect)
        double[] positions =
        [
            0.0, 0.0, 0.0, 0.5, 0.0, 0.0, 0.0, 0.5, 0.0,
            4.0, 0.0, 0.0, 4.5, 0.0, 0.0, 4.0, 0.5, 0.0,
        ];
        int[] triangles = [0, 1, 2, 3, 4, 5];

        var hits = new List<int>();
        ViewportRectSelect.CollectTrianglesInRect(
            positions, triangles, null, 0.0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, ViewportSection.DisabledClip,
            triangleStart: 0, triangleCount: 2, hits);
        Assert.Equal([0], hits);

        // Restricting range to triangle 1 yields no hit
        hits.Clear();
        ViewportRectSelect.CollectTrianglesInRect(
            positions, triangles, null, 0.0, 0.0, 0.0, 0.0, in viewProj, PixelWidth, PixelHeight,
            CenterRectMin, CenterRectMax, ViewportSection.DisabledClip,
            triangleStart: 1, triangleCount: 1, hits);
        Assert.Empty(hits);
    }
}

/// <summary>Tests for reading the hover ID buffer.</summary>
public class ViewportHoverTests
{
    [Fact]
    public void ReadId_HitPixel_ReturnsMeshAndTriangle()
    {
        // 2x2 pixels; part ID 3 (mesh index 2), triangle 42 at (1,0)
        var buffer = new uint[2 * 2 * 2];
        buffer[(0 * 2 + 1) * 2] = 3;
        buffer[(0 * 2 + 1) * 2 + 1] = 42;

        var hit = ViewportHover.ReadId(buffer, 2, 2, 1, 0);
        Assert.Equal((2, 42), hit);
    }

    [Fact]
    public void ReadId_Background_ReturnsNull()
    {
        var buffer = new uint[2 * 2 * 2];
        Assert.Null(ViewportHover.ReadId(buffer, 2, 2, 0, 0));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(2, 0)]
    [InlineData(0, 2)]
    public void ReadId_OutOfBounds_ReturnsNull(int x, int y)
    {
        var buffer = new uint[2 * 2 * 2];
        buffer[0] = 1;
        Assert.Null(ViewportHover.ReadId(buffer, 2, 2, x, y));
    }
}

/// <summary>Tests for generation tracking in async geometry construction.</summary>
public class GeometryBuildCoordinatorTests
{
    [Fact]
    public void Begin_ReturnsIncreasingGenerations()
    {
        var coordinator = new GeometryBuildCoordinator();
        var (g1, _) = coordinator.Begin();
        var (g2, _) = coordinator.Begin();

        Assert.True(g2 > g1);
        Assert.Equal(g2, coordinator.CurrentGeneration);
    }

    [Fact]
    public void Begin_CancelsPreviousToken()
    {
        var coordinator = new GeometryBuildCoordinator();
        var (g1, token1) = coordinator.Begin();
        Assert.False(token1.IsCancellationRequested);

        var (g2, token2) = coordinator.Begin();
        Assert.True(token1.IsCancellationRequested);   // Older build is cancelled
        Assert.False(token2.IsCancellationRequested);
        Assert.False(coordinator.IsCurrent(g1));       // Older generation results must not be applied
        Assert.True(coordinator.IsCurrent(g2));
    }

    [Fact]
    public void CancelAll_InvalidatesEverything()
    {
        var coordinator = new GeometryBuildCoordinator();
        var (generation, token) = coordinator.Begin();

        coordinator.CancelAll();
        Assert.True(token.IsCancellationRequested);
        Assert.False(coordinator.IsCurrent(generation));

        // Subsequent Begin starts a new generation normally
        var (next, nextToken) = coordinator.Begin();
        Assert.True(coordinator.IsCurrent(next));
        Assert.False(nextToken.IsCancellationRequested);
    }

    [Fact]
    public void RapidRestarts_OnlyLatestIsCurrent()
    {
        var coordinator = new GeometryBuildCoordinator();
        var generations = new List<(int Generation, CancellationToken Token)>();
        for (var i = 0; i < 5; i++)
        {
            generations.Add(coordinator.Begin());
        }

        for (var i = 0; i < 4; i++)
        {
            Assert.True(generations[i].Token.IsCancellationRequested);
            Assert.False(coordinator.IsCurrent(generations[i].Generation));
        }

        Assert.False(generations[4].Token.IsCancellationRequested);
        Assert.True(coordinator.IsCurrent(generations[4].Generation));
    }
}
