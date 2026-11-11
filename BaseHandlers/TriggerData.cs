using BundleFormat;
using BundleUtilities;
using PluginAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.IO;
using System.Numerics;
using System.Windows.Forms;

namespace BaseHandlers;

class DescriptiveCollectionEditor : CollectionEditor
{
    public DescriptiveCollectionEditor(Type type) : base(type) { }

    protected override CollectionForm CreateCollectionForm()
    {
        CollectionForm form = base.CreateCollectionForm();
        form.Shown += delegate
        {
            ShowDescription(form);
        };
        return form;
    }

    static void ShowDescription(Control control)
    {
        if (control is PropertyGrid grid)
            grid.HelpVisible = true;

        foreach (Control child in control.Controls)
        {
            ShowDescription(child);
        }
    }
}
public class CgsID
{
    private ulong mID = 0;

    public ulong ID
    {
        get
        {
            return mID;
        }
        set
        {
            mID = value;
        }
    }

    public static CgsID FromByteArray(byte[] data, int startIndex)
    {
        ulong id = BitConverter.ToUInt64(data, startIndex);
        CgsID result = new()
        {
            ID = id
        };

        return new();
    }

    public byte[] ToByteArray()
    {
        return BitConverter.GetBytes(mID);
    }

    public void Read(BinaryReader2 br)
    {
        mID = br.ReadUInt64();
    }

    public void Write(BinaryWriter2 bw)
    {
        bw.Write(mID);
    }

    public override string ToString()
    {
        return string.Format("{0:X16}", mID);
    }
}

public class StartingGrid
{
    public List<Vector4> StartingPositions { get; set; } = [];

    public List<Vector4> StartingDirections { get; set; } = [];

    public void Read(BinaryReader2 br)
    {
        StartingPositions = new(8);

        for (int i = 0; i < 8; i++)
        {
            StartingPositions[i] = new(
                br.ReadSingle(),
                br.ReadSingle(),
                br.ReadSingle(),
                br.ReadSingle());
        }

        StartingDirections = new(8);

        for (int i = 0; i < 8; i++)
        {
            StartingDirections[i] = new(
                br.ReadSingle(),
                br.ReadSingle(),
                br.ReadSingle(),
                br.ReadSingle());
        }
    }

    public void Write(BinaryWriter2 bw)
    {
        for (int i = 0; i < 8; i++)
        {
            bw.Write(StartingPositions[i].X);
            bw.Write(StartingPositions[i].Y);
            bw.Write(StartingPositions[i].Z);
            bw.Write(StartingPositions[i].W);
        }

        for (int i = 0; i < 8; i++)
        {
            bw.Write(StartingDirections[i].X);
            bw.Write(StartingDirections[i].Y);
            bw.Write(StartingDirections[i].Z);
            bw.Write(StartingDirections[i].W);
        }
    }
}

public class BoxRegion
{
    public float PositionX { get; set; } = 0;

    public float PositionY { get; set; } = 0;

    public float PositionZ { get; set; } = 0;

    public float RotationX { get; set; } = 0;

    public float RotationY { get; set; } = 0;

    public float RotationZ { get; set; } = 0;

    public float DimensionX { get; set; } = 0;

    public float DimensionY { get; set; } = 0;

    public float DimensionZ { get; set; } = 0;

    public void Read(BinaryReader2 br)
    {
        PositionX = br.ReadSingle();
        PositionY = br.ReadSingle();
        PositionZ = br.ReadSingle();
        RotationX = br.ReadSingle();
        RotationY = br.ReadSingle();
        RotationZ = br.ReadSingle();
        DimensionX = br.ReadSingle();
        DimensionY = br.ReadSingle();
        DimensionZ = br.ReadSingle();
    }

    public void Write(BinaryWriter2 bw)
    {
        bw.Write(PositionX);
        bw.Write(PositionY);
        bw.Write(PositionZ);
        bw.Write(RotationX);
        bw.Write(RotationY);
        bw.Write(RotationZ);
        bw.Write(DimensionX);
        bw.Write(DimensionY);
        bw.Write(DimensionZ);
    }
}

