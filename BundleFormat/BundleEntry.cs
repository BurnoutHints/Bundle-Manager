using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using BundleUtilities;

namespace BundleFormat;

public class EntryBlock
{
    public bool Compressed;
    public uint CompressedSize;
    public uint UncompressedSize;
    public uint UncompressedAlignment = 1; // default depending on file type, 1 (what empty blocks use) so new blocks never write alignment 0

    // As stored in the bundle, so saving can write unchanged compressed blocks without recompressing them
    private byte[] _rawData;
    public byte[] RawData
    {
        get => _rawData;
        set
        {
            _rawData = value;
            _data = null;
        }
    }

    // RawData inflated once, as the list, handlers and saving all read Data repeatedly
    private byte[] _data;
    public byte[] Data
    {
        get
        {
            if (RawData == null)
                return null;

            if (Compressed)
                return _data ??= RawData.Decompress();

            return RawData;
        }
        set
        {
            RawData = Compressed ? value.Compress() : value;
            if (Compressed)
                _data = value;

            UncompressedSize = (uint)value.Length;
            CompressedSize = (uint)RawData.Length;
        }
    }
}

public class EntryInfo
{
    public uint ID;
    public EntryType Type;
    public string Path;
    public DebugInfo DebugInfo;

    public EntryInfo(uint id, EntryType type, string path, DebugInfo debugInfo)
    {
        ID = id;
        Type = type;
        Path = path;
        DebugInfo = debugInfo;
    }
}

public struct Dependency
{
    public const int Size = 0x10; // uint64_t id, uint32_t pointer offset, uint32_t padding
    public ulong ID;
    public uint EntryPointerOffset;

    public static Dependency Read(BinaryReader2 br)
    {
        Dependency dep = new() { ID = br.ReadUInt64(), EntryPointerOffset = br.ReadUInt32() };
        br.ReadUInt32(); // Padding, not always zero on console
        return dep;
    }

    public readonly void Write(BinaryWriter2 bw)
    {
        bw.Write(ID);
        bw.Write(EntryPointerOffset);
        bw.Write(0u); // Padding
    }

    private static byte[] WriteTable(List<Dependency> imports, bool bigEndian)
    {
        MemoryStream ms = new();
        BinaryWriter2 bw = new(ms, bigEndian);
        foreach (Dependency dep in imports)
            dep.Write(bw);
        return ms.ToArray();
    }

    // Whether primary data already ends with this import table
    public static bool EndsWithTable(byte[] data, List<Dependency> imports, bool bigEndian)
    {
        byte[] table = WriteTable(imports, bigEndian);

        // Compare id and offset only, padding isn't always zero on console
        int start = data.Length - table.Length;
        bool present = start >= 0;
        for (int i = 0; present && i < table.Length; i++)
            present = i % Size >= 12 || data[start + i] == table[i];
        return present;
    }

    // Appends the import table to primary data unless it already ends with it
    public static byte[] AppendTable(byte[] data, List<Dependency> imports, bool bigEndian)
    {
        if (EndsWithTable(data, imports, bigEndian))
            return data;

        byte[] table = WriteTable(imports, bigEndian);
        int tableStart = (data.Length + 0xF) & ~0xF;
        byte[] result = new byte[tableStart + table.Length];
        data.CopyTo(result, 0);
        table.CopyTo(result, tableStart);
        return result;
    }

    // Guesses the import table at the end of primary data
    public static List<Dependency> ScanTable(byte[] data, bool bigEndian, int pointerSize)
    {
        List<Dependency> imports = [];

        if (data.Length % Size != 0)
            return imports;

        using BinaryReader2 br = new(new MemoryStream(data), bigEndian);

        for (int start = data.Length - Size; start >= 0; start -= Size)
        {
            br.BaseStream.Position = start;
            Dependency dep = Read(br);

            if (dep.ID == 0 || dep.ID > uint.MaxValue || dep.EntryPointerOffset % 4 != 0
                || (imports.Count > 0 && dep.EntryPointerOffset >= imports[^1].EntryPointerOffset))
                break;

            imports.Add(dep);
        }

        imports.Reverse();

        // Drop leading records until the last pointer slot fits before the table
        while (imports.Count > 0
            && imports[^1].EntryPointerOffset + pointerSize > data.Length - imports.Count * Size)
            imports.RemoveAt(0);

        return imports;
    }
}

