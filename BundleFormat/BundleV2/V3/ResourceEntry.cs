using Serialization.Attributes;

namespace BundleFormat.BundleV2.V3;

[AlignEnd]
internal class ResourceEntry
{
	[Unaligned]
	internal ID resourceId;
	[Unaligned]
	internal ulong importHash;
	[ArrayLength(4)]
	internal uint[] uncompressedSizeAndAlignment = [];
	[ArrayLength(4)]
	internal uint[] sizeAndAlignmentOnDisk = [];
	[ArrayLength(4)]
	internal uint[] diskOffset = [];
	internal uint importOffset;
	internal uint resourceTypeId;
	internal ushort importCount;
	internal byte flags;
	internal byte poolOffset;

	internal List<ImportEntry> ImportEntries { get; } = [];
}