public class TriggerRegion
{
    public enum RegionType
    {
        E_TYPE_LANDMARK = 0,
        E_TYPE_BLACKSPOT = 1,
        E_TYPE_GENERIC_REGION = 2,
        E_TYPE_VFXBOX_REGION = 3
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public BoxRegion mBoxRegion { get; set; } = new();

    public int mId { get; set; } = 0;

    public short miRegionIndex { get; set; } = 0;

    public RegionType meType { get; set; } = RegionType.E_TYPE_LANDMARK;

    private byte[] MuPad { get; set; } = new byte[1];

    public virtual void Read(BinaryReader2 br)
    {
        mBoxRegion = new();
        mBoxRegion.Read(br);
        mId = br.ReadInt32();
        miRegionIndex = br.ReadInt16();
        meType = (RegionType)br.ReadByte();
        MuPad = br.ReadBytes(1);
    }

    public virtual void Write(BinaryWriter2 bw)
    {
        mBoxRegion.Write(bw);

        bw.Write(mId);
        bw.Write(miRegionIndex);
        bw.Write((byte)meType);
        bw.Write(MuPad);
    }
}

public class Landmark : TriggerRegion
{
    public Landmark() : base()
    {
        // Set meType to E_TYPE_LANDMARK
        meType = RegionType.E_TYPE_LANDMARK;
    }

    public List<StartingGrid> mpaStartingGrids { get; set; } = [];

    private long startingGridOffsetPosition = 0;

    public byte muDesignIndex { get; set; } = 0;

    public byte muDistrict { get; set; } = 0;

    public byte mu8Flags { get; set; } = 0;

    public void Read(BinaryReader2 br, bool x64)
    {
        base.Read(br);

        if (x64)
            br.ReadBytes(4); // Padding before the 8-byte pointer

        long startingGridOffset = TriggerData.ReadPtr(br, x64);
        int miStartingGridCount = br.ReadByte();
        muDesignIndex = br.ReadByte();
        muDistrict = br.ReadByte();
        mu8Flags = br.ReadByte();

        if (x64)
            br.ReadBytes(4); // Padding

        long currentPosition = br.BaseStream.Position;
        br.BaseStream.Position = startingGridOffset;

        for (int i = 0; i < miStartingGridCount; i++)
        {
            StartingGrid startingGrid = new();
            startingGrid.Read(br);
            mpaStartingGrids.Add(startingGrid);
        }

        br.BaseStream.Position = currentPosition;

    }

    public void Write(BinaryWriter2 bw, bool x64)
    {
        base.Write(bw);

        if (x64)
            bw.WriteUniquePadding(4);

        startingGridOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(x64
            ? 8
            : 4);
        bw.Write((byte)mpaStartingGrids.Count);
        bw.Write(muDesignIndex);
        bw.Write(muDistrict);
        bw.Write(mu8Flags);

        if (x64)
            bw.WriteUniquePadding(4);
    }

    public void WriteStartingGrid(BinaryWriter2 bw, bool x64)
    {

        long currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = startingGridOffsetPosition;
        TriggerData.WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;

        foreach (StartingGrid grid in mpaStartingGrids)
        {
            grid.Write(bw);
        }
    }
}

public class GenericRegion : TriggerRegion
{
    public enum StuntCameraType
    {
        E_STUNT_CAMERA_TYPE_NO_CUTS = 0,
        E_STUNT_CAMERA_TYPE_CUSTOM = 1,
        E_STUNT_CAMERA_TYPE_NORMAL = 2
    }

