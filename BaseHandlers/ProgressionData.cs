using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

public class ProgressionRankData
{
    [Category("Traffic")] public float mfTrafficDensityRace { get; set; }

    [Category("Traffic")] public float mfBurningRouteTimeScale { get; set; }

    [Category("Traffic")] public float mfTrafficDensityBurningRoute { get; set; }

    [Category("Traffic")] public float mfTrafficDensityRoadRage { get; set; }

    [Category("Traffic")] public float mfTrafficDensitySurvival { get; set; }

    [Category("Traffic")] public float mfTrafficDensityPursuit { get; set; }

    [Category("Traffic")] public float mfShuntStrengthRace { get; set; }

    [Category("Traffic")] public float mfShuntStrengthRoadRage { get; set; }

    [Category("Traffic")] public float mfShuntStrengthMarkedMan { get; set; }

    [Category("Traffic")] public float mfLargeVehicleProbability { get; set; }

    [Category("Rank"), TypeConverter(typeof(HexConverter))] public uint muId { get; set; } // 0x28

    [Category("Rank"), Description("Eight values."), TypeConverter(typeof(NumberListConverter))]
    public float[] maOvertakingDifficulty { get; set; } = new float[8]; // 0x2C

    [Category("Rank")] public ushort mu16MedalThresholdToNextRank { get; set; } // 0x4C

    [Category("Rank")] public ushort mu16EventThresholdToNextRank { get; set; }

    [Category("Road Rage")] public ushort muRoadRageTakedownTarget { get; set; }

    [Category("Road Rage")] public ushort muRoadRageTime { get; set; }

    [Category("Road Rage")] public ushort muRoadRageTimeExtensions { get; set; }

    [Category("Road Rage")] public ushort muRoadRageExtensionTime { get; set; }

    [Category("Road Rage")] public ushort muRoadRageDamageLimit { get; set; }

    [Category("Road Rage")] public ushort muRoadRageTriggerExtension { get; set; }

    [Category("Rank")] public byte muRaceRivalsNumber { get; set; } // 0x5C

    [Category("Rank")] public byte muGauntletRivalsNumber { get; set; }

    [Category("Rank")] public byte muRoadRageRivalsNumber { get; set; }

    [Category("Rank")] public byte muNumGiftCars { get; set; }

    [Category("Rank")] public byte muNumWinsToRankUpRace { get; set; } // 0x60

    [Category("Rank")] public byte muNumWinsToRankUpStunt { get; set; }

    [Category("Rank")] public byte muNumWinsToRankUpRoadRage { get; set; }

    [Category("Rank")] public byte muNumWinsToRankUpMarkedMan { get; set; }

    [Category("Advanced"), Description("Revision 43 and later.")] public uint Padding64 { get; set; }

    [Category("Rank"), Description("Revision 43 and later; revision 42 ranks end before it and don't store it."), TypeConverter(typeof(TrafficCgsIDConverter))]
    public ulong mFreeCarForRankUpID { get; set; } // 0x68

    public override string ToString() => $"Rank {muId}";
}

public class EventJunction
{
    [TypeConverter(typeof(HexConverter))] public uint muID { get; set; }

    [TypeConverter(typeof(TrafficReferenceConverter))] public RaceEventData mpOfflineEvent { get; set; }

    [TypeConverter(typeof(TrafficReferenceConverter))] public RaceEventData mpOnlineEvent { get; set; }

    [Description("Revision 44 only (the bike pointer the wiki leaves unnamed)."), TypeConverter(typeof(TrafficReferenceConverter))]
    public RaceEventData BikeEvent { get; set; }

    public int miShotGroup { get; set; }

    [Category("Advanced"), Description("64-bit resources only.")] public uint Padding04 { get; set; }

    [Category("Advanced"), Description("64-bit resources only.")] public uint Padding24 { get; set; }

    public override string ToString() => $"ID: {muID}";
}

public class RaceEventData
{
    private static readonly string[] Modes = ["Race", "Road Rage", "Stunt", "Survivor", "Burning Route", "Pursuit"];

    [Category("Event"), TypeConverter(typeof(HexConverter))] public uint muId { get; set; }

    [Category("Event"), Description("Bit 0: crash breaker.")] public uint muFlags { get; set; }

    [Category("Event")] public float mfTrafficDensity { get; set; }

    [Category("Event")] public float mfBoostEarning { get; set; }

    [Category("Event"), TypeConverter(typeof(TrafficCgsIDConverter))] public ulong mSpecialEventCarId { get; set; }

    [Browsable(false)] public List<CheckpointData> mpaCheckpoints { get; set; } = [];

    [Category("Targets")] public float mfTimeLimitFast { get; set; }

    [Category("Targets")] public float mfTimeLimitSlow { get; set; }

    [Category("Targets"), Description("Six values."), TypeConverter(typeof(NumberListConverter))]
    public int[] miRankScore { get; set; } = new int[6];

    [Category("Targets"), Description("Six values."), TypeConverter(typeof(NumberListConverter))]
    public float[] mfRankTime { get; set; } = new float[6];

    [Category("Targets")] public float mfExtensionTime { get; set; }

