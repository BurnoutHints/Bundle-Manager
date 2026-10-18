using System.Collections.Generic;
using System.IO;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

public struct FlaptSection1Entry
{
    public byte Unknown00;
    public byte Unknown01;
    public byte Unknown02;
    public byte Unknown03;
    public byte Unknown04;
    public byte Unknown05;
    public byte Unknown06;
    public byte Unknown07;
    public short List1Count;
    public short Unknown0A;
    public int Unknown0C; // 0?

    public uint List1Offset;
    public List<short> List1;
    public uint Unknown14;
    public uint Unknown18;
    public uint Unknown1C;
    public uint Unknown20;
    public uint Unknown24;
    public uint Unknown28;
    public uint Unknown2C;
    public uint Unknown30;
    public uint Unknown34;
    public uint Unknown38;
    public uint Unknown3C;

    public uint NameOffset;
    public string Name;

    public override string ToString() => $"Name: {Name}";
}

public struct GeometryVertex
{
    public float X;
    public float Y;
    public U8Colour Colour;
    public float U;
    public float V;
}

// Text or font thing
public struct TextStyle
{
    public uint FontNameOffset;
    public string FontName;
    public U8Colour Colour;
    public float TextHeight;

    public override string ToString() => $"Font: {FontName}; Colour: {Colour}; Text height: {TextHeight}";
}

public struct FlaptSection5Entry
{
    public uint Hash;
    public uint CharacterNameOffset;
    public string CharacterName;

    public override string ToString() => $"Hash: 0x{Hash:X}; Character name: {CharacterName}";
}

public struct FlaptEventBinding
{
    public uint CharacterNameOffset;
    public string CharacterName;
    public uint EventNameOffset;
    public string EventName;
    public uint ActionNameOffset;
    public string ActionName;
    public int UnusedInt; // 0?

    public override string ToString() => $"Character: {CharacterName}; Event: {EventName}; Action: {ActionName}";
}

public struct StringPointer
{
    public uint StringOffset;
    public string String;

    public override string ToString() => String;
}

public class FlaptFile : IEntryData
{
    public uint FileSize;
    public float SecondsPerFrame; // 1/30
    public int Section1RecordCount;
    public uint Section1Offset;
    public int ImageIDCount;
    public uint ImageIDListOffset;
    public int GeometryVerticesCount;
    public uint GeometryVerticesOffset;
    public int TextStyleCount;
    public uint TextStyleListOffset;
    public int Section5And6RecordCount;
    public uint Section5Offset;
    public uint Section6Offset;
    public int EventBindsCount;
    public uint EventBindsOffset;
    public int TextStringsCount;
    public uint TextStringsOffset;
    public int DependencyRefStringsCount;
    public uint DependencyRefStringsOffset;
    public int CharacterRefStringsCount;
    public uint CharacterRefStringsOffset;

    public List<FlaptSection1Entry> Section1Entries;
    public List<sbyte> ImageIDs;
    public List<GeometryVertex> GeometryVertices;
    public List<TextStyle> TextStyleEntries;
    public List<FlaptSection5Entry> Section5Entries;
    public List<byte[]> Section6Entries;
    public List<FlaptEventBinding> EventBinds;
    public List<StringPointer> TextStrings;
    public List<StringPointer> DependencyRefStrings;
    public List<StringPointer> CharacterRefStrings;

    public FlaptFile()
    {
        Section1Entries = [];
        ImageIDs = [];
        GeometryVertices = [];
        TextStyleEntries = [];
        Section5Entries = [];
        Section6Entries = [];
        EventBinds = [];
        TextStrings = [];
        DependencyRefStrings = [];
        CharacterRefStrings = [];
    }