    public enum Type
    {
        E_TYPE_JUNK_YARD = 0,
        E_TYPE_BIKE_SHOP = 1,
        E_TYPE_GAS_STATION = 2,
        E_TYPE_BODY_SHOP = 3,
        E_TYPE_PAINT_SHOP = 4,
        E_TYPE_CAR_PARK = 5,
        E_TYPE_SIGNATURE_TAKEDOWN = 6,
        E_TYPE_KILLZONE = 7,
        E_TYPE_JUMP = 8,
        E_TYPE_SMASH = 9,
        E_TYPE_SIGNATURE_CRASH = 10,
        E_TYPE_SIGNATURE_CRASH_CAMERA = 11,
        E_TYPE_ROAD_LIMIT = 12,
        E_TYPE_OVERDRIVE_BOOST = 13,
        E_TYPE_OVERDRIVE_STRENGTH = 14,
        E_TYPE_OVERDRIVE_SPEED = 15,
        E_TYPE_OVERDRIVE_CONTROL = 16,
        E_TYPE_TIRE_SHOP = 17,
        E_TYPE_TUNING_SHOP = 18,
        E_TYPE_PICTURE_PARADISE = 19,
        E_TYPE_TUNNEL = 20,
        E_TYPE_OVERPASS = 21,
        E_TYPE_BRIDGE = 22,
        E_TYPE_WAREHOUSE = 23,
        E_TYPE_LARGE_OVERHEAD_OBJECT = 24,
        E_TYPE_NARROW_ALLEY = 25,
        E_TYPE_PASS_TUNNEL = 26,
        E_TYPE_PASS_OVERPASS = 27,
        E_TYPE_PASS_BRIDGE = 28,
        E_TYPE_PASS_WAREHOUSE = 29,
        E_TYPE_PASS_LARGEOVERHEADOBJECT = 30,
        E_TYPE_PASS_NARROWALLEY = 31,
        E_TYPE_RAMP = 32,
        E_TYPE_GOLD = 33,
        E_TYPE_ISLAND_ENTITLEMENT = 34
    }

    public GenericRegion() : base()
    {
        // Set meType to E_TYPE_GENERIC_REGION
        meType = RegionType.E_TYPE_GENERIC_REGION;
    }

    public int GroupID { get; set; } = 0;

    public short CameraCut1 { get; set; } = 0;

    public short CameraCut2 { get; set; } = 0;

    public StuntCameraType CameraType1 { get; set; } = 0;

    public StuntCameraType CameraType2 { get; set; } = 0;

    public Type GenericRegionType { get; set; } = Type.E_TYPE_JUNK_YARD;

    public sbyte IsOneWay { get; set; } = 0;

    public new void Read(BinaryReader2 br)
    {
        base.Read(br);
        GroupID = br.ReadInt32();
        CameraCut1 = br.ReadInt16();
        CameraCut2 = br.ReadInt16();
        CameraType1 = (StuntCameraType)br.ReadSByte();
        CameraType2 = (StuntCameraType)br.ReadSByte();
        GenericRegionType = (Type)br.ReadByte();
        IsOneWay = br.ReadSByte();
    }

    public new void Write(BinaryWriter2 bw)
    {
        base.Write(bw);
        bw.Write(GroupID);
        bw.Write(CameraCut1);
        bw.Write(CameraCut2);
        bw.Write((sbyte)CameraType1);
        bw.Write((sbyte)CameraType2);
        bw.Write((byte)GenericRegionType);
        bw.Write(IsOneWay);
    }
}

public class Blackspot : TriggerRegion
{
    public enum ScoreType
    {
        E_SCORE_TYPE_DISTANCE = 0,
        E_SCORE_TYPE_CAR_COUNT = 1
    }

    public Blackspot() : base()
    {
        // Set meType to E_TYPE_BLACKSPOT
        meType = RegionType.E_TYPE_BLACKSPOT;
    }

    public ScoreType muScoreType { get; set; } = ScoreType.E_SCORE_TYPE_DISTANCE;

    public int miScoreAmount { get; set; } = 0;

    public override void Read(BinaryReader2 br)
    {
        base.Read(br);

        muScoreType = (ScoreType)br.ReadByte();
        br.ReadBytes(3); // Padding
        miScoreAmount = br.ReadInt32();
    }

    public override void Write(BinaryWriter2 bw)
    {
        base.Write(bw);

        bw.Write((byte)muScoreType);
        bw.Write(new byte[3]); // Padding
        bw.Write(miScoreAmount);
    }
}

public class Killzone
{
    [Description("Triggers as GenericRegions. Uses region.mId")]
    public List<int> TriggerIds { get; set; } = [];

