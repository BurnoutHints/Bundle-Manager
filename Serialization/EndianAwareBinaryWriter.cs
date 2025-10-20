using Information.Enums;
using System.Buffers.Binary;
using System.Text;

namespace Serialization;

// Similar to the endian-aware BinaryReader subclass, just much more generic.
// Everything calls base.Write(), this simply reverses the endianness if needed.
// Also adds pointer writing.
public class EndianAwareBinaryWriter : BinaryWriter
{
	public Endianness Endianness { get; }
	public bool Is64Bit { get; }
	
	protected EndianAwareBinaryWriter(Endianness endianness = Endianness.Little, bool is64Bit = false) : base()
	{
		Endianness = endianness;
		Is64Bit = is64Bit;
	}

	public EndianAwareBinaryWriter(Stream output, Endianness endianness = Endianness.Little, bool is64Bit = false) : base(output, Encoding.UTF8, false)
	{
		Endianness = endianness;
		Is64Bit = is64Bit;
	}

	public EndianAwareBinaryWriter(Stream output, Encoding encoding, Endianness endianness = Endianness.Little, bool is64Bit = false) : base(output, encoding, false)
	{
		Endianness = endianness;
		Is64Bit = is64Bit;
	}

	public EndianAwareBinaryWriter(Stream output, Encoding encoding, bool leaveOpen, Endianness endianness = Endianness.Little, bool is64Bit = false) : base(output, encoding, leaveOpen)
	{
		Endianness = endianness;
		Is64Bit = is64Bit;
	}

	public void Align(uint alignment)
	{
		if (alignment == 0 || (alignment & (alignment - 1)) != 0)
			throw new Exception("Specified alignment was not a power of 2.");
		if (BaseStream.Position % alignment == 0)
			return;
		Seek(((alignment - (BaseStream.Position % alignment)) % alignment) - 1, SeekOrigin.Current);
		Write((byte)0);
	}

	public long Seek(long offset, SeekOrigin origin)
	{
		return BaseStream.Seek(offset, origin);
	}

	public override void Write(short value)
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
		{
			base.Write(BinaryPrimitives.ReverseEndianness(value));
			return;
		}
		base.Write(value);
	}

	public override void Write(ushort value)
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
		{
			base.Write(BinaryPrimitives.ReverseEndianness(value));
			return;
		}
		base.Write(value);
	}

	public override void Write(int value)
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
		{
			base.Write(BinaryPrimitives.ReverseEndianness(value));
			return;
		}
		base.Write(value);
	}

	public override void Write(uint value)
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
		{
			base.Write(BinaryPrimitives.ReverseEndianness(value));
			return;
		}
		base.Write(value);
	}

	public override void Write(long value)
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
		{
			base.Write(BinaryPrimitives.ReverseEndianness(value));
			return;
		}
		base.Write(value);
	}

	public override void Write(ulong value)
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
		{
			base.Write(BinaryPrimitives.ReverseEndianness(value));
			return;
		}
		base.Write(value);
	}

	public override void Write(float value)
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
		{
			base.Write(BitConverter.UInt32BitsToSingle(BinaryPrimitives.ReverseEndianness(BitConverter.SingleToUInt32Bits(value))));
			return;
		}
		base.Write(value);
	}

	public override void Write(double value)
	{
		if ((Endianness == Endianness.Big && BitConverter.IsLittleEndian)
			|| (Endianness == Endianness.Little) && !BitConverter.IsLittleEndian)
		{
			base.Write(BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReverseEndianness(BitConverter.DoubleToUInt64Bits(value))));
			return;
		}
		base.Write(value);
	}
}
