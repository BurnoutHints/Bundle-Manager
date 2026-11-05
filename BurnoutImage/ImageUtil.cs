using BCnEncoder.Shared;

namespace BurnoutImage;

internal static class ImageUtil
{
    public static byte[] DecompressImage(byte[] data, int width, int height, CompressionFormat compression)
    {
        return CompressionTools.DecompressTexture(data, width, height, compression);
    }

    public static byte[] CompressImage(string path, CompressionFormat compression)
    {
        return CompressionTools.CompressTexture(path, compression);
    }

    public static CompressionType DetectDDSFormat(
        uint pfFlags,
        uint pfFourCC,
        uint pfRGBBitCount,
        uint pfRBitMask,
        uint pfGBitMask,
        uint pfBBitMask,
        uint pfABitMask)
    {
        const uint ddpfalphapixels = 0x00000001;
        const uint ddpffourcc = 0x00000004;
        const uint ddpfrgb = 0x00000040;

        if ((pfFlags & ddpffourcc) != 0)
        {
            switch (pfFourCC)
            {
                case 0x31545844u:
                    return CompressionType.DXT1;
                case 0x35545844u:
                    return CompressionType.DXT5;
                case 0x00000015u:
                    return CompressionType.BGRA;
                case 0x0000001Cu:
                    return CompressionType.RGBA;
                case 0x000000FFu:
                    return CompressionType.ARGB;
            }

            return CompressionType.UNKNOWN;
        }

        if ((pfFlags & ddpfrgb) != 0 && pfRGBBitCount == 32)
        {
            if (pfRBitMask == 0x00FF0000u &&
                pfGBitMask == 0x0000FF00u &&
                pfBBitMask == 0x000000FFu)
            {
                if (pfABitMask == 0xFF000000u)
                    return CompressionType.BGRA;

                if ((pfFlags & ddpfalphapixels) == 0 && pfABitMask == 0)
                    return CompressionType.BGRA;
            }

            if (pfRBitMask == 0x000000FFu &&
                pfGBitMask == 0x0000FF00u &&
                pfBBitMask == 0x00FF0000u)
            {
                if (pfABitMask == 0xFF000000u)
                    return CompressionType.RGBA;

                if ((pfFlags & ddpfalphapixels) == 0 && pfABitMask == 0)
                    return CompressionType.RGBA;
            }
        }

        return CompressionType.UNKNOWN;
    }
}
