using System.Numerics;
using WorkbenchUI.Wpf.Viewport3D;

namespace WorkbenchUI.Wpf.Viewport3D.Tests;

public class ViewportCameraTests
{
    private const float Tolerance = 1e-4f;

    private static ViewportCamera CreateCamera() => new()
    {
        Target = Vector3.Zero,
        Yaw = 0.0,
        Pitch = 0.0,
        Distance = 10.0,
        SceneRadius = 5.0,
    };

    /// <summary>Helper to project a world-space point into clip space (NDC).</summary>
    private static Vector3 ProjectToNdc(ViewportCamera camera, Vector3 worldPoint, double aspect)
    {
        var viewProj = camera.GetViewMatrix() * camera.GetProjectionMatrix(aspect);
        var clip = Vector4.Transform(new Vector4(worldPoint, 1.0f), viewProj);
        return new Vector3(clip.X / clip.W, clip.Y / clip.W, clip.Z / clip.W);
    }

    [Fact]
    public void GetEyePosition_ZUp_YawZeroPitchZero_LiesOnXAxis()
    {
        var camera = CreateCamera();
        var eye = camera.GetEyePosition();

        Assert.Equal(10.0f, eye.X, Tolerance);
        Assert.Equal(0.0f, eye.Y, Tolerance);
        Assert.Equal(0.0f, eye.Z, Tolerance);
    }

    [Fact]
    public void GetEyePosition_ZUp_Pitch90_IsClampedBelowVertical()
    {
        var camera = CreateCamera();
        camera.Pitch = Math.PI / 2.0; // Straight-up pitch is clamped

        var eye = camera.GetEyePosition();
        Assert.True(eye.Z < 10.0f, "Should not reach straight up (gimbal singularity)");
        Assert.True(eye.Z > 9.9f, "Should reach up to 89.5 deg");
    }

    [Fact]
    public void GetEyeDirection_YUp_YawZeroPitchZero_LiesOnZAxis()
    {
        var camera = CreateCamera();
        camera.UpAxis = ViewportUpAxis.YUp;

        var dir = camera.GetEyeDirection();
        Assert.Equal(0.0f, dir.X, Tolerance);
        Assert.Equal(0.0f, dir.Y, Tolerance);
        Assert.Equal(1.0f, dir.Z, Tolerance);
    }

    [Fact]
    public void GetViewMatrix_TransformsTargetToViewDepth()
    {
        var camera = CreateCamera();
        camera.Target = new Vector3(3.0f, -2.0f, 1.5f);
        camera.Yaw = 0.7;
        camera.Pitch = 0.3;

        var view = camera.GetViewMatrix();
        var transformed = Vector3.Transform(camera.Target, view);

        // Look-at target maps to (0, 0, -distance) in right-handed view space
        Assert.Equal(0.0f, transformed.X, Tolerance);
        Assert.Equal(0.0f, transformed.Y, Tolerance);
        Assert.Equal(-10.0f, transformed.Z, 1e-3f);
    }

    [Fact]
    public void GetProjectionMatrix_PerspectiveAndOrthographic_MatchApparentSizeAtTarget()
    {
        var camera = CreateCamera();
        var halfHeight = (float)(camera.GetViewHeightAtTarget() / 2.0);
        var (_, up) = camera.GetViewBasis();
        var topPoint = camera.Target + up * halfHeight;

        camera.Projection = ViewportProjection.Perspective;
        var ndcPerspective = ProjectToNdc(camera, topPoint, aspect: 1.5);

        camera.Projection = ViewportProjection.Orthographic;
        var ndcOrtho = ProjectToNdc(camera, topPoint, aspect: 1.5);

        // A point at the top edge of the frustum at target depth maps to NDC y=1 in both projections
        Assert.Equal(1.0f, ndcPerspective.Y, 1e-3f);
        Assert.Equal(1.0f, ndcOrtho.Y, 1e-3f);
    }

    [Fact]
    public void Orbit_AccumulatesYawAndClampsPitch()
    {
        var camera = CreateCamera();
        camera.Orbit(0.5, 0.0);
        Assert.Equal(0.5, camera.Yaw, 1e-9);

        camera.Orbit(0.0, 10.0); // Excessive pitch is clamped
        Assert.True(camera.Pitch < Math.PI / 2.0);
    }

    [Fact]
    public void Orbit_YawWrapsAroundFullCircle()
    {
        var camera = CreateCamera();
        camera.Orbit(2.0 * Math.PI + 0.25, 0.0);
        Assert.Equal(0.25, camera.Yaw, 1e-9);
    }