    public List<CgsID> RegionIds { get; set; } = [];

    private long triggerOffsetPosition = 0;

    private long cgsidOffsetPosition = 0;

    public void Read(BinaryReader2 br, bool x64)
    {
        // Read the trigger pointer array
        long genericRegionOffset = TriggerData.ReadPtr(br, x64);
        int triggerCount = br.ReadInt32();

        if (x64)
            br.ReadBytes(4); // Padding

        // Read the region ID array
        long cgsOffset = TriggerData.ReadPtr(br, x64);
        int regionIDCount = br.ReadInt32();

        if (x64)
            br.ReadBytes(4); // Padding

        long currentPosition = br.BaseStream.Position;

        br.BaseStream.Position = genericRegionOffset;
        uint[] triggers = new uint[triggerCount];

        for (int i = 0; i < triggerCount; i++)
        {
            triggers[i] = (uint)TriggerData.ReadPtr(br, x64);
        }

        TriggerIds = [];

        foreach (uint trigger in triggers)
        {
            br.BaseStream.Position = trigger;
            GenericRegion region = new();
            region.Read(br);
            TriggerIds.Add(region.mId);
        }

        br.BaseStream.Position = cgsOffset;
        RegionIds = [];

        for (int i = 0; i < regionIDCount; i++)
        {
            CgsID id = new();
            id.Read(br);
            RegionIds.Add(id);
        }

        br.BaseStream.Position = currentPosition;
    }

    public void Write(BinaryWriter2 bw, bool x64)
    {
        triggerOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(x64
            ? 8
            : 4);
        bw.Write(TriggerIds.Count);

        if (x64)
            bw.WriteUniquePadding(4);

        cgsidOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(x64
            ? 8
            : 4);
        bw.Write(RegionIds.Count);

        if (x64)
            bw.WriteUniquePadding(4);
    }

    public void WritePointerStuff(BinaryWriter2 bw, Dictionary<int, uint> genericRegionOffsets, bool x64)
    {

        long currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = triggerOffsetPosition;
        TriggerData.WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;

        foreach (int trigger in TriggerIds)
        {
            TriggerData.WritePtr(bw, genericRegionOffsets[trigger], x64);
        }

        long paddingCount = 16 - (bw.BaseStream.Position % 16);

        if (paddingCount < 16)
        {
            for (int i = 0; i < paddingCount; i++)
                bw.Write((byte)0);
        }

        currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = cgsidOffsetPosition;
        TriggerData.WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;

        foreach (CgsID ids in RegionIds)
        {
            ids.Write(bw);
        }

        paddingCount = 16 - (bw.BaseStream.Position % 16);

        if (paddingCount < 16)
        {
            for (int i = 0; i < paddingCount; i++)
                bw.Write((byte)0);
        }
    }
}

public class SignatureStunt
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public CgsID mId { get; set; } = new();

    public long miCamera { get; set; } = 0;

    public List<int> stuntElementRegions { get; set; } = [];

    private long stuntElementOffsetPosition = 0;

    public void Read(BinaryReader2 br, bool x64)
    {
        mId = new();
        mId.Read(br);
        miCamera = br.ReadInt64();

        uint mppStuntElementsOffset = (uint)TriggerData.ReadPtr(br, x64);
        int miStuntElementCount = br.ReadInt32();

        if (x64)
            br.ReadBytes(4); // Padding

        long currentPosition = br.BaseStream.Position;
        br.BaseStream.Position = mppStuntElementsOffset;
        uint[] triggers = new uint[miStuntElementCount];

        for (int i = 0; i < miStuntElementCount; i++)
        {
            triggers[i] = (uint)TriggerData.ReadPtr(br, x64);
        }

        stuntElementRegions = [];

        foreach (uint trigger in triggers)
        {
            br.BaseStream.Position = trigger;
            GenericRegion region = new();
            region.Read(br);
            stuntElementRegions.Add(region.mId);
        }

        br.BaseStream.Position = currentPosition;
    }

