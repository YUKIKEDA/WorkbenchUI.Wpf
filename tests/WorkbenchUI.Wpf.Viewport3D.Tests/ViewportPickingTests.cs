using System.Numerics;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Viewport3D.Tests;

public class ViewportPickingTests
{
    private const double Width = 800.0;
    private const double Height = 600.0;

    private static ViewportCamera CreateCamera() => new()
    {
        Target = Vector3.Zero,
        Yaw = 0.0,
        Pitch = 0.0,
        Distance = 10.0,
        SceneRadius = 5.0,
    };

    private static Matrix4x4 GetViewProj(ViewportCamera camera) =>
        camera.GetViewMatrix() * camera.GetProjectionMatrix(Width / Height);

    [Fact]
    public void ProjectToPixel_Target_MapsToViewportCenter()
    {
        var camera = CreateCamera();
        var viewProj = GetViewProj(camera);

        var pixel = ViewportPicking.ProjectToPixel(Vector3.Zero, in viewProj, Width, Height);

        Assert.NotNull(pixel);
        Assert.Equal(Width / 2.0, pixel.Value.X, 1e-2);
        Assert.Equal(Height / 2.0, pixel.Value.Y, 1e-2);
    }

    [Fact]
    public void ProjectToPixel_PointAboveTarget_MapsAboveCenter()
    {
        var camera = CreateCamera();
        var viewProj = GetViewProj(camera);
        var (_, up) = camera.GetViewBasis();

        var pixel = ViewportPicking.ProjectToPixel(up * 1.0f, in viewProj, Width, Height);

        Assert.NotNull(pixel);
        // Screen Y increases downward, so the upper point has smaller Y than the center
        Assert.True(pixel.Value.Y < Height / 2.0);
        Assert.Equal(Width / 2.0, pixel.Value.X, 1e-2);
    }

    [Fact]
    public void ProjectToPixel_BehindCamera_ReturnsNull()
    {
        var camera = CreateCamera();
        var viewProj = GetViewProj(camera);

        // Camera is at +X (distance 10); x=+20 is behind the camera
        var pixel = ViewportPicking.ProjectToPixel(new Vector3(20.0f, 0.0f, 0.0f), in viewProj, Width, Height);

        Assert.Null(pixel);
    }

    [Fact]
    public void FindNearestNodeOnTriangle_ReturnsNodeClosestToCursor()
    {
        // Camera looks from +X toward the YZ plane; triangle lies on the YZ plane
        var camera = CreateCamera();
        var viewProj = GetViewProj(camera);

        var mesh = new ViewportMesh
        {
            Positions =
            [
                0.0, -2.0, -2.0, // node 0: bottom-left on screen
                0.0, 2.0, -2.0,  // node 1: bottom-right on screen
                0.0, 0.0, 2.0,   // node 2: top on screen
            ],
            TriangleIndices = [0, 1, 2],
        };

        // Click near the projected position of node 2 (top of screen)
        var node2Pixel = ViewportPicking.ProjectToPixel(
            new Vector3(0.0f, 0.0f, 2.0f), in viewProj, Width, Height)!.Value;
        var cursor = node2Pixel + new Vector2(3.0f, 3.0f);

        var nearest = ViewportPicking.FindNearestNodeOnTriangle(
            mesh, 0, 0.0, 0.0, 0.0, in viewProj, Width, Height, cursor);

        Assert.Equal(2, nearest);
    }

    [Fact]
    public void FindNearestNodeOnTriangle_InvalidTriangle_ReturnsNull()
    {
        var camera = CreateCamera();
        var viewProj = GetViewProj(camera);
        var mesh = new ViewportMesh
        {
            Positions = [0.0, 0.0, 0.0],
            TriangleIndices = [0, 0, 0],
        };

        var result = ViewportPicking.FindNearestNodeOnTriangle(
            mesh, 5, 0.0, 0.0, 0.0, in viewProj, Width, Height, Vector2.Zero);

        Assert.Null(result);
    }