    [Fact]
    public void Pan_MovesTargetPerpendicularToViewDirection()
    {
        var camera = CreateCamera();
        var before = camera.Target;
        camera.Pan(100.0, 0.0, viewportHeightPixels: 1000.0);

        var moved = camera.Target - before;
        var viewDir = -camera.GetEyeDirection();

        Assert.True(moved.Length() > 0.0f, "Panning should move the camera target");
        Assert.Equal(0.0f, Vector3.Dot(moved, viewDir), Tolerance);
    }

    [Fact]
    public void Pan_DistancePerPixelMatchesWorldPerPixel()
    {
        var camera = CreateCamera();
        var wpp = camera.GetWorldPerPixel(1000.0);
        var before = camera.Target;

        camera.Pan(100.0, 0.0, 1000.0);
        var movedDistance = (camera.Target - before).Length();

        Assert.Equal(100.0 * wpp, movedDistance, 1e-4);
    }

    [Fact]
    public void Zoom_ScalesDistance()
    {
        var camera = CreateCamera();
        camera.Zoom(0.5);
        Assert.Equal(5.0, camera.Distance, 1e-9);
    }

    [Fact]
    public void ZoomAt_Center_KeepsTargetFixed()
    {
        var camera = CreateCamera();
        var before = camera.Target;

        camera.ZoomAt(0.8, 400.0, 300.0, 800.0, 600.0);

        Assert.Equal(before.X, camera.Target.X, Tolerance);
        Assert.Equal(before.Y, camera.Target.Y, Tolerance);
        Assert.Equal(before.Z, camera.Target.Z, Tolerance);
        Assert.Equal(8.0, camera.Distance, 1e-9);
    }

    [Theory]
    [InlineData(ViewportProjection.Perspective)]
    [InlineData(ViewportProjection.Orthographic)]
    public void ZoomAt_Cursor_KeepsPointUnderCursorInvariant(ViewportProjection projection)
    {
        const double width = 800.0;
        const double height = 600.0;
        const double cursorX = 600.0;
        const double cursorY = 150.0;

        var camera = CreateCamera();
        camera.Projection = projection;
        camera.Yaw = 0.9;
        camera.Pitch = 0.4;

        // Compute the world-space point under the cursor at target depth
        var wpp = camera.GetWorldPerPixel(height);
        var (right, up) = camera.GetViewBasis();
        var pointUnderCursor = camera.Target
            + right * (float)((cursorX - width / 2.0) * wpp)
            - up * (float)((cursorY - height / 2.0) * wpp);

        var ndcBefore = ProjectToNdc(camera, pointUnderCursor, width / height);
        camera.ZoomAt(0.7, cursorX, cursorY, width, height);
        var ndcAfter = ProjectToNdc(camera, pointUnderCursor, width / height);

        // After zooming, the point under the cursor remains stationary on screen
        Assert.Equal(ndcBefore.X, ndcAfter.X, 1e-3f);
        Assert.Equal(ndcBefore.Y, ndcAfter.Y, 1e-3f);
    }

    [Fact]
    public void FitToBounds_CentersTargetAndContainsAllCorners()
    {
        var camera = CreateCamera();
        var bounds = new Bounds3D(-2.0, -1.0, 0.0, 4.0, 3.0, 5.0);

        camera.FitToBounds(bounds);

        Assert.Equal(bounds.CenterX, camera.Target.X, 1e-4);
        Assert.Equal(bounds.CenterY, camera.Target.Y, 1e-4);
        Assert.Equal(bounds.CenterZ, camera.Target.Z, 1e-4);
        Assert.Equal(bounds.Radius, camera.SceneRadius, 1e-6);

        // All 8 corners fit within NDC [-1, 1] (for landscape aspect >= 1)
        foreach (var x in new[] { bounds.MinX, bounds.MaxX })
        foreach (var y in new[] { bounds.MinY, bounds.MaxY })
        foreach (var z in new[] { bounds.MinZ, bounds.MaxZ })
        {
            var ndc = ProjectToNdc(camera, new Vector3((float)x, (float)y, (float)z), aspect: 1.0);
            Assert.InRange(ndc.X, -1.0f, 1.0f);
            Assert.InRange(ndc.Y, -1.0f, 1.0f);
            Assert.InRange(ndc.Z, 0.0f, 1.0f);
        }
    }

    [Fact]
    public void FitToBounds_EmptyBounds_DoesNothing()
    {
        var camera = CreateCamera();
        var distanceBefore = camera.Distance;

        camera.FitToBounds(Bounds3D.Empty);

        Assert.Equal(distanceBefore, camera.Distance);
    }

    [Fact]
    public void Changed_FiresOnCameraOperations()
    {
        var camera = CreateCamera();
        var count = 0;
        camera.Changed += (_, _) => count++;

        camera.Orbit(0.1, 0.1);
        camera.Pan(1.0, 1.0, 100.0);
        camera.Zoom(0.9);
        camera.Projection = ViewportProjection.Orthographic;

        Assert.Equal(4, count);
    }
}
