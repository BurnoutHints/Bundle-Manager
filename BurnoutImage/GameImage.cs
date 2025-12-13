using System;
using System.Text;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BCnEncoder.Shared;
using BundleUtilities;

namespace BurnoutImage
{
    public struct ImageInfo
    {
        public readonly byte[] Header;
        public readonly byte[] Data;

        public ImageInfo(byte[] header, byte[] data)
        {
            this.Header = header;
            this.Data = data;
        }
    }

    public class ImageHeader
    {
        public readonly PlatformType Platform;
        public readonly CompressionType CompressionType;
        public readonly int Width, Height;

        public ImageHeader(CompressionType compression, int width, int height, PlatformType platform = PlatformType.Remastered)
        {
            CompressionType = compression;
            Width = width;
            Height = height;
            Platform = platform;
        }
    }

    public static class GameImage
    {
        public static ImageInfo SetImage(string path, CompressionType compression, PlatformType platform = PlatformType.Remastered)
        {
            byte[] data;
            int width = 0, height = 0, numMips = 1;

            // Process body block

            // Use direct DDS contents if provided (no extra processing)
            if (string.Equals(Path.GetExtension(path), ".dds", StringComparison.OrdinalIgnoreCase))
            {
                using FileStream fs = File.OpenRead(path);
                using BinaryReader br = new(fs);

                uint magic = br.ReadUInt32();
                if (magic != 0x20534444)
                    throw new InvalidDataException("The provided DDS file is unsupported or corrupted.");

                fs.Seek(0xC, SeekOrigin.Begin);
                height = br.ReadInt32();
                width = br.ReadInt32();

                fs.Seek(0x1C, SeekOrigin.Begin);
                numMips = br.ReadInt32();
                if (numMips <= 0)
                    numMips = 1;

                fs.Seek(0x4C, SeekOrigin.Begin);
                uint pfSize = br.ReadUInt32();
                uint pfFlags = br.ReadUInt32();
                uint pfFourCC = br.ReadUInt32();
                uint pfRGBBits = br.ReadUInt32();
                uint pfRMask = br.ReadUInt32();
                uint pfGMask = br.ReadUInt32();
                uint pfBMask = br.ReadUInt32();
                uint pfAMask = br.ReadUInt32();

                compression = ImageUtil.DetectDdsFormat(
                    pfFlags,
                    pfFourCC,
                    pfRGBBits,
                    pfRMask,
                    pfGMask,
                    pfBMask,
                    pfAMask
                );

                if (compression == CompressionType.UNKNOWN)
                    compression = CompressionType.RGBA;

                const int ddsHeaderSize = 0x80;
                fs.Seek(ddsHeaderSize, SeekOrigin.Begin);
                data = br.ReadBytes((int)(fs.Length - ddsHeaderSize));

                br.Close();
            }
            // Process non-DDS textures
            else
            {
                Bitmap image = new(Image.FromFile(path));

                width = image.Width;
                height = image.Height;

                if (compression == CompressionType.BGRA)
                {
                    MemoryStream mspixels = new();

                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            Color pixel = image.GetPixel(j, i);
                            mspixels.WriteByte(pixel.B);
                            mspixels.WriteByte(pixel.G);
                            mspixels.WriteByte(pixel.R);
                            mspixels.WriteByte(pixel.A);
                        }
                    }

                    data = mspixels.ToArray();

                    mspixels.Close();
                }
                else
                {
                    CompressionFormat dxt = compression switch
                    {
                        CompressionType.DXT1 => CompressionFormat.Bc1,
                        CompressionType.DXT5 => CompressionFormat.Bc3,
                        _ => CompressionFormat.Unknown
                    };

                    data = ImageUtil.CompressImage(path, dxt);
                }
            }


            // Process header block
            MemoryStream msx = new();
            BinaryWriter2 bw = new(msx);

