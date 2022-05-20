using BundleFormat;
using PluginAPI;
using BundleUtilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System;

namespace LuaList
{

    public class LuaList : IEntryData
    {
        public int version { get; set; }    //0x0	0x4	int32_t Version number	1
        [Category("Undefined Datastructure"), Description("This is currently not implented and can be ignored")]
        public byte[] padding { get; set; } //0x4	0x4			padding		
        [TypeConverter(typeof(ULongHexTypeConverter))]
        public ulong CgsId { get; set; }     //0x8	0x8	CgsID List ID Encoded
        [Category("Undefined Datastructure"), Description("This is currently not implented and can be ignored")]
        public byte[] Unk0 { get; set; }    //0x10	0x4	Unk0* Script list Unk0 format	
        [Category("Undefined Datastructure"), Description("This is currently not implented and can be ignored")]
        public byte[] typePointer { get; set; }//0x14	0x4	char[32] * *		Types	
        [Category("Undefined Datastructure"), Description("This is currently not implented and can be ignored")]	
        public byte[] variablePointer { get; set; } //0x18	0x4	char[32] * *		Variables
        [ReadOnly(true), Category("Undefined Datastructure"), Description("This is currently not implented and can be ignored")]
        public int numScripts { get; set; }      //0x1C	0x4	uint32_t Num scripts		
        [Category("Undefined Datastructure"), Description("This is currently not implented and can be ignored")]
        public int dataLength { get; set; }      //0x20	0x4	uint32_t Data length Not including padding to end
        public string listTitle { get; set; }   //0x24	0x80	char[128] List title		
        [Category("Undefined Datastructure"), Description("This is currently not implented and can be ignored")]
        public byte numTypes { get; set; }     //0xA4	0x1	uint8_t Num types		
        [Category("Undefined Datastructure"), Description("This is currently not implented and can be ignored")]
        public byte numVariables { get; set; } //0xA5	0x1	uint8_t Num variables
        [Category("Undefined Datastructure"), Description("This is currently not implented and can be ignored")]
        public byte[] padding2 { get; set; } // 26 bytes long
        public List<LuaListEntry> entries { get; set; }
        [Category("Undefined Datastructure"), Description("This is currently not implented and can be ignored")]
        public byte[] unknownData { get; set; }

        public IEntryEditor GetEditor(BundleEntry entry)
        {
            LuaListEditor luaListEditor = new LuaListEditor();
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

        public int getLengthOfHeader() { 
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(BitConverter.GetBytes(version));
            bytes.Add(padding);
            bytes.Add(BitConverter.GetBytes(CgsId));
            bytes.Add(Unk0);
            bytes.Add(typePointer);
            bytes.Add(variablePointer);
            bytes.Add(BitConverter.GetBytes(numScripts));
            bytes.Add(BitConverter.GetBytes(dataLength));
            bytes.Add(Encoding.ASCII.GetBytes((listTitle.PadRight(128).Substring(0, 128).ToCharArray())));
            bytes.Add(BitConverter.GetBytes(numTypes));
            bytes.Add(BitConverter.GetBytes(numVariables));
            return bytes.SelectMany(i => i).Count();
        }

        public bool Read(BundleEntry entry, ILoader loader = null)
        {
            MemoryStream ms = entry.MakeStream();
            BinaryReader2 br = new BinaryReader2(ms);

            version = br.ReadInt32();
            padding = br.ReadBytes(4);
            CgsId = br.ReadUInt64();
            Unk0 = br.ReadBytes(4);
            typePointer = br.ReadBytes(4);
            variablePointer = br.ReadBytes(4);
            numScripts = br.ReadInt32();
            dataLength = br.ReadInt32();
            listTitle = br.ReadLenString(128);
            numTypes = br.ReadByte();
            numVariables = br.ReadByte();
            padding2 = br.ReadBytes(26);

            entries = new List<LuaListEntry>();
            for (int i = 0; i < numScripts; i++) {
                LuaListEntry luaentry = new LuaListEntry();
                luaentry.Read(loader, br);
                entries.Add(luaentry);
            }

            br.Close();
            ms.Close();

            return true;
        }

        public bool Write(BundleEntry entry)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(version);
            bw.Write(padding);
            bw.Write(CgsId);
            bw.Write(Unk0);
            bw.Write(typePointer);
            bw.Write(variablePointer);
            bw.Write(entries.Count);
            bw.Write(dataLength);
            bw.Write(listTitle.PadRight(128).Substring(0, 128).ToCharArray());
            bw.Write(numTypes);
            bw.Write(numVariables);
            bw.Write(padding2);
            foreach (LuaListEntry luaentry in entries)
            {
                luaentry.Write(bw);
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
}
