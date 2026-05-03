using System;
using System.Linq;
using System.IO;
using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using BCnEncoder.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.CompilerServices;

namespace BurnoutImage
{
    public static class CompressionTools
    {
        public static byte[] CompressTexture(string source, CompressionFormat compression)
        {
            BcEncoder encoder = new BcEncoder();
            encoder.OutputOptions.Format = compression;
            encoder.OutputOptions.FileFormat = OutputFileFormat.Dds;
            encoder.OutputOptions.GenerateMipMaps = false;
            encoder.OutputOptions.Quality = CompressionQuality.BestQuality;
            Image<Rgba32> image = Image.Load<Rgba32>(source);
            MemoryStream stream = new MemoryStream();
            encoder.EncodeToStream(image, stream);
            return new ArraySegment<byte>(stream.ToArray(), 0x80, (int)stream.Length - 0x80).ToArray<byte>();
        }

        public static byte[] DecompressTexture(byte[] source, int width, int height, CompressionFormat compression)
        {
            BcDecoder decoder = new BcDecoder();
            Image<Rgba32> image = decoder.DecodeRawToImageRgba32(source, width, height, compression);
            byte[] data = new byte[width * height * 4];
            image.CopyPixelDataTo(data);
            return data;
        }
    }
}