    [Browsable(false)] public List<EventStartGridSlot> maStartGridSlots { get; set; } = [.. Enumerable.Range(0, 7).Select(_ => new EventStartGridSlot())];

    [Category("Grid"), Description("How many of the seven start-grid slots are used.")] public uint muStartGridCount { get; set; }

    [Category("Event"), Description("0 race, 1 road rage, 2 stunt, 3 survivor, 4 burning route, 5 pursuit.")] public byte mu8Mode { get; set; }

    [Category("Event"), Description("0 race, 1 road rage, 2 burning home run.")] public byte mu8OnlineMode { get; set; }

    [Category("Grid")] public byte mu8StartRivalCount { get; set; }

    [Category("Grid")] public byte mu8AddRivalCount { get; set; }

    [Category("Targets")] public byte mu8TakeDownBronze { get; set; }

    [Category("Targets")] public byte mu8TakeDownSilver { get; set; }

    [Category("Targets")] public byte mu8TakeDownGold { get; set; }

    [Category("Targets")] public byte mu8DamageLimit { get; set; }

    [Category("Targets")] public byte mu8ExtensionTimeCount { get; set; }

    [Category("Event"), Description("0 euclidean, 1 x-biased, 2 y-biased.")] public byte mu8AStarType { get; set; }

    [Category("Event")] public sbyte mi8UnlockRank { get; set; }

    [Category("Advanced"), Description("1 byte in 32-bit resources, 5 in 64-bit ones.")] public byte[] Padding { get; set; } = [0];

    public override string ToString() => $"ID: {muId} ({(mu8Mode < Modes.Length
        ? Modes[mu8Mode]
        : $"mode {mu8Mode}")})";
}

public class EventStartGridSlot
{
    public uint muOpponentIndex { get; set; }

    public uint muPersonalityIndex { get; set; }

    public int miFastAIBalanceGraphIndex { get; set; }

    public int miSlowAIBalanceGraphIndex { get; set; }

    public byte muColourIndex { get; set; }

    [Description("Bit 0: can deviate from route, bit 1: can take shortcuts.")] public byte muFlags { get; set; }

    [Category("Advanced")] public ushort Padding12 { get; set; }

    public override string ToString() => $"Opponent {muOpponentIndex}";
}

public class CheckpointData
{
    public uint muLandmarkId { get; set; }

    [Description("How many of the eight block section IDs are used.")] public int miBlockSectionCount { get; set; }

    [Description("Eight values; unused ones are kept as stored."), TypeConverter(typeof(NumberListConverter))]
    public uint[] mauBlockSectionIds { get; set; } = new uint[8];

    public override string ToString() => $"Landmark {muLandmarkId} ({miBlockSectionCount} block sections)";
}

public class Rival
{
    [TypeConverter(typeof(TrafficCgsIDConverter))] public ulong mId { get; set; }

    [TypeConverter(typeof(TrafficCgsIDConverter))] public ulong mCarId { get; set; }

    public short miPersonalityIndex { get; set; }

    public short miPursuitTarget { get; set; }

    public sbyte miDistrictIndex { get; set; }

    public sbyte miUnlockRank { get; set; }

    public byte mu8NumMedalsToUnlock { get; set; }

    public bool mbIsUsedForRankUpGiftCar { get; set; }

    [Description("Up to 31 characters.")] public string macName { get; set; } = "";

    public override string ToString() => $"ID {TrafficCgsIDConverter.Label(mId)}: {macName} ({TrafficCgsIDConverter.Label(mCarId)})";
}

public class OpponentBalanceData
{
    [Description("Eight values."), TypeConverter(typeof(NumberListConverter))] public float[] mafAheadGraphPoints { get; set; } = new float[8];

    [Description("Eight values."), TypeConverter(typeof(NumberListConverter))] public float[] mafBehindGraphPoints { get; set; } = new float[8];

    [Description("Stored little-endian on every platform, as the original resource builder wrote it.")] public float mfCatchUpCutOffRatio { get; set; }
}

public class EventRacerPersonality
{
    public float mfMinAggression { get; set; }

    public float mfMaxAggression { get; set; }

    public float mfSkill { get; set; }

    public float mfSpeed { get; set; }
}

public class TrophyUnlockData
{
    public uint muNumberTrophyUnlock { get; set; }

    public ushort mu16UnlockType { get; set; }

    [Category("Advanced")] public ushort Padding06 { get; set; }

    [TypeConverter(typeof(TrafficCgsIDConverter))] public ulong mCarUnlockId { get; set; }

    public override string ToString() => $"Vehicle {TrafficCgsIDConverter.Label(mCarUnlockId)}";
}

public class CarOpponent
{
    [TypeConverter(typeof(TrafficCgsIDConverter))] public ulong mCarId { get; set; }

    public int miPersonalityIndex { get; set; }

    [Category("Advanced")] public uint Padding0C { get; set; }

    public override string ToString() => TrafficCgsIDConverter.Label(mCarId);
}

public class CarOpponentSet
{
    [Browsable(false)] public List<CarOpponent> maCarOpponents { get; set; } = [.. Enumerable.Range(0, 8).Select(_ => new CarOpponent())]; // 8 slots precede the player car

