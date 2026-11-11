using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

public struct ProgressionRankData
{
    public float mfTrafficDensityRace;
    public float mfBurningRouteTimeScale;
    public float mfTrafficDensityBurningRoute;
    public float mfTrafficDensityRoadRage;
    public float mfTrafficDensitySurvival;
    public float mfTrafficDensityPursuit;
    public float mfShuntStrengthRace;
    public float mfShuntStrengthRoadRage;
    public float mfShuntStrengthMarkedMan;
    public float mfLargeVehicleProbability;
    public uint muId; // 0x28
    public float[] maOvertakingDifficulty; // 0x2C, 8 entries
    public ushort mu16MedalThresholdToNextRank; // 0x4C
    public ushort mu16EventThresholdToNextRank;
    public ushort muRoadRageTakedownTarget;
    public ushort muRoadRageTime;
    public ushort muRoadRageTimeExtensions;
    public ushort muRoadRageExtensionTime;
    public ushort muRoadRageDamageLimit;
    public ushort muRoadRageTriggerExtension;
    public byte muRaceRivalsNumber; // 0x5C
    public byte muGauntletRivalsNumber;
    public byte muRoadRageRivalsNumber;
    public byte muNumGiftCars;
    public byte muNumWinsToRankUpRace;
    public byte muNumWinsToRankUpStunt;
    public byte muNumWinsToRankUpRoadRage;
    public byte muNumWinsToRankUpMarkedMan;
    public uint Padding64;
    public EncryptedString mFreeCarForRankUpID; // 0x68

    public override string ToString() => $"Rank {muId}";
}

public struct EventJunction
{
    public uint muID;
    public uint Padding04; // 64-bit resources only.
    public ulong OfflineEventOffset;
    public RaceEventData mpOfflineEvent;
    public ulong OnlineEventOffset;
    public RaceEventData mpOnlineEvent;
    public ulong BikeEventOffset;
    public RaceEventData BikeEvent;
    public int miShotGroup;
    public uint Padding24; // 64-bit resources only

    public override string ToString() => $"ID: {muID}";
}

public struct RaceEventData
{
    public uint muId;
    public uint muFlags;
    public float mfTrafficDensity;
    public float mfBoostEarning;
    public EncryptedString mSpecialEventCarId;
    public ulong CheckpointsOffset;
    public CheckpointData[] mpaCheckpoints;
    public int miCheckpointCount;
    public float mfTimeLimitFast;
    public float mfTimeLimitSlow;
    public int[] miRankScore; // 6 entries
    public float[] mfRankTime; // 6 entries
    public float mfExtensionTime;
    public EventStartGridSlot[] maStartGridSlots; // All 7 stored slots
    public uint muStartGridCount;
    public byte mu8Mode;
    public byte mu8OnlineMode;
    public byte mu8StartRivalCount;
    public byte mu8AddRivalCount;
    public byte mu8TakeDownBronze;
    public byte mu8TakeDownSilver;
    public byte mu8TakeDownGold;
    public byte mu8DamageLimit;
    public byte mu8ExtensionTimeCount;
    public byte mu8AStarType;
    public sbyte mi8UnlockRank;
    public byte[] Padding; // 1 byte in 32-bit resources, 5 bytes in 64-bit resources

    public override string ToString() => $"ID: {muId}";
}

public struct EventStartGridSlot
{
    public uint muOpponentIndex;
    public uint muPersonalityIndex;
    public int miFastAIBalanceGraphIndex;
    public int miSlowAIBalanceGraphIndex;
    public byte muColourIndex;
    public byte muFlags;
    public ushort Padding12;
}

public struct CheckpointData
{
    public uint muLandmarkId;
    public int miBlockSectionCount;
    public uint[] mauBlockSectionIds; // All 8 stored IDs

    public override string ToString() => $"Landmark {muLandmarkId} ({miBlockSectionCount} block sections)";
}

public struct Rival
{
    public EncryptedString mId;
    public EncryptedString mCarId;
    public short miPersonalityIndex;
    public short miPursuitTarget;
    public sbyte miDistrictIndex;
    public sbyte miUnlockRank;
    public byte mu8NumMedalsToUnlock;
    public bool mbIsUsedForRankUpGiftCar;
    public string macName;

    public override string ToString() => $"ID {mId}: {macName} ({mCarId})";
}

public struct OpponentBalanceData
{
    public float[] mafAheadGraphPoints; // 8 entries
    public float[] mafBehindGraphPoints; // 8 entries
    public float mfCatchUpCutOffRatio;
}

