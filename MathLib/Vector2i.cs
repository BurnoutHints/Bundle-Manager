using System;
using OpenTK.Mathematics;

namespace MathLib;

public struct Vector2I
{
    public int X, Y;

    public Vector2I(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Vector2I(Vector2 vec2)
    {
        X = (int)Math.Round(vec2.X);
        Y = (int)Math.Round(vec2.Y);
    }

    public static Vector2I operator +(Vector2I v1, Vector2I v2)
    {
        return new Vector2I(v1.X + v2.X, v1.Y + v2.Y);
    }

    public static Vector2I operator +(Vector2I v, int i)
    {
        return new Vector2I(v.X + i, v.Y + i);
    }

    public static Vector2I operator +(Vector2I v, float f)
    {
        return new Vector2I((int)(v.X + f), (int)(v.Y + f));
    }

    public static Vector2I operator -(Vector2I v1, Vector2I v2)
    {
        return new Vector2I(v1.X - v2.X, v1.Y - v2.Y);
    }
    
    public static Vector2I operator -(Vector2I v, int i)
    {
        return new Vector2I(v.X - i, v.Y - i);
    }

    public static Vector2I operator -(Vector2I v, float f)
    {
        return new Vector2I((int)(v.X - f), (int)(v.Y - f));
    }

    public static Vector2I operator *(Vector2I v1, Vector2I v2)
    {
        return new Vector2I(v1.X * v2.X, v1.Y * v2.Y);
    }

    public static Vector2I operator *(Vector2I v, int i)
    {
        return new Vector2I(v.X * i, v.Y * i);
    }

    public static Vector2I operator *(Vector2I v, float f)
    {
        return new Vector2I((int)(v.X * f), (int)(v.Y * f));
    }

    public static Vector2I operator /(Vector2I v1, Vector2I v2)
    {
        return new Vector2I(v1.X / v2.X, v1.Y / v2.Y);
    }

    public static Vector2I operator /(Vector2I v, int i)
    {
        return new Vector2I(v.X / i, v.Y / i);
    }

    public static Vector2I operator /(Vector2I v, float f)
    {
        return new Vector2I((int)(v.X / f), (int)(v.Y / f));
    }

    public override string ToString()
    {
        return "(" + X + ", " + Y + ")";
    }
}
