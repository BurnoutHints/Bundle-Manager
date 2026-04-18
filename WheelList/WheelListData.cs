using BundleFormat;
using BundleUtilities;
using PluginAPI;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WheelList
{
    public class WheelListData : IEntryData
    {
        public int NumWheels = 0;
        public int EntriesPtr = 0;
        public List<Wheel> Entries = [];

        public WheelListData()
        {
            
        }

        public IEntryEditor GetEditor(BundleEntry entry)
        {
            WheelListForm wheelList = new WheelListForm();
            wheelList.List = this;
            wheelList.Edit += () =>
            {
                Write(entry);
            };

            return wheelList;
        }

        public EntryType GetEntryType(BundleEntry entry)
        {
            return EntryType.WheelList;
        }

        public bool Read(BundleEntry entry, ILoader loader = null)
        {
            Clear();

            MemoryStream ms = new MemoryStream(entry.EntryBlocks[0].Data);
            BinaryReader2 br = new BinaryReader2(ms);
            br.BigEndian = entry.Console;

            NumWheels = br.ReadInt32();
            EntriesPtr = br.ReadInt32();
            br.BaseStream.Position = EntriesPtr;

            for (int i = 0; i < NumWheels; i++)
            {
                Wheel wheel = new();

                wheel.Index = i;
                wheel.ID = new EncryptedString(br.ReadUInt64());
                wheel.Name = Encoding.ASCII.GetString(br.ReadBytes(64));

                Entries.Add(wheel);
            }

            br.Close();

            return true;
        }

        public bool Write(BundleEntry entry)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter2 bw = new BinaryWriter2(ms);
            bw.BigEndian = entry.Console;

            bw.Write(Entries.Count);
            bw.Write(EntriesPtr);
            bw.BaseStream.Position = EntriesPtr;

            for (int i = 0; i < Entries.Count; i++)
            {
                Wheel wheel = Entries[i];

                bw.Write(wheel.ID.Encrypted);
                bw.WriteLenString(wheel.Name, 64);
            }

            bw.Align(0x10);

            bw.Flush();
            byte[] data = ms.ToArray();
            bw.Close();

            entry.EntryBlocks[0].Data = data;
            entry.Dirty = true;

            return true;
        }

        private void Clear()
        {
            NumWheels = default;
            EntriesPtr = default;
            Entries.Clear();
        }
    }

    public class Wheel
    {
        public int Index = -1;
        public EncryptedString ID = new(0);
        public string Name = new("");

        public Wheel()
        {

        }

        public Wheel(Wheel copy)
        {
            Index = copy.Index;
            ID = copy.ID;
            Name = copy.Name;
        }
    }
}
