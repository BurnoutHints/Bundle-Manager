using BundleFormat.Interfaces;
using BundleFormat.Types;
using Serialization;
using Serialization.Attributes;
using Serialization.Types;

namespace BundleFormat.ResourceTypes.ProgressionData.BP_19;

internal class ProgressionData : ISerializableResourceType
{
	internal uint versionNumber;
	internal uint size;
	[ArrayLengthField(nameof(playerCarIdCount))]
	internal ArrayPointer<CgsId> playerCarIds = new();
	internal uint playerCarIdCount;
	[ArrayLengthField(nameof(progressionRankCount))]
	internal ArrayPointer<ProgressionRankData> progressionRanks = new();
	internal uint progressionRankCount;
	[ArrayLengthField(nameof(eventJunctionCount))]
	internal ArrayPointer<EventJunction> eventJunctions = new();
	internal uint eventJunctionCount;
	[ArrayLengthField(nameof(eventCount))]
	internal ArrayPointer<RaceEventData> events = new();
	internal uint eventCount;
	[ArrayLengthField(nameof(rivalCount))]
	internal ArrayPointer<Rival> rivals = new();
	internal int rivalCount;
	[ArrayLengthField(nameof(aiBalanceCount))]
	internal ArrayPointer<OpponentBalanceData> aiBalances = new();
	internal uint aiBalanceCount;
	[ArrayLengthField(nameof(personalityCount))]
	internal ArrayPointer<EventRacerPersonality> personalities = new();
	internal uint personalityCount;
	[ArrayLengthField(nameof(trophyUnlockCount))]
	internal ArrayPointer<TrophyUnlockData> trophyUnlocks = new();
	internal uint trophyUnlockCount;
	[ArrayLengthField(nameof(carOpponentsCount))]
	internal ArrayPointer<CarOpponentSet> carOpponentSet = new();
	internal uint carOpponentsCount;

	[Tooltip("Vehicles to unlock at the beginning of the game.\nThis is not used in the final game.")]
	public CollectionDescriptor<CgsId> PlayerCarIds { get => playerCarIds.Data!; }
	[Tooltip("Licenses.\nSettings include traffic density and requirements to reach the next license.")]
	public CollectionDescriptor<ProgressionRankData> ProgressionRanks { get => progressionRanks.Data!; }
	[Tooltip("Mainland and island event junctions.\nThese point to their respective offline, online, and bike events.")]
	public CollectionDescriptor<EventJunction> EventJunctions { get => eventJunctions.Data!; }
	[Tooltip("Events.\nSettings include game mode, checkpoints, and score targets.")]
	public CollectionDescriptor<RaceEventData> Events { get => events.Data!; }
	[Tooltip("Roaming rivals.\nSettings include vehicle ID, which personality to use, and the number of medals needed to release it.")]
	public CollectionDescriptor<Rival> Rivals { get => rivals.Data!; }
	[Tooltip("Opponent balancing data.\nSettings include ahead/behind points and catch-up cut-off ratio.")]
	public CollectionDescriptor<OpponentBalanceData> AiBalances { get => aiBalances.Data!; }
	[Tooltip("Racer personalities.\nSettings include aggression, skill, and speed.")]
	public CollectionDescriptor<EventRacerPersonality> Personalities { get => personalities.Data!; }
	[Tooltip("Trophy cars, i.e., carbon cars.\nSettings include vehicle ID and unlock trigger.")]
	public CollectionDescriptor<TrophyUnlockData> TrophyUnlocks { get => trophyUnlocks.Data!; }
	[Tooltip("Per-vehicle opponent set.\nSettings include vehicle ID, rank required, and opponents.\nEach opponent has a vehicle ID and personality index.")]
	public CollectionDescriptor<CarOpponentSet> CarOpponentSet { get => carOpponentSet.Data!; }

	public static object Deserialize(Type type, IResource resource)
	{
		resource.Memory.Seek(0, SeekOrigin.Begin);
		return Deserializer.Deserialize(type, resource.Memory, resource.Config);
	}