    public void Write(BinaryWriter2 bw, bool x64)
    {
        mId.Write(bw);
        bw.Write(miCamera);
        stuntElementOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(x64
            ? 8
            : 4);
        bw.Write(stuntElementRegions.Count);

        if (x64)
            bw.WriteUniquePadding(4);
    }

    public void WriteStuntElements(BinaryWriter2 bw, Dictionary<int, uint> genericRegionOffsets, bool x64)
    {
        long currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = stuntElementOffsetPosition;
        bw.Write(currentPosition);
        bw.BaseStream.Position = currentPosition;

        foreach (int trigger in stuntElementRegions)
        {
            TriggerData.WritePtr(bw, genericRegionOffsets[trigger], x64);
        }
    }
}

public class RoamingLocation
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public Vector4 Position { get; set; } = new(0, 0, 0, 0);

    public byte DistrictIndex { get; set; } = 0;

    public void Read(BinaryReader2 br)
    {
        Position = new(
            br.ReadSingle(),
            br.ReadSingle(),
            br.ReadSingle(),
            br.ReadSingle());
        DistrictIndex = br.ReadByte();

        // Read and discard padding
        br.ReadBytes(15);
    }

    public void Write(BinaryWriter2 bw)
    {
        bw.Write(Position.X);
        bw.Write(Position.Y);
        bw.Write(Position.Z);
        bw.Write(Position.W);
        bw.Write(DistrictIndex);

        // Write padding
        bw.Write(new byte[15]);
    }
}

public class VFXBoxRegion : TriggerRegion
{
    public VFXBoxRegion() : base()
    {
        // Set meType to E_TYPE_VFXBOX_REGION
        meType = RegionType.E_TYPE_VFXBOX_REGION;
    }

    public override void Read(BinaryReader2 br)
    {
        base.Read(br);
    }

    public override void Write(BinaryWriter2 bw)
    {
        base.Write(bw);
    }
}

public class SpawnLocation
{
    public enum Type
    {
        E_TYPE_PLAYER_SPAWN = 0,
        E_TYPE_CAR_SELECT_LEFT = 1,
        E_TYPE_CAR_SELECT_RIGHT = 2,
        E_TYPE_CAR_UNLOCK = 3
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public Vector4 mPosition { get; set; } = new(0, 0, 0, 0);

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public Vector4 mDirection { get; set; } = new(0, 0, 0, 0);

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public CgsID mJunkyardId { get; set; } = new();

    public Type muType { get; set; } = 0;

    private byte[] padding = new byte[7];

    public void Read(BinaryReader2 br)
    {
        mPosition = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        mDirection = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        mJunkyardId = new();
        mJunkyardId.Read(br);
        muType = (Type)br.ReadByte();
        padding = br.ReadBytes(7);
    }

    public void Write(BinaryWriter2 bw)
    {
        bw.Write(mPosition.X);
        bw.Write(mPosition.Y);
        bw.Write(mPosition.Z);
        bw.Write(mPosition.W);
        bw.Write(mDirection.X);
        bw.Write(mDirection.Y);
        bw.Write(mDirection.Z);
        bw.Write(mDirection.W);
        mJunkyardId.Write(bw);
        bw.Write((byte)muType);
        bw.Write(padding);
    }
}

public class TriggerData : IEntryData
{
    public int miVersionNumber { get; set; }

    public uint muSize { get; set; }

    public Vector4 mPlayerStartPosition { get; set; }

    public Vector4 mPlayerStartDirection { get; set; }

    public List<Landmark> mpLandmarks { get; set; }

    public int miOnlineLandmarkCount { get; set; }

    public List<SignatureStunt> mpSignatureStunts { get; set; }

    public List<GenericRegion> mpGenericRegions { get; set; }

    [Editor(typeof(DescriptiveCollectionEditor), typeof(UITypeEditor))]
    public List<Killzone> mpKillzones { get; set; }

    public List<Blackspot> mpBlackspots { get; set; }

