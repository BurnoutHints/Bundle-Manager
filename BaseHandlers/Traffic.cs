using System.IO;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

public class Traffic : IEntryData
{
    public byte muDataVersion; // 44 up to v1.3, 45 after
    public ushort muNumHulls;
    public uint muSizeInBytes;
    public uint mpPvs;
    public uint mpapHulls;
    public uint mpapFlowTypes;
    public ushort muNumFlowTypes;
    public ushort muNumVehicleTypes;
    public byte muNumVehicleAssets;
    public byte muNumVehicleTraits;
    public ushort muNumKillZones;
    public ushort muNumKillZoneRegions;
    public uint mpaKillZoneIds;
    public uint mpaKillZones;
    public uint mpaKillZoneRegions;
    public uint mpaVehicleTypes;
    public uint mpaVehicleTypesUpdate;
    public uint mpaVehicleAssets;
    public uint mpaVehicleTraits;
    public TrafficLightCollection mTrafficLights;
    public byte muNumPaintColours;
    public uint mpaPaintColours;

    public class TrafficLightCollection
    {
        public ushort muNumTrafficLights;
        public ushort muNumTrafficLightTypes;
        public ushort muNumCoronas; 
        public uint mpaPosAndYRotations;
        public uint mpaInstanceIDs;
        public uint mpauInstanceTypes;
        public uint mpaTrafficLightTypes;
        public uint mpaCoronaTypes;
        public uint mpaCoronaPositions;
        public ushort[] mauInstanceHashOffsets;
        public uint mpauInstanceHashTable;
        public uint mpauInstanceHashToIndexLookup;
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        return null;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.TrafficData;
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        using MemoryStream ms = entry.MakeStream();
        using BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

        int pointerSize = entry.Platform == BundlePlatform.PCx64 ? 8 : 4;
        uint ReadPtr()
        {
            ms.Position = (ms.Position + pointerSize - 1) & -pointerSize;
            return pointerSize == 8 ? (uint)br.ReadUInt64() : br.ReadUInt32();
        }

        muDataVersion = br.ReadByte();
        br.ReadByte(); // Padding
        muNumHulls = br.ReadUInt16();
        muSizeInBytes = br.ReadUInt32();
        mpPvs = ReadPtr();
        mpapHulls = ReadPtr();
        mpapFlowTypes = ReadPtr();
        muNumFlowTypes = br.ReadUInt16();
        muNumVehicleTypes = br.ReadUInt16();
        muNumVehicleAssets = br.ReadByte();
        muNumVehicleTraits = br.ReadByte();
        muNumKillZones = br.ReadUInt16();
        muNumKillZoneRegions = br.ReadUInt16();
        mpaKillZoneIds = ReadPtr();
        mpaKillZones = ReadPtr();
        mpaKillZoneRegions = ReadPtr();
        mpaVehicleTypes = ReadPtr();
        mpaVehicleTypesUpdate = ReadPtr();
        mpaVehicleAssets = ReadPtr();
        mpaVehicleTraits = ReadPtr();

        mTrafficLights = new TrafficLightCollection
        {
            muNumTrafficLights = br.ReadUInt16(),
            muNumTrafficLightTypes = br.ReadUInt16(),
            muNumCoronas = br.ReadUInt16(),
            mpaPosAndYRotations = ReadPtr(),
            mpaInstanceIDs = ReadPtr(),
            mpauInstanceTypes = ReadPtr(),
            mpaTrafficLightTypes = ReadPtr(),
            mpaCoronaTypes = ReadPtr(),
            mpaCoronaPositions = ReadPtr(),
            mauInstanceHashOffsets = new ushort[129]
        };
        for (int i = 0; i < mTrafficLights.mauInstanceHashOffsets.Length; i++)
            mTrafficLights.mauInstanceHashOffsets[i] = br.ReadUInt16();
        mTrafficLights.mpauInstanceHashTable = ReadPtr();
        mTrafficLights.mpauInstanceHashToIndexLookup = ReadPtr();

        muNumPaintColours = br.ReadByte();
        mpaPaintColours = ReadPtr();

        return true;
    }

    public bool Write(BundleEntry entry)
    {
        return true;
    }
}
