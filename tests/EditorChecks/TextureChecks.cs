using System.Drawing;
using BundleFormat;
using BurnoutImage;

internal static partial class Program
{
    private static void CheckPS3Textures()
    {
        // renderengine::Texture: format, mips, dimension, cube, remap, then big-endian width/height at 0x8
        static byte[] Header(byte format, int width, int height)
        {
            byte[] header = new byte[0x30];
            header[0] = format;
            header[1] = 1;
            header[2] = 2;
            header[8] = (byte)(width >> 8);
            header[9] = (byte)width;
            header[10] = (byte)(height >> 8);
            header[11] = (byte)height;
            return header;
        }

        static Color Expected(int x, int y) => Color.FromArgb(200, x * 60, y * 120, 7);

        // 4×2 A8R8G8B8: swizzled (0x85) interleaves x then y for one bit, then x's high bit. linear (0xA5) is row major
        byte[] swizzled = new byte[4 * 2 * 4], linear = new byte[swizzled.Length];

        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                Color c = Expected(x, y);
                byte[] argb = [c.A, c.R, c.G, c.B];
                argb.CopyTo(swizzled, ((x >> 1) << 2 | (y & 1) << 1 | (x & 1)) * 4);
                argb.CopyTo(linear, (y * 4 + x) * 4);
            }
        }

        foreach ((byte format, byte[] body, string name) in new[] { ((byte)0x85, swizzled, "swizzled"), ((byte)0xA5, linear, "linear") })
        {
            using Bitmap image = (Bitmap)GameImage.GetImage(Header(format, 4, 2), body, BundlePlatform.PS3);
            bool match = true;

            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 4; x++)
                    match &= image.GetPixel(x, y) == Expected(x, y);

            Check(match, $"PS3 {name} A8R8G8B8 previews in place with its colours");
        }

        // DXT1 block in PC byte order: colour 0 is pure red (RGB565 0xF800), every index 0
        using Bitmap dxt = (Bitmap)GameImage.GetImage(Header(0x86, 4, 4), [0x00, 0xF8, 0, 0, 0, 0, 0, 0], BundlePlatform.PS3);
        Check(dxt.GetPixel(2, 2).ToArgb() == Color.Red.ToArgb(), "PS3 DXT1 previews in RGBA order");
    }
}
