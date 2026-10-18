using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;

namespace BurnoutImage;

public static class CompressionTools
{
    public static byte[] CompressTexture(string source, CompressionFormat compression)
    {
        BcEncoder encoder = new();
        encoder.OutputOptions.Format = compression;
        encoder.OutputOptions.GenerateMipMaps = false;
        encoder.OutputOptions.Quality = CompressionQuality.BestQuality;

        using Bitmap image = new(source);
        Rectangle area = new(0, 0, image.Width, image.Height);
        BitmapData bits = image.LockBits(area, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb); // BGRA in memory
        try
        {
            byte[] pixels = new byte[image.Width * image.Height * 4];
            for (int y = 0; y < image.Height; y++)
                Marshal.Copy(bits.Scan0 + y * bits.Stride, pixels, y * image.Width * 4, image.Width * 4);

            return encoder.EncodeToRawBytes(pixels, image.Width, image.Height, BCnEncoder.Encoder.PixelFormat.Bgra32)[0];
        }
        finally
        {
            image.UnlockBits(bits);
        }
    }

    public static byte[] DecompressTexture(byte[] source, int width, int height, CompressionFormat compression)
    {
        ColorRgba32[] pixels = new BcDecoder().DecodeRaw(source, width, height, compression);
        return MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray();
    }
}
