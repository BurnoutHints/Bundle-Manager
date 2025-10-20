using Serialization.Attributes;

namespace BundleFormat.BundleV2.V5;

[AlignEnd]
internal class ResourceEntry
{
	internal ID resourceId;
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
	internal byte streamOffset;

	internal List<ImportEntry> ImportEntries { get; } = [];
}