public struct EventRacerPersonality
{
    public float mfMinAggression;
    public float mfMaxAggression;
    public float mfSkill;
    public float mfSpeed;
}

public struct TrophyUnlockData
{
    public uint muNumberTrophyUnlock;
    public ushort mu16UnlockType;
    public ushort Padding06;
    public EncryptedString mCarUnlockId;

    public override string ToString() => $"Vehicle {mCarUnlockId}";
}

public struct CarOpponent
{
    public EncryptedString mCarId;
    public int miPersonalityIndex;
    public uint Padding0C;
}

public struct CarOpponentSet
{
    public CarOpponent[] maCarOpponents; // All 8 slots precede the player car ID
    public EncryptedString mPlayerCarId; // 0x80
    public int miRank; // 0x88
    public int miOpponentCount; // 0x8C

    public override string ToString() => $"Player vehicle {mPlayerCarId}";
}

public class ProgressionData : IEntryData
{
    public uint muVersionNumber;
    public uint muSize;
    public ulong PlayerCarIDsOffset;
    public uint muPlayerCarIdCount;
    public ulong ProgressionRanksOffset;
    public uint muProgressionRankCount;
    public ulong EventJunctionsOffset;
    public uint muEventJunctionCount;
    public ulong EventsOffset;
    public uint muEventCount;
    public ulong RivalsOffset;
    public int miRivalCount;
    public ulong AIBalancesOffset;
    public uint muAIBalanceCount;
    public ulong PersonalitiesOffset;
    public uint muPersonalityCount;
    public ulong TrophyUnlocksOffset;
    public uint muTrophyUnlockCount;
    public ulong CarOpponentSetOffset;
    public uint muCarOpponentsCount;
    public uint? BaseEventJunctionCount;
    public List<uint> PointerPadding = [];
    public List<EncryptedString> mpaPlayerCarIds = [];
    public List<ProgressionRankData> mpaProgressionRanks = [];
    public List<EventJunction> mpaEventJunctions = [];
    public SortedDictionary<ulong, RaceEventData> mpaEvents = [];
    public List<Rival> mpaRivals = [];
    public List<OpponentBalanceData> mpaAIBalances = [];
    public List<EventRacerPersonality> mpaPersonalities = [];
    public List<TrophyUnlockData> mpaTrophyUnlocks = [];
    public List<CarOpponentSet> mpaCarOpponentSet = [];

    public IEntryEditor GetEditor(BundleEntry entry) => null;

    public EntryType GetEntryType(BundleEntry entry) => EntryType.ProgressionData;

