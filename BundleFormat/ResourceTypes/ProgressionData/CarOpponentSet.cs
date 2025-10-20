using BundleFormat.Enums;
using BundleFormat.Types;
using Serialization.Attributes;
using Serialization.Types;

namespace BundleFormat.ResourceTypes.ProgressionData;

internal class CarOpponentSet
{
	[ArrayLength(8)]
	[ArrayLengthField(nameof(opponentCount))]
	internal CollectionDescriptor<CarOpponent> carOpponents = new();
	internal CgsId playerCarId = new();
	internal int rank;
	internal int opponentCount;

	public CollectionDescriptor<CarOpponent> CarOpponents { get => carOpponents; }
	public CgsId PlayerCarId { get => playerCarId; set => playerCarId = value; }
	public Rank Rank
	{
		get
		{
			if (rank < (int)Rank.LearnersPermit
				|| rank > (int)Rank.EliteLicense)
				return Rank.Invalid;
			return (Rank)rank;
		}
		set
		{
			if (value < Rank.LearnersPermit
				|| value > Rank.EliteLicense)
			{
				rank = 0;
				return;
			}
			rank = (int)value;
		}
	}
}
