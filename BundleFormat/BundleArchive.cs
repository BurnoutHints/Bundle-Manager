using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using BundleUtilities;

namespace BundleFormat
{
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
        PS3 = 3// << 24 // Big Endian
    }

    public class BundleArchive
    {
        public static readonly byte[] BND1Magic = new byte[] { 0x62, 0x6E, 0x64, 0x6C };
        public static readonly byte[] BND2Magic = new byte[] { 0x62, 0x6E, 0x64, 0x32 };

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

        private bool _dirty;
        public bool Dirty
        {
            get
            {
                if (_dirty)
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
                _dirty = value;
                if (!_dirty)
                {
                    foreach(BundleEntry entry in Entries)
                    {
                        entry.Dirty = false;
                    }
                }
            }
        }

        public BundleArchive()
        {
            this.Entries = new List<BundleEntry>();
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
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(rst);

            XmlElement root = doc["ResourceStringTable"];
            XmlNodeList resources = root.GetElementsByTagName("Resource");

            Dictionary<ulong, DebugInfo> debugInfo = new Dictionary<ulong, DebugInfo>();

            foreach (XmlElement ele in resources)
            {
                if (ele.Name != "Resource")
                    continue;

                if (!ulong.TryParse(ele.Attributes["id"].Value, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out ulong resourceID))
                    continue;

                DebugInfo debug;
                debug.Name = ele.Attributes["name"].Value;
                debug.TypeName = ele.Attributes["type"].Value;

                debugInfo.Add(resourceID, debug);
            }

            return debugInfo;
        }

        private void ProcessRST(string rst)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(rst);

            XmlElement root = doc["ResourceStringTable"];
            XmlNodeList resources = root.GetElementsByTagName("Resource");

            foreach (XmlElement ele in resources)
            {
                if (!ulong.TryParse(ele.Attributes["id"].Value, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out ulong resourceID))
                    continue;

                foreach (var entry in Entries)
                {
                    if (entry.ID != resourceID)
                        continue;

                    entry.DebugInfo.Name = ele.Attributes["name"].Value;
                    entry.DebugInfo.TypeName = ele.Attributes["type"].Value;
                    break;
                }
            }
        }

        private void RebuildRST()
        {
            XmlDocument doc = new XmlDocument();
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
            using (Stream s = File.OpenRead(path))
            {
                try
                {
                    BinaryReader2 br = new BinaryReader2(s);

                    BundleArchive result = new BundleArchive();
                    result.Path = path;

                    if (!result.Read(br))
                    {
                        br.Close();

                        return null;
                    }

                    br.Close();

                    return result;
                }
                catch (IOException ex)
                {
                    Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
                    return null;
                } catch (ReadFailedError ex)
                {
                    Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
                    return null;
                }
            }
        }

        public bool Read(BinaryReader2 br)
        {
            int type;
            byte[] bytes = br.ReadBytes(4);
            if (bytes.Matches(BND1Magic))
                type = 1;
            else if (bytes.Matches(BND2Magic))
                type = 2;
            else
                return false;

            BNDVersion = type;

            switch (type)
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

            List<ulong> resourceIds = new List<ulong>();
            for (int i = 0; i < entryCount; i++)
                resourceIds.Add(br.ReadUInt64());

            br.BaseStream.Position = idTableOffset;

            foreach (ulong resourceId in resourceIds)
            {
                BundleEntry entry = new BundleEntry(this);
                entry.EntryBlocks = new EntryBlock[3];
                entry.ID = resourceId;

                br.ReadUInt32(); // unknown mem stuff

                entry.DependenciesListOffset = br.ReadInt32();
                entry.Type = (EntryType)br.ReadUInt32();

                //uint[] sizes = new uint[2];

                if (compressed != 0)
                {
                    //sizes[0] = br.ReadUInt32();
                    entry.EntryBlocks[0] = new EntryBlock();
                    entry.EntryBlocks[0].Compressed = true;
                    entry.EntryBlocks[0].CompressedSize = br.ReadUInt32();
                    br.ReadUInt32(); // Alignment value, should be 1
                    br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                    br.ReadUInt32(); // Alignment value, should be 1
                    //sizes[1] = br.ReadUInt32();
                    entry.EntryBlocks[1] = new EntryBlock();
                    entry.EntryBlocks[1].Compressed = true;
                    entry.EntryBlocks[1].CompressedSize = br.ReadUInt32();
                    br.ReadUInt32(); // Alignment value, should be 1
                    br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                    br.ReadUInt32(); // Alignment value, should be 1
                    br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                    br.ReadUInt32(); // Alignment value, should be 1
                } else
                {
                    //sizes[0] = br.ReadUInt32();
                    entry.EntryBlocks[0] = new EntryBlock();
                    entry.EntryBlocks[0].Compressed = false;
                    entry.EntryBlocks[0].UncompressedSize = br.ReadUInt32();
                    entry.EntryBlocks[0].UncompressedAlignment = br.ReadUInt32();
                    br.ReadUInt32(); // other blocks. Maybe used but I'm ignoring it.
                    br.ReadUInt32(); // Alignment value
                    //sizes[1] = br.ReadUInt32();
                    entry.EntryBlocks[1] = new EntryBlock();
                    entry.EntryBlocks[1].Compressed = false;
                    entry.EntryBlocks[1].UncompressedSize = br.ReadUInt32();
                    entry.EntryBlocks[1].UncompressedAlignment = br.ReadUInt32();
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
                        entry.EntryBlocks[mappedBlock] = new EntryBlock();
                    EntryBlock entryBlock = entry.EntryBlocks[mappedBlock];

                    //uint readSize = sizes[mappedBlock];
                    uint readSize = compressed != 0 ? entryBlock.CompressedSize : entryBlock.UncompressedSize;
                    if (readSize == 0)
                    {
                        entryBlock.RawData = null;
                        continue;
                    }

                    long pos = br.BaseStream.Position;
                    br.BaseStream.Position = readOffset;

                    entryBlock.RawData = br.ReadBytes((int)readSize);

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
                {
                    Dependency dep = new Dependency();
                    dep.ID = br.ReadUInt64();
                    dep.EntryPointerOffset = br.ReadUInt32();

                    br.ReadUInt32(); // Skip

                    entry.Dependencies.Add(dep);
                }
            }

            BundleEntry rst = GetEntryByID(0xC039284A);
            if (rst == null)
                return true;

            BinaryReader2 br2 = new BinaryReader2(rst.MakeStream());
            br2.BigEndian = false;

            // TODO: Store only the debug info and not the full string
            ResourceStringTable = br2.ReadLenString(br2.ReadInt32());

            br2.Close();

            // Cover Criterion's broken XML writer.
            if (ResourceStringTable.StartsWith("</ResourceStringTable>"))
                ResourceStringTable = ResourceStringTable.Remove(1, 1);
            string badLine = "</ResourceStringTable>\n\t";
            int index = ResourceStringTable.IndexOf(badLine);
            if (index > -1)
                ResourceStringTable = ResourceStringTable.Remove(index, badLine.Length);

            ProcessRST(ResourceStringTable);

            Entries.Remove(rst);

            return true;
        }

        private bool ReadBND2(BinaryReader2 br)
        {
            br.BaseStream.Position += 4;

            int platform = br.ReadInt32();
            if (platform != 1)
                platform = BinaryPrimitives.ReverseEndianness(platform);
            Platform = (BundlePlatform)platform;
            br.BigEndian = Console;

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
            uint[] fileBlockOffsets = new uint[3];
            fileBlockOffsets[0] = br.ReadUInt32();
            fileBlockOffsets[1] = br.ReadUInt32();
            fileBlockOffsets[2] = br.ReadUInt32();
            Flags = (Flags)br.ReadInt32();

            // 8 Bytes Padding

            br.BaseStream.Position = IDBlockOffset;

            for (int i = 0; i < EntryCount; i++)
            {
                BundleEntry entry = new BundleEntry(this);

                entry.Index = i;

                entry.Platform = Platform;

                entry.ID = br.ReadUInt64();

                entry.References = br.ReadUInt64();

                entry.EntryBlocks = new EntryBlock[3];
                for (int j = 0; j < entry.EntryBlocks.Length; j++)
                {
                    entry.EntryBlocks[j] = new EntryBlock();
                    entry.EntryBlocks[j].Compressed = Flags.HasFlag(Flags.Compressed);
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
                    uint readSize = compressed ? entry.EntryBlocks[j].CompressedSize : entry.EntryBlocks[j].UncompressedSize;
                    if (readSize == 0)
                    {
                        block.RawData = null;
                        br.BaseStream.Position = lastAddr;
                        continue;
                    }

                    block.RawData = br.ReadBytes((int)readSize);
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

        public void Write(string path)
        {
            Stream s = File.Open(path, FileMode.Create);
            BinaryWriter2 bw = new BinaryWriter2(s);
            bw.BigEndian = Console;

            Write(bw);

            bw.Flush();
            bw.Close();
        }

        public void Write(BinaryWriter2 bw)
        {
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

            List<long[]> entryDataPointerPos = new List<long[]>();

            List<byte[][]> compressedBlocks = new List<byte[][]>();

            for (int i = 0; i < this.Entries.Count; i++)
            {
                BundleEntry entry = this.Entries[i];

                compressedBlocks.Add(new byte[3][]);

                bw.Write(entry.ID);
                bw.Write(entry.References);

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
                        compressedBlocks[i][j] = entryBlock.Data.Compress();
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
                    BundleEntry entry = Entries[j];
                    EntryBlock entryBlock = entry.EntryBlocks[i];
                    bool compressed = Flags.HasFlag(Flags.Compressed);
                    uint size = compressed ? (compressedBlocks[j][i] == null ? 0 : (uint)compressedBlocks[j][i].Length) : (uint)entryBlock.Data.Length;

                    if (size > 0)
                    {
                        long pos = bw.BaseStream.Position;
                        bw.BaseStream.Position = entryDataPointerPos[j][i];
                        bw.Write((uint)(pos - blockStart));
                        bw.BaseStream.Position = pos;

                        if (compressed)
                            bw.Write(compressedBlocks[j][i]);
                        else
                            bw.Write(entryBlock.Data);

                        bw.Align((i != 0 && j != Entries.Count - 1) ? (byte)0x80 : (byte)16);
                    }
                }

                if (i != 2)
                    bw.Align(0x80);
            }
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
                BinaryReader2 br = new BinaryReader2(s);

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
            List<uint> result = new List<uint>();

            Stream s = File.Open(path, FileMode.Open, FileAccess.Read);
            BinaryReader2 br = new BinaryReader2(s);

            if (!VerifyMagic(br, BND2Magic))
            {
                br.Close();
                s.Close();
                return null;
            }

            br.BaseStream.Position += 4;

            int platformInt = br.ReadInt32();
            if (platformInt != 1)
                platformInt = BinaryPrimitives.ReverseEndianness(platformInt);
            BundlePlatform platform = (BundlePlatform)platformInt;
            br.BigEndian = platform == BundlePlatform.X360 || platform == BundlePlatform.PS3;

            br.BaseStream.Position = 0x10;
            int fileCount = br.ReadInt32();
            int metaStart = br.ReadInt32();

            br.BaseStream.Position = metaStart;

            for (int i = 0; i < fileCount; i++)
            {
                uint id = br.ReadUInt32();
                br.BaseStream.Position += 0x3C;

                result.Add(id);
            }

            br.Close();
            s.Close();

            return result;
        }

        public static List<EntryInfo> GetEntryInfos(string path, bool console = false)
        {
            List<EntryInfo> result = new List<EntryInfo>();

            Stream s = File.Open(path, FileMode.Open, FileAccess.Read);
            BinaryReader2 br = new BinaryReader2(s);

            if (!VerifyMagic(br, BND2Magic))
            {
                br.Close();
                s.Close();
                return null;
            }

            br.BaseStream.Position += 4;

            int platformInt = br.ReadInt32();
            if (platformInt != 1)
                platformInt = BinaryPrimitives.ReverseEndianness(platformInt);
            BundlePlatform platform = (BundlePlatform)platformInt;
            br.BigEndian = platform == BundlePlatform.X360 || platform == BundlePlatform.PS3;
            
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
                uint id = br.ReadUInt32();
                br.BaseStream.Position += 0x34;
                EntryType type = (EntryType)br.ReadUInt32();
                br.BaseStream.Position += 0x4;

                DebugInfo debug = default;
                
                if (debugInfo != null && debugInfo.ContainsKey(id))
                    debug = debugInfo[id];

                result.Add(new EntryInfo(id, type, path, debug));
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
            BinaryReader2 br = new BinaryReader2(s);

            if (!VerifyMagic(br, BND2Magic))
            {
                timer.StopLog();
                br.Close();
                s.Close();
                return EntryType.Invalid;
            }
            
            br.BaseStream.Position += 4;

            int platformInt = br.ReadInt32();
            if (platformInt != 1)
                platformInt = BinaryPrimitives.ReverseEndianness(platformInt);
            BundlePlatform platform = (BundlePlatform)platformInt;
            br.BigEndian = platform == BundlePlatform.X360 || platform == BundlePlatform.PS3;
            
            br.BaseStream.Position = 0x10;
            int fileCount = br.ReadInt32();
            int metaStart = br.ReadInt32();

            br.BaseStream.Position = metaStart;

            for (int i = 0; i < fileCount; i++)
            {
                uint id2 = br.ReadUInt32();
                br.BaseStream.Position += 0x34;
                EntryType type = (EntryType) br.ReadUInt32();
                br.BaseStream.Position += 0x4;

                if (id2 == id)
                {
                    result = type;
                    break;
                }
            }

            br.Close();
            s.Close();

            timer.StopLog();

            return result;
        }
    }
}