    public List<VFXBoxRegion> mpVFXBoxRegions { get; set; }

    public List<RoamingLocation> mpRoamingLocations { get; set; }

    public List<SpawnLocation> mpSpawnLocations { get; set; }

    private List<uint> TriggerOffsets { get; set; }

    // Decomp has 8-byte pointers, so each pointer/count pair is padded out to 16 bytes
    internal static long ReadPtr(BinaryReader2 br, bool x64) => x64
        ? (long)br.ReadUInt64()
        : br.ReadUInt32();

    internal static void WritePtr(BinaryWriter2 bw, long ptr, bool x64)
    {
        if (x64)
            bw.Write((ulong)ptr);
        else
            bw.Write((uint)ptr);
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        MemoryStream ms = entry.MakeStream();
        BinaryReader2 reader = new(ms)
        {
            BigEndian = entry.Console
        };

        bool x64 = entry.Platform == BundlePlatform.PCx64;

        int readCount()
        {
            int count = reader.ReadInt32();

            if (x64)
                reader.ReadBytes(4); // Padding

            return count;
        }

        miVersionNumber = reader.ReadInt32();
        muSize = reader.ReadUInt32();
        reader.ReadBytes(8);// skip 8 bytes of padding
        mPlayerStartPosition = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        mPlayerStartDirection = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        // read landmarks
        long landmarkTriggersOffset = ReadPtr(reader, x64);
        int miLandmarkCount = reader.ReadInt32();
        miOnlineLandmarkCount = reader.ReadInt32();

        long signatureStuntsOffset = ReadPtr(reader, x64);
        int miSignatureStuntCount = readCount();

        long genericRegionsOffset = ReadPtr(reader, x64);
        int miGenericRegionCount = readCount();

        long killzoneOffset = ReadPtr(reader, x64);
        int miKillzoneCount = readCount();

        long blackspotOffset = ReadPtr(reader, x64);
        int miBlackspotCount = readCount();

        long vfxBoxRegionOffset = ReadPtr(reader, x64);
        int miVFXBoxRegionCount = readCount();

        long roamingLocationOffset = ReadPtr(reader, x64);
        int miRoamingLocationCount = readCount();

        long spawnLocationOffset = ReadPtr(reader, x64);
        int miSpawnLocationCount = readCount();

        long triggerRegionOffset = ReadPtr(reader, x64);
        int miRegionCount = reader.ReadInt32();

        reader.BaseStream.Position = landmarkTriggersOffset;
        mpLandmarks = [];

        for (int i = 0; i < miLandmarkCount; i++)
        {
            Landmark landmark = new();
            landmark.Read(reader, x64);
            mpLandmarks.Add(landmark);
        }

        reader.BaseStream.Position = signatureStuntsOffset;

        // read signature stunts
        mpSignatureStunts = [];

        for (int i = 0; i < miSignatureStuntCount; i++)
        {
            SignatureStunt stunt = new();
            stunt.Read(reader, x64);
            mpSignatureStunts.Add(stunt);
        }

        reader.BaseStream.Position = genericRegionsOffset;

        // read generic regions
        mpGenericRegions = [];

        for (int i = 0; i < miGenericRegionCount; i++)
        {
            GenericRegion region = new();
            region.Read(reader);
            mpGenericRegions.Add(region);
        }

        reader.BaseStream.Position = killzoneOffset;

        // read killzones
        mpKillzones = [];

        for (int i = 0; i < miKillzoneCount; i++)
        {
            Killzone killzone = new();
            killzone.Read(reader, x64);
            mpKillzones.Add(killzone);
        }

        // read blackspots
        reader.BaseStream.Position = blackspotOffset;
        mpBlackspots = [];

        for (int i = 0; i < miBlackspotCount; i++)
        {
            Blackspot spot = new();
            spot.Read(reader);
            mpBlackspots.Add(spot);
        }

        reader.BaseStream.Position = vfxBoxRegionOffset;

        // read VFX box regions
        mpVFXBoxRegions = [];

        for (int i = 0; i < miVFXBoxRegionCount; i++)
        {
            VFXBoxRegion box = new();
            box.Read(reader);
            mpVFXBoxRegions.Add(box);
        }

        reader.BaseStream.Position = roamingLocationOffset;

        // read roaming locations
        mpRoamingLocations = [];

        for (int i = 0; i < miRoamingLocationCount; i++)
        {
            RoamingLocation location = new();
            location.Read(reader);
            mpRoamingLocations.Add(location);
        }

        reader.BaseStream.Position = spawnLocationOffset;

        mpSpawnLocations = [];

        for (int i = 0; i < miSpawnLocationCount; i++)
        {
            SpawnLocation location = new();
            location.Read(reader);
            mpSpawnLocations.Add(location);
        }

        reader.BaseStream.Position = triggerRegionOffset;

        // read TriggerRegion (miGenericCount + miLandmarkCount)
        TriggerOffsets = [];

        for (int i = 0; i < miRegionCount; i++)
        {
            uint section6Entry = (uint)ReadPtr(reader, x64);
            TriggerOffsets.Add(section6Entry);
        }

        return true;
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        TriggerDataEditor triggerDataEditor = new()
        {
            Trigger = this
        };

        triggerDataEditor.EditEvent += () =>
        {
            Write(entry);
        };
        return triggerDataEditor;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.TriggerData;
    }

