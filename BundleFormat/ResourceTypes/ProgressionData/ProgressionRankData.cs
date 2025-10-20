using BundleFormat.Types;
using Serialization.Attributes;
using Serialization.Types;

namespace BundleFormat.ResourceTypes.ProgressionData;

internal class ProgressionRankData
{
	internal float trafficDensityRace;
	internal float burningRouteTimeScale;
	internal float trafficDensityBurningRoute;
	internal float trafficDensityRoadRage;
	internal float trafficDensitySurvival;
	internal float trafficDensityPursuit;
	internal float shuntStrengthRace;
	internal float shuntStrengthRoadRage;
	internal float shuntStrengthMarkedMan;
	internal float largeVehicleProbability;
	internal uint id;
	[ArrayLength(8)]
	internal CollectionDescriptor<float> overtakingDifficulty = new();
	internal ushort medalThresholdToNextRank;
	internal ushort eventThresholdToNextRank;
	internal ushort roadRageTakedownTarget;
	internal ushort roadRageTime;
	internal ushort roadRageTimeExtensions;
	internal ushort roadRageExtensionTime;
	internal ushort roadRageDamageLimit;
	internal ushort roadRageTriggerExtension;
	internal byte raceRivalsNumber;
	internal byte gauntletRivalsNumber;
	internal byte roadRageRivalsNumber;
	internal byte numGiftCars;
	internal byte numWinsToRankUpRace;
	internal byte numWinsToRankUpStunt;
	internal byte numWinsToRankUpRoadRage;
	internal byte numWinsToRankUpMarkedMan;
	internal CgsId freeCarForRankUpId = new();

	public float TrafficDensityRace { get => trafficDensityRace; set => trafficDensityRace = value; }
	public float BurningRouteTimeScale { get => burningRouteTimeScale; set => burningRouteTimeScale = value; }
	public float TrafficDensityBurningRoute { get => trafficDensityBurningRoute; set => trafficDensityBurningRoute = value; }
	public float TrafficDensityRoadRage { get => trafficDensityRoadRage; set => trafficDensityRoadRage = value; }
	public float TrafficDensitySurvival { get => trafficDensitySurvival; set => trafficDensitySurvival = value; }
	public float TrafficDensityPursuit { get => trafficDensityPursuit; set => trafficDensityPursuit = value; }
	public float ShuntStrengthRace { get => shuntStrengthRace; set => shuntStrengthRace = value; }
	public float ShuntStrengthRoadRage { get => shuntStrengthRoadRage; set => shuntStrengthRoadRage = value; }
	public float ShuntStrengthMarkedMan { get => shuntStrengthMarkedMan; set => shuntStrengthMarkedMan = value; }
	public float LargeVehicleProbability { get => largeVehicleProbability; set => largeVehicleProbability = value; }
	public uint Id { get => id; set => id = value; }
	public CollectionDescriptor<float> OvertakingDifficulty { get => overtakingDifficulty; }
	public ushort MedalThresholdToNextRank { get => medalThresholdToNextRank; set => medalThresholdToNextRank = value; }
	public ushort EventThresholdToNextRank { get => eventThresholdToNextRank; set => eventThresholdToNextRank = value; }
	public ushort RoadRageTakedownTarget { get => roadRageTakedownTarget; set => roadRageTakedownTarget = value; }
	public ushort RoadRageTime { get => roadRageTime; set => roadRageTime = value; }
	public ushort RoadRageTimeExtensions { get => roadRageTimeExtensions; set => roadRageTimeExtensions = value; }
	public ushort RoadRageExtensionTime { get => roadRageExtensionTime; set => roadRageExtensionTime = value; }
	public ushort RoadRageDamageLimit { get => roadRageDamageLimit; set => roadRageDamageLimit = value; }
	public ushort RoadRageTriggerExtension { get => roadRageTriggerExtension; set => roadRageTriggerExtension = value; }
	public byte RaceRivalsNumber { get => raceRivalsNumber; set => raceRivalsNumber = value; }
	public byte GauntletRivalsNumber { get => gauntletRivalsNumber; set => gauntletRivalsNumber = value; }
	public byte RoadRageRivalsNumber { get => roadRageRivalsNumber; set => roadRageRivalsNumber = value; }
	public byte NumGiftCars { get => numGiftCars; set => numGiftCars = value; }
	public byte NumWinsToRankUpRace { get => numWinsToRankUpRace; set => numWinsToRankUpRace = value; }
	public byte NumWinsToRankUpStunt { get => numWinsToRankUpStunt; set => numWinsToRankUpStunt = value; }
	public byte NumWinsToRankUpRoadRage { get => numWinsToRankUpRoadRage; set => numWinsToRankUpRoadRage = value; }
	public byte NumWinsToRankUpMarkedMan { get => numWinsToRankUpMarkedMan; set => numWinsToRankUpMarkedMan = value; }
	public CgsId FreeCarForRankUpId { get => freeCarForRankUpId; set => freeCarForRankUpId = value;	}
}
