using System.IO;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

public class IDList : IEntryData
{
    public int ReferenceEntryIDOffset;
    public int Unknown2; // Might be a count of some sort
    public int Unknown3;
    public int Unknown4;
    public ulong ReferenceEntryID;
    public int Unknown6;
    public int Unknown7;

    public IDList()
    {
        
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        return null;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.EntryList;
    }

    private void Clear()
    {
        ReferenceEntryIDOffset = default;
        Unknown2 = default;
        Unknown3 = default;
        Unknown4 = default;
        ReferenceEntryID = default;
        Unknown6 = default;
        Unknown7 = default;
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        Clear();

        MemoryStream ms = entry.MakeStream();
        BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

        ReferenceEntryIDOffset = br.ReadInt32();
        Unknown2 = br.ReadInt32();
        Unknown3 = br.ReadInt32();
        Unknown4 = br.ReadInt32();
        ReferenceEntryID = br.ReadUInt64();
        Unknown6 = br.ReadInt32();
        Unknown7 = br.ReadInt32();

        br.Close();
        ms.Close();

        return true;
    }

    public bool Write(BundleEntry entry)
    {
        MemoryStream ms = new();
        BinaryWriter2 bw = new(ms);
        bw.BigEndian = entry.Console;

        bw.Write(ReferenceEntryIDOffset);
        bw.Write(Unknown2);
        bw.Write(Unknown3);
        bw.Write(Unknown4);
        bw.Write(ReferenceEntryID);
        bw.Write(Unknown6);
        bw.Write(Unknown7);

        bw.Flush();

        byte[] data = ms.ToArray();

        bw.Close();
        ms.Close();

        entry.EntryBlocks[0].Data = data;
        entry.Dirty = true;

        return true;
    }
}
