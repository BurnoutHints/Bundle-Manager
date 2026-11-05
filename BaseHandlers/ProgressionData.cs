using System;
using System.Collections.Generic;
using System.IO;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

// This data would include:
// - Race Rivals Number
// - Large Vehicle Probability
// - Traffic Density Race
// - Traffic Density Road Rage
// - Traffic Density Burning Route
// - Traffic Density Survival (Marked Man)
public struct LicenceData
{
    public float Unknown00;
    public float Unknown04;
    public float Unknown08; // 1
    public float Unknown0C;
    public float Unknown10;
    public float Unknown14;
    public float Unknown18; // 1
    public float Unknown1C; // 1
    public float Unknown20; // 1
    public float Unknown24;
    public int GameDBID; // for what?
    public float Unknown2C;
    public float Unknown30;
    public float Unknown34;
    public float Unknown38;
    public float Unknown3C;
    public float Unknown40;
    public float Unknown44;
    public float Unknown48;
    public short NumEventWinsRequired;
    public short Unknown4E; // 0
    public short Unknown50;
    public short Unknown52; // default SR time limits?
    public short Unknown54; // 5
    public short Unknown56; // 10 (previously 5 pre-1.6)
    public short Unknown58; // 5
    public short Unknown5A; // 5
    public byte Unknown5C; // 7
    public byte Unknown5D; // 0
    public byte Unknown5E; // 7
    public byte Unknown5F; // 0
    public byte Unknown60;
    public byte Unknown61;
    public byte Unknown62;
    public byte Unknown63;

    // 4 bytes padding
    public EncryptedString VehicleUnlocked;
}

public struct EventJunction
{
    public int GameDBID; // to identify the event (used in save files)
    public uint CarEventDataOffset;
    public EventData CarEventData;
    public uint BTTEventDataOffset;
    public EventData BTTEventData;
    public uint BikeEventDataOffset;
    public EventData BikeEventData;
    public int UnknownGameDBID;

    public override string ToString() => $"ID: {GameDBID}";
}

public struct EventData
{
    public int GameDBID; // not used in save
    public int AppearsWhen; // 0 = cars/daytime bikes, 3 = midnight bights, 4 = island
    public float TrafficDensityMultiplier;
    public float Unknown0C; // 1
    public EncryptedString LockVehicleID; // for Burning Routes but can apply to other events
    public uint EventLandmarkDataOffset;
    public EventLandmarkData[] EventLandmarkData;
    public int EventLandmarkCount;
    public float TargetTimeUnused; // 20 for marked man but never read
    public float TargetTime; // 40 for marked man but never read
    public int StuntRunTargetScoreTier1;
    public int StuntRunTargetScoreTier2;
    public int StuntRunTargetScoreTier3;
    public int StuntRunTargetScoreTier4;
    public int StuntRunTargetScoreTier5;
    public int StuntRunTargetScoreTier6;
    public float StuntRunTimeLimitTier1;
    public float StuntRunTimeLimitTier2;
    public float StuntRunTimeLimitTier3;
    public float StuntRunTimeLimitTier4;
    public float StuntRunTimeLimitTier5;
    public float StuntRunTimeLimitTier6;
    public int Unknown58; // 0
    public EventRivalData[] RivalData;
    public int RivalDataCount; // 7 for non-races, despite not really being necessary?
    public byte Type; // 0 = Race, 1 = Road Rage, 2 = Stunt Run, 3 = Marked Man, 4 = Burning Route (and bike events and island tour), 5 = Race but with the Marked Man map icon
    public byte UnknownED; // 0, 2 (0 for non-races)
    public byte NumberOfAIRivalsToUse; // 0 for non-races
    public byte UnknownEF; // 0
    public int UnknownF0; // 0
    public byte UnknownF4; // 0
    public byte UnknownF5; // 0, 1, 2 (0 for RR & SR)

    // 2 byte padding
    public override string ToString() => $"ID: {GameDBID}";
}

public struct EventRivalData
{
    public int Unknown00;
    public int Unknown04;
    public int Unknown08;
    public int Unknown0C;
    public byte Unknown10;
    public byte Unknown11;
    public byte Unknown12; // garbage
    public byte Unknown13; // garbage
}

public struct EventLandmarkData
{
    public int TriggerID;
    public int NumAIGameDBIDs;
    public int[] AIGameDBIDs;

    public override string ToString() => $"Trigger {TriggerID} ({NumAIGameDBIDs} AI IDs)";
}