    [TypeConverter(typeof(TrafficCgsIDConverter))] public ulong mPlayerCarId { get; set; } // 0x80

    public int miRank { get; set; } // 0x88

    [Description("How many of the eight opponents are used.")] public int miOpponentCount { get; set; } // 0x8C

    public override string ToString() => $"Player vehicle {TrafficCgsIDConverter.Label(mPlayerCarId)}";
}

// putting this in a class so the tree can edit them
public class PlayerCarID
{
    [TypeConverter(typeof(TrafficCgsIDConverter))] public ulong mCarId { get; set; }

    public override string ToString() => TrafficCgsIDConverter.Label(mCarId);
}

public class ProgressionData : IEntryData
{
    private const int HeaderSize = 0x50, JunctionSize = 0x10, BikeJunctionSize = 0x14, EventSize = 0xF8, CheckpointSize = 0x28,
        RivalSize = 0x38, BalanceSize = 0x44, PersonalitySize = 0x10, TrophySize = 0x10, OpponentSetSize = 0x90;

    [Category("Resource"), Description("42: Aug 31 / Sep 4 2007 builds - 43: Sep 28 to 1.3 - 44: 1.4+ " +
        "Changing 43 to 42 or back converts the rank layout when saved.")]
    public uint muVersionNumber { get; set; }

    [Category("Resource"), ReadOnly(true), Description("As read. Recalculated when saved.")] public uint muSize { get; set; }

    [Category("Resource"), Description("Remastered 64-bit only: the junction count without Big Surf Island.")]
    public uint? BaseEventJunctionCount { get; set; }

    [Category("Resource"), ReadOnly(true), Description("Native 64-bit layout (Remastered). It can be viewed but not saved.")]
    public bool Wide { get; set; }

    [Browsable(false)] public List<uint> PointerPadding { get; set; } = [];

    [Browsable(false)] public List<PlayerCarID> mpaPlayerCarIds { get; set; } = [];

    [Browsable(false)] public List<ProgressionRankData> mpaProgressionRanks { get; set; } = [];

    [Browsable(false)] public List<EventJunction> mpaEventJunctions { get; set; } = [];

    [Browsable(false)] public List<RaceEventData> mpaEvents { get; set; } = [];

    [Browsable(false)] public List<Rival> mpaRivals { get; set; } = [];

    [Browsable(false)] public List<OpponentBalanceData> mpaAIBalances { get; set; } = [];

    [Browsable(false)] public List<EventRacerPersonality> mpaPersonalities { get; set; } = [];

    [Browsable(false)] public List<TrophyUnlockData> mpaTrophyUnlocks { get; set; } = [];

    [Browsable(false)] public List<CarOpponentSet> mpaCarOpponentSet { get; set; } = [];

    public IEntryEditor GetEditor(BundleEntry entry) => new ProgressionEditor(this, entry);

    public EntryType GetEntryType(BundleEntry entry) => EntryType.ProgressionData;

    // Revision 43 adds padding and mFreeCarForRankUpID
    private static int RankSize(uint version) => version <= 42
        ? 0x64
        : 0x70;

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        using BinaryReader2 reader = new(entry.MakeStream(), entry.Console);
        uint version = reader.ReadUInt32();

        if (version < 42)
            throw new InvalidDataException($"Progression revision {version} isn't supported; revisions 42 to 44 are.");

        bool wide = entry.Platform == BundlePlatform.PCx64 && version >= 44; // Decomp x64 transcodes keep 32-bit offsets
        List<uint> padding = [];
        uint size = reader.ReadUInt32();
        ulong[] offsets = new ulong[9];
        long[] counts = new long[9];

        for (int i = 0; i < 9; i++)
        {
            offsets[i] = wide
                ? reader.ReadUInt64()
                : reader.ReadUInt32();
            counts[i] = i == 4
                ? reader.ReadInt32() // miRivalCount is signed
                : reader.ReadUInt32();

            if (wide && i < 8)
                padding.Add(reader.ReadUInt32());
        }

        ulong firstAllocation = offsets.Where(offset => offset != 0).DefaultIfEmpty(ulong.MaxValue).Min();
        uint? baseJunctions = version >= 44 && (ulong)reader.BaseStream.Position + 4 <= firstAllocation && reader.BaseStream.Position + 4 <= reader.BaseStream.Length
            ? reader.ReadUInt32()
            : null;

        List<PlayerCarID> cars = ReadTable(reader, offsets[0], counts[0], 8, () => new PlayerCarID
        {
            mCarId = reader.ReadUInt64()
        });

        List<ProgressionRankData> ranks = ReadTable(reader, offsets[1], counts[1], RankSize(version), () => ReadRank(reader, version));
        List<(EventJunction Junction, ulong Offline, ulong Online, ulong Bike)> junctions = ReadTable(reader, offsets[2], counts[2], wide
            ? 0x28
            : version >= 44
                ? BikeJunctionSize
                : JunctionSize, () => ReadJunction(reader, wide, version >= 44));
        Dictionary<ulong, RaceEventData> byOffset = [];
        List<(RaceEventData Event, ulong Checkpoints, int Count)> events = ReadTable(reader, offsets[3], counts[3], wide
            ? 0x100
            : EventSize, () =>
        {
            ulong at = (ulong)reader.BaseStream.Position;
            (RaceEventData race, ulong checkpoints, int count) = ReadEvent(reader, wide);
            byOffset[at] = race;
            return (race, checkpoints, count);
        });

