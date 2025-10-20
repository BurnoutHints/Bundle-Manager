using BundleFormat.Enums;
using BundleFormat.Types;
using Serialization.Attributes;
using Serialization.Types;

namespace BundleFormat.ResourceTypes.ProgressionData.BP_19;

[AlignEnd]
[DisplayMember(nameof(Id))]
internal class RaceEventData
{
	[Flags]
	internal enum Flags : uint
	{
		MidnightRide1 = 1,
		MidnightRide2 = 1 << 1,
		IslandEvent = 1 << 2
	}

	internal enum Mode : byte
	{
		Race,
		RoadRage,
		StuntRun,
		MarkedMan,
		BurningRoute,
		Pursuit
	}

	internal enum OnlineMode : byte
	{
		Race,
		RoadRage,
		BurningHomeRun
	}

	internal enum AStarSearchType : byte
	{
		Euclidean,
		EuclideanXBiased,
		EuclideanYBiased
	}

	internal uint id;
	internal uint flags;
	internal float trafficDensity;
	internal float boostEarning;
	internal CgsId specialEventCarId = new();
	[ArrayLengthField(nameof(checkpointCount))]
	internal ArrayPointer<CheckpointData> checkpoints = new();
	internal int checkpointCount;
	internal float timeLimitFast;
	internal float timeLimitSlow;
	[ArrayLength(6)]
	internal CollectionDescriptor<int> rankScore = new();
	[ArrayLength(6)]
	internal CollectionDescriptor<float> rankTime = new();
	internal float extensionTime;
	[ArrayLength(7)]
	[ArrayLengthField(nameof(startGridCount))]
	internal CollectionDescriptor<EventStartGridSlot> startGridSlots = new();
	internal uint startGridCount;
	internal byte mode;
	internal byte onlineMode;
	internal byte startRivalCount;
	internal byte addRivalCount;
	internal byte takedownBronze;
	internal byte takedownSilver;
	internal byte takedownGold;
	internal byte damageLimit;
	internal byte extensionTimeCount;
	internal byte aStarType;
	internal sbyte unlockRank;

	public uint Id { get => id; set => id = value; }
	[Tooltip("When set with MidnightRide2, makes the event available at night.")]
	public bool MidnightRide1
	{
		get => (flags & (uint)Flags.MidnightRide1) != 0;
		set
		{
			if (value)
				flags |= (uint)Flags.MidnightRide1;
			else
				flags &= (uint)~Flags.MidnightRide1;
		}
	}
	[Tooltip("When set with MidnightRide1, makes the event available at night.")]
	public bool MidnightRide2
	{
		get => (flags & (uint)Flags.MidnightRide2) != 0;
		set
		{
			if (value)
				flags |= (uint)Flags.MidnightRide2;
			else
				flags &= (uint)~Flags.MidnightRide2;
		}
	}
	public bool IslandEvent
	{
		get => (flags & (uint)Flags.IslandEvent) != 0;
		set
		{
			if (value)
				flags |= (uint)Flags.IslandEvent;
			else
				flags &= (uint)~Flags.IslandEvent;
		}
	}
	public float TrafficDensity { get => trafficDensity; set => trafficDensity = value; }
	public float BoostEarning { get => boostEarning; set => boostEarning = value; }
	public CgsId SpecialEventCarId { get => specialEventCarId; set => specialEventCarId = value; }
	public CollectionDescriptor<CheckpointData> Checkpoints { get => checkpoints.Data!; }
	public float TimeLimitFast { get => timeLimitFast; set => timeLimitFast = value; }
	public float TimeLimitSlow { get => timeLimitSlow; set => timeLimitSlow = value; }
	public CollectionDescriptor<int> RankScore { get => rankScore; }
	public CollectionDescriptor<float> RankTime { get => rankTime; }
	public float ExtensionTime { get => extensionTime; set => extensionTime = value; }
	public CollectionDescriptor<EventStartGridSlot> StartGridSlots { get => startGridSlots; }
	public Mode GameMode { get => (Mode)mode; set => mode = (byte)value; }
	public OnlineMode OnlineGameMode { get => (OnlineMode)onlineMode; set => onlineMode = (byte)value; }
	public byte StartRivalCount { get => startRivalCount; set => startRivalCount = value; }
	public byte AddRivalCount { get => addRivalCount; set => addRivalCount = value; }
	public byte TakedownBronze { get => takedownBronze; set => takedownBronze = value; }
	public byte TakedownSilver { get => takedownSilver; set => takedownSilver = value; }
	public byte TakedownGold { get => takedownGold; set => takedownGold = value; }
	public byte DamageLimit { get => damageLimit; set => damageLimit = value; }
	public byte ExtensionTimeCount { get => extensionTimeCount; set => extensionTimeCount = value; }
	public AStarSearchType AStarType { get => (AStarSearchType)aStarType; set => aStarType = (byte)value; }
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
				unlockRank = 0;
				return;
			}
			unlockRank = (sbyte)value;
		}
	}
}
