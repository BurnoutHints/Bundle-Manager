using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using BundleUtilities;

namespace BundleFormat
{
    public enum VertexAttributeType
    {
        Invalid = 0,
        Positions = 1,
        Normals = 3,
        UV1 = 5,
        UV2 = 6,
        BoneIndexes = 13,
        BoneWeights = 14,
        Tangents = 15
    };

    public struct VertexAttribute
    {
        public VertexAttributeType Type;
        public uint Unknown1;
        public uint Offset;
        public uint Unknown2;
        public uint Size;
    }

    public class VertexDesc
    {
        public int Unknown1;
        public int Unknown2;
        public int Unknown3;
        public int AttributeCount;
        public List<VertexAttribute> Attributes;

        public VertexDesc()
        {
            Attributes = new List<VertexAttribute>();
        }

        public static VertexDesc Read(BundleEntry entry)
        {
            VertexDesc result = new VertexDesc();

            MemoryStream ms = entry.MakeStream();
            BinaryReader2 br = new BinaryReader2(ms);
            br.BigEndian = entry.Console;
            bool x64 = entry.Platform == BundlePlatform.PCx64; // decomp is like the X360 layout but little endian

            result.Unknown1 = br.ReadInt32();
            result.Unknown2 = br.ReadInt32();
            result.Unknown3 = br.ReadInt32();
            result.AttributeCount = br.ReadInt32();
            if (x64) // u16 element count @0x08
                result.AttributeCount = (ushort)result.Unknown3;

            for (int i = 0; i < result.AttributeCount; i++)
            {
                VertexAttribute attribute = new VertexAttribute();
                
                if (result.Unknown2 != 0 && !x64) // BPR
                {
                    attribute.Type = (VertexAttributeType)br.ReadInt32();
                    if (!Enum.IsDefined(typeof(VertexAttributeType), attribute.Type))
                        MessageBox.Show("Unknown vertex attribute type: " + attribute.Type);
                    attribute.Unknown1 = br.ReadUInt32();
                    attribute.Offset = br.ReadUInt32();
                    attribute.Unknown2 = br.ReadUInt32();
                    attribute.Size = br.ReadUInt32();
                }
                else
                {
                    br.BaseStream.Position += 1;
                    attribute.Size = br.ReadByte();
                    attribute.Offset = br.ReadByte();

                    // These are probably in the following bytes but I'll need to check where.
                    attribute.Unknown1 = 0;
                    attribute.Unknown2 = 0;

                    br.BaseStream.Position += 8;

                    byte type = br.ReadByte();
                    switch (type)
                    {
                        case 1:
                            attribute.Type = VertexAttributeType.Positions;
                            break;
                        case 3:
                            attribute.Type = VertexAttributeType.Normals;
                            break;
                        case 6:
                            attribute.Type = VertexAttributeType.UV1;
                            break;
                        case 7:
                            attribute.Type = VertexAttributeType.UV2;
                            break;
                        case 14:
                            attribute.Type = VertexAttributeType.BoneIndexes;
                            break;
                        case 15:
                            attribute.Type = VertexAttributeType.BoneWeights;
                            break;
                        case 21:
                            attribute.Type = VertexAttributeType.Tangents;
                            break;
                        default:
                            attribute.Type = VertexAttributeType.Invalid;
                            MessageBox.Show("Unknown vertex attribute type: " + type);
                            break;
                    }

                    br.BaseStream.Position += 4;
                }

                if (x64)
                {
                    byte[] data = entry.EntryBlocks[0].Data;
                    int element = 0x10 + 0x10 * i;
                    attribute.Offset = BitConverter.ToUInt16(data, element + 2);
                    attribute.Unknown1 = BitConverter.ToUInt32(data, element + 4);
                    attribute.Size = data[0x10 + 0x10 * result.AttributeCount + i];
                }

                result.Attributes.Add(attribute);
            }

            br.Close();
            ms.Close();

            return result;
        }

        public void Write(BundleEntry entry)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter2 bw = new BinaryWriter2(ms);
            bw.BigEndian = entry.Console;

            bw.Write(Unknown1);
            bw.Write(Unknown2);
            bw.Write(Unknown3);
            bw.Write(Attributes.Count);
            foreach (VertexAttribute attribute in Attributes)
            {
                bw.Write((int)attribute.Type);
                bw.Write(attribute.Unknown1);
                bw.Write(attribute.Offset);
                bw.Write(attribute.Unknown2);
                bw.Write(attribute.Size);
            }

            bw.Flush();
            byte[] Data = ms.GetBuffer();
            bw.Close();
            ms.Close();

            entry.EntryBlocks[0].Data = Data;
            entry.Dirty = true;
        }
    }
}
