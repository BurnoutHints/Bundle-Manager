using Serialization.Attributes;

namespace BundleFormat.BundleV2.V2;

internal class ResourceEntry
{
	internal ID resourceId;
	internal ulong importHash;
	[ArrayLength(3)]
	internal uint[] uncompressedSizeAndAlignment = [];
	[ArrayLength(3)]
	internal uint[] sizeAndAlignmentOnDisk = [];
	[ArrayLength(3)]
	internal uint[] diskOffset = [];
	internal uint importOffset;
	internal uint resourceTypeId;
	internal ushort importCount;
	internal byte flags;
	internal byte streamIndex;

	internal List<ImportEntry> ImportEntries { get; } = [];
}