    public bool Write(BundleEntry entry)
    {
        MemoryStream ms = new();
        BinaryWriter2 bw = new(ms)
        {
            BigEndian = entry.Console
        };

        bool x64 = entry.Platform == BundlePlatform.PCx64;
        int ptrSize = x64
            ? 8
            : 4;

        void writeCount(int count)
        {
            bw.Write(count);

            if (x64)
                bw.WriteUniquePadding(4); // Padding
        }

        // Engine indexes mppRegions[miRegionIndex]; table must be contiguous
        List<TriggerRegion> regions = [.. mpVFXBoxRegions, .. mpBlackspots, .. mpGenericRegions, .. mpLandmarks];

        // Ensure unique region IDs across generic, landmark, blackspot, and VFX regions
        int nextID = 0;

        foreach (TriggerRegion region in regions)
            nextID = Math.Max(nextID, region.mId + 1);

        HashSet<int> usedIDs = [];

        foreach (TriggerRegion region in regions)
            if (!usedIDs.Add(region.mId))
                region.mId = nextID++;

        TriggerRegion[] regionTable = new TriggerRegion[regions.Count];
        List<TriggerRegion> unplacedRegions = [];

        foreach (TriggerRegion region in regions)
        {
            if (region.miRegionIndex >= 0 && region.miRegionIndex < regionTable.Length && regionTable[region.miRegionIndex] == null)
                regionTable[region.miRegionIndex] = region;
            else
                unplacedRegions.Add(region);
        }

        int freeIndex = 0;

        foreach (TriggerRegion region in unplacedRegions)
        {
            while (regionTable[freeIndex] != null)
                freeIndex++;

            regionTable[freeIndex] = region;
            region.miRegionIndex = (short)freeIndex;
        }

        Dictionary<TriggerRegion, uint> regionOffsets = [];

        bw.Write(miVersionNumber);
        long sizePosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(4);
        bw.WriteUniquePadding(8); //padding
        bw.Write(mPlayerStartPosition.X);
        bw.Write(mPlayerStartPosition.Y);
        bw.Write(mPlayerStartPosition.Z);
        bw.Write(mPlayerStartPosition.W);
        bw.Write(mPlayerStartDirection.X);
        bw.Write(mPlayerStartDirection.Y);
        bw.Write(mPlayerStartDirection.Z);
        bw.Write(mPlayerStartDirection.W);

        long landmarkOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(ptrSize);
        bw.Write(mpLandmarks.Count);
        bw.Write(miOnlineLandmarkCount);
        long signatureStuntskOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(ptrSize);
        writeCount(mpSignatureStunts.Count);
        long genericRegionsOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(ptrSize);
        writeCount(mpGenericRegions.Count);
        long killzoneOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(ptrSize);
        writeCount(mpKillzones.Count);
        long blackspotOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(ptrSize);
        writeCount(mpBlackspots.Count);
        long vfxBoxRegionsOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(ptrSize);
        writeCount(mpVFXBoxRegions.Count);
        long roamingLocationsOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(ptrSize);
        writeCount(mpRoamingLocations.Count);
        long spawnLocationsOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(ptrSize);
        writeCount(mpSpawnLocations.Count);
        long triggerOffsetPosition = bw.BaseStream.Position;
        bw.WriteUniquePadding(ptrSize);
        bw.Write(mpLandmarks.Count + mpGenericRegions.Count + mpBlackspots.Count + mpVFXBoxRegions.Count);
        bw.WriteUniquePadding(4); // padding

        long currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = landmarkOffsetPosition;
        WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;

        foreach (Landmark landmark in mpLandmarks)
        {
            regionOffsets.Add(landmark, (uint)bw.BaseStream.Position);
            landmark.Write(bw, x64);
        }

        bw.WritePadding();

        currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = signatureStuntskOffsetPosition;
        WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;

        foreach (SignatureStunt stunt in mpSignatureStunts)
        {
            stunt.Write(bw, x64);
        }

        bw.WritePadding();

        currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = genericRegionsOffsetPosition;
        WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;
        Dictionary<int, uint> genericRegionOffsets = [];

        foreach (GenericRegion region in mpGenericRegions)
        {
            genericRegionOffsets.Add(region.mId, (uint)bw.BaseStream.Position);
            regionOffsets.Add(region, (uint)bw.BaseStream.Position);
            region.Write(bw);
        }

        bw.WritePadding();

        currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = killzoneOffsetPosition;
        WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;

        foreach (Killzone killzone in mpKillzones)
        {
            killzone.Write(bw, x64);
        }

        bw.WritePadding();

        currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = blackspotOffsetPosition;
        WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;

        foreach (Blackspot blackspot in mpBlackspots)
        {
            regionOffsets.Add(blackspot, (uint)bw.BaseStream.Position);
            blackspot.Write(bw);
        }

        bw.WritePadding();

        currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = vfxBoxRegionsOffsetPosition;
        WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;

        foreach (VFXBoxRegion region in mpVFXBoxRegions)
        {
            regionOffsets.Add(region, (uint)bw.BaseStream.Position);
            region.Write(bw);
        }

        bw.WritePadding();

        currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = roamingLocationsOffsetPosition;
        WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;

        foreach (RoamingLocation location in mpRoamingLocations)
        {
            location.Write(bw);
        }

        bw.WritePadding();

        currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = spawnLocationsOffsetPosition;
        WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;

        foreach (SpawnLocation location in mpSpawnLocations)
        {
            location.Write(bw);
        }

        bw.WritePadding();

        foreach (Landmark land in mpLandmarks)
        {
            land.WriteStartingGrid(bw, x64);
        }

        foreach (SignatureStunt stunt in mpSignatureStunts)
        {
            stunt.WriteStuntElements(bw, genericRegionOffsets, x64);
        }

        foreach (Killzone killzone in mpKillzones)
        {
            killzone.WritePointerStuff(bw, genericRegionOffsets, x64);
        }

        currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = triggerOffsetPosition;
        WritePtr(bw, currentPosition, x64);
        bw.BaseStream.Position = currentPosition;

        foreach (TriggerRegion region in regionTable)
            WritePtr(bw, regionOffsets[region], x64);

        if (x64)
            bw.WritePadding(); // Decomp's size includes the trailing padding

        currentPosition = bw.BaseStream.Position;
        bw.BaseStream.Position = sizePosition;
        bw.Write((uint)currentPosition);
        bw.BaseStream.Position = currentPosition;
        bw.WritePadding();

        bw.Flush();

        byte[] data = ms.ToArray();

        bw.Close();
        ms.Close();

        entry.EntryBlocks[0].Data = data;
        entry.Dirty = true;

        return true;
    }
}
