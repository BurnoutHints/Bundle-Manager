using System.Collections.Generic;
using System.IO;
using System.Linq;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers.EnvironmentSettings;

// BrnWorld::EnvironmentSettings::TimeLine, see https://burnout.wiki/wiki/Environment_Timeline
// A location's mpfKeyframeTimes and mppKeyframes share a count, so each pair is kept as one TimeLineKeyframe.

public class TimeLineKeyframe
{
    public const float SecondsPerDay = 24 * 60 * 60;

    // Time of day in seconds
    public float mfTime { get; set; }

    // ID of the imported keyframe
    public ulong mpKeyframe { get; set; }
}

public class TimeLineLocation
{
    // In time order, the game looks times up by bracketing them
    public List<TimeLineKeyframe> Keyframes { get; set; } = [];
}

public class EnvironmentTimeLine : IEntryData
{
    public uint muVersion { get; set; } = 1;
    public List<TimeLineLocation> mpLocationDatii { get; set; } = [];

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        using MemoryStream ms = entry.MakeStream();
        using BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

        // Decomp has 8-byte pointers, aligned to 8 bytes
        bool x64 = entry.Platform == BundlePlatform.PCx64;
        int ptrSize = x64 ? 8 : 4;
        long ReadPtr()
        {
            if (!x64)
                return br.ReadUInt32();
            br.Align(8);
            return (long)br.ReadUInt64();
        }

        muVersion = br.ReadUInt32();
        if (muVersion != 1)
            throw new ReadFailedError($"Unsupported environment time line version {muVersion}, expected 1.");
        int numLocations = br.ReadInt32();
        long locations = ReadPtr();

        // The keyframe pointers are filled by the imports
        Dictionary<long, ulong> imports = entry.GetDependencies().ToDictionary(d => (long)d.EntryPointerOffset, d => d.EntryID);

        mpLocationDatii = [];
        for (int i = 0; i < numLocations; i++)
        {
            br.BaseStream.Position = locations + i * (x64 ? 0x18 : 0xC);
            int numKeyframes = br.ReadInt32();
            long times = ReadPtr();
            long keyframes = ReadPtr();

            TimeLineLocation location = new();
            for (int k = 0; k < numKeyframes; k++)
            {
                br.BaseStream.Position = times + k * 4;
                location.Keyframes.Add(new TimeLineKeyframe { mfTime = br.ReadSingle(), mpKeyframe = imports.GetValueOrDefault(keyframes + k * ptrSize) });
            }
            mpLocationDatii.Add(location);
        }

        return true;
    }

    // Keyframes are saved in time order.
    public byte[] Serialize(BundleEntry entry, out int importCount)
    {
        for (int i = 0; i < mpLocationDatii.Count; i++)
        {
            List<TimeLineKeyframe> keyframes = mpLocationDatii[i].Keyframes;
            if (keyframes.Count == 0)
                throw new InvalidDataException($"Location {i} has no keyframes, it needs at least one.");
            if (keyframes.FirstOrDefault(k => k.mfTime < 0 || k.mfTime > TimeLineKeyframe.SecondsPerDay) is TimeLineKeyframe late)
                throw new InvalidDataException($"Location {i} has a keyframe at {late.mfTime} seconds, times go from 0 to {TimeLineKeyframe.SecondsPerDay}.");
            if (keyframes.Any(k => k.mpKeyframe == 0))
                throw new InvalidDataException($"Location {i} has a time without a keyframe.");
        }

        using MemoryStream ms = new();
        using BinaryWriter2 bw = new(ms);
        bw.BigEndian = entry.Console;

        bool x64 = entry.Platform == BundlePlatform.PCx64;
        void Align(int alignment)
        {
            while (ms.Position % alignment != 0)
                bw.Write((byte)0);
        }
        void Ptr(long value)
        {
            if (!x64)
            {
                bw.Write((uint)value);
                return;
            }
            Align(8);
            bw.Write((ulong)value);
        }
        void Patch(long pos, long value)
        {
            long end = ms.Position;
            ms.Position = pos;
            Ptr(value);
            ms.Position = end;
        }

        bw.Write(muVersion);
        bw.Write(mpLocationDatii.Count);
        Ptr(0x10); // The locations follow the header

        Align(16);
        List<(long Times, long Keyframes)> pointers = [];
        foreach (TimeLineLocation location in mpLocationDatii)
        {
            bw.Write(location.Keyframes.Count);
            Align(x64 ? 8 : 4);
            long times = ms.Position;
            Ptr(0);
            long keyframes = ms.Position;
            Ptr(0);
            pointers.Add((times, keyframes));
        }

        // Each location's keyframe pointers, which the imports fill, then its times
        List<Dependency> imports = [];
        for (int i = 0; i < mpLocationDatii.Count; i++)
        {
            TimeLineKeyframe[] keyframes = [.. mpLocationDatii[i].Keyframes.OrderBy(k => k.mfTime)];

            Align(16);
            Patch(pointers[i].Keyframes, ms.Position);
            foreach (TimeLineKeyframe keyframe in keyframes)
            {
                imports.Add(new Dependency { ID = keyframe.mpKeyframe, EntryPointerOffset = (uint)ms.Position });
                Ptr(0);
            }

            Align(16);
            Patch(pointers[i].Times, ms.Position);
            foreach (TimeLineKeyframe keyframe in keyframes)
                bw.Write(keyframe.mfTime);
        }
        Align(16);
        bw.Flush();

        importCount = imports.Count;
        return Dependency.AppendTable(ms.ToArray(), imports, entry.Console);
    }

    public bool Write(BundleEntry entry)
    {
        byte[] data = Serialize(entry, out int importCount);

        entry.EntryBlocks[0].Data = data;
        entry.Dependencies.Clear(); // A BND1 import list would hide the table
        entry.DependencyCount = (short)importCount;
        entry.DependenciesListOffset = importCount > 0 ? data.Length - importCount * Dependency.Size : 0;
        entry.Dirty = true;

        return true;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.BrnEnvironmentTimeLine;
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        return new PropertyGridEditor("Environment Time Line Editor", () => Serialize(entry, out _), () => Write(entry),
            [.. mpLocationDatii.Select((location, i) => ($"Location {i}", (object)new TimeLineView(location, entry)))]);
    }
}
