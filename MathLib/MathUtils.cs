using OpenTK.Mathematics;
using System;

namespace MathLib;

public static class MathUtils
{
    public static Vector3 MinBounds(params Vector3[] points)
    {
        Vector3 result = new(points[0].X, points[0].Y, points[0].Z);
        for (int i = 1; i < points.Length; i++)
        {
            result.X = Math.Min(points[i].X, result.X);
            result.Y = Math.Min(points[i].Y, result.Y);
            result.Z = Math.Min(points[i].Z, result.Z);
        }
        return result;
    }

    public static Vector3 MaxBounds(params Vector3[] points)
    {
        Vector3 result = new(points[0].X, points[0].Y, points[0].Z);
        for (int i = 1; i < points.Length; i++)
        {
            result.X = Math.Max(points[i].X, result.X);
            result.Y = Math.Max(points[i].Y, result.Y);
            result.Z = Math.Max(points[i].Z, result.Z);
        }
        return result;
    }
}
