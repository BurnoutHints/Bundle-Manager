using Serialization.Attributes;
using Serialization.Types;

namespace BundleFormat.ResourceTypes.ProgressionData;

internal class OpponentBalanceData
{
	[ArrayLength(8)]
	internal CollectionDescriptor<float> aheadGraphPoints = new();
	[ArrayLength(8)]
	internal CollectionDescriptor<float> behindGraphPoints = new();
	internal float catchupCutoffRatio;

	public CollectionDescriptor<float> AheadGraphPoints { get => aheadGraphPoints; }
	public CollectionDescriptor<float> BehindGraphPoints { get => behindGraphPoints; }
	public float CatchupCutoffRatio { get => catchupCutoffRatio; set => catchupCutoffRatio = value; }
}
