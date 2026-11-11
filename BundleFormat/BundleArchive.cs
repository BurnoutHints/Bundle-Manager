using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using BundleUtilities;

namespace BundleFormat;

[Flags]
public enum Flags
{
    Compressed = 1,
    UnusedFlag1 = 2, // Always set
    UnusedFlag2 = 4, // Always set
    HasResourceStringTable = 8
}

public enum BundlePlatform
{
    PC = 1,
    X360 = 2,// << 24, // Big Endian
    PS3 = 3, // << 24  // Big Endian
    PCx64 = 4
}

public class BundleArchive
{
    public static readonly byte[] BND1Magic = [0x62, 0x6E, 0x64, 0x6C];
    public static readonly byte[] BND2Magic = [0x62, 0x6E, 0x64, 0x32];

    public string Path;
    public int BNDVersion;

    public int Version;
    public BundlePlatform Platform;
    public int RSTOffset; // Normally 30
    public int EntryCount;
    public int IDBlockOffset;

    //public int HeadStart;
    //public int BodyStart;
    //public int ArchiveSize;
    public Flags Flags;

    public bool Console => Platform == BundlePlatform.X360 || Platform == BundlePlatform.PS3;

    public string ResourceStringTable;
    public List<BundleEntry> Entries;

    private const ulong StringTableID = 0xC039284A;
    private const int BND1EntrySize = 0x70, BND1Blocks = 5, BND1DescriptorsSize = 0x28;

    // BND1 saves patch the original file since unhandled memory types and runtime fields are preserved in-place
    private byte[] bnd1File;
    private List<BundleEntry> bnd1Entries;

    private bool dirty;

    public bool Dirty
    {
        get
        {
            if (dirty)
                return true;

            foreach (BundleEntry entry in Entries)
            {
                if (entry.Dirty)
                    return true;
            }

            return false;
        }
        set
        {
            dirty = value;

            if (!dirty)
            {
                foreach (BundleEntry entry in Entries)
                {
                    entry.Dirty = false;
                }
            }
        }
    }

    public BundleArchive()
    {
        Entries = [];
    }

    public BundleEntry GetEntryByID(ulong id)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            BundleEntry entry = Entries[i];

