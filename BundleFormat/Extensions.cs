using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text;

namespace BundleFormat
{
    public static class Extensions
    {
        public static string AsString(this byte[] self)
        {
            if (self == null)
                return "";
            StringBuilder sb = new StringBuilder();

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

        public static string MakePreview(this byte[] self, int start, int end)
        {
            byte[] subArray = new byte[end - start];
            int index = 0;
            for (int i = start; i < start + end; i++)
            {
                subArray[index++] = self[i];
            }
            return subArray.AsString();
        }

        public static byte[] Compress(this byte[] self)
        {
            MemoryStream uncompressed = new(self);
            MemoryStream compressed = new();
            using (ZLibStream zlibStream = new(compressed, CompressionLevel.SmallestSize, true))
            {
                uncompressed.CopyTo(zlibStream);
            }
            return compressed.ToArray();
        }
        
        public static byte[] Decompress(this byte[] self)
        {
            MemoryStream decompressed = new();
            using (ZLibStream zlibStream = new(new MemoryStream(self), CompressionMode.Decompress))
            {
                zlibStream.CopyTo(decompressed);
            }
            return decompressed.ToArray();
        }

        public static bool Matches(this byte[] self, byte[] other)
        {
            if (self == null || other == null)
                return false;
            if (self.Length != other.Length)
                return false;

            for (int i = 0; i < self.Length; i++)
            {
                if (self[i] != other[i])
                    return false;
            }

            return true;
        }

        public static byte[] toBytes(this Vector4 vec, bool swap = false)
        {
            List<byte> bytes = new List<byte>();
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
            return bytes.ToArray();
        }
    }
}
