using Serialization.Attributes;

namespace BundleFormat.BundleV2.V2;

internal class BundleV2
{
	[ArrayLength(4)]
	internal sbyte[] magicNumber = [];
	internal uint version;
	internal uint platform;
	internal uint debugDataOffset;
	internal uint resourceEntriesCount;
	internal uint resourceEntriesOffset;
	[ArrayLength(3)]
	internal uint[] resourceDataOffset = [];
	internal uint flags;
}
