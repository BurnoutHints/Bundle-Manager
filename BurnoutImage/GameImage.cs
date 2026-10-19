using System;
using System.Buffers.Binary;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BCnEncoder.Shared;
using BundleFormat;
using BundleUtilities;

namespace BurnoutImage;

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
    public const string FileFilter = "Image Files(*.dds,*.bmp;*.gif;*.jpg;*.png;*.tif)|*.dds;*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif;*.tiff";

    public static (int Width, int Height, bool HasAlpha) InspectImage(string path)
    {
        using Bitmap image = new(path);
        Rectangle area = new(0, 0, image.Width, image.Height);
        System.Drawing.Imaging.BitmapData bits = image.LockBits(area, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            byte[] row = new byte[image.Width * 4]; // BGRA
            for (int y = 0; y < image.Height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(bits.Scan0 + y * bits.Stride, row, 0, row.Length);
                for (int a = 3; a < row.Length; a += 4)
                    if (row[a] != 255)
                        return (image.Width, image.Height, true);
            }
            return (image.Width, image.Height, false);
        }
        finally
        {
            image.UnlockBits(bits);
        }
    }

    // Bytes of pixel data for a texture, including its smaller mips
    public static int TextureSize(CompressionType compression, int width, int height, int mips = 1)
    {
        int size = 0;
        for (int i = 0; i < mips; i++, width = Math.Max(1, width / 2), height = Math.Max(1, height / 2))
            size += compression is CompressionType.DXT1 or CompressionType.DXT5
                ? ((width + 3) / 4) * ((height + 3) / 4) * (compression == CompressionType.DXT1 ? 8 : 16)
                : width * height * 4;
        return size;
    }

    public static ImageInfo SetImage(string path, CompressionType compression, PlatformType platform = PlatformType.Remastered, byte[] template = null)
    {
        if (platform is not (PlatformType.PC or PlatformType.PCx64 or PlatformType.Remastered))
            throw new NotSupportedException($"Images can't be converted to {platform} textures yet.");

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

            if (data.Length < TextureSize(compression, width, height, numMips))
                throw new InvalidDataException($"The DDS file has less pixel data than a {width}×{height} {compression} texture with {numMips} mips needs.");
        }
        // Process non-DDS textures
        else
        {
            using Bitmap image = new(path);

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


        // Direct3D can't create block compressed textures of other sizes, and the game crashes on them
        if (compression is CompressionType.DXT1 or CompressionType.DXT5 && (width % 4 != 0 || height % 4 != 0))
            throw new InvalidDataException($"{compression} textures need a width and height that are multiples of 4, but this image is {width}×{height}. Resize it or import it uncompressed.");

        // Process header block
        if (platform == PlatformType.Remastered)
        {
            // Remastered Texture header: https://burnout.wiki/wiki/Texture/Burnout_Paradise/Remastered
            byte[] header = template?.Length is 0x40 or 0x60 ? (byte[])template.Clone() : new byte[0x40];
            Span<byte> h = header;
            int p = header.Length == 0x60 ? 4 : 0;    // 64-bit pointers move Dimension 4 bytes
            int f = header.Length == 0x60 ? 0x10 : 0; // and Format onwards 0x10 bytes
            BinaryPrimitives.WriteInt32LittleEndian(h[(0x08 + p)..], 7); // Dimension: 2D
            BinaryPrimitives.WriteInt32LittleEndian(h[(0x1C + f)..], compression switch
            {
                CompressionType.ARGB => 0x1C, // v One of these may be incorrect
                CompressionType.RGBA => 0x1C, // ^
                CompressionType.BGRA => 0x57,
                CompressionType.DXT1 => 0x47,
                CompressionType.DXT5 => 0x4D,
            });
            BinaryPrimitives.WriteUInt16LittleEndian(h[(0x24 + f)..], (ushort)width);
            BinaryPrimitives.WriteUInt16LittleEndian(h[(0x26 + f)..], (ushort)height);
            BinaryPrimitives.WriteUInt16LittleEndian(h[(0x28 + f)..], 1); // Depth
            BinaryPrimitives.WriteUInt16LittleEndian(h[(0x2A + f)..], 1); // Array size
            h[0x2C + f] = 0;                                              // Most detailed mip
            h[0x2D + f] = (byte)numMips;                                  // Mip levels
            return new ImageInfo(header, data);
        }

        MemoryStream msx = new();
        BinaryWriter2 bw = new(msx);

        if (platform == PlatformType.PC || platform == PlatformType.PCx64)
        {
            if (platform == PlatformType.PC)
            {
                // TUB PC
                bw.Write(0);                // Data pointer
                bw.Write(0);                // Texture interface pointer
                bw.Write(0);                // Padding
                bw.Write((ushort)1);        // Pool
                bw.Write((byte)0);          // ?
                bw.Write((byte)0);          // ?
            }
            else
            {
                // Decomp. See b5-decomp src/pc/gcm/renderengine/texture.h
                bw.Write(new byte[0x1C]);   // 3 pointers, 4 bools
            }
            bw.Write(compression switch     // D3DFORMAT
            {
                CompressionType.DXT1 => 0x31545844, // "DXT1"
                CompressionType.DXT5 => 0x35545844, // "DXT5"
                CompressionType.RGBA => 0x20,       // A8B8G8R8
                _ => 0x15                           // A8R8G8B8
            });
            bw.Write((ushort)width);        // Width
            bw.Write((short)height);        // Height
            bw.Write((byte)1);              // Depth
            bw.Write((byte)numMips);        // MipLevels
            bw.Write((byte)0);              // Texture type
            bw.Write((byte)0);              // Flags
            bw.Write(new byte[platform == PlatformType.PC ? 4 : 8]); // 0x10 alignment
        }

        bw.Flush();
        bw.Close();

        return new ImageInfo(msx.ToArray(), data);
    }

    public static ImageHeader GetImageHeader(byte[] data, BundlePlatform? bundle = null)
    {
        MemoryStream ms = new(data);
        BinaryReader2 br = new(ms);

        PlatformType platform = DetectPlatform(data, bundle);
        if (platform == PlatformType.X360)
        {
            X360Fetch f = new(data);
            CompressionType type = (f.Format, f.Swizzle) switch
            {
                (18, _) => CompressionType.DXT1,     // GPUTEXTUREFORMAT_DXT1
                (20, _) => CompressionType.DXT5,     // GPUTEXTUREFORMAT_DXT4_5
                (6, 0x688) => CompressionType.RGBA,  // GPUTEXTUREFORMAT_8_8_8_8, named by the order
                (6, 0x60A) => CompressionType.BGRA,  // its swizzle reads the bytes in
                _ => CompressionType.UNKNOWN
            };
            return new ImageHeader(type, f.Width, f.Height, PlatformType.X360);
        }
        else if (platform == PlatformType.Remastered)
        {
            br.BaseStream.Seek(0x00, SeekOrigin.Begin);

            // 32/64 bit offsets
            var (oFormat, oSize) = data.Length switch
            {
                0x40 => (0x1C, 0x24),
                0x60 => (0x2C, 0x34),
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
        else if (platform == PlatformType.PC || platform == PlatformType.PCx64)
        {
            br.BaseStream.Seek(platform == PlatformType.PC ? 0x10 : 0x1C, SeekOrigin.Begin);

            // D3DFORMAT
            CompressionType type = br.ReadInt32() switch
            {
                0x00000015  => CompressionType.BGRA,
                0x00000020  => CompressionType.RGBA,
                0x000000FF  => CompressionType.ARGB,
                0x31545844  => CompressionType.DXT1, // "DXT1"
                0x35545844  => CompressionType.DXT5, // "DXT5"
                _           => CompressionType.UNKNOWN
            };

            int width = br.ReadInt16();
            int height = br.ReadInt16();

            br.Close();

            return new ImageHeader(type, width, height, platform);
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
            throw new InvalidDataException($"A texture header of 0x{data.Length:X} bytes isn't one Bundle Manager knows.");
        }
    }

    public static Image GetImage(byte[] data, byte[] extraData, BundlePlatform? bundle = null)
    {
        if (extraData == null) // ¯\_(ツ)_/¯
            return null;

        try
        {
            ImageHeader header = GetImageHeader(data, bundle);
            if (header.Width <= 0 || header.Height <= 0)
                throw new InvalidDataException($"The texture header gives a size of {header.Width}×{header.Height}.");

            if (header.Platform == PlatformType.X360)
                extraData = LinearX360(data, extraData, header.CompressionType);
            else if (header.CompressionType == CompressionType.UNKNOWN)
                throw new NotSupportedException($"This {header.Platform} texture's format isn't supported yet.");

            int needed = TextureSize(header.CompressionType, header.Width, header.Height);
            if (extraData.Length < needed)
                throw new InvalidDataException($"The texture data is {extraData.Length} bytes, but a {header.Width}×{header.Height} {header.CompressionType} texture needs {needed}.");

            byte[] pixels = header.CompressionType switch
            {
                CompressionType.DXT1 => ImageUtil.DecompressImage(extraData, header.Width, header.Height, CompressionFormat.Bc1),
                CompressionType.DXT5 => ImageUtil.DecompressImage(extraData, header.Width, header.Height, CompressionFormat.Bc3),
                _ => extraData
            };

            int[] bits = new int[header.Width * header.Height];

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

                bits[dstIndex] = argb;

                srcIndex += 4;
                dstIndex++;
            }

            // Make sure we create a Bitmap that has ownership of its pixels so disposing it frees them
            Bitmap image = new(header.Width, header.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Drawing.Imaging.BitmapData locked = image.LockBits(new Rectangle(0, 0, header.Width, header.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Marshal.Copy(bits, 0, locked.Scan0, bits.Length);
            image.UnlockBits(locked);
            return image;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            return null;
        }
    }

    // Xbox 360 GPUTEXTURE_FETCH_CONSTANT. Fields as in Volatility's TextureX360
    private readonly struct X360Fetch(byte[] header)
    {
        private uint Word(int i) => BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0x1C + i * 4));
        public bool Tiled => Word(0) >> 31 != 0;
        public int Pitch => (int)(Word(0) >> 22 & 0x1FF) * 32; // Texels
        public int Endian => (int)(Word(1) >> 6 & 3);
        public int Format => (int)(Word(1) & 0x3F);
        public int Width => (int)(Word(2) & 0x1FFF) + 1;
        public int Height => (int)(Word(2) >> 13 & 0x1FFF) + 1;
        public int Swizzle => (int)(Word(3) >> 1 & 0xFFF);
        public bool PackedMips => (Word(5) >> 11 & 1) != 0;
    }

    // Copies the top mip of an Xbox 360 texture (cube face 0) out of its tiling and GPU byte order into the
    // layout the other platforms use. Tiling and mip packing follow Xenia's texture_address.h and texture_util.cc
    private static byte[] LinearX360(byte[] header, byte[] data, CompressionType type)
    {
        X360Fetch f = new(header);
        int bytesPerBlock = type switch
        {
            CompressionType.DXT1 => 8,
            CompressionType.DXT5 => 16,
            CompressionType.RGBA or CompressionType.BGRA => 4,
            _ => throw new NotSupportedException($"Xbox 360 texture format {f.Format} with swizzle {f.Swizzle:X3} is not supported yet.")
        };
        int blockSize = bytesPerBlock == 4 ? 1 : 4;
        int width = (f.Width + blockSize - 1) / blockSize, height = (f.Height + blockSize - 1) / blockSize;
        int pitch = (f.Pitch / blockSize + 31) & ~31; // Rows are padded to whole 32 block tiles

        // Mips of 16 texels or less share one tile, and when the texture is that small its top mip is in there too
        int offsetX = 0, offsetY = 0;
        if (f.PackedMips && Math.Min(f.Width, f.Height) <= 16)
        {
            if (BitOperations.RoundUpToPowerOf2((uint)f.Width) > BitOperations.RoundUpToPowerOf2((uint)f.Height))
                offsetY = 16 / blockSize;
            else
                offsetX = 16 / blockSize;
        }

        byte[] linear = new byte[width * height * bytesPerBlock];
        int log2Bpb = BitOperations.Log2((uint)bytesPerBlock);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int src = f.Tiled ? Tiled2D(x + offsetX, y + offsetY, pitch, log2Bpb) : ((y + offsetY) * pitch + x + offsetX) << log2Bpb;
                if (src + bytesPerBlock <= data.Length)
                    Array.Copy(data, src, linear, (y * width + x) * bytesPerBlock, bytesPerBlock);
            }
        }

        Span<uint> words = MemoryMarshal.Cast<byte, uint>(linear.AsSpan());
        switch (f.Endian)
        {
            case 1: // 8in16
                Span<ushort> halves = MemoryMarshal.Cast<byte, ushort>(linear.AsSpan());
                BinaryPrimitives.ReverseEndianness(halves, halves);
                break;
            case 2: // 8in32
                BinaryPrimitives.ReverseEndianness(words, words);
                break;
            case 3: // 16in32
                foreach (ref uint word in words)
                    word = BitOperations.RotateLeft(word, 16);
                break;
        }
        return linear;
    }

    // Byte offset of block (x, y) in a tiled surface whose rows are pitch blocks long
    private static int Tiled2D(int x, int y, int pitch, int log2Bpb)
    {
        int outerInner = ((((y >> 5) * (pitch >> 5) + (x >> 5)) << 6) | ((y >> 1 & 7) << 3) | (x & 7)) << log2Bpb;
        int bank = y >> 4 & 1;
        int pipe = (x >> 3 & 3) ^ ((y >> 3 & 1) << 1);
        return (y & 1) << 4 | pipe << 6 | bank << 11
             | (outerInner & 0xF) | (outerInner >> 4 & 1) << 5 | (outerInner >> 5 & 7) << 8 | outerInner >> 8 << 12;
    }

    public static PlatformType DetectPlatform(byte[] data, BundlePlatform? bundle = null)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        int len = data.Length;

        PlatformType? known = (bundle, len) switch
        {
            (BundlePlatform.X360, 0x40) => PlatformType.X360,
            (BundlePlatform.PS3, 0x30) => PlatformType.PS3,
            (BundlePlatform.PCx64, 0x30) => PlatformType.PCx64,
            (BundlePlatform.PC, 0x20) => PlatformType.PC,
            (BundlePlatform.PC or BundlePlatform.PCx64, 0x40 or 0x60) => PlatformType.Remastered,
            _ => null
        };
        if (known != null)
            return known.Value;

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

        // PS3 starts with format/mip/dimension bytes; decomp x64 starts with null pointers and bools
        if (len == 0x30)
            return data.AsSpan(0, 0x1C).IndexOfAnyExcept((byte)0) < 0 ? PlatformType.PCx64 : PlatformType.PS3;

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
    X360,
    PCx64
}
