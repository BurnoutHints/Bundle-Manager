using System;
using OpenTK.Mathematics;

namespace MathLib;

public struct Vector3I
{
    public int X, Y, Z;

    public Vector3I(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3I(Vector3 vec3)
    {
        X = (int)Math.Round(vec3.X);
        Y = (int)Math.Round(vec3.Y);
        Z = (int)Math.Round(vec3.Z);
    }

    public static Vector3I operator +(Vector3I v1, Vector3I v2)
    {
        return new Vector3I(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
    }

    public static Vector3I operator +(Vector3I v, int i)
    {
        return new Vector3I(v.X + i, v.Y + i, v.Z + i);
    }

    public static Vector3I operator +(Vector3I v, float f)
    {
        return new Vector3I((int)(v.X + f), (int)(v.Y + f), (int)(v.Z + f));
    }

    public static Vector3I operator -(Vector3I v1, Vector3I v2)
    {
        return new Vector3I(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
    }
    
    public static Vector3I operator -(Vector3I v, int i)
    {
        return new Vector3I(v.X - i, v.Y - i, v.Z - i);
    }

    public static Vector3I operator -(Vector3I v, float f)
    {
        return new Vector3I((int)(v.X - f), (int)(v.Y - f), (int)(v.Z - f));
    }

    public static Vector3I operator *(Vector3I v1, Vector3I v2)
    {
        return new Vector3I(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
    }

    public static Vector3I operator *(Vector3I v, int i)
    {
        return new Vector3I(v.X * i, v.Y * i, v.Z * i);
    }

    public static Vector3I operator *(Vector3I v, float f)
    {
        return new Vector3I((int)(v.X * f), (int)(v.Y * f), (int)(v.Z * f));
    }

    public static Vector3I operator /(Vector3I v1, Vector3I v2)
    {
        return new Vector3I(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z);
    }

    public static Vector3I operator /(Vector3I v, int i)
    {
        return new Vector3I(v.X / i, v.Y / i, v.Z / i);
    }

    public static Vector3I operator /(Vector3I v, float f)
    {
        return new Vector3I((int)(v.X / f), (int)(v.Y / f), (int)(v.Z / f));
    }

    public override string ToString()
    {
        return "(" + X + ", " + Y + ", " + Z + ")";
    }
}