public struct Rival
{
    public long RivalID;
    public EncryptedString VehicleID;
    public short Unknown10;
    public short Unknown12; // 1
    public byte Unknown14;
    public byte Unknown15; // 7F
    public byte WinsUntilRoam;
    public bool DoesNotRoam; // Alternatively: unlocked via licence upgrade
    public string Name;

    public override string ToString() => $"ID {RivalID}: {Name} ({VehicleID})";
}

public struct ProgressionSection6Entry
{
    public float Unknown00;
    public float Unknown04;
    public float Unknown08;
    public float Unknown0C;
    public float Unknown10;
    public float Unknown14;
    public float Unknown18;
    public float Unknown1C;
    public float Unknown20;
    public float Unknown24;
    public float Unknown28;
    public float Unknown2C;
    public float Unknown30;
    public float Unknown34;
    public float Unknown38;
    public float Unknown3C;
    public byte Unknown40; // CD
    public byte Unknown41; // CC
    public byte Unknown42; // 4C
    public byte Unknown43; // 3F
}

public struct ProgressionSection7Entry
{
    public float Unknown00;
    public float Unknown04;
    public float Unknown08;
    public float Unknown0C;
}

public struct CarbonCarUnlockData
{
    public int Unknown00; // 0
    public short Unknown04;
    public byte Unknown06; // 0 in remaster, prob garbage
    public byte Unknown07; // 0 in remaster, prob garbage
    public EncryptedString VehicleID;

    public override string ToString() => $"Vehicle {VehicleID}";
}

public struct PlayerOpponentsData
{
    public EncryptedString PlayerVehicleID;
    public int Unknown08;
    public int Unknown0C; // 0
    public EncryptedString Opponent1VehicleID;
    public int Unknown18;
    public int Unknown1C; // 0
    public EncryptedString Opponent2VehicleID;
    public int Unknown28;
    public int Unknown2C; // 0
    public EncryptedString Opponent3VehicleID;
    public int Unknown38;
    public int Unknown3C; // 0
    public EncryptedString Opponent4VehicleID;
    public int Unknown48;
    public int Unknown4C; // 0
    public EncryptedString Opponent5VehicleID;
    public int Unknown58;
    public int Unknown5C; // 0
    public EncryptedString Opponent6VehicleID;
    public int Unknown68;
    public int Unknown6C; // 0
    public EncryptedString Opponent7VehicleID;
    public int Unknown78;
    public int Unknown7C; // 0
    public EncryptedString Opponent8VehicleID;
    public int Unknown88; // 0, 2, 4
    public int NumOpponents;

    public override string ToString() => $"Player Vehicle ID {PlayerVehicleID}";
}

public class ProgressionData : IEntryData
{
    public int FormatRevision;
    public uint FileSize;
    public uint InitialCarIDListOffset;
    public int InitialCarIDCount;
    public uint LicenceDataOffset;
    public int LicenceDataCount;
    public uint EventJunctionOffset;
    public int EventJunctionCount;
    public uint EventDataOffset;
    public int EventDataCount;
    public uint RivalOffset;
    public int RivalCount;
    public uint Section6Offset;
    public int Section6Count;
    public uint Section7Offset;
    public int Section7Count;
    public uint CarbonCarUnlockDataOffset;
    public int CarbonCarUnlockDataCount;
    public uint PlayerOpponentsDataOffset;
    public int PlayerOpponentsDataCount;

    public List<EncryptedString> InitialCarIDEntries;
    public List<LicenceData> LicenceDataEntries;
    public List<EventJunction> EventJunctionEntries;
    public SortedDictionary<uint, EventData> EventDataEntries;
    public List<Rival> RivalEntries;
    public List<ProgressionSection6Entry> Section6Entries;
    public List<ProgressionSection7Entry> Section7Entries;
    public List<CarbonCarUnlockData> CarbonCarUnlockDataEntries;
    public List<PlayerOpponentsData> PlayerOpponentsDataEntries;

    public ProgressionData()
    {
        InitialCarIDEntries = [];
        LicenceDataEntries = [];
        EventJunctionEntries = [];
        EventDataEntries = [];
        RivalEntries = [];
        Section6Entries = [];
        Section7Entries = [];
        CarbonCarUnlockDataEntries = [];
        PlayerOpponentsDataEntries = [];
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        return null;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.ProgressionData;
    }

