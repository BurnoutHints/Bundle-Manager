using Serialization.Attributes;

namespace BundleFormat.BundleV2.V5;

internal class BundleV2
{
	[ArrayLength(4)]
	internal sbyte[] magicNumber = [];
	internal ushort version;
	internal ushort platform;
	internal uint debugDataOffset;
	internal uint resourceEntriesCount;
	internal uint resourceEntriesOffset;
	[ArrayLength(4)]
	internal uint[] resourceDataOffset = [];
	internal uint flags;
	internal ID defaultResourceId;
	internal int defaultResourceStreamIndex;
	[ArrayLength(4, 15)]
	internal sbyte[,] streamName = { };
}
