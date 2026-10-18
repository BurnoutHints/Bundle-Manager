using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace LangEditor
{
    public class Language : IEntryData
    {
        private enum LanguageId : uint
        {
            E_LANGUAGE_ARABIC,
            E_LANGUAGE_CHINESE,
            E_LANGUAGE_CHINESE_SIMPLIFIED,
            E_LANGUAGE_CHINESE_TRADITIONAL,
            E_LANGUAGE_CZECH,
            E_LANGUAGE_DANISH,
            E_LANGUAGE_DUTCH,
            E_LANGUAGE_ENGLISH_US,
            E_LANGUAGE_ENGLISH_UK,
            E_LANGUAGE_FINNISH,
            E_LANGUAGE_FRENCH,
            E_LANGUAGE_GERMAN,
            E_LANGUAGE_GREEK,
            E_LANGUAGE_HEBREW,
            E_LANGUAGE_HUNGARIAN,
            E_LANGUAGE_ITALIAN,
            E_LANGUAGE_JAPANESE,
            E_LANGUAGE_KOREAN,
            E_LANGUAGE_NORWEGIAN,
            E_LANGUAGE_POLISH,
            E_LANGUAGE_PORTUGUESE_BRAZIL,
            E_LANGUAGE_PORTUGUESE_PORTUGAL,
            E_LANGUAGE_SPANISH,
            E_LANGUAGE_SWEDISH,
            E_LANGUAGE_THAI
        }
        
        private LanguageId meLanguageID;
        private uint muSize; // Number of entries
        internal Dictionary<uint, string> mpEntries;

        public Language()
        {
            mpEntries = new Dictionary<uint, string>();
        }

        public bool Read(BundleEntry entry, ILoader loader = null)
        {
            mpEntries.Clear(); // Clear any entries from a previous Bundle

            MemoryStream ms = entry.MakeStream();
            BinaryReader2 br = new BinaryReader2(ms);
            br.BigEndian = entry.Console;

            bool x64 = entry.Platform == BundlePlatform.PCx64;

            meLanguageID = (LanguageId)br.ReadUInt32();
            muSize = br.ReadUInt32();
            ms.Position = x64 ? (long)br.ReadUInt64() : br.ReadUInt32();

            for (int i = 0; i < muSize; ++i)
            {
                uint hash = br.ReadUInt32();
                if (x64)
                    br.ReadUInt32(); // Padding
                mpEntries.Add(hash, br.ReadCStringPtr());
                if (x64)
                    br.ReadUInt32(); // High half of the string pointer
            }
            mpEntries.Remove(0); // If padding is present, remove it

            br.Close();

            return true;
        }

        public bool Write(BundleEntry entry)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter2 bw = new BinaryWriter2(ms);
            bw.BigEndian = entry.Console;

            bool x64 = entry.Platform == BundlePlatform.PCx64;
            int entriesPtr = x64 ? 0x10 : 0xC;
            int entrySize = x64 ? 0x10 : 8;

            bw.Write((uint)meLanguageID);
            bw.Write(mpEntries.Count);
            if (x64) // Pointer is always the same
                bw.Write((long)entriesPtr);
            else
                bw.Write(entriesPtr);

            // Write at entries position so string position calculations are not needed later
            // and to avoid double iteration
            ms.Position = mpEntries.Count * entrySize + entriesPtr - 1;
            bw.Write((byte)0);
            ms.Position = entriesPtr;

            long lastPos;
            foreach (KeyValuePair<uint, string> langEntry in mpEntries)
            {
                bw.Write(langEntry.Key); // Hash + padding
                if (x64)
                {
                    bw.Write(0); // Padding
                    bw.Write(ms.Length); // String pointer
                }
                else
                    bw.Write((uint)ms.Length); // String pointer
                lastPos = ms.Position;
                ms.Position = ms.Length;
                if (langEntry.Value != null)
                    bw.Write(langEntry.Value.ToCharArray()); // String data
                bw.Write((byte)0); // Null terminator
                ms.Position = lastPos;
            }

            bw.Flush();
            ms.Position = ms.Length;
            bw.Align(0x10);
            byte[] data = ms.ToArray();
            bw.Close();

            entry.EntryBlocks[0].Data = data;
            entry.Dirty = true;

            return true;
        }

        public EntryType GetEntryType(BundleEntry entry)
        {
            return EntryType.Language;
        }

        public IEntryEditor GetEditor(BundleEntry entry)
        {
            LangEdit edit = new LangEdit();
            edit.Lang = this;
            edit.Changed += () =>
            {
                edit.Lang.Write(entry);
            };

            return edit;
        }

        public static uint HashID(string id)
        {
            /*uint result = 0xFFFFFFFF;
            for (int i = 0; i < id.Length; i++)
            {
                byte b = (byte)id[i];
                //if (b == 0)
                //    break;
                byte newByte = (byte)((b - 0x41) & 0xFF);
                if (newByte <= 0x19)
                    b += 0x20;
                byte lowByte = (byte)(result & 0xFF);
                lowByte = (byte)((lowByte ^ b) & 0xFF);
                result = (result >> 8) & 0x00FFFFFF;
                result = (result ^ Crc32.Crc32BTable[lowByte]) & 0xFFFFFFFF;
            }
            result = (~result) & 0xFFFFFFFF;

            return result;*/

            byte[] message = Encoding.UTF8.GetBytes(id);
            UInt32 hash = UInt32.MaxValue;
            for (UInt32 i = 0; i < message.Length; i++)
            {
                UInt32 index = message[i];
                index ^= (hash & 0x000000FF);
                hash = Crc32.Crc32BTable[index] ^ (hash >> 8);
            }

            return hash;
        }
    }
}
