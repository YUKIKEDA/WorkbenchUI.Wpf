namespace WorkbenchUI.Wpf.Viewport3D;

/// <summary>
/// Pure helper functions for mesh deformation calculations.
/// </summary>
internal static class ViewportDeformation
{
    /// <summary>Default auto-scale target: scales maximum displacement to 5% of the model size.</summary>
    public const double DefaultTargetFraction = 0.05;

    /// <summary>
    /// Returns the maximum displacement vector magnitude in a 3N displacement array.
    /// Ignores null, empty, or trailing incomplete elements.
    /// </summary>
    public static double GetMaxDisplacementMagnitude(double[]? displacements)
    {
        if (displacements is null)
        {
            return 0.0;
        }

        var max = 0.0;
        for (var i = 0; i + 2 < displacements.Length; i += 3)
        {
            var x = displacements[i];
            var y = displacements[i + 1];
            var z = displacements[i + 2];
            if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z))
            {
                continue;
            }

            var magnitude = Math.Sqrt(x * x + y * y + z * z);
            if (magnitude > max)
            {
                max = magnitude;
            }
        }

        return max;
    }

    /// <summary>
    /// Computes a recommended deformation scale factor such that
    /// maxDisplacement * scale ~= modelSize * targetFraction (analogous to CAE auto-scale).
    /// Returns 1.0 (harmless identity) when displacement or model size is non-positive.
    /// </summary>
    public static double ComputeSuggestedScale(
        double maxDisplacement, double modelSize, double targetFraction = DefaultTargetFraction)
    {
        if (maxDisplacement <= 0.0 || modelSize <= 0.0 || targetFraction <= 0.0)
        {
            return 1.0;
        }

        return modelSize * targetFraction / maxDisplacement;
    }

    /// <summary>
    /// Returns the sinusoidal animation factor in [-1, 1] for modal oscillation at the given elapsed time:
    /// 0 at t=0, +1 at t=T/4, -1 at t=3T/4. Returns 1 (static) when period is non-positive or non-finite.
    /// </summary>
    public static double GetAnimationFactor(double elapsedSeconds, double periodSeconds)
    {
        if (periodSeconds <= 0.0 || !double.IsFinite(periodSeconds))
        {
            return 1.0;
        }

        return Math.Sin(2.0 * Math.PI * elapsedSeconds / periodSeconds);
    }

    /// <summary>
    /// Converts a displacement array into a zero-initialized float array of length 3 * vertexCount for GPU upload.
    /// Null or trailing missing entries remain zero (no displacement).
    /// </summary>
    public static float[] ToDisplacementArray(double[]? displacements, int vertexCount)
    {
        var result = new float[vertexCount * 3];
        if (displacements is null)
        {
            return result;
        }

        var count = Math.Min(displacements.Length, result.Length);
        for (var i = 0; i < count; i++)
        {
            result[i] = (float)displacements[i];
        }

        return result;
    }
}
