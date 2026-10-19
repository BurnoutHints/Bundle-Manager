using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using BundleFormat;
using BundleUtilities;
using BurnoutImage;
using PluginAPI;

namespace PVSFormat;

public struct ZoneList
{
    //public List<Vector2> Points; // Redundant; use Zones[zoneIndex].Points
    public List<Zone> Zones;
    //public List<uint> ZonePointStarts; // Per-zone start point index. Unnecessary as they are always sequential multiples of 4
    //public List<short> ZonePointCounts; // Per-zone point count. Unnecessary as there are always 4 points
    public uint TotalZones;
    //public uint TotalPoints; // Unnecessary, calculated during writing

    public ZoneList()
    {
        Zones = [];
        //ZonePointStarts = new();
        //ZonePointCounts = new();
    }
}

public struct Zone
{
    public List<Vector2> Points;
    //public List<Neighbour> SafeNeighbours; // Unnecessary, always empty
    public List<Neighbour> UnsafeNeighbours;
    public ulong ZoneId;
    //public short ZoneType; // Unnecessary, always 0
    //public short NumPoints; // Unnecessary, always 4
    //public short NumSafeNeighbours; // Unnecessary, always 0
    public short NumUnsafeNeighbours;
    //public ulong Flags; // Unnecessary, always 0

    public Zone()
    {
        Points = [];
        for (int i = 0; i < 400; i += 100)
            Points.Add(new(i % 200, i > 100 ? 0 : 100)); // 0x0,100x0,0x100,100x100
        //SafeNeighbours = new();
        UnsafeNeighbours = [];
    }
}

public struct Neighbour
{
    //Zone Zone; // Redundant, replaced by ZoneId
    public ulong ZoneId; 
    public NeighbourFlags Flags;
}

[Flags]
public enum NeighbourFlags
{
    E_RENDERFLAG_NONE = 0x0,
    E_NEIGHBOURFLAG_RENDER = 0x1,
    E_NEIGHBOURFLAG_IMMEDIATE = 0x2
}

public class PVS : IEntryData
{
    public ZoneList data;

    private Image _gameMap;
    internal ulong selectedZoneId = ulong.MaxValue;

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        data = new();

        BinaryReader2 br = new(entry.MakeStream());
        br.BigEndian = entry.Console;

        // Decomp has 8-byte pointers so the zone fields after the pointers shift by 8
        bool x64 = entry.Platform == BundlePlatform.PCx64;
        int ptrSize = x64 ? 8 : 4;
        int zoneIdOffset = x64 ? 0x18 : 0x10;
        long ReadPtr() => x64 ? (long)br.ReadUInt64() : br.ReadUInt32();

        // Get zone count
        br.BaseStream.Position = 4 * ptrSize;
        data.TotalZones = br.ReadUInt32();

        // Get zones
        br.BaseStream.Position = ptrSize;
        br.BaseStream.Position = ReadPtr();
        long zonePos;
        long unsafeNeighboursPos;
        long unsafeNeighbourZonePos;
        for (int i = 0; i < data.TotalZones; ++i)
        {
            Zone zone = new();
            zonePos = br.BaseStream.Position;

            // Get points
            br.BaseStream.Position = ReadPtr();
            for (int j = 0; j < 4; ++j)
            {
                zone.Points[j] = new(br.ReadSingle(), br.ReadSingle());
                br.SkipUniquePadding(8);
            }

            // Get unsafe neighbours
            br.BaseStream.Position = zonePos + zoneIdOffset + 0xE;
            zone.NumUnsafeNeighbours = br.ReadInt16();
            br.BaseStream.Position = zonePos + 2 * ptrSize;
            unsafeNeighboursPos = ReadPtr();
            for (int j = 0; j < zone.NumUnsafeNeighbours; ++j)
            {
                Neighbour neighbour = new();
                br.BaseStream.Position = unsafeNeighboursPos + 0x10 * j;
                unsafeNeighbourZonePos = ReadPtr();
                neighbour.Flags = (NeighbourFlags)br.ReadUInt32();
                br.BaseStream.Position = unsafeNeighbourZonePos + zoneIdOffset;
                neighbour.ZoneId = br.ReadUInt64();
                zone.UnsafeNeighbours.Add(neighbour);
            }

            // Get zone ID
            br.BaseStream.Position = zonePos + zoneIdOffset;
            zone.ZoneId = br.ReadUInt64();
            br.BaseStream.Position = zonePos + 0x30; // Seek to next zone

            data.Zones.Add(zone);
        }

        br.Close();

        _gameMap = GetGameMap(entry.Archive);