    private void Clear()
    {

    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        Clear();

        MemoryStream ms = entry.MakeStream();
        BinaryReader2 br = new(ms)
        {
            BigEndian = entry.Console
        };

        FormatRevision = br.ReadInt32();
        FileSize = br.ReadUInt32();
        InitialCarIDListOffset = br.ReadUInt32();
        InitialCarIDCount = br.ReadInt32();
        LicenceDataOffset = br.ReadUInt32();
        LicenceDataCount = br.ReadInt32();
        EventJunctionOffset = br.ReadUInt32();
        EventJunctionCount = br.ReadInt32();
        EventDataOffset = br.ReadUInt32();
        EventDataCount = br.ReadInt32();
        RivalOffset = br.ReadUInt32();
        RivalCount = br.ReadInt32();
        Section6Offset = br.ReadUInt32();
        Section6Count = br.ReadInt32();
        Section7Offset = br.ReadUInt32();
        Section7Count = br.ReadInt32();
        CarbonCarUnlockDataOffset = br.ReadUInt32();
        CarbonCarUnlockDataCount = br.ReadInt32();
        PlayerOpponentsDataOffset = br.ReadUInt32();
        PlayerOpponentsDataCount = br.ReadInt32();

        br.BaseStream.Position = InitialCarIDListOffset;

        for (int i = 0; i < InitialCarIDCount; i++)
        {
            InitialCarIDEntries.Add(new(br.ReadUInt64()));
        }

        br.BaseStream.Position = LicenceDataOffset;

        for (int i = 0; i < LicenceDataCount; i++)
        {
            LicenceData licenceData = new()
            {
                Unknown00 = br.ReadSingle(),
                Unknown04 = br.ReadSingle(),
                Unknown08 = br.ReadSingle(),
                Unknown0C = br.ReadSingle(),
                Unknown10 = br.ReadSingle(),
                Unknown14 = br.ReadSingle(),
                Unknown18 = br.ReadSingle(),
                Unknown1C = br.ReadSingle(),
                Unknown20 = br.ReadSingle(),
                Unknown24 = br.ReadSingle(),
                GameDBID = br.ReadInt32(),
                Unknown2C = br.ReadSingle(),
                Unknown30 = br.ReadSingle(),
                Unknown34 = br.ReadSingle(),
                Unknown38 = br.ReadSingle(),
                Unknown3C = br.ReadSingle(),
                Unknown40 = br.ReadSingle(),
                Unknown44 = br.ReadSingle(),
                Unknown48 = br.ReadSingle(),
                NumEventWinsRequired = br.ReadInt16(),
                Unknown4E = br.ReadInt16(),
                Unknown50 = br.ReadInt16(),
                Unknown52 = br.ReadInt16(),
                Unknown54 = br.ReadInt16(),
                Unknown56 = br.ReadInt16(),
                Unknown58 = br.ReadInt16(),
                Unknown5A = br.ReadInt16(),
                Unknown5C = br.ReadByte(),
                Unknown5D = br.ReadByte(),
                Unknown5E = br.ReadByte(),
                Unknown5F = br.ReadByte(),
                Unknown60 = br.ReadByte(),
                Unknown61 = br.ReadByte(),
                Unknown62 = br.ReadByte(),
                Unknown63 = br.ReadByte()
            };

            br.BaseStream.Position += 4; // padding
            licenceData.VehicleUnlocked = new(br.ReadUInt64());

            LicenceDataEntries.Add(licenceData);
        }

        br.BaseStream.Position = EventJunctionOffset;

        for (int i = 0; i < EventJunctionCount; i++)
        {
            EventJunction eventJunction = new()
            {
                GameDBID = br.ReadInt32(),
                CarEventDataOffset = br.ReadUInt32(),
                BTTEventDataOffset = br.ReadUInt32()
            };

            if (FormatRevision >= 44)
                eventJunction.BikeEventDataOffset = br.ReadUInt32();

            eventJunction.UnknownGameDBID = br.ReadInt32();

            EventJunctionEntries.Add(eventJunction);
        }

        br.BaseStream.Position = EventDataOffset;

        for (int i = 0; i < EventDataCount; i++)
        {
            long origPosition = br.BaseStream.Position;

            EventData eventData = new()
            {
                GameDBID = br.ReadInt32(),
                AppearsWhen = br.ReadInt32(),
                TrafficDensityMultiplier = br.ReadSingle(),
                Unknown0C = br.ReadSingle(),
                LockVehicleID = new(br.ReadUInt64()),
                EventLandmarkDataOffset = br.ReadUInt32(),
                EventLandmarkCount = br.ReadInt32(),
                TargetTimeUnused = br.ReadSingle(),
                TargetTime = br.ReadSingle(),
                StuntRunTargetScoreTier1 = br.ReadInt32(),
                StuntRunTargetScoreTier2 = br.ReadInt32(),
                StuntRunTargetScoreTier3 = br.ReadInt32(),
                StuntRunTargetScoreTier4 = br.ReadInt32(),
                StuntRunTargetScoreTier5 = br.ReadInt32(),
                StuntRunTargetScoreTier6 = br.ReadInt32(),
                StuntRunTimeLimitTier1 = br.ReadSingle(),
                StuntRunTimeLimitTier2 = br.ReadSingle(),
                StuntRunTimeLimitTier3 = br.ReadSingle(),
                StuntRunTimeLimitTier4 = br.ReadSingle(),
                StuntRunTimeLimitTier5 = br.ReadSingle(),
                StuntRunTimeLimitTier6 = br.ReadSingle(),
                Unknown58 = br.ReadInt32(),

                RivalData = new EventRivalData[7]
            };

            for (int j = 0; j < 7; j++)
            {
                EventRivalData rivalData = new()
                {
                    Unknown00 = br.ReadInt32(),
                    Unknown04 = br.ReadInt32(),
                    Unknown08 = br.ReadInt32(),
                    Unknown0C = br.ReadInt32(),
                    Unknown10 = br.ReadByte(),
                    Unknown11 = br.ReadByte(),
                    Unknown12 = br.ReadByte(),
                    Unknown13 = br.ReadByte()
                };

                eventData.RivalData[j] = rivalData;
            }

            eventData.RivalDataCount = br.ReadInt32();
            Array.Resize(ref eventData.RivalData, eventData.RivalDataCount);

            eventData.Type = br.ReadByte();
            eventData.UnknownED = br.ReadByte();
            eventData.NumberOfAIRivalsToUse = br.ReadByte();
            eventData.UnknownEF = br.ReadByte();
            eventData.UnknownF0 = br.ReadInt32();
            eventData.UnknownF4 = br.ReadByte();
            eventData.UnknownF5 = br.ReadByte();
            br.BaseStream.Position += 2; // padding

            long oldPos = br.BaseStream.Position;
            eventData.EventLandmarkData = new EventLandmarkData[eventData.EventLandmarkCount];
            br.BaseStream.Position = eventData.EventLandmarkDataOffset;

            for (int j = 0; j < eventData.EventLandmarkCount; j++)
            {
                EventLandmarkData landmarkData = new()
                {
                    TriggerID = br.ReadInt32(),
                    NumAIGameDBIDs = br.ReadInt32()
                };

                landmarkData.AIGameDBIDs = new int[landmarkData.NumAIGameDBIDs];

                for (int k = 0; k < 8; k++)
                {
                    int gameDBID = br.ReadInt32();

                    if (k < landmarkData.NumAIGameDBIDs)
                        landmarkData.AIGameDBIDs[k] = gameDBID;
                }

                eventData.EventLandmarkData[j] = landmarkData;
            }

            br.BaseStream.Position = oldPos;

            EventDataEntries.Add((uint)origPosition, eventData);
        }

        for (int i = 0; i < EventJunctionCount; i++)
        {
            EventJunction junction = EventJunctionEntries[i];

            if (junction.CarEventDataOffset != 0)
                junction.CarEventData = EventDataEntries[junction.CarEventDataOffset];

            if (junction.BTTEventDataOffset != 0)
                junction.BTTEventData = EventDataEntries[junction.BTTEventDataOffset];

            if (junction.BikeEventDataOffset != 0)
                junction.BikeEventData = EventDataEntries[junction.BikeEventDataOffset];

            EventJunctionEntries[i] = junction;
        }

        br.BaseStream.Position = RivalOffset;

        for (int i = 0; i < RivalCount; i++)
        {
            Rival rival = new()
            {
                RivalID = br.ReadInt64(),
                VehicleID = new(br.ReadUInt64()),
                Unknown10 = br.ReadInt16(),
                Unknown12 = br.ReadInt16(),
                Unknown14 = br.ReadByte(),
                Unknown15 = br.ReadByte(),
                WinsUntilRoam = br.ReadByte(),
                DoesNotRoam = br.ReadBoolean(),
                Name = br.ReadLenString(0x20)
            };

            RivalEntries.Add(rival);
        }

        br.BaseStream.Position = Section6Offset;

        for (int i = 0; i < Section6Count; i++)
        {
            ProgressionSection6Entry section6Entry = new()
            {
                Unknown00 = br.ReadSingle(),
                Unknown04 = br.ReadSingle(),
                Unknown08 = br.ReadSingle(),
                Unknown0C = br.ReadSingle(),
                Unknown10 = br.ReadSingle(),
                Unknown14 = br.ReadSingle(),
                Unknown18 = br.ReadSingle(),
                Unknown1C = br.ReadSingle(),
                Unknown20 = br.ReadSingle(),
                Unknown24 = br.ReadSingle(),
                Unknown28 = br.ReadSingle(),
                Unknown2C = br.ReadSingle(),
                Unknown30 = br.ReadSingle(),
                Unknown34 = br.ReadSingle(),
                Unknown38 = br.ReadSingle(),
                Unknown3C = br.ReadSingle(),
                Unknown40 = br.ReadByte(),
                Unknown41 = br.ReadByte(),
                Unknown42 = br.ReadByte(),
                Unknown43 = br.ReadByte()
            };

            Section6Entries.Add(section6Entry);
        }

        br.BaseStream.Position = Section7Offset;

        for (int i = 0; i < Section7Count; i++)
        {
            ProgressionSection7Entry section7Entry = new()
            {
                Unknown00 = br.ReadSingle(),
                Unknown04 = br.ReadSingle(),
                Unknown08 = br.ReadSingle(),
                Unknown0C = br.ReadSingle()
            };

            Section7Entries.Add(section7Entry);
        }

        br.BaseStream.Position = CarbonCarUnlockDataOffset;

        for (int i = 0; i < CarbonCarUnlockDataCount; i++)
        {
            CarbonCarUnlockData unlockData = new()
            {
                Unknown00 = br.ReadInt32(),
                Unknown04 = br.ReadInt16(),
                Unknown06 = br.ReadByte(),
                Unknown07 = br.ReadByte(),
                VehicleID = new(br.ReadUInt64())
            };

            CarbonCarUnlockDataEntries.Add(unlockData);
        }

        br.BaseStream.Position = PlayerOpponentsDataOffset;

        for (int i = 0; i < PlayerOpponentsDataCount; i++)
        {
            PlayerOpponentsData opponentsData = new()
            {
                PlayerVehicleID = new(br.ReadUInt64()),
                Unknown08 = br.ReadInt32(),
                Unknown0C = br.ReadInt32(),
                Opponent1VehicleID = new(br.ReadUInt64()),
                Unknown18 = br.ReadInt32(),
                Unknown1C = br.ReadInt32(),
                Opponent2VehicleID = new(br.ReadUInt64()),
                Unknown28 = br.ReadInt32(),
                Unknown2C = br.ReadInt32(),
                Opponent3VehicleID = new(br.ReadUInt64()),
                Unknown38 = br.ReadInt32(),
                Unknown3C = br.ReadInt32(),
                Opponent4VehicleID = new(br.ReadUInt64()),
                Unknown48 = br.ReadInt32(),
                Unknown4C = br.ReadInt32(),
                Opponent5VehicleID = new(br.ReadUInt64()),
                Unknown58 = br.ReadInt32(),
                Unknown5C = br.ReadInt32(),
                Opponent6VehicleID = new(br.ReadUInt64()),
                Unknown68 = br.ReadInt32(),
                Unknown6C = br.ReadInt32(),
                Opponent7VehicleID = new(br.ReadUInt64()),
                Unknown78 = br.ReadInt32(),
                Unknown7C = br.ReadInt32(),
                Opponent8VehicleID = new(br.ReadUInt64()),
                Unknown88 = br.ReadInt32(),
                NumOpponents = br.ReadInt32()
            };

            PlayerOpponentsDataEntries.Add(opponentsData);
        }

        br.Close();
        ms.Close();

        return true;
    }

    public bool Write(BundleEntry entry)
    {
        MemoryStream ms = new();
        BinaryWriter2 bw = new(ms)
        {
            BigEndian = entry.Console
        };

        // TODO: Write
        bw.Flush();
        byte[] data = ms.ToArray();
        bw.Close();
        ms.Close();

        entry.EntryBlocks[0].Data = data;
        entry.Dirty = true;

        return true;
    }
}
