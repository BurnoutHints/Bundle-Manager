using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text;

namespace BundleFormat;

public static class Extensions
{
    extension(byte[] self)
    {
        public string AsString()
        {
            if (self == null)
                return "";
            StringBuilder sb = new();

            for (int i = 0; i < self.Length; i++)
            {
                sb.Append(self[i].ToString("X2"));
                if ((i + 1) % 16 == 0)
                    sb.Append("\r\n");
                else
                    sb.Append(" ");
            }

            return sb.ToString();
        }

        // Shorter data previews what there is
        public string MakePreview(int start, int end)
        {
            return self[Math.Min(start, self.Length)..Math.Min(end, self.Length)].AsString();
        }

        public byte[] Compress()
        {
            MemoryStream uncompressed = new(self);
            MemoryStream compressed = new();
            using (ZLibStream zlibStream = new(compressed, CompressionLevel.SmallestSize, true))
            {
                uncompressed.CopyTo(zlibStream);
            }
            return compressed.ToArray();
        }

        public byte[] Decompress()
        {
            MemoryStream decompressed = new();
            using (ZLibStream zlibStream = new(new MemoryStream(self), CompressionMode.Decompress))
            {
                zlibStream.CopyTo(decompressed);
            }
            return decompressed.ToArray();
        }

        public bool Matches(byte[] other)
        {
            return self != null && other != null && self.AsSpan().SequenceEqual(other);
        }
    }

    extension(Vector4 vec)
    {
        public byte[] toBytes(bool swap = false)
        {
            List<byte> bytes = [];
            if (swap)
            {
                bytes.AddRange(BitConverter.GetBytes(BinaryPrimitives.ReadSingleBigEndian(BitConverter.GetBytes(vec.X))));
                bytes.AddRange(BitConverter.GetBytes(BinaryPrimitives.ReadSingleBigEndian(BitConverter.GetBytes(vec.Y))));
                bytes.AddRange(BitConverter.GetBytes(BinaryPrimitives.ReadSingleBigEndian(BitConverter.GetBytes(vec.Z))));
                bytes.AddRange(BitConverter.GetBytes(BinaryPrimitives.ReadSingleBigEndian(BitConverter.GetBytes(vec.W))));
            }
            else
            {
                bytes.AddRange(BitConverter.GetBytes(vec.X));
                bytes.AddRange(BitConverter.GetBytes(vec.Y));
                bytes.AddRange(BitConverter.GetBytes(vec.Z));
                bytes.AddRange(BitConverter.GetBytes(vec.W));
            }
            return [.. bytes];
        }
    }
}
