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
        BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

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
            InitialCarIDEntries.Add(new EncryptedString(br.ReadUInt64()));
        }


        br.BaseStream.Position = LicenceDataOffset;
        for (int i = 0; i < LicenceDataCount; i++)
        {
            LicenceData licenceData = new();

            licenceData.Unknown00 = br.ReadSingle();
            licenceData.Unknown04 = br.ReadSingle();
            licenceData.Unknown08 = br.ReadSingle();
            licenceData.Unknown0C = br.ReadSingle();
            licenceData.Unknown10 = br.ReadSingle();
            licenceData.Unknown14 = br.ReadSingle();
            licenceData.Unknown18 = br.ReadSingle();
            licenceData.Unknown1C = br.ReadSingle();
            licenceData.Unknown20 = br.ReadSingle();
            licenceData.Unknown24 = br.ReadSingle();
            licenceData.GameDBID = br.ReadInt32();
            licenceData.Unknown2C = br.ReadSingle();
            licenceData.Unknown30 = br.ReadSingle();
            licenceData.Unknown34 = br.ReadSingle();
            licenceData.Unknown38 = br.ReadSingle();
            licenceData.Unknown3C = br.ReadSingle();
            licenceData.Unknown40 = br.ReadSingle();
            licenceData.Unknown44 = br.ReadSingle();
            licenceData.Unknown48 = br.ReadSingle();
            licenceData.NumEventWinsRequired = br.ReadInt16();
            licenceData.Unknown4E = br.ReadInt16();
            licenceData.Unknown50 = br.ReadInt16();
            licenceData.Unknown52 = br.ReadInt16();
            licenceData.Unknown54 = br.ReadInt16();
            licenceData.Unknown56 = br.ReadInt16();
            licenceData.Unknown58 = br.ReadInt16();
            licenceData.Unknown5A = br.ReadInt16();
            licenceData.Unknown5C = br.ReadByte();
            licenceData.Unknown5D = br.ReadByte();
            licenceData.Unknown5E = br.ReadByte();
            licenceData.Unknown5F = br.ReadByte();
            licenceData.Unknown60 = br.ReadByte();
            licenceData.Unknown61 = br.ReadByte();
            licenceData.Unknown62 = br.ReadByte();
            licenceData.Unknown63 = br.ReadByte();
            br.BaseStream.Position += 4; // padding
            licenceData.VehicleUnlocked = new EncryptedString(br.ReadUInt64());
            
            LicenceDataEntries.Add(licenceData);
        }


        br.BaseStream.Position = EventJunctionOffset;
        for (int i = 0; i < EventJunctionCount; i++)
        {
            EventJunction eventJunction = new();

            eventJunction.GameDBID = br.ReadInt32();
            eventJunction.CarEventDataOffset = br.ReadUInt32();
            eventJunction.BTTEventDataOffset = br.ReadUInt32();
            if (FormatRevision >= 44)
                eventJunction.BikeEventDataOffset = br.ReadUInt32();
            eventJunction.UnknownGameDBID = br.ReadInt32();

            EventJunctionEntries.Add(eventJunction);
        }


        br.BaseStream.Position = EventDataOffset;
        for (int i = 0; i < EventDataCount; i++)
        {
            long origPosition = br.BaseStream.Position;

            EventData eventData = new();

            eventData.GameDBID = br.ReadInt32();
            eventData.AppearsWhen = br.ReadInt32();
            eventData.TrafficDensityMultiplier = br.ReadSingle();
            eventData.Unknown0C = br.ReadSingle();
            eventData.LockVehicleID = new EncryptedString(br.ReadUInt64());
            eventData.EventLandmarkDataOffset = br.ReadUInt32();
            eventData.EventLandmarkCount = br.ReadInt32();
            eventData.TargetTimeUnused = br.ReadSingle();
            eventData.TargetTime = br.ReadSingle();
            eventData.StuntRunTargetScoreTier1 = br.ReadInt32();
            eventData.StuntRunTargetScoreTier2 = br.ReadInt32();
            eventData.StuntRunTargetScoreTier3 = br.ReadInt32();
            eventData.StuntRunTargetScoreTier4 = br.ReadInt32();
            eventData.StuntRunTargetScoreTier5 = br.ReadInt32();
            eventData.StuntRunTargetScoreTier6 = br.ReadInt32();
            eventData.StuntRunTimeLimitTier1 = br.ReadSingle();
            eventData.StuntRunTimeLimitTier2 = br.ReadSingle();
            eventData.StuntRunTimeLimitTier3 = br.ReadSingle();
            eventData.StuntRunTimeLimitTier4 = br.ReadSingle();
            eventData.StuntRunTimeLimitTier5 = br.ReadSingle();
            eventData.StuntRunTimeLimitTier6 = br.ReadSingle();
            eventData.Unknown58 = br.ReadInt32();

            eventData.RivalData = new EventRivalData[7];
            for (int j = 0; j < 7; j++)
            {
                EventRivalData rivalData = new();

                rivalData.Unknown00 = br.ReadInt32();
                rivalData.Unknown04 = br.ReadInt32();
                rivalData.Unknown08 = br.ReadInt32();
                rivalData.Unknown0C = br.ReadInt32();
                rivalData.Unknown10 = br.ReadByte();
                rivalData.Unknown11 = br.ReadByte();
                rivalData.Unknown12 = br.ReadByte();
                rivalData.Unknown13 = br.ReadByte();

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
                EventLandmarkData landmarkData = new();

                landmarkData.TriggerID = br.ReadInt32();
                landmarkData.NumAIGameDBIDs = br.ReadInt32();
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
            Rival rival = new();

            rival.RivalID = br.ReadInt64();
            rival.VehicleID = new EncryptedString(br.ReadUInt64());
            rival.Unknown10 = br.ReadInt16();
            rival.Unknown12 = br.ReadInt16();
            rival.Unknown14 = br.ReadByte();
            rival.Unknown15 = br.ReadByte();
            rival.WinsUntilRoam = br.ReadByte();
            rival.DoesNotRoam = br.ReadBoolean();
            rival.Name = br.ReadLenString(0x20);

            RivalEntries.Add(rival);
        }


        br.BaseStream.Position = Section6Offset;
        for (int i = 0; i < Section6Count; i++)
        {
            ProgressionSection6Entry section6Entry = new();

            section6Entry.Unknown00 = br.ReadSingle();
            section6Entry.Unknown04 = br.ReadSingle();
            section6Entry.Unknown08 = br.ReadSingle();
            section6Entry.Unknown0C = br.ReadSingle();
            section6Entry.Unknown10 = br.ReadSingle();
            section6Entry.Unknown14 = br.ReadSingle();
            section6Entry.Unknown18 = br.ReadSingle();
            section6Entry.Unknown1C = br.ReadSingle();
            section6Entry.Unknown20 = br.ReadSingle();
            section6Entry.Unknown24 = br.ReadSingle();
            section6Entry.Unknown28 = br.ReadSingle();
            section6Entry.Unknown2C = br.ReadSingle();
            section6Entry.Unknown30 = br.ReadSingle();
            section6Entry.Unknown34 = br.ReadSingle();
            section6Entry.Unknown38 = br.ReadSingle();
            section6Entry.Unknown3C = br.ReadSingle();
            section6Entry.Unknown40 = br.ReadByte();
            section6Entry.Unknown41 = br.ReadByte();
            section6Entry.Unknown42 = br.ReadByte();
            section6Entry.Unknown43 = br.ReadByte();

            Section6Entries.Add(section6Entry);
        }


        br.BaseStream.Position = Section7Offset;
        for (int i = 0; i < Section7Count; i++)
        {
            ProgressionSection7Entry section7Entry = new();

            section7Entry.Unknown00 = br.ReadSingle();
            section7Entry.Unknown04 = br.ReadSingle();
            section7Entry.Unknown08 = br.ReadSingle();
            section7Entry.Unknown0C = br.ReadSingle();

            Section7Entries.Add(section7Entry);
        }


        br.BaseStream.Position = CarbonCarUnlockDataOffset;
        for (int i = 0; i < CarbonCarUnlockDataCount; i++)
        {
            CarbonCarUnlockData unlockData = new();

            unlockData.Unknown00 = br.ReadInt32();
            unlockData.Unknown04 = br.ReadInt16();
            unlockData.Unknown06 = br.ReadByte();
            unlockData.Unknown07 = br.ReadByte();
            unlockData.VehicleID = new EncryptedString(br.ReadUInt64());

            CarbonCarUnlockDataEntries.Add(unlockData);
        }


        br.BaseStream.Position = PlayerOpponentsDataOffset;
        for (int i = 0; i < PlayerOpponentsDataCount; i++)
        {
            PlayerOpponentsData opponentsData = new();

            opponentsData.PlayerVehicleID = new EncryptedString(br.ReadUInt64());
            opponentsData.Unknown08 = br.ReadInt32();
            opponentsData.Unknown0C = br.ReadInt32();
            opponentsData.Opponent1VehicleID = new EncryptedString(br.ReadUInt64());
            opponentsData.Unknown18 = br.ReadInt32();
            opponentsData.Unknown1C = br.ReadInt32();
            opponentsData.Opponent2VehicleID = new EncryptedString(br.ReadUInt64());
            opponentsData.Unknown28 = br.ReadInt32();
            opponentsData.Unknown2C = br.ReadInt32();
            opponentsData.Opponent3VehicleID = new EncryptedString(br.ReadUInt64());
            opponentsData.Unknown38 = br.ReadInt32();
            opponentsData.Unknown3C = br.ReadInt32();
            opponentsData.Opponent4VehicleID = new EncryptedString(br.ReadUInt64());
            opponentsData.Unknown48 = br.ReadInt32();
            opponentsData.Unknown4C = br.ReadInt32();
            opponentsData.Opponent5VehicleID = new EncryptedString(br.ReadUInt64());
            opponentsData.Unknown58 = br.ReadInt32();
            opponentsData.Unknown5C = br.ReadInt32();
            opponentsData.Opponent6VehicleID = new EncryptedString(br.ReadUInt64());
            opponentsData.Unknown68 = br.ReadInt32();
            opponentsData.Unknown6C = br.ReadInt32();
            opponentsData.Opponent7VehicleID = new EncryptedString(br.ReadUInt64());
            opponentsData.Unknown78 = br.ReadInt32();
            opponentsData.Unknown7C = br.ReadInt32();
            opponentsData.Opponent8VehicleID = new EncryptedString(br.ReadUInt64());
            opponentsData.Unknown88 = br.ReadInt32();
            opponentsData.NumOpponents = br.ReadInt32();

            PlayerOpponentsDataEntries.Add(opponentsData);
        }


        br.Close();
        ms.Close();

        return true;
    }

    public bool Write(BundleEntry entry)
    {
        MemoryStream ms = new();
        BinaryWriter2 bw = new(ms);
        bw.BigEndian = entry.Console;

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
