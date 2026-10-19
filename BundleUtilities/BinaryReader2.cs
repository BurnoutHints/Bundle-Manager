using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BundleUtilities;

public class BinaryReader2 : BinaryReader
{
    public bool BigEndian { get; set; }

    public BinaryReader2(Stream input, bool bigEndian = false) : base(input)
    {
        BigEndian = bigEndian;
    }

    // Reused for every value, as handlers read millions of them
    private readonly byte[] _value = new byte[8];

    // The next count bytes, leaving the position alone when fewer are left
    private ReadOnlySpan<byte> Next(int count)
    {
        if (BaseStream.Length - BaseStream.Position < count)
            throw new EndOfStreamException();
        BaseStream.ReadExactly(_value, 0, count);
        return _value.AsSpan(0, count);
    }

    public override short ReadInt16() => BigEndian ? BinaryPrimitives.ReadInt16BigEndian(Next(2)) : BinaryPrimitives.ReadInt16LittleEndian(Next(2));
    public override int ReadInt32() => BigEndian ? BinaryPrimitives.ReadInt32BigEndian(Next(4)) : BinaryPrimitives.ReadInt32LittleEndian(Next(4));
    public override long ReadInt64() => BigEndian ? BinaryPrimitives.ReadInt64BigEndian(Next(8)) : BinaryPrimitives.ReadInt64LittleEndian(Next(8));
    public override ushort ReadUInt16() => BigEndian ? BinaryPrimitives.ReadUInt16BigEndian(Next(2)) : BinaryPrimitives.ReadUInt16LittleEndian(Next(2));
    public override uint ReadUInt32() => BigEndian ? BinaryPrimitives.ReadUInt32BigEndian(Next(4)) : BinaryPrimitives.ReadUInt32LittleEndian(Next(4));
    public override ulong ReadUInt64() => BigEndian ? BinaryPrimitives.ReadUInt64BigEndian(Next(8)) : BinaryPrimitives.ReadUInt64LittleEndian(Next(8));
    public override float ReadSingle() => BigEndian ? BinaryPrimitives.ReadSingleBigEndian(Next(4)) : BinaryPrimitives.ReadSingleLittleEndian(Next(4));
    public override double ReadDouble() => BigEndian ? BinaryPrimitives.ReadDoubleBigEndian(Next(8)) : BinaryPrimitives.ReadDoubleLittleEndian(Next(8));

    public void Align(int alignment)
    {
        if (BaseStream.Position % alignment == 0)
            return;
        BaseStream.Position = alignment * ((BaseStream.Position + (alignment - 1)) / alignment);
    }

    public string ReadCStr()
    {
        List<byte> bytes = [];

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
        return Encoding.UTF8.GetString([.. bytes]);
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

    // size bytes, one character each, up to the first null
    public string ReadLenString(int size)
    {
        byte[] bytes = new byte[size];
        BaseStream.ReadExactly(bytes);
        int end = Array.IndexOf(bytes, (byte)0);
        return Encoding.Latin1.GetString(bytes, 0, end < 0 ? size : end);
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