    [Fact]
    public void FindNodesInRectangle_ReturnsOnlyNodesInsideRect()
    {
        var camera = CreateCamera();
        var viewProj = GetViewProj(camera);

        var mesh = new ViewportMesh
        {
            Positions =
            [
                0.0, -2.0, -2.0, // node 0
                0.0, 2.0, -2.0,  // node 1
                0.0, 0.0, 2.0,   // node 2
            ],
            TriangleIndices = [0, 1, 2],
        };

        // Small rectangle around node 2's projected position
        var node2Pixel = ViewportPicking.ProjectToPixel(
            new Vector3(0.0f, 0.0f, 2.0f), in viewProj, Width, Height)!.Value;
        var rectMin = node2Pixel - new Vector2(10.0f, 10.0f);
        var rectMax = node2Pixel + new Vector2(10.0f, 10.0f);

        var nodes = ViewportPicking.FindNodesInRectangle(
            mesh, [0], 0.0, 0.0, 0.0, in viewProj, Width, Height, rectMin, rectMax);

        Assert.Single(nodes);
        Assert.Contains(2, nodes);
    }

    [Fact]
    public void FindNodesInRectangle_FullViewportRect_ReturnsAllTriangleNodes()
    {
        var camera = CreateCamera();
        var viewProj = GetViewProj(camera);

        var mesh = new ViewportMesh
        {
            Positions =
            [
                0.0, -2.0, -2.0,
                0.0, 2.0, -2.0,
                0.0, 0.0, 2.0,
            ],
            TriangleIndices = [0, 1, 2],
        };

        var nodes = ViewportPicking.FindNodesInRectangle(
            mesh, [0], 0.0, 0.0, 0.0, in viewProj, Width, Height,
            Vector2.Zero, new Vector2((float)Width, (float)Height));

        Assert.Equal(3, nodes.Count);
    }

    [Fact]
    public void FindNearestNodeOnTriangle_DeformationScaleFollowsDisplacedPosition()
    {
        // Camera looks from +X toward the YZ plane. Node 2 moves by (0,0,+2)*2 to z=6,
        // while node 0 moves into node 2's original position (near z=2)
        var camera = CreateCamera();
        var viewProj = GetViewProj(camera);

        var mesh = new ViewportMesh
        {
            Positions =
            [
                0.0, -2.0, -2.0, // node 0
                0.0, 2.0, -2.0,  // node 1
                0.0, 0.0, 2.0,   // node 2
            ],
            TriangleIndices = [0, 1, 2],
            Displacements =
            [
                0.0, 1.0, 2.0,   // node 0 -> (0, 0, 2) at scale 2
                0.0, 0.0, 0.0,
                0.0, 0.0, 2.0,   // node 2 -> (0, 0, 6) at scale 2
            ],
        };

        // Click node 2's original position (0,0,2)
        var cursor = ViewportPicking.ProjectToPixel(
            new Vector3(0.0f, 0.0f, 2.0f), in viewProj, Width, Height)!.Value;

        // Undeformed -> hits node 2; deformation scale 2 -> hits node 0 which moved there
        Assert.Equal(2, ViewportPicking.FindNearestNodeOnTriangle(
            mesh, 0, 0.0, 0.0, 0.0, in viewProj, Width, Height, cursor));
        Assert.Equal(0, ViewportPicking.FindNearestNodeOnTriangle(
            mesh, 0, 0.0, 0.0, 0.0, in viewProj, Width, Height, cursor, deformationScale: 2.0));
    }

    [Fact]
    public void FindNodesInRectangle_RecenteringOriginIsApplied()
    {
        var camera = CreateCamera();
        var viewProj = GetViewProj(camera);

        // Large-coordinate mesh (origin 1e6) evaluates accurately in local space when origin is passed
        const double origin = 1.0e6;
        var mesh = new ViewportMesh
        {
            Positions =
            [
                origin, origin - 2.0, origin - 2.0,
                origin, origin + 2.0, origin - 2.0,
                origin, origin, origin + 2.0,
            ],
            TriangleIndices = [0, 1, 2],
        };

        var nodes = ViewportPicking.FindNodesInRectangle(
            mesh, [0], origin, origin, origin, in viewProj, Width, Height,
            Vector2.Zero, new Vector2((float)Width, (float)Height));

        Assert.Equal(3, nodes.Count);
    }
}