    private void Clear()
    {
        BaseEventJunctionCount = null;
        PointerPadding.Clear();
        mpaPlayerCarIds.Clear();
        mpaProgressionRanks.Clear();
        mpaEventJunctions.Clear();
        mpaEvents.Clear();
        mpaRivals.Clear();
        mpaAIBalances.Clear();
        mpaPersonalities.Clear();
        mpaTrophyUnlocks.Clear();
        mpaCarOpponentSet.Clear();
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        Clear();
        using BinaryReader2 reader = new(entry.MakeStream(), entry.Console);
        muVersionNumber = reader.ReadUInt32();
        muSize = reader.ReadUInt32();

        // Decomp retains original 32-bit offsets
        bool wide = entry.Platform == BundlePlatform.PCx64 && muVersionNumber >= 44;
        PlayerCarIDsOffset = ReadPointer(reader, wide);
        muPlayerCarIdCount = ReadCount(reader, wide);
        ProgressionRanksOffset = ReadPointer(reader, wide);
        muProgressionRankCount = ReadCount(reader, wide);
        EventJunctionsOffset = ReadPointer(reader, wide);
        muEventJunctionCount = ReadCount(reader, wide);
        EventsOffset = ReadPointer(reader, wide);
        muEventCount = ReadCount(reader, wide);
        RivalsOffset = ReadPointer(reader, wide);
        miRivalCount = reader.ReadInt32();

        if (wide)
            PointerPadding.Add(reader.ReadUInt32());

        AIBalancesOffset = ReadPointer(reader, wide);
        muAIBalanceCount = ReadCount(reader, wide);
        PersonalitiesOffset = ReadPointer(reader, wide);
        muPersonalityCount = ReadCount(reader, wide);
        TrophyUnlocksOffset = ReadPointer(reader, wide);
        muTrophyUnlockCount = ReadCount(reader, wide);
        CarOpponentSetOffset = ReadPointer(reader, wide);
        muCarOpponentsCount = reader.ReadUInt32();

        ulong[] tableOffsets =
        [
            PlayerCarIDsOffset,
            ProgressionRanksOffset,
            EventJunctionsOffset,
            EventsOffset,
            RivalsOffset,
            AIBalancesOffset,
            PersonalitiesOffset,
            TrophyUnlocksOffset,
            CarOpponentSetOffset
        ];
        ulong firstAllocation = tableOffsets.Where(offset => offset != 0).DefaultIfEmpty(ulong.MaxValue).Min();

        if (muVersionNumber >= 44 && (ulong)reader.BaseStream.Position + 4 <= firstAllocation && reader.BaseStream.Position + 4 <= reader.BaseStream.Length)
            BaseEventJunctionCount = reader.ReadUInt32();

        ReadTable(reader, PlayerCarIDsOffset, muPlayerCarIdCount, 8, () => mpaPlayerCarIds.Add(new(reader.ReadUInt64())));
        ReadTable(reader, ProgressionRanksOffset, muProgressionRankCount, 0x70, () => mpaProgressionRanks.Add(ReadRank(reader)));
        ReadTable(reader, EventJunctionsOffset, muEventJunctionCount, wide
            ? 0x28
            : muVersionNumber >= 44
                ? 0x14
                : 0x10, () => mpaEventJunctions.Add(ReadJunction(reader, wide, muVersionNumber >= 44)));
        ReadTable(reader, EventsOffset, muEventCount, wide
            ? 0x100
            : 0xF8, () =>
        {
            ulong offset = (ulong)reader.BaseStream.Position;
            mpaEvents.Add(offset, ReadEvent(reader, wide));
        });

        foreach (ulong offset in mpaEvents.Keys.ToArray())
        {
            RaceEventData current = mpaEvents[offset];
            List<CheckpointData> checkpoints = [];
            ReadTable(reader, current.CheckpointsOffset, current.miCheckpointCount, 0x28, () => checkpoints.Add(ReadCheckpoint(reader)));
            current.mpaCheckpoints = [.. checkpoints];
            mpaEvents[offset] = current;
        }

        for (int index = 0; index < mpaEventJunctions.Count; index++)
        {
            EventJunction junction = mpaEventJunctions[index];
            junction.mpOfflineEvent = ResolveEvent(junction.OfflineEventOffset);
            junction.mpOnlineEvent = ResolveEvent(junction.OnlineEventOffset);
            junction.BikeEvent = ResolveEvent(junction.BikeEventOffset);
            mpaEventJunctions[index] = junction;
        }

        ReadTable(reader, RivalsOffset, miRivalCount, 0x38, () => mpaRivals.Add(ReadRival(reader)));
        ReadTable(reader, AIBalancesOffset, muAIBalanceCount, 0x44, () => mpaAIBalances.Add(ReadBalance(reader)));
        ReadTable(reader, PersonalitiesOffset, muPersonalityCount, 0x10, () => mpaPersonalities.Add(new()
        {
            mfMinAggression = reader.ReadSingle(),
            mfMaxAggression = reader.ReadSingle(),
            mfSkill = reader.ReadSingle(),
            mfSpeed = reader.ReadSingle()
        }));
        ReadTable(reader, TrophyUnlocksOffset, muTrophyUnlockCount, 0x10, () => mpaTrophyUnlocks.Add(new()
        {
            muNumberTrophyUnlock = reader.ReadUInt32(),
            mu16UnlockType = reader.ReadUInt16(),
            Padding06 = reader.ReadUInt16(),
            mCarUnlockId = new(reader.ReadUInt64())
        }));
        ReadTable(reader, CarOpponentSetOffset, muCarOpponentsCount, 0x90, () => mpaCarOpponentSet.Add(new()
        {
            maCarOpponents = ReadValues(8, () => new CarOpponent()
            {
                mCarId = new(reader.ReadUInt64()),
                miPersonalityIndex = reader.ReadInt32(),
                Padding0C = reader.ReadUInt32()
            }),
            mPlayerCarId = new(reader.ReadUInt64()),
            miRank = reader.ReadInt32(),
            miOpponentCount = reader.ReadInt32()
        }));
        return true;
    }

    private uint ReadCount(BinaryReader2 reader, bool wide)
    {
        uint count = reader.ReadUInt32();

        if (wide)
            PointerPadding.Add(reader.ReadUInt32());

        return count;
    }

    private static ulong ReadPointer(BinaryReader2 reader, bool wide) => wide
        ? reader.ReadUInt64()
        : reader.ReadUInt32();