        return true;
    }

    public bool Write(BundleEntry entry)
    {
        MemoryStream ms = new();
        BinaryWriter2 bw = new(ms);
        bw.BigEndian = entry.Console;

        bool x64 = entry.Platform == BundlePlatform.PCx64;
        void WritePtr(uint ptr)
        {
            if (x64)
                bw.Write((ulong)ptr);
            else
                bw.Write(ptr);
        }

        // Update neighbours in case of zone deletion
        for (int i = 0; i < data.TotalZones; ++i)
        {
            for (int j = 0; j < data.Zones[i].NumUnsafeNeighbours; ++j)
            {
                if (data.Zones.FindIndex(z => z.ZoneId == data.Zones[i].UnsafeNeighbours[j].ZoneId) == -1)
                {
                    Zone zone = data.Zones[i];
                    zone.UnsafeNeighbours.RemoveAt(j);
                    zone.NumUnsafeNeighbours--;
                    data.Zones[i] = zone;
                }
            }
        }

        // Get necessary information before writing
        uint zonesPtr = x64 ? 0x30u : 0x20u;
        uint zonesLength = data.TotalZones * 0x30;
        uint neighboursPtr = zonesPtr + zonesLength;
        uint neighborsLength = 0;
        for (int i = 0; i < data.Zones.Count; ++i)
            neighborsLength += (uint)data.Zones[i].NumUnsafeNeighbours * 0x10;
        uint pointsPtr = neighboursPtr + neighborsLength;
        uint pointsLength = (uint)(data.Zones.Count * 0x4 * 0x10);
        uint zoneStartsPtr = pointsPtr + pointsLength;
        uint zoneStartsLength = data.TotalZones * 0x4;
        uint zoneCountsPtr = zoneStartsPtr + zoneStartsLength;
        if (zoneCountsPtr % 0x10 != 0) // Align
            zoneCountsPtr = (zoneCountsPtr & 0xFFFFFFF0) + 0x10;

        // Write ZoneList
        WritePtr(pointsPtr);
        WritePtr(zonesPtr);
        WritePtr(zoneStartsPtr);
        WritePtr(zoneCountsPtr);
        bw.Write(data.TotalZones);
        bw.Write(data.TotalZones * 0x4);

        // Write zones
        uint neighboursPosition = 0;
        for (int i = 0; i < data.TotalZones; ++i)
        {
            bw.BaseStream.Position = zonesPtr + i * 0x30;
            WritePtr((uint)(pointsPtr + i * 0x4 * 0x10)); // Points
            WritePtr(0); // Safe neighbours
            if (data.Zones[i].NumUnsafeNeighbours == 0) // Unsafe neighbours
                WritePtr(0);
            else
                WritePtr(neighboursPtr + neighboursPosition);
            if (!x64)
                bw.Write(0); // Padding
            bw.Write(data.Zones[i].ZoneId);
            bw.Write((short)0); // Zone type
            bw.Write((short)4); // Point count
            bw.Write((short)0); // Safe neighbour count
            bw.Write(data.Zones[i].NumUnsafeNeighbours); // Unsafe neighbour count
            bw.Write(0); // Flags

            // Write points
            bw.BaseStream.Position = pointsPtr + i * 0x4 * 0x10;
            for (int j = 0; j < 4; ++j)
            {
                bw.Write(data.Zones[i].Points[j].X);
                bw.Write(data.Zones[i].Points[j].Y);
                bw.BaseStream.Position += 0x8;
            }

            // Write neighbours
            for (int j = 0; j < data.Zones[i].NumUnsafeNeighbours; ++j)
            {
                bw.BaseStream.Position = neighboursPtr + neighboursPosition;
                ulong zoneId = data.Zones[i].UnsafeNeighbours[j].ZoneId;
                int neighbourZoneIndex = data.Zones.FindIndex(z => z.ZoneId == zoneId);
                uint neighbourZonePtr = (uint)(zonesPtr + neighbourZoneIndex * 0x30);
                WritePtr(neighbourZonePtr);
                bw.Write((uint)data.Zones[i].UnsafeNeighbours[j].Flags);
                neighboursPosition += 0x10;
            }
        }

        // Write zone start point indices
        bw.BaseStream.Position = zoneStartsPtr;
        for (int i = 0; i < data.TotalZones * 4; i += 4)
            bw.Write(i);

        // Write zone point counts
        bw.BaseStream.Position = zoneCountsPtr;
        for (int i = 0; i < data.TotalZones; ++i)
            bw.Write((short)4);

        bw.Align(16);

        bw.Flush();
        byte[] rawData = ms.ToArray();
        bw.Close();
        ms.Close();

        entry.EntryBlocks[0].Data = rawData;
        entry.Dirty = true;

        return true;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.ZoneList;
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        PVSEditor pvsForm = new();
        pvsForm.GameMap = _gameMap;
        pvsForm.Open(this);

        pvsForm.Edit += () =>
        {
            Write(entry);
        };

        return pvsForm;
    }

    private Image GetGameMap(BundleArchive archive)
    {
        ulong id = 0x9F55039D;
        BundleEntry descEntry1 = archive.GetEntryByID(id);
        if (descEntry1 == null)
        {
            string file = BundleCache.GetFileByEntryID(id);
            if (!string.IsNullOrEmpty(file))
            {
                BundleArchive archive2 = BundleArchive.Read(file);
                if (archive2 != null)
                    descEntry1 = archive2.GetEntryByID(id);
            }
        }

        if (descEntry1 == null)
        {
            string path = Path.GetDirectoryName(archive.Path) + Path.DirectorySeparatorChar + "GUITEXTURES.BIN";
            if (Path.Exists(path))
            {
                BundleArchive archive2 = BundleArchive.Read(path);
                if (archive2 != null)
                    descEntry1 = archive2.GetEntryByID(id);
            }
        }

        Image image = null;

        if (descEntry1 != null && descEntry1.Type == EntryType.Texture)
        {
            image = GameImage.GetImage(descEntry1.EntryBlocks[0].Data, descEntry1.EntryBlocks[1].Data, descEntry1.Platform);

            if (image != null)
                TextureCache.AddToCache(id, image);
        }
        return image;
    }
}