public struct DebugInfo
{
    public string Name;
    public string TypeName;
}

public class BundleEntry
{
    public BundleArchive Archive;
    //public BundleReference Archive;

    public int Index;

    public ulong ID;
    public int DependenciesListOffset;
    public short DependencyCount;
    public List<Dependency> Dependencies;

    public DebugInfo DebugInfo;

    public EntryBlock[] EntryBlocks;

    public bool HasHeader => HasSection(0);
    public bool HasBody => HasSection(1);
    public bool HasThird => HasSection(2);

    public EntryType Type;

    public BundlePlatform Platform;
    public bool Console => Platform == BundlePlatform.X360 || Platform == BundlePlatform.PS3;

    public bool Dirty;

    public BundleEntry(BundleArchive archive)
    {
        Archive = archive;
        /*Archive = new BundleReference();
        Archive.Path = archive.Path;
        Archive.EntryCount = (uint)archive.Entries.Count;*/
        Dependencies = [];
    }

    public ulong ImportID => Imports().Aggregate(0UL, (id, dep) => id | dep.ID);

    // Secondary data is memory type 2 on PS3 and 1 elsewhere
    public int SecondaryBlock => HasSection(1) ? 1 : (HasSection(2) || Platform == BundlePlatform.PS3) ? 2 : 1;

    public bool HasSection(int section)
    {
        return EntryBlocks != null &&
               section < EntryBlocks.Length &&
               section >= 0 &&
               EntryBlocks[section]?.Data is { Length: > 0 };
    }

    // Read only, as Data is cached and saving writes unchanged blocks as they were read
    public MemoryStream MakeStream(bool body = false)
    {
        if (EntryBlocks == null)
            return null;

        return new MemoryStream(EntryBlocks[body ? 1 : 0].Data, false);
    }

    // The import table: the bundle's own list (BND1), or the one at the end of primary data
    private List<Dependency> Imports()
    {
        if (Dependencies.Count > 0 || DependencyCount == 0)
            return Dependencies;

        List<Dependency> imports = [];
        using BinaryReader2 br = new(MakeStream(), Console);
        br.BaseStream.Position = DependenciesListOffset;
        for (int i = 0; i < DependencyCount; i++)
            imports.Add(Dependency.Read(br));
        return imports;
    }

    public List<BundleDependency> GetDependencies()
    {
        List<BundleDependency> result = [];
        foreach (Dependency dep in Imports())
        {
            // TODO: look in other bundles through BundleCache.GetFileByEntryID
            int index = Archive.Entries.FindLastIndex(e => e.ID == dep.ID);
            result.Add(new BundleDependency
            {
                EntryID = dep.ID,
                EntryPointerOffset = (int)dep.EntryPointerOffset,
                EntryIndex = Math.Max(index, 0),
                Entry = index >= 0 ? Archive.Entries[index] : null
            });
        }
        return result;
    }

    public string DetectName()
    {
        if (!string.IsNullOrWhiteSpace(DebugInfo.Name))
            return DebugInfo.Name;

        return ResourceDB.Lookup(ID) ?? Archive.GuessName(ID) ?? "";
    }

    public static Color GetColor(EntryType type) => type switch
    {
        EntryType.Texture => Color.Orange,
        EntryType.Material => Color.HotPink,
        EntryType.Renderable => Color.Aquamarine,
        EntryType.InstanceList => Color.BlueViolet,
        EntryType.EntryList => Color.Tomato,
        EntryType.Model => Color.Yellow,
        EntryType.PolygonSoupList => Color.Goldenrod,
        EntryType.GraphicsSpec => Color.SeaGreen,
        _ => Color.Transparent
    };

    public static byte GetMemoryChunkCount(EntryType type) => type switch
    {
        EntryType.Texture => 2,
        EntryType.IndexBuffer => 2,
        EntryType.VertexBuffer => 2,
        EntryType.Renderable => 2,
        EntryType.ShaderProgramBuffer => 2,
        EntryType.VFXMeshCollection => 2,
        _ => 1,
    };
}
