using Serialization.Attributes;

namespace BundleFormat.BundleV2.V3;

internal class BundleV2
{
	[ArrayLength(4)]
	internal sbyte[] magicNumber = [];
	internal uint version;
	internal uint platform;
	internal uint debugDataOffset;
	internal uint resourceEntriesCount;
	internal uint resourceEntriesOffset;
	[ArrayLength(4)]
	internal uint[] resourceDataOffset = [];
	internal uint flags;
}
