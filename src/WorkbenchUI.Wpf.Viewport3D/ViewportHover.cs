namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// CPU-side pure functions for hover pre-highlighting.
/// Reads from the ID buffer captured when the scene is idle (two uints per pixel:
/// R = part ID + 1, G = global triangle index).
/// </summary>
internal static class ViewportHover
{
    /// <summary>Number of uints per pixel (R32G32_UInt).</summary>
    public const int UintsPerPixel = 2;

    /// <summary>
    /// Reads the hit at the specified pixel from the ID buffer. Returns null for out-of-bounds or background (part ID 0).
    /// The returned MeshIndex is the index within the visible mesh list at the time of capture.
    /// </summary>
    public static (int MeshIndex, int TriangleIndex)? ReadId(
        uint[] buffer, int width, int height, int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
        {
            return null;
        }

        var offset = (y * width + x) * UintsPerPixel;
        if (offset + 1 >= buffer.Length)
        {
            return null;
        }

        var part = buffer[offset];
        if (part == 0)
        {
            return null;
        }

        return ((int)part - 1, (int)buffer[offset + 1]);
    }
}
