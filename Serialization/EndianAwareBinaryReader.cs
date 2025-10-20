using Information.Enums;
using System.Buffers.Binary;
using System.Text;

namespace Serialization;

// Internally, BinaryReader calls BinaryPrimitives.Read*LittleEndian(ReadOnlySpan<byte> source)
// which itself just calls ReverseEndianness(* value) if the machine and target endianness differs.
// The Read*() overrides here simply reverse the endianness again if needed.
// Also adds pointer reading.
public class EndianAwareBinaryReader : BinaryReader
{
	public Endianness Endianness { get; }
	public bool Is64Bit { get; }

	public EndianAwareBinaryReader(Stream input, Endianness endianness = Endianness.Little, bool is64Bit = false) : base(input)
	{
		Endianness = endianness;
		Is64Bit = is64Bit;
	}
	public EndianAwareBinaryReader(Stream input, Encoding encoding, Endianness endianness = Endianness.Little, bool is64Bit = false) : base(input, encoding)
	{
		Endianness = endianness;
		Is64Bit = is64Bit;
	}
	public EndianAwareBinaryReader(Stream input, Encoding encoding, bool leaveOpen, Endianness endianness = Endianness.Little, bool is64Bit = false) : base(input, encoding, leaveOpen)
	{
		Endianness = endianness;
		Is64Bit = is64Bit;
	}

	public long Seek(long offset, SeekOrigin origin)
	{
		return BaseStream.Seek(offset, origin);
	}

	public void Align(uint alignment)
	{
		if (alignment == 0 || (alignment & (alignment - 1)) != 0)
			throw new Exception("Specified alignment was not a power of 2.");
		Seek((alignment - (BaseStream.Position % alignment)) % alignment, SeekOrigin.Current);
	}

	public override short ReadInt16()
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
			return BinaryPrimitives.ReverseEndianness(base.ReadInt16());
		return base.ReadInt16();
	}

	public override ushort ReadUInt16()
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
			return BinaryPrimitives.ReverseEndianness(base.ReadUInt16());
		return base.ReadUInt16();
	}

	public override int ReadInt32()
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
			return BinaryPrimitives.ReverseEndianness(base.ReadInt32());
		return base.ReadInt32();
	}

	public override uint ReadUInt32()
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
			return BinaryPrimitives.ReverseEndianness(base.ReadUInt32());
		return base.ReadUInt32();
	}

	public override long ReadInt64()
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
			return BinaryPrimitives.ReverseEndianness(base.ReadInt64());
		return base.ReadInt64();
	}

	public override ulong ReadUInt64()
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
			return BinaryPrimitives.ReverseEndianness(base.ReadUInt64());
		return base.ReadUInt64();
	}

	public override float ReadSingle()
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
			return BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(BitConverter.SingleToUInt32Bits(base.ReadSingle())));
		return base.ReadSingle();
	}

	public override double ReadDouble()
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
			return BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(BitConverter.DoubleToUInt64Bits(base.ReadDouble())));
		return base.ReadDouble();
	}
}
