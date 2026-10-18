using System;

namespace BurnoutImage;

internal static class Util
{
    public static int FloatToInt(float a, int limit)
    {
        // use ANSI round-to-zero behaviour to get round-to-nearest
        int i = (int)(a + 0.5f);

        // clamp to the limit
        if (i < 0)
            i = 0;
        else if (i > limit)
            i = limit;

        // done
        return i;
    }

    public static int FloatTo565(Vec3 colour)
    {
        // get the components in the correct range
        int r = FloatToInt(31.0f * colour.X, 31);
        int g = FloatToInt(63.0f * colour.X, 63);
        int b = FloatToInt(31.0f * colour.X, 31);

        // pack into a single value
        return (r << 11) | (g << 5) | b;
    }

    // reverse byte order (16-bit)
    public static ushort ReverseBytes(ushort value)
    {
        return (ushort)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
    }

    // reverse byte order (32-bit)
    public static uint ReverseBytes(uint value)
    {
        return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
               (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
    }

    // reverse byte order (64-bit)
    public static ulong ReverseBytes(ulong value)
    {
        return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
               (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
               (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
               (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
    }

    // reverse byte order (16-bit)
    public static short ReverseBytes(short value)
    {
        ushort v = (ushort)value;
        return (short)((v & 0xFFU) << 8 | (v & 0xFF00U) >> 8);
    }

    // reverse byte order (32-bit)
    public static int ReverseBytes(int value)
    {
        uint v = (uint)value;
        return (int)((v & 0x000000FFU) << 24 | (v & 0x0000FF00U) << 8 |
               (v & 0x00FF0000U) >> 8 | (v & 0xFF000000U) >> 24);
    }

    // reverse byte order (64-bit)
    public static long ReverseBytes(long value)
    {
        ulong v = (ulong)value;
        return (long)((v & 0x00000000000000FFUL) << 56 | (v & 0x000000000000FF00UL) << 40 |
               (v & 0x0000000000FF0000UL) << 24 | (v & 0x00000000FF000000UL) << 8 |
               (v & 0x000000FF00000000UL) >> 8 | (v & 0x0000FF0000000000UL) >> 24 |
               (v & 0x00FF000000000000UL) >> 40 | (v & 0xFF00000000000000UL) >> 56);
    }
}

internal struct Vec3
{
    public float X, Y, Z;

    public Vec3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vec3(float xyz) : this(xyz, xyz, xyz)
    {

    }
}

internal struct Vec4
{
    public float X, Y, Z, W;

    public Vec4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
}