	public static void Serialize(object obj, IResource resource)
	{
		var progressionData = (ProgressionData)obj;
		progressionData.UpdatePointers(resource.Config.Platform.Is64Bit, out ulong position);
		progressionData.size = (uint)Serializer.Align(position, 0x10);
		resource.Memory.Seek(0, SeekOrigin.Begin);
		resource.Memory.SetLength(0);
		Serializer.Serialize(resource.Memory, resource.Config, obj);
	}

	private void UpdatePointers(bool is64Bit, out ulong position)
	{
		ulong currentPosition = Serializer.StructureSizes.GetValueOrDefault(new(typeof(ProgressionData), is64Bit));

		Serializer.UpdateArrayAddress(ref currentPosition, playerCarIds, sizeof(ulong), playerCarIdCount);
		Serializer.UpdateArrayAddress(ref currentPosition, progressionRanks, typeof(ProgressionRankData), is64Bit, progressionRankCount);
		Serializer.UpdateArrayAddress(ref currentPosition, eventJunctions, typeof(EventJunction), is64Bit, eventJunctionCount);
		Serializer.UpdateArrayAddress(ref currentPosition, events, typeof(RaceEventData), is64Bit, eventCount);
		Serializer.UpdateArrayAddress(ref currentPosition, rivals, typeof(Rival), is64Bit, (ulong)rivalCount);
		Serializer.UpdateArrayAddress(ref currentPosition, aiBalances, typeof(OpponentBalanceData), is64Bit, aiBalanceCount);
		Serializer.UpdateArrayAddress(ref currentPosition, personalities, typeof(EventRacerPersonality), is64Bit, personalityCount);
		Serializer.UpdateArrayAddress(ref currentPosition, trophyUnlocks, typeof(TrophyUnlockData), is64Bit, trophyUnlockCount);
		Serializer.UpdateArrayAddress(ref currentPosition, carOpponentSet, typeof(CarOpponentSet), is64Bit, carOpponentsCount);

		ulong raceEventDataLength = Serializer.StructureSizes.GetValueOrDefault(new(typeof(RaceEventData), is64Bit));
		UpdateJunctionPointers(raceEventDataLength);

		ulong checkpointDataLength = Serializer.StructureSizes.GetValueOrDefault(new(typeof(CheckpointData), is64Bit));
		UpdateCheckpointPointers(ref currentPosition, checkpointDataLength);

		position = currentPosition;
	}

	private void UpdateJunctionPointers(ulong raceEventDataLength)
	{
		foreach (var junction in eventJunctions.Data!.Collection)
		{
			UpdateJunctionPointer(junction.offlineEvent, raceEventDataLength);
			if (junction.onlineEvent.Data != null)
				UpdateJunctionPointer(junction.onlineEvent, raceEventDataLength);
			if (junction.bikeEvent.Data != null)
				UpdateJunctionPointer(junction.bikeEvent, raceEventDataLength);
		}
	}

	private void UpdateJunctionPointer(InstancePointer<RaceEventData> eventPointer, ulong raceEventDataLength)
	{
		uint eventId = eventPointer.Data!.id;
		var eventEntry = events.Data!.Collection.First(e => e.id == eventId);
		int eventIndex = events.Data!.Collection.IndexOf(eventEntry);
		ulong newAddress = events.Address + (raceEventDataLength * (ulong)eventIndex);
		eventPointer.Address = newAddress;
	}

	private void UpdateCheckpointPointers(ref ulong currentPosition, ulong checkpointDataLength)
	{
		foreach (var eventData in events.Data!.Collection)
			UpdateCheckpointPointer(eventData, ref currentPosition, checkpointDataLength);
	}

	private void UpdateCheckpointPointer(RaceEventData eventData, ref ulong currentPosition, ulong checkpointDataLength)
	{
		eventData.checkpoints.Address = currentPosition;
		currentPosition += checkpointDataLength * (ulong)eventData.checkpointCount;
	}
}