            if (entry.ID == id)
                return entry;
        }

        return null;
    }

    // Replaces any entry with the same ID. Retain ID sorting like retail bundles
    public void AddEntry(BundleEntry entry)
    {
        Entries.RemoveAll(e => e.ID == entry.ID);
        int index = Entries.FindIndex(e => e.ID > entry.ID);
        Entries.Insert(index < 0
            ? Entries.Count
            : index, entry);

        for (int i = 0; i < Entries.Count; i++)
            Entries[i].Index = i;
    }

    public BundleEntry CreateEntry(ulong id, EntryType type, string name, byte[] primary, int importCount, byte[] secondary = null)
    {
        BundleEntry template = Entries.FirstOrDefault(e => e.Type == type);

        int secondaryBlock = template?.SecondaryBlock ?? (Platform == BundlePlatform.PS3
            ? 2
            : 1);

        byte[][] data = new byte[3][];
        data[0] = primary;
        data[secondaryBlock] = secondary;

        BundleEntry entry = new(this)
        {
            ID = id,
            Type = type,
            Platform = Platform,
            DependencyCount = (short)importCount,
            DependenciesListOffset = importCount > 0
                ? primary.Length - importCount * Dependency.Size
                : 0,
            DebugInfo = new()
            {
                Name = name,
                TypeName = type.ToString()
            },
            Dirty = true,
            EntryBlocks = new EntryBlock[3]
        };

        for (int i = 0; i < entry.EntryBlocks.Length; i++)
        {
            EntryBlock block = new()
            {
                Compressed = false
            };

            if (data[i] != null)
            {
                block.Data = data[i];
                block.UncompressedAlignment = template != null && template.HasSection(i)
                    ? template.EntryBlocks[i].UncompressedAlignment
                    : 16;
            }

            entry.EntryBlocks[i] = block;
        }

        return entry;
    }

    private Dictionary<ulong, string> guessedNames;
    private (string Path, int Count) guessedNamesKey;

    public string GuessName(ulong id)
    {
        if (guessedNames == null || guessedNamesKey != (Path, Entries.Count))
        {
            guessedNames = GuessNames();
            guessedNamesKey = (Path, Entries.Count);
        }

        return guessedNames.GetValueOrDefault(id);
    }

    private Dictionary<ulong, string> GuessNames()
    {
        List<string> names = ["worldvault", "postfxvault", "cameravault"];

        string currentFile = System.IO.Path.GetFileNameWithoutExtension(Path)?.ToUpper();

        if (currentFile != null)
        {
            if (currentFile.StartsWith("TRK_UNIT") && currentFile.EndsWith("_GR"))
            {
                string trackID = currentFile[8..].Replace("_GR", "").ToLower();
                names.AddRange(["trk_unit" + trackID + "_list", "prp_inst_" + trackID, "prp_gl__" + trackID,
                    "trk_unit" + trackID + "_passby", "trk_unit" + trackID + "_emitter"]);
            }

            names.Add(currentFile.ToLower() + ".swf");

            if (currentFile.StartsWith("WHE_") && currentFile.EndsWith("_GR"))
                names.Add(currentFile[4..].Replace("_GR", "").ToLower() + "_graphics");

            if (currentFile.StartsWith("VEH_"))
            {
                if (currentFile.EndsWith("_AT"))
                {
                    string vehicleID = currentFile[4..].Replace("_AT", "").ToLower();
                    string shownID = currentFile[4..].Replace("_AT", "");
                    names.AddRange([shownID + "_AttribSys", shownID + "DeformationModel", vehicleID + "_bpr",
                        vehicleID + "_anim", vehicleID + "_trafficstub", vehicleID + "_vanm"]);
                }
                else if (currentFile.EndsWith("_CD"))
                    names.Add(currentFile[4..].Replace("_CD", "").ToLower());
                else if (currentFile.EndsWith("_GR"))
                    names.Add(currentFile[4..].Replace("_GR", "").ToLower() + "_graphics");
            }
        }

        // WorldCol names
        for (int i = 0; i < Entries.Count; i++)
        {
            names.Add("trk_col_" + i);
            names.Add("trk_clil" + i);
        }

        // Resource IDs hash lowercase names; preserve original casing for display
        Dictionary<ulong, string> guesses = [];

        foreach (string name in names)
            guesses.TryAdd(CRC32.HashCRC32B(name.ToLowerInvariant()), name);

        return guesses;
    }

    public string GetEntryNameByID(ulong id)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            BundleEntry entry = Entries[i];

            if (entry.ID == id)
                return entry.DetectName();
        }

        return null;
    }

    public bool ContainsEntry(ulong id)
    {
        return GetEntryByID(id) != null;
    }

    private static Dictionary<ulong, DebugInfo> GetDebugInfoFromRST(string rst)
    {
        XmlDocument doc = new();

        try
        {
            doc.LoadXml(rst);
        }
        catch (XmlException)
        {
            // Cover Criterion's broken XML writer
            if (rst.StartsWith("</ResourceStringTable>"))
                rst = rst.Remove(1, 1);

            doc.LoadXml(rst.Replace("</ResourceStringTable>\n\t", ""));
        }

        Dictionary<ulong, DebugInfo> debugInfo = [];
        XmlElement root = doc["ResourceStringTable"];

        if (root == null)
            return debugInfo;

        foreach (XmlElement ele in root.GetElementsByTagName("Resource"))
        {
            if (ulong.TryParse(ele.GetAttribute("id"), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong resourceID))
                debugInfo[resourceID] = new()
                {
                    Name = ele.GetAttribute("name"),
                    TypeName = ele.GetAttribute("type")
                };
        }

        return debugInfo;
    }

    private void ProcessRST(string rst)
    {
        Dictionary<ulong, DebugInfo> debugInfo = GetDebugInfoFromRST(rst);

        foreach (BundleEntry entry in Entries)
        {
            if (debugInfo.TryGetValue(entry.ID, out DebugInfo debug))
                entry.DebugInfo = debug;
        }
    }

    private void RebuildRST()
    {
        XmlDocument doc = new();
        XmlElement root = doc.CreateElement("ResourceStringTable");
        doc.AppendChild(root);

        bool hasNamedResources = false;

        foreach (BundleEntry entry in Entries)
        {
            string name = entry.DebugInfo.Name?.Trim();

            if (string.IsNullOrEmpty(name))
                continue;

            XmlElement resource = doc.CreateElement("Resource");
            resource.SetAttribute("id", entry.ID.ToString("X8"));
            resource.SetAttribute("name", name);
            resource.SetAttribute(
                "type",
                string.IsNullOrWhiteSpace(entry.DebugInfo.TypeName)
                    ? entry.Type.ToString()
                    : entry.DebugInfo.TypeName
            );

            root.AppendChild(resource);
            hasNamedResources = true;
        }

        if (!hasNamedResources)
        {
            ResourceStringTable = null;
            Flags &= ~Flags.HasResourceStringTable;
            return;
        }

        ResourceStringTable = doc.OuterXml;
        Flags |= Flags.HasResourceStringTable;
    }

    public static BundleArchive Read(string path)
    {
        try
        {
            return Open(path);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
            return null;
        }
    }

    public static BundleArchive Open(string path)
    {
        using BinaryReader2 br = new(File.OpenRead(path));
        BundleArchive result = new()
        {
            Path = path
        };

        if (!result.Read(br))
            throw new ReadFailedError("This file isn't a bundle Bundle Manager can read.");

        return result;
    }

    public bool Read(BinaryReader2 br)
    {
        int currentType;
        byte[] bytes = br.ReadBytes(4);

        if (bytes.Matches(BND1Magic))
            currentType = 1;
        else if (bytes.Matches(BND2Magic))
            currentType = 2;
        else
            return false;

        BNDVersion = currentType;

        switch (currentType)
        {
            case 1:
                if (!ReadBND1(br))
                    return false;
                break;
            case 2:
                if (!ReadBND2(br))
                    return false;
                break;
        }

        br.Close();

        return true;
    }

    private bool ReadBND1(BinaryReader2 br)
    {
        br.BigEndian = true;

        long afterMagic = br.BaseStream.Position;
        br.BaseStream.Position = 0;
        bnd1File = br.ReadBytes((int)br.BaseStream.Length);
        br.BaseStream.Position = afterMagic;

        // Version number?
        br.ReadUInt32();

        uint entryCount = br.ReadUInt32();

        uint[] dataBlockSizes = new uint[5];

        for (int i = 0; i < dataBlockSizes.Length; i++)
        {
            dataBlockSizes[i] = br.ReadUInt32();
            br.BaseStream.Position += 4; // Padding
        }

        br.BaseStream.Position += 0x14; // Unknown Data

        uint idListOffset = br.ReadUInt32();
        uint idTableOffset = br.ReadUInt32();

        br.ReadUInt32(); // dependency block
        br.ReadUInt32(); // start of data block

        Platform = BundlePlatform.X360; // Xbox only for now
        br.ReadUInt32(); // might be platform (2 being X360)

        uint compressed = br.ReadUInt32();

        if (compressed != 0)
            Flags = Flags.Compressed; // TODO
        else
            Flags = 0;

        br.ReadUInt32(); // unknown purpose sometimes the same as entryCount

        uint uncompressedInfoOffset = br.ReadUInt32();

        br.ReadUInt32(); // main memory alignment
        br.ReadUInt32(); // graphics memory alignment

        Entries.Clear();

        br.BaseStream.Position = idListOffset;

        List<ulong> resourceIDs = [];

        for (int i = 0; i < entryCount; i++)
            resourceIDs.Add(br.ReadUInt64());

        br.BaseStream.Position = idTableOffset;

        foreach (ulong resourceID in resourceIDs)
        {
            BundleEntry entry = new(this)
            {
                EntryBlocks = [null, null, new()], // Only blocks 0, 1, and 3 are mapped
                ID = resourceID,
                Platform = Platform
            };

            br.ReadUInt32(); // unknown mem stuff

            entry.DependenciesListOffset = br.ReadInt32();
            entry.Type = (EntryType)br.ReadUInt32();

            //uint[] sizes = new uint[2];
            if (compressed != 0)
            {
                //sizes[0] = br.ReadUInt32();
                entry.EntryBlocks[0] = new()
                {
                    Compressed = true,
                    CompressedSize = br.ReadUInt32()
                };
                br.ReadUInt32(); // Alignment value, should be 1
                br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                br.ReadUInt32(); // Alignment value, should be 1

                //sizes[1] = br.ReadUInt32();
                entry.EntryBlocks[1] = new()
                {
                    Compressed = true,
                    CompressedSize = br.ReadUInt32()
                };
                br.ReadUInt32(); // Alignment value, should be 1
                br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                br.ReadUInt32(); // Alignment value, should be 1
                br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                br.ReadUInt32(); // Alignment value, should be 1
            }
            else
            {
                //sizes[0] = br.ReadUInt32();
                entry.EntryBlocks[0] = new()
                {
                    Compressed = false,
                    UncompressedSize = br.ReadUInt32(),
                    UncompressedAlignment = br.ReadUInt32()
                };
                br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                br.ReadUInt32(); // Alignment value

                //sizes[1] = br.ReadUInt32();
                entry.EntryBlocks[1] = new()
                {
                    Compressed = false,
                    UncompressedSize = br.ReadUInt32(),
                    UncompressedAlignment = br.ReadUInt32()
                };
                br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                br.ReadUInt32(); // Alignment value
                br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                br.ReadUInt32(); // Alignment value
            }

            uint dataBlockStartOffset = 0;

            for (int j = 0; j < dataBlockSizes.Length; j++)
            {
                if (j > 0)
                    dataBlockStartOffset += dataBlockSizes[j - 1];

                uint readOffset = br.ReadUInt32() + dataBlockStartOffset;
                br.ReadUInt32(); // 1

                if (j != 0 && j != 2)
                    continue; // Not supporting blocks 2, 4 and 5 right now.

                int mappedBlock = j;

                if (j == 2)
                    mappedBlock = 1;

                if (entry.EntryBlocks[mappedBlock] == null)
                    entry.EntryBlocks[mappedBlock] = new();

                EntryBlock entryBlock = entry.EntryBlocks[mappedBlock];

                //uint readSize = sizes[mappedBlock];
                uint readSize = compressed != 0
                    ? entryBlock.CompressedSize
                    : entryBlock.UncompressedSize;

                if (readSize == 0)
                {
                    entryBlock.RawData = null;
                    continue;
                }

                long pos = br.BaseStream.Position;
                br.BaseStream.Position = readOffset;

                entryBlock.RawData = ReadBlock(br, readSize, resourceID, j);

                br.BaseStream.Position = pos;
            }

            br.BaseStream.Position += 0x14; // unknown mem stuff

            Entries.Add(entry);
        }

        if (compressed != 0)
        {
            br.BaseStream.Position = uncompressedInfoOffset;

            for (int i = 0; i < Entries.Count; i++)
            {
                BundleEntry entry = Entries[i];

                //uint[] sizes = new uint[2];

                //sizes[0] = br.ReadUInt32();
                entry.EntryBlocks[0].UncompressedSize = br.ReadUInt32();
                entry.EntryBlocks[0].UncompressedAlignment = br.ReadUInt32();
                br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                br.ReadUInt32(); // Alignment value

                //sizes[1] = br.ReadUInt32();
                entry.EntryBlocks[1].UncompressedSize = br.ReadUInt32();
                entry.EntryBlocks[1].UncompressedAlignment = br.ReadUInt32();
                br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                br.ReadUInt32(); // Alignment value
                br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                br.ReadUInt32(); // Alignment value

                //for (int j = 0; j < 2; j++)
                //{
                //    EntryBlock entryBlock = entry.EntryBlocks[j];
                //    if (sizes[j] > 0)
                //        entryBlock.Data = entryBlock.Data.Decompress((int)sizes[j]);
                //}
            }
        }

        for (int i = 0; i < Entries.Count; i++)
        {
            BundleEntry entry = Entries[i];
            uint depOffset = (uint)entry.DependenciesListOffset;

            if (depOffset == 0)
                continue;

            br.BaseStream.Position = depOffset;

            entry.DependencyCount = (short)br.ReadUInt32();

            if (br.ReadUInt32() != 0)
                return false;

            for (int j = 0; j < entry.DependencyCount; j++)
                entry.Dependencies.Add(Dependency.Read(br));
        }

        bnd1Entries = [.. Entries];

        BundleEntry rst = GetEntryByID(StringTableID);

        if (rst != null)
        {
            BinaryReader2 br2 = new(rst.MakeStream())
            {
                BigEndian = false
            };

            // TODO: Store only the debug info and not the full string
            ResourceStringTable = br2.ReadLenString(br2.ReadInt32());

            br2.Close();

            ProcessRST(ResourceStringTable);

            Entries.Remove(rst);
        }

        for (int i = 0; i < Entries.Count; i++)
            Entries[i].Index = i;

        return true;
    }

    private static byte[] ReadBlock(BinaryReader2 br, uint size, ulong id, int block)
    {
        byte[] data = br.ReadBytes((int)size);

        if (data.Length != size)
            throw new ReadFailedError($"The file is cut short: resource 0x{id:X8} needs {size} bytes in block {block}, but only {data.Length} are left.");

        return data;
    }

    private bool ReadBND2(BinaryReader2 br)
    {
        br.BaseStream.Position += 4;

        Platform = ReadPlatform(br);

        br.BaseStream.Position -= 8;
        Version = br.ReadInt32();

        if (Version != 2)
        {
            throw new ReadFailedError("Unsupported Bundle Version: " + Version);
        }

        br.BaseStream.Position += 4;

        RSTOffset = br.ReadInt32();
        EntryCount = br.ReadInt32();
        IDBlockOffset = br.ReadInt32();
        uint[] fileBlockOffsets = [br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32()];
        Flags = (Flags)br.ReadInt32();

        // 8 Bytes Padding
        br.BaseStream.Position = IDBlockOffset;

        for (int i = 0; i < EntryCount; i++)
        {
            BundleEntry entry = new(this)
            {
                Index = i,

                Platform = Platform,

                ID = br.ReadUInt64()
            };

            br.ReadUInt64(); // Import Resource ID, which we now recompute on write

            entry.EntryBlocks = new EntryBlock[3];

            for (int j = 0; j < entry.EntryBlocks.Length; j++)
            {
                entry.EntryBlocks[j] = new()
                {
                    Compressed = Flags.HasFlag(Flags.Compressed)
                };
            }

            //uint[] blockUncompressedSizes = new uint[3];
            for (int j = 0; j < entry.EntryBlocks.Length; j++)
            {
                uint uncompressedSize = br.ReadUInt32();

                //blockUncompressedSizes[j] = uncompressedSize & ~(0xFU << 28);
                entry.EntryBlocks[j].UncompressedSize = uncompressedSize & ~(0xFU << 28);
                entry.EntryBlocks[j].UncompressedAlignment = (uint)(1 << ((int)uncompressedSize >> 28));
            }

            //uint[] blockCompressedSizes = new uint[3];
            for (int j = 0; j < entry.EntryBlocks.Length; j++)
            {
                //blockCompressedSizes[j] = br.ReadUInt32();
                entry.EntryBlocks[j].CompressedSize = br.ReadUInt32();
            }

            for (int j = 0; j < entry.EntryBlocks.Length; j++)
            {
                uint blockOffset = br.ReadUInt32();
                long lastAddr = br.BaseStream.Position;
                br.BaseStream.Position = blockOffset + fileBlockOffsets[j];

                EntryBlock block = entry.EntryBlocks[j];

                bool compressed = Flags.HasFlag(Flags.Compressed);

                //uint readSize = compressed ? blockCompressedSizes[j] : blockUncompressedSizes[j];
                uint readSize = compressed
                    ? entry.EntryBlocks[j].CompressedSize
                    : entry.EntryBlocks[j].UncompressedSize;

                if (readSize == 0)
                {
                    block.RawData = null;
                    br.BaseStream.Position = lastAddr;
                    continue;
                }

                block.RawData = ReadBlock(br, readSize, entry.ID, j);

                //if (compressed)
                //    block.Data = block.Data.Decompress((int)blockUncompressedSizes[j]);
                br.BaseStream.Position = lastAddr;
            }

            entry.DependenciesListOffset = br.ReadInt32();
            entry.Type = (EntryType)br.ReadInt32();
            entry.DependencyCount = br.ReadInt16();

            br.BaseStream.Position += 2; // Padding

            entry.Dirty = false;

            Entries.Add(entry);
        }

        if (Flags.HasFlag(Flags.HasResourceStringTable))
        {
            br.BaseStream.Position = RSTOffset;

            // TODO: Store only the debug info and not the full string
            ResourceStringTable = br.ReadCStr();

            ProcessRST(ResourceStringTable);
        }

        return true;
    }

    public void Write(string path, CancellationToken cancel = default)
    {
        string temp = path + ".saving";

        try
        {
            using (BinaryWriter2 bw = new(File.Create(temp), Console))
                Write(bw, cancel);
            cancel.ThrowIfCancellationRequested();
            File.Move(temp, path, true);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    // Rebuilds BND1 by patching modified blocks and adjusting downstream offsets
    private byte[] BuildBND1()
    {
        if (bnd1File == null)
            throw new NotSupportedException("Only BND1 bundles opened from a file can be saved.");

        uint read(int at) => BinaryPrimitives.ReadUInt32BigEndian(bnd1File.AsSpan(at));

        byte[] file = [.. bnd1File];

        void write(int at, uint value) => BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(at), value);

        HashSet<BundleEntry> known = [.. bnd1Entries];

        if (Entries.Any(e => !known.Contains(e)) || Entries.Count != known.Count(e => e.ID != StringTableID))
            throw new NotSupportedException("Adding or removing resources isn't supported when saving a BND1 bundle.");

        int idList = (int)read(0x48), table = (int)read(0x4C), uncompressedTable = (int)read(0x64);
        bool compressed = (read(0x5C) & 1) != 0;
        uint[] chunkStarts = new uint[BND1Blocks + 1];

        for (int j = 0; j < BND1Blocks; j++)
            chunkStarts[j + 1] = chunkStarts[j] + read(0x0C + j * 8);

        List<(int At, int Length, byte[] Bytes)> changes = [];

        for (int i = 0; i < bnd1Entries.Count; i++)
        {
            BundleEntry entry = bnd1Entries[i];
            int record = table + i * BND1EntrySize;

            if (BinaryPrimitives.ReadUInt64BigEndian(file.AsSpan(idList + i * 8)) != entry.ID || read(record + 8) != (uint)entry.Type)
                throw new NotSupportedException($"Changing the ID or type of resource 0x{entry.ID:X8} isn't supported when saving a BND1 bundle.");

            List<Dependency> imports = entry.GetImports();
            int importTable = (int)read(record + 4);

            if (imports.Count != (importTable == 0
                ? 0
                : (int)read(importTable)))
                throw new NotSupportedException($"Changing the number of imports of resource 0x{entry.ID:X8} isn't supported when saving a BND1 bundle.");

            for (int j = 0; j < imports.Count; j++)
            {
                int import = importTable + 8 + j * Dependency.Size;
                BinaryPrimitives.WriteUInt64BigEndian(file.AsSpan(import), imports[j].ID);
                write(import + 8, imports[j].EntryPointerOffset);
            }

            // Primary and secondary data blocks (memory types 0 and 2)
            for (int j = 0; j <= 2; j += 2)
            {
                EntryBlock block = entry.EntryBlocks[j / 2];
                bool tableInPrimary = j == 0 && entry.DependencyCount > 0 && entry.Dependencies.Count == 0;
                byte[] stored = block?.RawData ?? [];

                if (tableInPrimary)
                    stored = compressed
                        ? entry.GetPrimaryWithoutImports().Compress()
                        : entry.GetPrimaryWithoutImports();

                int length = (int)read(record + 0x0C + j * 8), at = (int)(read(record + 0x34 + j * 8) + chunkStarts[j]);

                if (length == 0 && stored.Length == 0 || length > 0 && stored.AsSpan().SequenceEqual(bnd1File.AsSpan(at, length)))
                    continue;

                if (length == 0)
                    throw new NotSupportedException($"Adding a data block to resource 0x{entry.ID:X8} isn't supported when saving a BND1 bundle.");

                write(record + 0x0C + j * 8, (uint)stored.Length);

                if (compressed)
                    write(uncompressedTable + i * BND1DescriptorsSize + j * 8, (uint)(tableInPrimary
                        ? entry.GetPrimaryWithoutImports().Length
                        : block?.Data?.Length ?? 0));

                changes.Add((at, length, stored));
            }
        }

        if (changes.Count == 0)
            return file;

        changes.Sort((a, b) => a.At.CompareTo(b.At));

        uint alignment = 0x10;

        if (!compressed)
        {
            for (int j = 0; j < BND1Blocks; j++)
                alignment = Math.Max(alignment, read(0x10 + j * 8));

            for (int i = 0; i < bnd1Entries.Count; i++)
                for (int j = 0; j < BND1Blocks; j++)
                    if (read(table + i * BND1EntrySize + 0x0C + j * 8) != 0)
                        alignment = Math.Max(alignment, read(table + i * BND1EntrySize + 0x10 + j * 8));
        }

        int step = (int)System.Numerics.BitOperations.RoundUpToPowerOf2(alignment), total = 0;
        int[] moved = new int[changes.Count], starts = new int[changes.Count];

        for (int k = 0; k < changes.Count; k++)
        {
            total += changes[k].Bytes.Length - changes[k].Length;

            if (changes[k].At + changes[k].Length < file.Length)
                total = (total + step - 1) & -step;

            moved[k] = total;
            starts[k] = changes[k].At;
        }

        uint movedTo(uint position)
        {
            int found = Array.BinarySearch(starts, (int)position);
            found = found >= 0
                ? found
                : ~found;
            return found == 0
                ? position
                : (uint)(position + moved[found - 1]);
        }

        uint[] newStarts = [.. chunkStarts.Select(movedTo)];

        for (int j = 0; j < BND1Blocks; j++)
            write(0x0C + j * 8, newStarts[j + 1] - newStarts[j]);

        for (int i = 0; i < bnd1Entries.Count; i++)
        {
            int record = table + i * BND1EntrySize;

            for (int j = 0; j < BND1Blocks; j++)
                if (read(record + 0x0C + j * 8) != 0)
                    write(record + 0x34 + j * 8, movedTo(read(record + 0x34 + j * 8) + chunkStarts[j]) - newStarts[j]);
        }

        byte[] result = new byte[file.Length + total];
        int from = 0, to = 0;

        for (int k = 0; k < changes.Count; k++)
        {
            Array.Copy(file, from, result, to, changes[k].At - from);
            to += changes[k].At - from;
            changes[k].Bytes.CopyTo(result, to);
            from = changes[k].At + changes[k].Length;
            to = from + moved[k];
        }

        Array.Copy(file, from, result, to, file.Length - from);
        return result;
    }

    public void Write(BinaryWriter2 bw, CancellationToken cancel = default)
    {
        if (BNDVersion == 1)
        {
            bnd1File = BuildBND1();
            bw.Write(bnd1File);
            return;
        }

        RebuildRST();

        bw.Write(BND2Magic);
        bw.Write(Version);
        bw.Write((int)Platform);

        long rstOffset = bw.BaseStream.Position;
        bw.Write(0);

        bw.Write(Entries.Count);

        long idBlockOffsetPos = bw.BaseStream.Position;
        bw.Write(0);

        long[] fileBlockOffsetsPos = new long[3];

        for (int i = 0; i < fileBlockOffsetsPos.Length; i++)
        {
            fileBlockOffsetsPos[i] = bw.BaseStream.Position;
            bw.BaseStream.Position += 4;
        }

        bw.Write((int)Flags);

        bw.Align(16);

        long currentOffset = bw.BaseStream.Position;
        bw.BaseStream.Position = rstOffset;
        bw.Write((uint)currentOffset);
        bw.BaseStream.Position = currentOffset;

        if (Flags.HasFlag(Flags.HasResourceStringTable))
        {
            // TODO: Rebuild RST from DebugInfo
            bw.WriteCStr(ResourceStringTable);

            bw.Align(16);
        }

        // ID Block
        currentOffset = bw.BaseStream.Position;
        bw.Seek((int)idBlockOffsetPos, SeekOrigin.Begin);
        bw.Write((int)currentOffset);
        bw.Seek((int)currentOffset, SeekOrigin.Begin);

        List<long[]> entryDataPointerPos = [];

        List<byte[][]> compressedBlocks = [];

        bool compress = Flags.HasFlag(Flags.Compressed);

        for (int i = 0; i < Entries.Count; i++)
        {
            cancel.ThrowIfCancellationRequested();
            BundleEntry entry = Entries[i];

            compressedBlocks.Add(new byte[3][]);

            bw.Write(entry.ID);
            bw.Write(entry.ImportID);

            for (int j = 0; j < entry.EntryBlocks.Length; j++)
            {
                EntryBlock entryBlock = entry.EntryBlocks[j];
                uint uncompressedSize = 0;

                if (entryBlock.Data != null)
                    uncompressedSize = (uint)entryBlock.Data.Length;

                //self.Write((entryBlock.UncompressedAlignment << 28) | uncompressedSize);
                bw.Write((uint)(uncompressedSize | (BitScan.BitScanReverse(entryBlock.UncompressedAlignment) << 28)));
            }

            for (int j = 0; j < entry.EntryBlocks.Length; j++)
            {
                EntryBlock entryBlock = entry.EntryBlocks[j];

                if (entryBlock.Data == null)
                    bw.Write((uint)0);
                else
                {
                    // Compress raw data if not already compressed
                    compressedBlocks[i][j] = !compress
                        ? entryBlock.Data
                        : entryBlock.Compressed
                            ? entryBlock.RawData
                            : entryBlock.Data.Compress();
                    bw.Write(compressedBlocks[i][j].Length);
                }
            }

            entryDataPointerPos.Add(new long[3]);

            for (int j = 0; j < entryDataPointerPos[i].Length; j++)
            {
                entryDataPointerPos[i][j] = bw.BaseStream.Position;
                bw.BaseStream.Position += 4;
            }

            bw.Write(entry.DependenciesListOffset);
            bw.Write((int)entry.Type);
            bw.Write(entry.DependencyCount);

            bw.BaseStream.Position += 2; // Padding
        }

        // Data Block
        for (int i = 0; i < 3; i++)
        {
            long blockStart = bw.BaseStream.Position;
            bw.BaseStream.Position = fileBlockOffsetsPos[i];
            bw.Write((uint)blockStart);
            bw.BaseStream.Position = blockStart;

            for (int j = 0; j < Entries.Count; j++)
            {
                cancel.ThrowIfCancellationRequested();
                byte[] diskData = compressedBlocks[j][i];

                if (diskData != null && diskData.Length > 0)
                {
                    long pos = bw.BaseStream.Position;
                    bw.BaseStream.Position = entryDataPointerPos[j][i];
                    bw.Write((uint)(pos - blockStart));
                    bw.BaseStream.Position = pos;

                    bw.Write(diskData);

                    bw.Align((i != 0 && j != Entries.Count - 1)
                        ? (byte)0x80
                        : (byte)16);
                }
            }

            if (i != 2)
                bw.Align(0x80);
        }
    }

    private static BundlePlatform ReadPlatform(BinaryReader2 br)
    {
        int platform = br.ReadInt32();
        br.BigEndian = platform > 0xFF;
        return (BundlePlatform)(br.BigEndian
            ? BinaryPrimitives.ReverseEndianness(platform)
            : platform);
    }

    public static bool VerifyMagic(BinaryReader2 br, byte[] magic)
    {
        byte[] readMagic = br.ReadBytes(magic.Length);

        if (readMagic.Matches(magic))
            return true;

        return false;
    }

    public static bool IsBundle(string path)
    {
        bool result;

        try
        {
            Stream s = File.Open(path, FileMode.Open, FileAccess.Read);
            BinaryReader2 br = new(s);

            result = VerifyMagic(br, BND2Magic);

            br.Close();
            s.Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message + "\n\n" + ex.StackTrace);
            result = false;
        }

        return result;
    }

    public static List<uint> GetEntryIDs(string path, bool console = false)
    {
        List<uint> result = [];

        Stream s = File.Open(path, FileMode.Open, FileAccess.Read);
        BinaryReader2 br = new(s);

        if (!VerifyMagic(br, BND2Magic))
        {
            br.Close();
            s.Close();
            return null;
        }

        br.BaseStream.Position += 4;

        ReadPlatform(br);

        br.BaseStream.Position = 0x10;
        int fileCount = br.ReadInt32();
        int metaStart = br.ReadInt32();

        br.BaseStream.Position = metaStart;

        for (int i = 0; i < fileCount; i++)
        {
            uint id = (uint)br.ReadUInt64();
            br.BaseStream.Position += 0x38;

            result.Add(id);
        }

        br.Close();
        s.Close();

        return result;
    }

    public static List<EntryInfo> GetEntryInfos(string path, bool console = false)
    {
        List<EntryInfo> result = [];

        Stream s = File.Open(path, FileMode.Open, FileAccess.Read);
        BinaryReader2 br = new(s);

        if (!VerifyMagic(br, BND2Magic))
        {
            br.Close();
            s.Close();
            return null;
        }

        br.BaseStream.Position += 4;

        ReadPlatform(br);

        br.BaseStream.Position = 0xC;
        uint rstOffset = br.ReadUInt32();
        int fileCount = br.ReadInt32();
        int metaStart = br.ReadInt32();

        br.BaseStream.Position += 0xC;
        Flags flags = (Flags)br.ReadInt32();

        Dictionary<ulong, DebugInfo> debugInfo = null;

        if (flags.HasFlag(Flags.HasResourceStringTable))
        {
            br.BaseStream.Position = rstOffset;

            // TODO: Store only the debug info and not the full string
            string rst = br.ReadCStr();

            debugInfo = GetDebugInfoFromRST(rst);
        }

        br.BaseStream.Position = metaStart;

        for (int i = 0; i < fileCount; i++)
        {
            uint id = (uint)br.ReadUInt64();
            br.BaseStream.Position += 0x30;
            EntryType currentType = (EntryType)br.ReadUInt32();
            br.BaseStream.Position += 0x4;

            DebugInfo debug = default;

            if (debugInfo != null && debugInfo.ContainsKey(id))
                debug = debugInfo[id];

            result.Add(new(id, currentType, path, debug));
        }

        br.Close();
        s.Close();

        return result;
    }

    public static EntryType GetEntryType(string path, uint id, bool console = false)
    {
        DebugTimer timer = DebugTimer.Start("GetEntryType");
        EntryType result = EntryType.Invalid;

        Stream s = File.Open(path, FileMode.Open, FileAccess.Read);
        BinaryReader2 br = new(s);

        if (!VerifyMagic(br, BND2Magic))
        {
            timer.StopLog();
            br.Close();
            s.Close();
            return EntryType.Invalid;
        }

        br.BaseStream.Position += 4;

        ReadPlatform(br);

        br.BaseStream.Position = 0x10;
        int fileCount = br.ReadInt32();
        int metaStart = br.ReadInt32();

        br.BaseStream.Position = metaStart;

        for (int i = 0; i < fileCount; i++)
        {
            uint id2 = (uint)br.ReadUInt64();
            br.BaseStream.Position += 0x30;
            EntryType currentType = (EntryType)br.ReadUInt32();
            br.BaseStream.Position += 0x4;

            if (id2 == id)
            {
                result = currentType;
                break;
            }
        }

        br.Close();
        s.Close();

        timer.StopLog();

        return result;
    }
}