    private static void ReadTable(BinaryReader2 reader, ulong offset, long count, int stride, Action read)
    {
        if (count == 0)
            return;

        if (count < 0 || count > int.MaxValue || offset > (ulong)reader.BaseStream.Length ||
            (ulong)count > ((ulong)reader.BaseStream.Length - offset) / (uint)stride)
            throw new InvalidDataException("A progression table lies outside the resource.");

        reader.BaseStream.Position = (long)offset;

        for (int index = 0; index < count; index++)
            read();
    }

    private static T[] ReadValues<T>(int count, Func<T> read)
    {
        T[] values = new T[count];

        for (int index = 0; index < count; index++)
            values[index] = read();

        return values;
    }

    private static ProgressionRankData ReadRank(BinaryReader2 reader) => new()
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
        muNumWinsToRankUpMarkedMan = reader.ReadByte(),
        Padding64 = reader.ReadUInt32(),
        mFreeCarForRankUpID = new(reader.ReadUInt64())
    };

    private static EventJunction ReadJunction(BinaryReader2 reader, bool wide, bool bikes)
    {
        EventJunction junction = new()
        {
            muID = reader.ReadUInt32()
        };

        if (wide)
            junction.Padding04 = reader.ReadUInt32();

        junction.OfflineEventOffset = ReadPointer(reader, wide);
        junction.OnlineEventOffset = ReadPointer(reader, wide);
        junction.BikeEventOffset = bikes
            ? ReadPointer(reader, wide)
            : 0;
        junction.miShotGroup = reader.ReadInt32();

        if (wide)
            junction.Padding24 = reader.ReadUInt32();

        return junction;
    }

    private static RaceEventData ReadEvent(BinaryReader2 reader, bool wide)
    {
        RaceEventData current = new()
        {
            muId = reader.ReadUInt32(),
            muFlags = reader.ReadUInt32(),
            mfTrafficDensity = reader.ReadSingle(),
            mfBoostEarning = reader.ReadSingle(),
            mSpecialEventCarId = new(reader.ReadUInt64()),
            CheckpointsOffset = ReadPointer(reader, wide),
            miCheckpointCount = reader.ReadInt32(),
            mfTimeLimitFast = reader.ReadSingle(),
            mfTimeLimitSlow = reader.ReadSingle(),
            miRankScore = ReadValues(6, reader.ReadInt32),
            mfRankTime = ReadValues(6, reader.ReadSingle),
            mfExtensionTime = reader.ReadSingle(),
            maStartGridSlots = ReadValues(7, () => new EventStartGridSlot()
            {
                muOpponentIndex = reader.ReadUInt32(),
                muPersonalityIndex = reader.ReadUInt32(),
                miFastAIBalanceGraphIndex = reader.ReadInt32(),
                miSlowAIBalanceGraphIndex = reader.ReadInt32(),
                muColourIndex = reader.ReadByte(),
                muFlags = reader.ReadByte(),
                Padding12 = reader.ReadUInt16()
            }),
            muStartGridCount = reader.ReadUInt32(),
            mu8Mode = reader.ReadByte(),
            mu8OnlineMode = reader.ReadByte(),
            mu8StartRivalCount = reader.ReadByte(),
            mu8AddRivalCount = reader.ReadByte(),
            mu8TakeDownBronze = reader.ReadByte(),
            mu8TakeDownSilver = reader.ReadByte(),
            mu8TakeDownGold = reader.ReadByte(),
            mu8DamageLimit = reader.ReadByte(),
            mu8ExtensionTimeCount = reader.ReadByte(),
            mu8AStarType = reader.ReadByte(),
            mi8UnlockRank = reader.ReadSByte(),
            Padding = reader.ReadBytes(wide
                ? 5
                : 1)
        };

        if (current.muStartGridCount > 7)
            throw new InvalidDataException("A progression event has more than 7 start-grid slots.");

        return current;
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

    private RaceEventData ResolveEvent(ulong offset)
    {
        if (offset == 0)
            return default;

        return mpaEvents.TryGetValue(offset, out RaceEventData current)
            ? current
            : throw new InvalidDataException("A progression junction refers to an event outside the event table.");
    }

    private static Rival ReadRival(BinaryReader2 reader) => new()
    {
        mId = new(reader.ReadUInt64()),
        mCarId = new(reader.ReadUInt64()),
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
            mafBehindGraphPoints = ReadValues(8, reader.ReadSingle)
        };

        // The original resource builder left this float little-endian on consoles
        byte[] cutoff = new byte[4];
        reader.BaseStream.ReadExactly(cutoff);
        balance.mfCatchUpCutOffRatio = BinaryPrimitives.ReadSingleLittleEndian(cutoff);
        return balance;
    }

    public bool Write(BundleEntry entry) => false;
}
