using BundleFormat.Types;
using Serialization.Attributes;

namespace BundleFormat.ResourceTypes.ProgressionData;

[AlignEnd]
internal class CarOpponent
{
	internal CgsId carId = new();
	internal int personalityIndex;

	public CgsId CarId { get => carId; set => carId = value; }
	[FromArray(nameof(BPR.ProgressionData.Personalities))]
	public int PersonalityIndex { get => personalityIndex; set => personalityIndex = value; }
}
