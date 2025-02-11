using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace BundleUtilities
{
    public class BinaryWriter2 : BinaryWriter
    {
        public bool BigEndian { get; set; }

        public BinaryWriter2(Stream input, bool bigEndian = false) : base(input)
        {
            BigEndian = bigEndian;
        }

        public override void Write(short value)
        {
            if (BigEndian)
                base.Write(BinaryPrimitives.ReverseEndianness(value));
            else
                base.Write(value);
        }

        public override void Write(int value)
        {
            if (BigEndian)
                base.Write(BinaryPrimitives.ReverseEndianness(value));
            else
                base.Write(value);
        }

        public override void Write(long value)
        {
            if (BigEndian)
                base.Write(BinaryPrimitives.ReverseEndianness(value));
            else
                base.Write(value);
        }

        public override void Write(ushort value)
        {
            if (BigEndian)
                base.Write(BinaryPrimitives.ReverseEndianness(value));
            else
                base.Write(value);
        }

        public override void Write(uint value)
        {
            if (BigEndian)
                base.Write(BinaryPrimitives.ReverseEndianness(value));
            else
                base.Write(value);
        }

        public override void Write(ulong value)
        {
            if (BigEndian)
                base.Write(BinaryPrimitives.ReverseEndianness(value));
            else
                base.Write(value);
        }

        public override void Write(float value)
        {
            if (BigEndian)
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                base.Write(bytes);
            }
            else
                base.Write(value);
        }

        public override void Write(double value)
        {
            if (BigEndian)
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                base.Write(bytes);
            }
            else
                base.Write(value);
        }

        public void Align(byte alignment)
        {
            if (BaseStream.Position % alignment == 0)
                return;
            BaseStream.Position = alignment * ((BaseStream.Position + (alignment - 1)) / alignment);
            BaseStream.Position--;
            Write((byte)0);
        }

        public void WriteCStr(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            for (int i = 0; i < bytes.Length; i++)
                Write(bytes[i]);
            Write((byte)0);
        }

        public void WriteLenString(string s, int len)
        {
            for (int i = 0; i < len; i++)
            {
                if (i < s.Length)
                    Write((byte)s[i]);
                else
                    Write((byte)0);
            }
        }

        public void WriteUniquePadding(int numberOfPadding)
        {
            for (int i = 0; i < numberOfPadding; i++)
                Write((byte)0);
        }

        // Add padding: Has to be divisible by 16, else add padding
        public void WritePadding()
        {
            long currentLength = BaseStream.Length;
            if (currentLength % 16 != 0)
            {
                for (int i = 0; i < (16 - currentLength % 16); i++)
                    Write((byte)0);
            };
        }
    }
}
