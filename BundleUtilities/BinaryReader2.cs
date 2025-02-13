using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BundleUtilities
{
    public class BinaryReader2 : BinaryReader
    {
        public bool BigEndian { get; set; }

        public BinaryReader2(Stream input, bool bigEndian = false) : base(input)
        {
            BigEndian = bigEndian;
        }

        public override short ReadInt16()
        {
            if (BaseStream.Length - BaseStream.Position < 2)
                throw new EndOfStreamException();
            if (BigEndian)
                return BinaryPrimitives.ReadInt16BigEndian(ReadBytes(2));
            return BinaryPrimitives.ReadInt16LittleEndian(ReadBytes(2));
        }

        public override int ReadInt32()
        {
            if (BaseStream.Length - BaseStream.Position < 4)
                throw new EndOfStreamException();
            if (BigEndian)
                return BinaryPrimitives.ReadInt32BigEndian(ReadBytes(4));
            return BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));
        }

        public override long ReadInt64()
        {
            if (BaseStream.Length - BaseStream.Position < 8)
                throw new EndOfStreamException();
            if (BigEndian)
                return BinaryPrimitives.ReadInt64BigEndian(ReadBytes(8));
            return BinaryPrimitives.ReadInt64LittleEndian(ReadBytes(8));
        }

        public override ushort ReadUInt16()
        {
            if (BaseStream.Length - BaseStream.Position < 2)
                throw new EndOfStreamException();
            if (BigEndian)
                return BinaryPrimitives.ReadUInt16BigEndian(ReadBytes(2));
            return BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(2));
        }

        public override uint ReadUInt32()
        {
            if (BaseStream.Length - BaseStream.Position < 4)
                throw new EndOfStreamException();
            if (BigEndian)
                return BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(4));
            return BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(4));
        }

        public override ulong ReadUInt64()
        {
            if (BaseStream.Length - BaseStream.Position < 8)
                throw new EndOfStreamException();
            if (BigEndian)
                return BinaryPrimitives.ReadUInt64BigEndian(ReadBytes(8));
            return BinaryPrimitives.ReadUInt64LittleEndian(ReadBytes(8));
        }

        public override float ReadSingle()
        {
            if (BaseStream.Length - BaseStream.Position < 4)
                throw new EndOfStreamException();
            if (BigEndian)
                return BinaryPrimitives.ReadSingleBigEndian(ReadBytes(4));
            return BinaryPrimitives.ReadSingleLittleEndian(ReadBytes(4));
        }

        public override double ReadDouble()
        {
            if (BaseStream.Length - BaseStream.Position < 8)
                throw new EndOfStreamException();
            if (BigEndian)
                return BinaryPrimitives.ReadDoubleBigEndian(ReadBytes(8));
            return BinaryPrimitives.ReadDoubleLittleEndian(ReadBytes(8));
        }

        public void Align(int alignment)
        {
            if (BaseStream.Position % alignment == 0)
                return;
            BaseStream.Position = alignment * ((BaseStream.Position + (alignment - 1)) / alignment);
        }

        public string ReadCStr()
        {
            List<byte> bytes = new List<byte>();

            try
            {
                while (true)
                {
                    byte c = ReadByte();
                    if (c == '\0')
                        break;
                    bytes.Add(c);
                }
            }
            catch (EndOfStreamException)
            {
                // Ignore
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public string ReadCStringPtr()
        {
            uint ptr = ReadUInt32();
            long pos = BaseStream.Position;
            BaseStream.Position = ptr;
            string result = ReadCStr();
            BaseStream.Position = pos;

            return result;
        }

        public string ReadLenString(int size)
        {
            string result = "";
            bool readNull = false;
            for (int i = 0; i < size; i++)
            {
                char c = (char)ReadByte();
                if (c == '\0')
                    readNull = true;
                if (!readNull)
                    result += c;
            }

            return result;
        }

        public bool EOF()
        {
            return BaseStream.Position >= BaseStream.Length;
        }

        public byte Peek()
        {
            byte b = ReadByte();
            BaseStream.Seek(-1, SeekOrigin.Current);
            return b;
        }

        public void SkipUniquePadding(int numberOfBytes)
        {
            BaseStream.Position = BaseStream.Position + numberOfBytes;
        }

        public void SkipPadding()
        {
            long currentLength = BaseStream.Position;
            if (currentLength % 16 != 0)
            {
                BaseStream.Position = BaseStream.Position + (16 - currentLength % 16);
            };
        }
    }
}
