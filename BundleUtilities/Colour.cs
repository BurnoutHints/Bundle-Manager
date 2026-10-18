namespace BundleUtilities;

public class U8Colour
{
    public byte Red;
    public byte Green;
    public byte Blue;
    public byte Alpha;

    private U8Colour(byte red, byte green, byte blue, byte alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    public static U8Colour FromRGBA(byte red, byte green, byte blue, byte alpha)
    {
        return new U8Colour(red, green, blue, alpha);
    }

    public static U8Colour FromARGB32(uint value)
    {
        return new U8Colour((byte)((value & 0xFF0000) >> 16), (byte)((value & 0xFF00) >> 8), (byte)(value & 0xFF), (byte)((value & 0xFF000000) >> 24));
    }

    public override string ToString() => $"RGBA({Red}, {Green}, {Blue}, {Alpha})";
}

public class FColour
{
    public float Red;
    public float Green;
    public float Blue;
    public float Alpha;

    private FColour(float red, float green, float blue, float alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    public static FColour FromRGBA(float red, float green, float blue, float alpha)
    {
        return new FColour(red, green, blue, alpha);
    }

    public override string ToString() => $"RGBA({Red}, {Green}, {Blue}, {Alpha})";
}
