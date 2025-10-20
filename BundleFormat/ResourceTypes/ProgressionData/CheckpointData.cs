using Serialization.Attributes;
using Serialization.Types;

namespace BundleFormat.ResourceTypes.ProgressionData;

internal class CheckpointData
{
	internal uint landmarkId;
	internal int blockSectionCount;
	[ArrayLength(8)]
	[ArrayLengthField(nameof(blockSectionCount))]
	internal CollectionDescriptor<uint> blockSectionIds = new();

	public uint LandmarkId { get => landmarkId; set => landmarkId = value; }
	public int BlockSectionCount { get => blockSectionCount; set => blockSectionCount = value; }
	public CollectionDescriptor<uint> BlockSectionIds { get => blockSectionIds; }
}