        foreach ((RaceEventData race, ulong checkpoints, int count) in events)
            race.mpaCheckpoints = ReadTable(reader, checkpoints, count, CheckpointSize, () => ReadCheckpoint(reader));

        RaceEventData resolve(ulong offset) => offset == 0
            ? null
            : byOffset.TryGetValue(offset, out RaceEventData race)
                ? race
                : throw new InvalidDataException("A progression junction refers to an event outside the event table.");

        foreach ((EventJunction junction, ulong offline, ulong online, ulong bike) in junctions)
        {
            junction.mpOfflineEvent = resolve(offline);
            junction.mpOnlineEvent = resolve(online);
            junction.BikeEvent = resolve(bike);
        }

        List<Rival> rivals = ReadTable(reader, offsets[4], counts[4], RivalSize, () => ReadRival(reader));
        List<OpponentBalanceData> balances = ReadTable(reader, offsets[5], counts[5], BalanceSize, () => ReadBalance(reader));
        List<EventRacerPersonality> personalities = ReadTable(reader, offsets[6], counts[6], PersonalitySize, () => new EventRacerPersonality
        {
            mfMinAggression = reader.ReadSingle(),
            mfMaxAggression = reader.ReadSingle(),
            mfSkill = reader.ReadSingle(),
            mfSpeed = reader.ReadSingle()
        });

        List<TrophyUnlockData> trophies = ReadTable(reader, offsets[7], counts[7], TrophySize, () => new TrophyUnlockData
        {
            muNumberTrophyUnlock = reader.ReadUInt32(),
            mu16UnlockType = reader.ReadUInt16(),
            Padding06 = reader.ReadUInt16(),
            mCarUnlockId = reader.ReadUInt64()
        });

        List<CarOpponentSet> opponents = ReadTable(reader, offsets[8], counts[8], OpponentSetSize, () => new CarOpponentSet
        {
            maCarOpponents = [.. Enumerable.Range(0, 8).Select(_ => new CarOpponent
            {
                mCarId = reader.ReadUInt64(),
                miPersonalityIndex = reader.ReadInt32(),
                Padding0C = reader.ReadUInt32()
            })],
            mPlayerCarId = reader.ReadUInt64(),
            miRank = reader.ReadInt32(),
            miOpponentCount = reader.ReadInt32()
        });