    private void Clear()
    {
        FileSize = default;
        SecondsPerFrame = default;
        Section1RecordCount = default;
        Section1Offset = default;
        ImageIDCount = default;
        ImageIDListOffset = default;
        GeometryVerticesCount = default;
        GeometryVerticesOffset = default;
        TextStyleCount = default;
        TextStyleListOffset = default;
        Section5And6RecordCount = default;
        Section5Offset = default;
        Section6Offset = default;
        EventBindsCount = default;
        EventBindsOffset = default;
        TextStringsCount = default;
        TextStringsOffset = default;
        DependencyRefStringsCount = default;
        DependencyRefStringsOffset = default;
        CharacterRefStringsCount = default;
        CharacterRefStringsOffset = default;

        Section1Entries.Clear();
        ImageIDs.Clear();
        GeometryVertices.Clear();
        TextStyleEntries.Clear();
        Section5Entries.Clear();
        Section6Entries.Clear();
        EventBinds.Clear();
        TextStrings.Clear();
        DependencyRefStrings.Clear();
        CharacterRefStrings.Clear();
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        Clear();

        MemoryStream ms = entry.MakeStream();
        BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

        bool x64 = entry.Platform == BundlePlatform.PCx64;
        uint ReadPtr()
        {
            if (!x64)
                return br.ReadUInt32();
            br.BaseStream.Position = (br.BaseStream.Position + 7) & ~7;
            return (uint)br.ReadUInt64();
        }

        br.BaseStream.Position += 4; // magic or something: 0C B0 B0 B0
        FileSize = br.ReadUInt32();
        SecondsPerFrame = br.ReadSingle();
        Section1RecordCount = br.ReadInt32();
        Section1Offset = ReadPtr();
        ImageIDCount = br.ReadInt32();
        ImageIDListOffset = ReadPtr();
        GeometryVerticesCount = br.ReadInt32();
        GeometryVerticesOffset = ReadPtr();
        TextStyleCount = br.ReadInt32();
        TextStyleListOffset = ReadPtr();
        Section5And6RecordCount = br.ReadInt32();
        Section5Offset = ReadPtr();
        Section6Offset = ReadPtr();
        EventBindsCount = br.ReadInt32();
        EventBindsOffset = ReadPtr();
        TextStringsCount = br.ReadInt32();
        TextStringsOffset = ReadPtr();
        DependencyRefStringsCount = br.ReadInt32();
        DependencyRefStringsOffset = ReadPtr();
        CharacterRefStringsCount = br.ReadInt32();
        CharacterRefStringsOffset = ReadPtr();


        br.BaseStream.Position = Section1Offset;
        for (int i = 0; i < Section1RecordCount; i++)
        {
            FlaptSection1Entry section1Entry = new();

            section1Entry.Unknown00 = br.ReadByte();
            section1Entry.Unknown01 = br.ReadByte();
            section1Entry.Unknown02 = br.ReadByte();
            section1Entry.Unknown03 = br.ReadByte();
            section1Entry.Unknown04 = br.ReadByte();
            section1Entry.Unknown05 = br.ReadByte();
            section1Entry.Unknown06 = br.ReadByte();
            section1Entry.Unknown07 = br.ReadByte();
            section1Entry.List1Count = br.ReadInt16();
            section1Entry.Unknown0A = br.ReadInt16();
            section1Entry.Unknown0C = (int)ReadPtr(); // File pointer, 0 on disk

            section1Entry.List1Offset = ReadPtr();
            section1Entry.Unknown14 = ReadPtr();
            section1Entry.Unknown18 = ReadPtr();
            section1Entry.Unknown1C = ReadPtr();
            section1Entry.Unknown20 = ReadPtr();
            section1Entry.Unknown24 = ReadPtr();
            section1Entry.Unknown28 = ReadPtr();
            section1Entry.Unknown2C = ReadPtr();
            section1Entry.Unknown30 = ReadPtr();
            section1Entry.Unknown34 = ReadPtr();
            section1Entry.Unknown38 = ReadPtr();
            section1Entry.Unknown3C = ReadPtr();

            section1Entry.NameOffset = ReadPtr();


            long oldPos = br.BaseStream.Position;

            if (section1Entry.List1Offset != 0)
            {
                br.BaseStream.Position = section1Entry.List1Offset;
                section1Entry.List1 = new List<short>(section1Entry.List1Count);
                for (int j = 0; j < section1Entry.List1Count; j++)
                {
                    section1Entry.List1.Add(br.ReadInt16());
                }
            }

            if (section1Entry.NameOffset != 0)
            {
                br.BaseStream.Position = section1Entry.NameOffset;
                section1Entry.Name = br.ReadCStr();
            }

            br.BaseStream.Position = oldPos;


            Section1Entries.Add(section1Entry);
        }


        br.BaseStream.Position = ImageIDListOffset;
        for (int i = 0; i < ImageIDCount; i++)
        {
            ImageIDs.Add(br.ReadSByte());
            br.BaseStream.Position += x64 ? 7 : 3; // Weird padding?
        }


        br.BaseStream.Position = GeometryVerticesOffset;
        for (int i = 0; i < GeometryVerticesCount; i++)
        {
            GeometryVertex vertex = new();

            vertex.X = br.ReadSingle();
            vertex.Y = br.ReadSingle();
            vertex.Colour = U8Colour.FromARGB32(br.ReadUInt32());
            vertex.U = br.ReadSingle();
            vertex.V = br.ReadSingle();

            GeometryVertices.Add(vertex);
        }


        br.BaseStream.Position = TextStyleListOffset;
        for (int i = 0; i < TextStyleCount; i++)
        {
            TextStyle textStyle = new();

            textStyle.FontNameOffset = ReadPtr();

            long oldPos = br.BaseStream.Position;
            br.BaseStream.Position = textStyle.FontNameOffset;
            textStyle.FontName = br.ReadCStr();
            br.BaseStream.Position = oldPos;

            textStyle.Colour = U8Colour.FromARGB32(br.ReadUInt32());
            textStyle.TextHeight = br.ReadSingle();

            TextStyleEntries.Add(textStyle);
        }


        br.BaseStream.Position = Section5Offset;
        for (int i = 0; i < Section5And6RecordCount; i++)
        {
            FlaptSection5Entry section5Entry = new();

            section5Entry.Hash = br.ReadUInt32();
            section5Entry.CharacterNameOffset = ReadPtr();

            long oldPos = br.BaseStream.Position;
            br.BaseStream.Position = section5Entry.CharacterNameOffset;
            section5Entry.CharacterName = br.ReadCStr();
            br.BaseStream.Position = oldPos;

            Section5Entries.Add(section5Entry);
        }

        br.BaseStream.Position = Section6Offset;
        for (int i = 0; i < Section5And6RecordCount; i++)
        {
            byte[] bytes = new byte[33];

            for (int j = 0; j < 33; j++)
            {
                bytes[j] = br.ReadByte();
            }

            Section6Entries.Add(bytes);
        }


        br.BaseStream.Position = EventBindsOffset;
        for (int i = 0; i < EventBindsCount; i++)
        {
            FlaptEventBinding eventBinding = new();

            eventBinding.CharacterNameOffset = ReadPtr();
            eventBinding.EventNameOffset = ReadPtr();
            eventBinding.ActionNameOffset = ReadPtr();
            eventBinding.UnusedInt = (int)ReadPtr();

            long oldPos = br.BaseStream.Position;

            br.BaseStream.Position = eventBinding.CharacterNameOffset;
            eventBinding.CharacterName = br.ReadCStr();

            if (eventBinding.EventNameOffset != 0)
            {
                br.BaseStream.Position = eventBinding.EventNameOffset;
                eventBinding.EventName = br.ReadCStr();
            }

            if (eventBinding.ActionNameOffset != 0)
            {
                br.BaseStream.Position = eventBinding.ActionNameOffset;
                eventBinding.ActionName = br.ReadCStr();
            }

            br.BaseStream.Position = oldPos;
            
            EventBinds.Add(eventBinding);
        }


        br.BaseStream.Position = TextStringsOffset;
        for (int i = 0; i < TextStringsCount; i++)
        {
            StringPointer textString = new();

            textString.StringOffset = ReadPtr();

            long oldPos = br.BaseStream.Position;
            br.BaseStream.Position = textString.StringOffset;
            textString.String = br.ReadCStr();
            br.BaseStream.Position = oldPos;

            TextStrings.Add(textString);
        }


        br.BaseStream.Position = DependencyRefStringsOffset;
        for (int i = 0; i < DependencyRefStringsCount; i++)
        {
            StringPointer dependencyRefString = new();

            dependencyRefString.StringOffset = ReadPtr();

            long oldPos = br.BaseStream.Position;
            br.BaseStream.Position = dependencyRefString.StringOffset;
            dependencyRefString.String = br.ReadCStr();
            br.BaseStream.Position = oldPos;

            DependencyRefStrings.Add(dependencyRefString);
        }


        br.BaseStream.Position = CharacterRefStringsOffset;
        for (int i = 0; i < CharacterRefStringsCount; i++)
        {
            StringPointer characterRefString = new();

            characterRefString.StringOffset = ReadPtr();

            long oldPos = br.BaseStream.Position;
            br.BaseStream.Position = characterRefString.StringOffset;
            characterRefString.String = br.ReadCStr();
            br.BaseStream.Position = oldPos;

            CharacterRefStrings.Add(characterRefString);
        }


        br.Close();
        ms.Close();

        return true;
    }

    public bool Write(BundleEntry entry)
    {
        // TODO
        return true;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.FlaptFile;
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        return null;
    }
}
