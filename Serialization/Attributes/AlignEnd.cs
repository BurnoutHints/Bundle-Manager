namespace Serialization.Attributes;

// Informs the de/serializer that this class or struct has padding after it.
// Its end will be aligned to the size of its largest member, up to 16 bytes,
// or a manually specified power of 2.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AlignEndAttribute : Attribute
{
	public uint Alignment { get; }
	public AlignEndAttribute(uint alignment = 0)
	{
		if ((alignment & (alignment - 1)) == 0)
			Alignment = alignment;
	}
}