        // Nothing changes until the whole resource has read
        muVersionNumber = version;
        muSize = size;
        Wide = wide;
        BaseEventJunctionCount = baseJunctions;
        PointerPadding = padding;
        mpaPlayerCarIds = cars;
        mpaProgressionRanks = ranks;
        mpaEventJunctions = [.. junctions.Select(j => j.Junction)];
        mpaEvents = [.. events.Select(e => e.Event)];
        mpaRivals = rivals;
        mpaAIBalances = balances;
        mpaPersonalities = personalities;
        mpaTrophyUnlocks = trophies;
        mpaCarOpponentSet = opponents;
        return true;
    }

    private static List<T> ReadTable<T>(BinaryReader2 reader, ulong offset, long count, int stride, Func<T> read)
    {
        if (count == 0)
            return [];

        if (count < 0 || count > int.MaxValue || offset > (ulong)reader.BaseStream.Length ||
            (ulong)count > ((ulong)reader.BaseStream.Length - offset) / (uint)stride)
            throw new InvalidDataException("A progression table lies outside the resource.");

        List<T> items = new((int)count);

        for (long index = 0; index < count; index++)
        {
            reader.BaseStream.Position = (long)offset + index * stride;
            items.Add(read());
        }

        return items;
    }

    private static T[] ReadValues<T>(int count, Func<T> read) => [.. Enumerable.Range(0, count).Select(_ => read())];

    private static ProgressionRankData ReadRank(BinaryReader2 reader, uint version)
    {
        ProgressionRankData rank = new()
        {
            mfTrafficDensityRace = reader.ReadSingle(),
            mfBurningRouteTimeScale = reader.ReadSingle(),
            mfTrafficDensityBurningRoute = reader.ReadSingle(),
            mfTrafficDensityRoadRage = reader.ReadSingle(),
            mfTrafficDensitySurvival = reader.ReadSingle(),
            mfTrafficDensityPursuit = reader.ReadSingle(),
            mfShuntStrengthRace = reader.ReadSingle(),
            mfShuntStrengthRoadRage = reader.ReadSingle(),
            mfShuntStrengthMarkedMan = reader.ReadSingle(),
            mfLargeVehicleProbability = reader.ReadSingle(),
            muId = reader.ReadUInt32(),
            maOvertakingDifficulty = ReadValues(8, reader.ReadSingle),
            mu16MedalThresholdToNextRank = reader.ReadUInt16(),
            mu16EventThresholdToNextRank = reader.ReadUInt16(),
            muRoadRageTakedownTarget = reader.ReadUInt16(),
            muRoadRageTime = reader.ReadUInt16(),
            muRoadRageTimeExtensions = reader.ReadUInt16(),
            muRoadRageExtensionTime = reader.ReadUInt16(),
            muRoadRageDamageLimit = reader.ReadUInt16(),
            muRoadRageTriggerExtension = reader.ReadUInt16(),
            muRaceRivalsNumber = reader.ReadByte(),
            muGauntletRivalsNumber = reader.ReadByte(),
            muRoadRageRivalsNumber = reader.ReadByte(),
            muNumGiftCars = reader.ReadByte(),
            muNumWinsToRankUpRace = reader.ReadByte(),
            muNumWinsToRankUpStunt = reader.ReadByte(),
            muNumWinsToRankUpRoadRage = reader.ReadByte(),
            muNumWinsToRankUpMarkedMan = reader.ReadByte()
        };

        if (version >= 43)
        {
            rank.Padding64 = reader.ReadUInt32();
            rank.mFreeCarForRankUpID = reader.ReadUInt64();
        }

        return rank;
    }

    private static (EventJunction, ulong, ulong, ulong) ReadJunction(BinaryReader2 reader, bool wide, bool bikes)
    {
        EventJunction junction = new()
        {
            muID = reader.ReadUInt32()
        };

        if (wide)
            junction.Padding04 = reader.ReadUInt32();

        ulong pointer() => wide
            ? reader.ReadUInt64()
            : reader.ReadUInt32();

        ulong offline = pointer(), online = pointer();
        ulong bike = bikes
            ? pointer()
            : 0;
        junction.miShotGroup = reader.ReadInt32();

        if (wide)
            junction.Padding24 = reader.ReadUInt32();

        return (junction, offline, online, bike);
    }

    private static (RaceEventData, ulong, int) ReadEvent(BinaryReader2 reader, bool wide)
    {
        RaceEventData race = new()
        {
            muId = reader.ReadUInt32(),
            muFlags = reader.ReadUInt32(),
            mfTrafficDensity = reader.ReadSingle(),
            mfBoostEarning = reader.ReadSingle(),
            mSpecialEventCarId = reader.ReadUInt64()
        };

        ulong checkpoints = wide
            ? reader.ReadUInt64()
            : reader.ReadUInt32();
        int count = reader.ReadInt32();
        race.mfTimeLimitFast = reader.ReadSingle();
        race.mfTimeLimitSlow = reader.ReadSingle();
        race.miRankScore = ReadValues(6, reader.ReadInt32);
        race.mfRankTime = ReadValues(6, reader.ReadSingle);
        race.mfExtensionTime = reader.ReadSingle();
        race.maStartGridSlots = [.. Enumerable.Range(0, 7).Select(_ => new EventStartGridSlot
        {
            muOpponentIndex = reader.ReadUInt32(),
            muPersonalityIndex = reader.ReadUInt32(),
            miFastAIBalanceGraphIndex = reader.ReadInt32(),
            miSlowAIBalanceGraphIndex = reader.ReadInt32(),
            muColourIndex = reader.ReadByte(),
            muFlags = reader.ReadByte(),
            Padding12 = reader.ReadUInt16()
        })];
        race.muStartGridCount = reader.ReadUInt32();
        race.mu8Mode = reader.ReadByte();
        race.mu8OnlineMode = reader.ReadByte();
        race.mu8StartRivalCount = reader.ReadByte();
        race.mu8AddRivalCount = reader.ReadByte();
        race.mu8TakeDownBronze = reader.ReadByte();
        race.mu8TakeDownSilver = reader.ReadByte();
        race.mu8TakeDownGold = reader.ReadByte();
        race.mu8DamageLimit = reader.ReadByte();
        race.mu8ExtensionTimeCount = reader.ReadByte();
        race.mu8AStarType = reader.ReadByte();
        race.mi8UnlockRank = reader.ReadSByte();
        race.Padding = reader.ReadBytes(wide
            ? 5
            : 1);

        if (race.muStartGridCount > 7)
            throw new InvalidDataException("A progression event has more than 7 start-grid slots.");

        return (race, checkpoints, count);
    }

    private static CheckpointData ReadCheckpoint(BinaryReader2 reader)
    {
        CheckpointData checkpoint = new()
        {
            muLandmarkId = reader.ReadUInt32(),
            miBlockSectionCount = reader.ReadInt32(),
            mauBlockSectionIds = ReadValues(8, reader.ReadUInt32)
        };

        if (checkpoint.miBlockSectionCount is < 0 or > 8)
            throw new InvalidDataException("A progression checkpoint has an invalid block-section count.");

        return checkpoint;
    }

    private static Rival ReadRival(BinaryReader2 reader) => new()
    {
        mId = reader.ReadUInt64(),
        mCarId = reader.ReadUInt64(),
        miPersonalityIndex = reader.ReadInt16(),
        miPursuitTarget = reader.ReadInt16(),
        miDistrictIndex = reader.ReadSByte(),
        miUnlockRank = reader.ReadSByte(),
        mu8NumMedalsToUnlock = reader.ReadByte(),
        mbIsUsedForRankUpGiftCar = reader.ReadBoolean(),
        macName = reader.ReadLenString(0x20)
    };

    private static OpponentBalanceData ReadBalance(BinaryReader2 reader)
    {
        OpponentBalanceData balance = new()
        {
            mafAheadGraphPoints = ReadValues(8, reader.ReadSingle),
            mafBehindGraphPoints = ReadValues(8, reader.ReadSingle),

            // The original resource builder left this float little-endian on consoles
            mfCatchUpCutOffRatio = BinaryPrimitives.ReadSingleLittleEndian(reader.ReadBytes(4))
        };

        return balance;
    }

    // Anything that would stop the resource from saving, or that the game rejects on load
    public IEnumerable<TrafficRules.Issue> Problems()
    {
        if (Wide)
            yield return new(this, nameof(Wide), "Saving 64-bit Remastered progression isn't supported.");

        if (muVersionNumber is < 42 or > 44)
            yield return new(this, nameof(muVersionNumber), "Revision must be 42, 43 or 44.");

        HashSet<RaceEventData> events = new(mpaEvents, ReferenceEqualityComparer.Instance);

        foreach (EventJunction junction in mpaEventJunctions)
        {
            foreach ((RaceEventData race, string property) in new[]
            {
                (junction.mpOfflineEvent, nameof(EventJunction.mpOfflineEvent)),
                (junction.mpOnlineEvent, nameof(EventJunction.mpOnlineEvent)),
                (junction.BikeEvent, nameof(EventJunction.BikeEvent))
            })
                if (race != null && !events.Contains(race))
                    yield return new(junction, property, $"Junction {junction.muID} refers to an event that isn't in the event list.");

            if (junction.BikeEvent != null && muVersionNumber < 44)
                yield return new(junction, nameof(EventJunction.BikeEvent), $"Junction {junction.muID} has a bike event, which needs revision 44.");
        }

        foreach (ProgressionRankData rank in mpaProgressionRanks.Where(r => r.maOvertakingDifficulty?.Length != 8))
            yield return new(rank, nameof(ProgressionRankData.maOvertakingDifficulty), $"{rank} needs exactly 8 overtaking difficulties.");

        foreach (RaceEventData race in mpaEvents)
        {
            if (race.maStartGridSlots.Count != 7)
                yield return new(race.maStartGridSlots, null, $"Event {race.muId} needs exactly 7 start-grid slots (unused ones included).");

            if (race.muStartGridCount > 7)
                yield return new(race, nameof(RaceEventData.muStartGridCount), $"Event {race.muId} can use at most 7 start-grid slots.");

            if (race.miRankScore?.Length != 6)
                yield return new(race, nameof(RaceEventData.miRankScore), $"Event {race.muId} needs exactly 6 rank scores.");

            if (race.mfRankTime?.Length != 6)
                yield return new(race, nameof(RaceEventData.mfRankTime), $"Event {race.muId} needs exactly 6 rank times.");

            if (race.Padding?.Length != 1 && !Wide)
                yield return new(race, nameof(RaceEventData.Padding), $"Event {race.muId} padding must be 1 byte.");

            foreach (CheckpointData checkpoint in race.mpaCheckpoints)
            {
                if (checkpoint.mauBlockSectionIds?.Length != 8)
                    yield return new(checkpoint, nameof(CheckpointData.mauBlockSectionIds), $"Event {race.muId} has a checkpoint without exactly 8 block section IDs.");

                if (checkpoint.miBlockSectionCount is < 0 or > 8)
                    yield return new(checkpoint, nameof(CheckpointData.miBlockSectionCount), $"Event {race.muId} has a checkpoint using more than 8 block sections.");
            }
        }

        foreach (Rival rival in mpaRivals.Where(r => (r.macName ?? "").Length > 31 || (r.macName ?? "").Any(c => c > 0xFF)))
            yield return new(rival, nameof(Rival.macName), $"Rival {rival.macName} needs a name of up to 31 Latin-1 characters.");

        foreach (OpponentBalanceData balance in mpaAIBalances.Where(b => b.mafAheadGraphPoints?.Length != 8 || b.mafBehindGraphPoints?.Length != 8))
            yield return new(balance, nameof(OpponentBalanceData.mafAheadGraphPoints), "AI balance graphs need exactly 8 ahead and 8 behind points.");

        foreach (CarOpponentSet set in mpaCarOpponentSet)
        {
            if (set.maCarOpponents.Count != 8)
                yield return new(set.maCarOpponents, null, $"{set} needs exactly 8 opponent slots (unused ones included).");

            if (set.miOpponentCount is < 0 or > 8)
                yield return new(set, nameof(CarOpponentSet.miOpponentCount), $"{set} can use at most 8 opponents.");
        }
    }

    // Lays the resource out as the original builder did: header, the nine tables in order, then each event's
    // checkpoints in event order, every table and checkpoint array 16-byte aligned. Unchanged data saves unchanged.
    public byte[] Serialize(bool bigEndian)
    {
        if (Problems().FirstOrDefault() is TrafficRules.Issue issue)
            throw new InvalidOperationException(issue.Message);

        uint version = muVersionNumber;
        int rankSize = RankSize(version);
        int junctionSize = version >= 44
            ? BikeJunctionSize
            : JunctionSize;
        long cursor = HeaderSize + (BaseEventJunctionCount.HasValue
            ? 4
            : 0);

        long place(int count, int stride)
        {
            cursor = (cursor + 15) & ~15L;
            long at = cursor;
            cursor += (long)count * stride;
            return at;
        }

        long[] tables =
        [
            place(mpaPlayerCarIds.Count, 8),
            place(mpaProgressionRanks.Count, rankSize),
            place(mpaEventJunctions.Count, junctionSize),
            place(mpaEvents.Count, EventSize),
            place(mpaRivals.Count, RivalSize),
            place(mpaAIBalances.Count, BalanceSize),
            place(mpaPersonalities.Count, PersonalitySize),
            place(mpaTrophyUnlocks.Count, TrophySize),
            place(mpaCarOpponentSet.Count, OpponentSetSize)
        ];
        long[] checkpoints = [.. mpaEvents.Select(race => place(race.mpaCheckpoints.Count, CheckpointSize))];
        long size = cursor;
        Dictionary<RaceEventData, long> eventAt = new(ReferenceEqualityComparer.Instance);

        for (int i = 0; i < mpaEvents.Count; i++)
            eventAt[mpaEvents[i]] = tables[3] + (long)i * EventSize;

        using MemoryStream stream = new();
        using BinaryWriter2 writer = new(stream, bigEndian);
        writer.Write(version);
        writer.Write((uint)size);
        int[] counts = [mpaPlayerCarIds.Count, mpaProgressionRanks.Count, mpaEventJunctions.Count, mpaEvents.Count, mpaRivals.Count,
            mpaAIBalances.Count, mpaPersonalities.Count, mpaTrophyUnlocks.Count, mpaCarOpponentSet.Count];

        for (int i = 0; i < 9; i++)
        {
            writer.Write((uint)tables[i]);
            writer.Write((uint)counts[i]);
        }

        if (BaseEventJunctionCount is uint baseJunctions)
            writer.Write(baseJunctions);

        void at(long position)
        {
            writer.Align(16);

            if (stream.Position != position)
                throw new InvalidOperationException("Progression layout mismatch.");
        }

        uint pointer(RaceEventData race) => race == null
            ? 0
            : (uint)eventAt[race];

        at(tables[0]);

        foreach (PlayerCarID car in mpaPlayerCarIds)
            writer.Write(car.mCarId);

        at(tables[1]);

        foreach (ProgressionRankData rank in mpaProgressionRanks)
        {
            foreach (float value in (ReadOnlySpan<float>)[rank.mfTrafficDensityRace, rank.mfBurningRouteTimeScale, rank.mfTrafficDensityBurningRoute,
                rank.mfTrafficDensityRoadRage, rank.mfTrafficDensitySurvival, rank.mfTrafficDensityPursuit, rank.mfShuntStrengthRace,
                rank.mfShuntStrengthRoadRage, rank.mfShuntStrengthMarkedMan, rank.mfLargeVehicleProbability])
                writer.Write(value);

            writer.Write(rank.muId);

            foreach (float value in rank.maOvertakingDifficulty)
                writer.Write(value);

            foreach (ushort value in (ReadOnlySpan<ushort>)[rank.mu16MedalThresholdToNextRank, rank.mu16EventThresholdToNextRank, rank.muRoadRageTakedownTarget,
                rank.muRoadRageTime, rank.muRoadRageTimeExtensions, rank.muRoadRageExtensionTime, rank.muRoadRageDamageLimit, rank.muRoadRageTriggerExtension])
                writer.Write(value);

            writer.Write([rank.muRaceRivalsNumber, rank.muGauntletRivalsNumber, rank.muRoadRageRivalsNumber, rank.muNumGiftCars,
                rank.muNumWinsToRankUpRace, rank.muNumWinsToRankUpStunt, rank.muNumWinsToRankUpRoadRage, rank.muNumWinsToRankUpMarkedMan]);

            if (version >= 43)
            {
                writer.Write(rank.Padding64);
                writer.Write(rank.mFreeCarForRankUpID);
            }
        }

        at(tables[2]);

        foreach (EventJunction junction in mpaEventJunctions)
        {
            writer.Write(junction.muID);
            writer.Write(pointer(junction.mpOfflineEvent));
            writer.Write(pointer(junction.mpOnlineEvent));

            if (version >= 44)
                writer.Write(pointer(junction.BikeEvent));

            writer.Write(junction.miShotGroup);
        }

        at(tables[3]);

        for (int i = 0; i < mpaEvents.Count; i++)
        {
            RaceEventData race = mpaEvents[i];
            writer.Write(race.muId);
            writer.Write(race.muFlags);
            writer.Write(race.mfTrafficDensity);
            writer.Write(race.mfBoostEarning);
            writer.Write(race.mSpecialEventCarId);
            writer.Write((uint)checkpoints[i]);
            writer.Write(race.mpaCheckpoints.Count);
            writer.Write(race.mfTimeLimitFast);
            writer.Write(race.mfTimeLimitSlow);

            foreach (int score in race.miRankScore)
                writer.Write(score);

            foreach (float time in race.mfRankTime)
                writer.Write(time);

            writer.Write(race.mfExtensionTime);

            foreach (EventStartGridSlot slot in race.maStartGridSlots)
            {
                writer.Write(slot.muOpponentIndex);
                writer.Write(slot.muPersonalityIndex);
                writer.Write(slot.miFastAIBalanceGraphIndex);
                writer.Write(slot.miSlowAIBalanceGraphIndex);
                writer.Write(slot.muColourIndex);
                writer.Write(slot.muFlags);
                writer.Write(slot.Padding12);
            }

            writer.Write(race.muStartGridCount);
            writer.Write([race.mu8Mode, race.mu8OnlineMode, race.mu8StartRivalCount, race.mu8AddRivalCount, race.mu8TakeDownBronze,
                race.mu8TakeDownSilver, race.mu8TakeDownGold, race.mu8DamageLimit, race.mu8ExtensionTimeCount, race.mu8AStarType]);
            writer.Write(race.mi8UnlockRank);
            writer.Write(race.Padding);
        }

        at(tables[4]);

        foreach (Rival rival in mpaRivals)
        {
            writer.Write(rival.mId);
            writer.Write(rival.mCarId);
            writer.Write(rival.miPersonalityIndex);
            writer.Write(rival.miPursuitTarget);
            writer.Write(rival.miDistrictIndex);
            writer.Write(rival.miUnlockRank);
            writer.Write(rival.mu8NumMedalsToUnlock);
            writer.Write(rival.mbIsUsedForRankUpGiftCar);
            writer.WriteLenString(rival.macName ?? "", 0x20);
        }

        at(tables[5]);

        foreach (OpponentBalanceData balance in mpaAIBalances)
        {
            foreach (float value in balance.mafAheadGraphPoints.Concat(balance.mafBehindGraphPoints))
                writer.Write(value);

            byte[] cutoff = new byte[4];
            BinaryPrimitives.WriteSingleLittleEndian(cutoff, balance.mfCatchUpCutOffRatio);
            writer.Write(cutoff);
        }

        at(tables[6]);

        foreach (EventRacerPersonality personality in mpaPersonalities)
        {
            writer.Write(personality.mfMinAggression);
            writer.Write(personality.mfMaxAggression);
            writer.Write(personality.mfSkill);
            writer.Write(personality.mfSpeed);
        }

        at(tables[7]);

        foreach (TrophyUnlockData trophy in mpaTrophyUnlocks)
        {
            writer.Write(trophy.muNumberTrophyUnlock);
            writer.Write(trophy.mu16UnlockType);
            writer.Write(trophy.Padding06);
            writer.Write(trophy.mCarUnlockId);
        }

        at(tables[8]);

        foreach (CarOpponentSet set in mpaCarOpponentSet)
        {
            foreach (CarOpponent opponent in set.maCarOpponents)
            {
                writer.Write(opponent.mCarId);
                writer.Write(opponent.miPersonalityIndex);
                writer.Write(opponent.Padding0C);
            }

            writer.Write(set.mPlayerCarId);
            writer.Write(set.miRank);
            writer.Write(set.miOpponentCount);
        }

        for (int i = 0; i < mpaEvents.Count; i++)
        {
            at(checkpoints[i]);

            foreach (CheckpointData checkpoint in mpaEvents[i].mpaCheckpoints)
            {
                writer.Write(checkpoint.muLandmarkId);
                writer.Write(checkpoint.miBlockSectionCount);

                foreach (uint id in checkpoint.mauBlockSectionIds)
                    writer.Write(id);
            }
        }

        if (stream.Position != size)
            throw new InvalidOperationException("Progression layout mismatch.");

        writer.Align(16);
        return stream.ToArray();
    }

    public bool Write(BundleEntry entry)
    {
        if (Wide)
            return false;

        byte[] bytes = Serialize(entry.Console);
        entry.SetPrimary(bytes, entry.GetImports());
        muSize = entry.Console
            ? BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(4))
            : BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
        return true;
    }
}

public sealed class ProgressionEditor : ResourceEditor
{
    private readonly ProgressionData data;

    public ProgressionEditor(ProgressionData source, BundleEntry initialEntry) : base(initialEntry, "Progression", "progression")
    {
        data = (ProgressionData)ObjectTreeEditor.Clone(source);
        ProgressionData previous = TrafficReferences.Progression;
        TrafficReferences.Progression = data;
        Disposed += (_, _) => TrafficReferences.Progression = previous;
        TrafficLabels.Install();
        Initialize(data, "ProgressionData", null);
        Tree.SearchPrompt = "Search event, junction, rank and rival IDs or names";
        Tree.CollectionLimit = (_, list) => list switch
        {
            nameof(RaceEventData.maStartGridSlots) => 7,
            nameof(CarOpponentSet.maCarOpponents) => 8,
            _ => int.MaxValue
        };
    }

    protected override List<TrafficRules.Issue> FindIssues() => [.. data.Problems()];

    protected override void WriteResource() => data.Write(ResourceEntry);
}