            if (platform == PlatformType.Remastered)
            {
                // Remastered Texture header: https://burnout.wiki/wiki/Texture/Remastered
                bw.Write(0); // Texture interface pointer
                bw.Write(0); // Usage
                bw.Write(7); // Dimension
                bw.Write(0); // Pixel data pointer
                bw.Write(0); // Shader resource view interface pointer 1
                bw.Write(0); // Shader resource view interface pointer 2
                bw.Write(0); // ?
                int format = compression switch
                {
                    CompressionType.ARGB => 0x1C, // v One of these may be incorrect
                    CompressionType.RGBA => 0x1C, // ^
                    CompressionType.BGRA => 0x57,
                    CompressionType.DXT1 => 0x47,
                    CompressionType.DXT5 => 0x4D,
                };
                bw.Write(format);
                bw.Write(0); // Flags
                bw.Write((ushort)width); // Width
                bw.Write((ushort)height); // Height
                bw.Write((ushort)1); // Depth
                bw.Write((ushort)1); // Array size
                bw.Write((byte)0); // Most detailed mip
                bw.Write((byte)numMips); // Mip levels
                bw.Write((ushort)0); // ?
                bw.Write(0); // ? pointer
                bw.Write(0); // Array index (unused)
                bw.Write(0); // Contents size (unused)
                bw.Write(0); // Texture data (unused)
            }
            else if (platform == PlatformType.PC)
            {
                // Original game header: https://burnout.wiki/wiki/Texture/PC
                bw.Write(0); // Data pointer
                bw.Write(0); // Texture interface pointer
                bw.Write(0); // Padding
                bw.Write((ushort)1); // Pool
                bw.Write((byte)0); // ?
                bw.Write((byte)0); // ?
                if (compression == CompressionType.DXT1 || compression == CompressionType.DXT5) // Format
                    bw.Write(Encoding.ASCII.GetBytes(compression.ToString()));
                else
                    bw.Write(0x15); // A8R8G8B8
                bw.Write((ushort)width); // Width
                bw.Write((short)height); // Height
                bw.Write((byte)1); // Depth
                bw.Write(numMips); // MipLevels
                bw.Write((byte)0); // Texture type
                bw.Write((byte)0); // Flags
            }

            bw.Flush();
            bw.Close();

