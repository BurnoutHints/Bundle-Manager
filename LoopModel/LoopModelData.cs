using BundleFormat;
using BundleUtilities;
using PluginAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace LoopModel
{
    public class LoopModelData : IEntryData
    {
        const UInt32 version = 7;
        const UInt32 magic = 0x59444E41;
        bool is64Bit = false;

        List<Partial> partials;

        public LoopModelData()
        {
            partials = [];
        }

        public void Clear()
        {
            partials.Clear();
        }
        
        public EntryType GetEntryType(BundleEntry entry)
        {
            return EntryType.LoopModel;
        }

        public IEntryEditor GetEditor(BundleEntry entry)
        {
            LoopModelEditor loopModel = new();

            return loopModel;
        }

        public bool Read(BundleEntry entry, ILoader loader = null)
        {
            Clear();

            MemoryStream ms = new(entry.EntryBlocks[0].Data);
            BinaryReader2 br = new(ms, entry.Console);

            UInt32 versionCheck = br.ReadUInt32();
            if (versionCheck != version)
            {
                MessageBox.Show("Unknown version: expected 7, got " + versionCheck.ToString(), "Failed to read resource", MessageBoxButtons.OK);
                return false;
            }

            UInt32 magicCheck = br.ReadUInt32();
            if (magicCheck != magic)
            {
                MessageBox.Show("Unknown magic: expected 0x59444E41, got 0x" + magicCheck.ToString("X8"), "Failed to read resource", MessageBoxButtons.OK);
                return false;
            }

            UInt32 partialsPointer = br.ReadUInt32();

            // The above is actually 64-bit on some systems, but will never exceed 32-bit storage in practice.
            // Use a hack to make reading/writing cross-platform under the assumption there are no loop models without partials.
            UInt32 numPartials = br.ReadUInt32();
            if (numPartials == 0)
            {
                numPartials = br.ReadUInt32();
                is64Bit = true;
            }

            for (int i = 0; i < numPartials; ++i)
            {
                br.BaseStream.Position = partialsPointer + i * (is64Bit ? 0x10 : 0xC);
                partials.Add(new Partial(br, is64Bit));
            }

            return true;
        }

        public bool Write(BundleEntry entry)
        {
            MemoryStream ms = new();
            BinaryWriter2 bw = new(ms, entry.Console);

            bw.Write(version);
            bw.Write(magic);
            Int32 partialsPointer = is64Bit ? 0x18 : 0x14;
            if (is64Bit)
                bw.Write((Int64)partialsPointer);
            else
                bw.Write(partialsPointer);
            bw.Write(partials.Count);

            // Write partials without graphs pointers
            int numPriorGraphs = 0;
            int numPriorPoints = 0;
            for (int i = 0; i < partials.Count; ++i)
            {
                bw.BaseStream.Position = partialsPointer + i * (is64Bit ? 0x10 : 0xC);
                partials[i].Write(bw, is64Bit, partialsPointer, partials.Count, ref numPriorGraphs, ref numPriorPoints);
            }

            bw.Align(0x10);
            UInt32 resourceLength = (UInt32)bw.BaseStream.Position;
            bw.BaseStream.Position = is64Bit ? 0x14 : 0x10;
            bw.Write(resourceLength);

            return true;
        }
    }

    public class Partial
    {
        List<Graph> graphs;
        UInt32 waveName = 0; // Name hash

        public Partial()
        {
            graphs = [];
        }

        public Partial(BinaryReader2 br, bool is64Bit)
        {
            graphs = [];
            
            waveName = br.ReadUInt32();
            Int64 graphsPointer = is64Bit ? br.ReadInt64() : br.ReadInt32();
            Byte numGraphs = br.ReadByte();

            for (int i = 0; i < numGraphs; ++i)
            {
                br.BaseStream.Position = graphsPointer + i * (is64Bit ? 0x10 : 8);
                graphs.Add(new Graph(br, is64Bit));
            }
        }

        public void Write(BinaryWriter2 bw, bool is64Bit, Int64 partialsPointer, int numPartials, ref int numPriorGraphs, ref int numPriorPoints)
        {
            bw.Write(waveName);
            Int64 graphsPointer = partialsPointer;
            if (is64Bit)
                graphsPointer += numPartials * 0x10 + numPriorGraphs * 0x10 + numPriorPoints * 8;
            else
                graphsPointer += numPartials * 0xC + numPriorGraphs * 8 + numPriorPoints * 8;
            if (is64Bit)
                bw.Write(graphsPointer);
            else
                bw.Write((Int32)graphsPointer);
            bw.Write((Byte)graphs.Count);
            bw.WriteUniquePadding(3);

            bw.BaseStream.Position = graphsPointer;
            for (int i = 0; i < graphs.Count; ++i)
            {
                bw.BaseStream.Position = graphsPointer + i * (is64Bit ? 0x10 : 8);
                graphs[i].Write(bw, is64Bit, graphsPointer, graphs.Count, ref numPriorPoints);
                numPriorGraphs++;
            }
        }

        public void Clear()
        {
            graphs.Clear();
        }
    }

    public class Graph
    {
        List<Point> points;
        SByte xAxis = 0;
        SByte yAxis = 0;

        public Graph()
        {
            points = [];
        }

        public Graph(BinaryReader2 br, bool is64Bit)
        {
            points = [];
            
            Int64 pointsPointer = is64Bit ? br.ReadInt64() : br.ReadInt32();
            Byte numPoints = br.ReadByte();
            xAxis = br.ReadSByte();
            yAxis = br.ReadSByte();

            for (int i = 0; i < numPoints; ++i)
            {
                br.BaseStream.Position = pointsPointer + i * 8;
                points.Add(new Point(br));
            }
        }

        public void Clear()
        {
            points.Clear();
        }

        public void Write(BinaryWriter2 bw, bool is64Bit, Int64 graphsPointer, int numGraphs, ref int numPriorPoints)
        {
            Int64 pointsPointer = graphsPointer + numGraphs * (is64Bit ? 0x10 : 8) + numPriorPoints * 8;
            if (is64Bit)
                bw.Write(pointsPointer);
            else
                bw.Write((Int32)pointsPointer);
            bw.Write((Byte)points.Count);
            bw.Write(xAxis);
            bw.Write(yAxis);
            if (is64Bit)
                bw.WriteUniquePadding(5);
            else
                bw.WriteUniquePadding(1);

            bw.BaseStream.Position = pointsPointer;
            for (int i = 0; i < points.Count; ++i)
            {
                points[i].Write(bw);
                numPriorPoints++;
            }
        }
    }

    public class Point
    {
        float x = 0;
        float y = 0;

        public Point(BinaryReader2 br)
        {
            x = br.ReadSingle();
            y = br.ReadSingle();
        }

        public void Write(BinaryWriter2 bw)
        {
            bw.Write(x);
            bw.Write(y);
        }
    }
}
