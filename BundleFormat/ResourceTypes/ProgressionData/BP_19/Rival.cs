using BundleFormat.Enums;
using BundleFormat.Enums.BP_19;
using BundleFormat.Types;
using Serialization.Attributes;
using Serialization.Types;
using System.Text;

namespace BundleFormat.ResourceTypes.ProgressionData.BP_19;

// 1.9
internal class Rival
{
	internal ulong id = new();
	internal CgsId carId = new();
	internal short personalityIndex;
	internal short pursuitTarget;
	internal sbyte districtIndex;
	internal sbyte unlockRank;
	internal byte numMedalsToUnlock;
	internal bool isUsedForRankUpGiftCar;
	[ArrayLength(32)]
	internal CollectionDescriptor<sbyte> name = new();

	public ulong Id { get => id; set => id = value; }
	public CgsId CarId { get => carId; set => carId = value; }
	[FromArray(nameof(ProgressionData.Personalities))]
	public int PersonalityIndex { get => personalityIndex; set => personalityIndex = (short)value; }
	public short PursuitTarget { get => pursuitTarget; set => pursuitTarget = value; }
	public District DistrictIndex { get => (District)districtIndex; set => districtIndex = (sbyte)value; }
	public Rank UnlockRank
	{
		get
		{
			if (unlockRank < (sbyte)Rank.LearnersPermit
				|| unlockRank > (sbyte)Rank.EliteLicense)
				return Rank.Invalid;
			return (Rank)unlockRank;
		}
		set
		{
			if (value < Rank.LearnersPermit
				|| value > Rank.EliteLicense)
			{
				unlockRank = 127;
				return;
			}
			unlockRank = (sbyte)value;
		}
	}
	public byte NumMedalsToUnlock { get => numMedalsToUnlock; set => numMedalsToUnlock = value; }
	public bool IsUsedForRankUpGiftCar { get => isUsedForRankUpGiftCar; set => isUsedForRankUpGiftCar = value; }
	[MaxStringLength(32)]
	public string Name
	{
		get
		{
			StringBuilder nameBuilder = new();
			sbyte character;
			for (int i = 0; i < 32; i++)
			{
				character = name.Collection[i];
				if (character == 0)
					break;
				nameBuilder.Append((char)character);
			}
			return nameBuilder.ToString();
		}
		set
		{
			name.Collection.Clear();
			var array = (Array.ConvertAll(Encoding.ASCII.GetBytes(value), c => Convert.ToSByte(c)));
			for (int i = 0; i < 32; i++)
			{
				if (i < array.Length)
					name.Collection.Add(array[i]);
				else
					name.Collection.Add(0);
			}
		}
	}
}
