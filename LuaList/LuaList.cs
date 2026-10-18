using BundleFormat;
using PluginAPI;
using BundleUtilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System;

namespace LuaList;

public class LuaList : IEntryData
{
    public int version { get; set; }

    [TypeConverter(typeof(EncryptedStringConverter))]
    public EncryptedString CgsId { get; set; }

    public int dataLength;
    public string listTitle { get; set; }
    public List<LuaListEntry> entries { get; set; }
    public List<string> types { get; set; }
    public List<string> variables { get; set; }
    public IEntryEditor GetEditor(BundleEntry entry)
    {
        LuaListEditor luaListEditor = new();
        luaListEditor.LuaList = this;
        luaListEditor.EditEvent += () =>
        {
            Write(entry);
        };
        return luaListEditor;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.LUAList;
    }

    public int getLengthOfHeader()
    {
        List<byte[]> bytes =
        [
            BitConverter.GetBytes(version),
            new byte[4],
            BitConverter.GetBytes(CgsId.Encrypted),
            BitConverter.GetBytes(192),
            BitConverter.GetBytes(1231),
            BitConverter.GetBytes(312312),
            BitConverter.GetBytes(entries.Count),
            BitConverter.GetBytes(dataLength),
            Encoding.ASCII.GetBytes((listTitle.PadRight(128)[..128].ToCharArray())),
            [(byte)types.Count()],
            [(byte)variables.Count()],
            new byte[26],
        ];
        return bytes.SelectMany(i => i).Count();
    }

    public int getLengthOfTypes()
    {
        if (types.Count == 0)
        {
            return 0;
        }
        return (types.Count * 32) + 32; // Pointer Array of 32 bytes
    }

    public int getLengthOfVariables()
    {
        if (variables.Count == 0)
        {
            return 0;
        }
        return (variables.Count * 32) + 32; // Pointer Array of 32 bytes
    }

    public int getLengthOfEntries()
    {
        return entries.Sum(i => i.getDataSize());
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        MemoryStream ms = entry.MakeStream();
        BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

        version = br.ReadInt32();
        br.ReadBytes(4);
        CgsId = new EncryptedString(br.ReadUInt64());
        var entriesPointer = br.ReadInt32();
        var typePointer = br.ReadInt32();
        var variablePointer = br.ReadInt32();
        var numScripts = br.ReadInt32();
        var dataLength = br.ReadInt32();
        listTitle = br.ReadLenString(128);
        var numTypes = br.ReadByte();
        var numVariables = br.ReadByte();
        br.ReadBytes(26);

        entries = [];
        for (int i = 0; i < numScripts; i++)
        {
            LuaListEntry luaentry = new();
            luaentry.Read(loader, br);
            entries.Add(luaentry);
        }

        br.BaseStream.Position = typePointer;
        long currentAddress = br.BaseStream.Position;
        types = [];
        for (int i = 0; i < numTypes; i++)
        {
            int pointer = br.ReadInt32();
            currentAddress = br.BaseStream.Position;
            br.BaseStream.Position = pointer;
            types.Add(br.ReadLenString(32));
            br.BaseStream.Position = currentAddress;
        }
        br.BaseStream.Position = variablePointer;
        variables = [];
        for (int i = 0; i < numVariables; i++)
        {
            int pointer = br.ReadInt32();
            currentAddress = br.BaseStream.Position;
            br.BaseStream.Position = pointer;
            if (dataLength - pointer > 31)
            {
                variables.Add(br.ReadLenString(32));
            }
            else
            {
                Console.WriteLine(dataLength - pointer);
                variables.Add(br.ReadLenString(dataLength - pointer));
            }
            br.BaseStream.Position = currentAddress;
        }
        Console.WriteLine(getLengthOfHeader() + getLengthOfEntries() + getLengthOfTypes() + getLengthOfVariables());
        Console.WriteLine(dataLength);
        br.Close();
        ms.Close();

        return true;
    }

    public bool Write(BundleEntry entry)
    {
        MemoryStream ms = new();
        BinaryWriter2 bw = new(ms);
        bw.BigEndian = entry.Console;
        bw.Write(version);
        bw.WriteUniquePadding(4);
        bw.Write(CgsId.Encrypted);
        bw.Write(getLengthOfHeader());
        bw.Write(getLengthOfHeader() + getLengthOfEntries());
        bw.Write(getLengthOfHeader() + getLengthOfEntries() + getLengthOfTypes());
        bw.Write(entries.Count);
        bw.Write(getLengthOfHeader() + getLengthOfEntries() + getLengthOfTypes() + getLengthOfVariables()); //Calculate this
        bw.Write(listTitle.PadRight(128, '\0')[..128].ToCharArray());
        bw.Write((byte)types.Count);
        bw.Write((byte)variables.Count);
        bw.WriteUniquePadding(26);
        foreach (LuaListEntry luaentry in entries)
        {
            luaentry.Write(bw);
        }
        var counter = 0;
        var pointerAddress = bw.BaseStream.Position;
        // Skip pointer address for now
        bw.BaseStream.Position = bw.BaseStream.Position + 32;
        foreach (string type in types)
        {
            // Save pointer to text
            int pointer = (int)bw.BaseStream.Position;
            bw.Write(type.PadRight(32, '\0')[..32].ToCharArray());
            long currentAddress = bw.BaseStream.Position;
            // Go to pointer array
            bw.BaseStream.Position = pointerAddress + (4 * counter);
            bw.Write(pointer);
            counter++;
            // Go back to old position
            bw.BaseStream.Position = currentAddress;
        }
        counter = 0;
        pointerAddress = bw.BaseStream.Position;
        // Skip pointer address for now
        bw.BaseStream.Position = bw.BaseStream.Position + 32;
        foreach (string variable in variables)
        {
            // Save pointer to text
            int pointer = (int)bw.BaseStream.Position;
            bw.Write(variable.PadRight(32, '\0')[..32].ToCharArray());
            long currentAddress = bw.BaseStream.Position;
            // Go to pointer array
            bw.BaseStream.Position = pointerAddress + (4 * counter);
            bw.Write(pointer);
            counter++;
            // Go back to old position
            bw.BaseStream.Position = currentAddress;
        }
        bw.Flush();
        byte[] data = ms.ToArray();
        bw.Close();
        ms.Close();

        entry.EntryBlocks[0].Data = data;
        entry.Dirty = true;
        return true;
    }
}