            return new ImageInfo(msx.ToArray(), data);
        }

        public static ImageHeader GetImageHeader(byte[] data)
        {
            try
            {
                MemoryStream ms = new(data);
                BinaryReader2 br = new(ms);

                PlatformType platform = DetectPlatform(data);
                if (platform == PlatformType.X360)
                {
                    throw new Exception("Xbox 360 textures are not supported yet.");
                }
                else if (platform == PlatformType.Remastered)
                {
                    br.BaseStream.Seek(0x00, SeekOrigin.Begin);

                    // 32/64 bit offsets
                    var (oFormat, oSize) = data.Length switch
                    {
                        0x40 => (0x1C, 0x24),
                        0x60 => (0x24, 0x34),
                        _ => throw new InvalidDataException()
                    };

                    br.BaseStream.Seek(oFormat, SeekOrigin.Begin);

                    // DXGI_Format
                    CompressionType type = br.ReadUInt32() switch
                    {
                        0x00000057  => CompressionType.BGRA,
                        0x0000001C  => CompressionType.RGBA,
                        0x000000FF  => CompressionType.ARGB,
                        0x00000047  => CompressionType.DXT1,
                        0x0000004D  => CompressionType.DXT5,
                        _           => CompressionType.UNKNOWN
                    };

                    br.BaseStream.Seek(oSize, SeekOrigin.Begin);

                    int width = br.ReadInt16();
                    int height = br.ReadInt16();

                    br.Close();

                    return new ImageHeader(type, width, height, PlatformType.Remastered);
                }
                else if (platform == PlatformType.PC)
                {
                    br.BaseStream.Seek(0x10, SeekOrigin.Begin);

                    // D3DFORMAT
                    CompressionType type = br.ReadInt32() switch
                    {
                        0x00000015  => CompressionType.BGRA,
                        0x000000FF  => CompressionType.ARGB,
                        0x31545844  => CompressionType.DXT1, // "DXT1"
                        0x35545844  => CompressionType.DXT5, // "DXT5"
                        _           => CompressionType.UNKNOWN
                    };

                    int width = br.ReadInt16();
                    int height = br.ReadInt16();

                    br.Close();

                    return new ImageHeader(type, width, height, PlatformType.PC);
                }
                else if (platform == PlatformType.PS3) // PS3
                {
                    br.BaseStream.Seek(0x00, SeekOrigin.Begin);

                    CompressionType type = br.ReadByte() switch
                    {
                        0x85 => CompressionType.ARGB,
                        0x86 => CompressionType.DXT1,
                        0x88 => CompressionType.DXT5,
                        _ => CompressionType.UNKNOWN
                    };

                    br.BaseStream.Seek(0x08, SeekOrigin.Begin);

                    ushort width = Util.ReverseBytes(br.ReadUInt16());
                    ushort height = Util.ReverseBytes(br.ReadUInt16());

                    br.Close();

                    return new ImageHeader(type, width, height, PlatformType.PS3);
                }
                else
                {
                    throw new InvalidDataException($"Unknown image header platform.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }

            return null;
        }

        public static Image GetImage(byte[] data, byte[] extraData)
        {
            if (extraData == null) // ¯\_(ツ)_/¯
                return null;

            try
            {
                ImageHeader header = GetImageHeader(data);

                if (header.Platform == PlatformType.X360)
                {
                    throw new Exception("Xbox 360 textures are not supported yet.");
                }
                else
                {
                    byte[] pixels = header.CompressionType switch
                    {
                        CompressionType.DXT1 => ImageUtil.DecompressImage(extraData, header.Width, header.Height, CompressionFormat.Bc1),
                        CompressionType.DXT5 => ImageUtil.DecompressImage(extraData, header.Width, header.Height, CompressionFormat.Bc3),
                        _ => extraData
                    };

                    DirectBitmap bitmap = new(header.Width, header.Height);

                    int rO, gO, bO, aO;

                    if (header.CompressionType == CompressionType.DXT1
                     || header.CompressionType == CompressionType.DXT5)
                    {
                        if (header.Platform == PlatformType.PS3)
                        {
                            rO = 1; gO = 2; bO = 3; aO = 0; // ARGB
                        }
                        else
                        {
                            rO = 0; gO = 1; bO = 2; aO = 3; // RGBA
                        }
                    }
                    else
                    {
                        switch (header.CompressionType)
                        {
                            case CompressionType.BGRA:
                                rO = 2; gO = 1; bO = 0; aO = 3;
                                break;
                            case CompressionType.ARGB:
                                rO = 1; gO = 2; bO = 3; aO = 0;
                                break;
                            default:
                                rO = 0; gO = 1; bO = 2; aO = 3;
                                break;
                        }
                    }

                    int srcIndex = 0;
                    int dstIndex = 0;
                    int pixelCount = header.Width * header.Height;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        byte r = pixels[srcIndex + rO];
                        byte g = pixels[srcIndex + gO];
                        byte b = pixels[srcIndex + bO];
                        byte a = pixels[srcIndex + aO];

                        int argb = (a << 24) | (r << 16) | (g << 8) | b;

                        bitmap.Bits[dstIndex] = argb;

                        srcIndex += 4;
                        dstIndex++;
                    }

                    return bitmap.Bitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
                return null;
            }
        }

        public static PlatformType DetectPlatform(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            int len = data.Length;

            using MemoryStream ms = new(data);
            using BinaryReader2 br = new(ms);

            if (len == 0x40 || len == 0x60)
            {
                if (len == 0x40)
                {
                    ms.Seek(0x0, SeekOrigin.Begin);

                    if (br.ReadUInt32() != 0)
                        return PlatformType.X360;
                }

                return PlatformType.Remastered;
            }

            if (len == 0x20)
            {
                ms.Seek(0x4, SeekOrigin.Begin);

                if (br.ReadUInt32() == 0) // Texture interface ptr
                    return PlatformType.PC;
            }

            if (len == 0x30)
                return PlatformType.PS3;

            return PlatformType.Invalid;
        }
    }

    public enum CompressionType
    {
        UNKNOWN = -1,

        RGBA,
        ARGB,
        BGRA,
        DXT1,
        DXT5
    }

    public enum PlatformType
    {
        Invalid = -1,

        Remastered,
        PC,
        PS3,
        X360
    }
}
