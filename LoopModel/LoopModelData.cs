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
        private const UInt32 version = 7;
        private const UInt32 magic = 0x59444E41;
        private bool is64Bit = false;

        private List<Partial> _partials;
        public List<Partial> Partials { get => _partials; }

        public LoopModelData()
        {
            _partials = [];
        }

        public void Clear()
        {
            _partials.Clear();
        }
        
        public EntryType GetEntryType(BundleEntry entry)
        {
            return EntryType.LoopModel;
        }

        public IEntryEditor GetEditor(BundleEntry entry)
        {
            LoopModelEditor loopModel = new();
            loopModel.Data = this;
            loopModel.Edit += () =>
            {
                Write(entry);
            };

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
                MessageBox.Show("Unknown version: expected " + version.ToString() + ", got " + versionCheck.ToString(), "Failed to read resource", MessageBoxButtons.OK);
                return false;
            }

            UInt32 magicCheck = br.ReadUInt32();
            if (magicCheck != magic)
            {
                MessageBox.Show("Unknown magic: expected 0x" + magic.ToString("X8") + ", got 0x" + magicCheck.ToString("X8"), "Failed to read resource", MessageBoxButtons.OK);
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

            // Game hardcodes the number of partials to be 10
            if (numPartials != 10)
            {
                MessageBox.Show("Incorrect number of partials: expected 10, got " + numPartials.ToString(), "Error");
                return false;
            }

            // Read LoopModel data
            for (int i = 0; i < numPartials; ++i)
            {
                br.BaseStream.Position = partialsPointer + i * (is64Bit ? 0x10 : 0xC);
                _partials.Add(new Partial(br, is64Bit));
            }

            if (!CheckGraphErrors())
                return false;

            // Read Registry resource to obtain the wave hashes/strings
            var registryResources = entry.Archive.Entries.FindAll(e => e.Type == EntryType.Registry);
            if (registryResources.Count < 1 || registryResources.Count > 1)
            {
                MessageBox.Show("Expected 1 Registry resource, but " + registryResources.Count.ToString() + "were found.\n"
                    + "Partial names will fall back to hashes.", "Warning");
                foreach (Partial partial in _partials)
                    partial.SetWaveNameAsHash();
            }
            else
            {
                Dictionary<UInt32, String> waveNameDict = GetRegistryWaveStringMap(registryResources[0]);
                foreach (Partial partial in _partials)
                {
                    if (waveNameDict.TryGetValue(partial.WaveNameHash, out String waveName))
                        partial.SetWaveName(waveName);
                    else
                        partial.SetWaveNameAsHash(); // Should never be hit
                }
            }

            br.Close();
            ms.Close();
            return true;
        }

        private bool CheckGraphErrors()
        {
            for (int i = 0; i < _partials.Count; ++i)
            {
                // Game hardcodes the number of graphs in each partial to be 3
                if (_partials[i].Graphs.Count != 3)
                {
                    MessageBox.Show("Incorrect number of graphs: expected 3, got " + _partials[i].Graphs.Count.ToString()
                        + "\nfor partial at index " + i.ToString(), "Error");
                    return false;
                }

                for (int j = 0; j < _partials[i].Graphs.Count; ++j)
                {
                    // Game hardcodes the axes of each graph
                    if (j == 0 && _partials[i].Graphs[j].XAxis != 1)
                    {
                        MessageBox.Show("Incorrect X axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                            + "\nExpected 1 (E_RPM), got " + _partials[i].Graphs[j].XAxis.ToString(), "Error");
                        return false;
                    }
                    if (j == 0 && _partials[i].Graphs[j].YAxis != 2)
                    {
                        MessageBox.Show("Incorrect Y axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                            + "\nExpected 2 (E_PITCH), got " + _partials[i].Graphs[j].YAxis.ToString(), "Error");
                        return false;
                    }
                    if (j == 1 && _partials[i].Graphs[j].XAxis != 1)
                    {
                        MessageBox.Show("Incorrect X axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                            + "\nExpected 1 (E_RPM), got " + _partials[i].Graphs[j].XAxis.ToString(), "Error");
                        return false;
                    }
                    if (j == 1 && _partials[i].Graphs[j].YAxis != 1)
                    {
                        MessageBox.Show("Incorrect Y axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                            + "\nExpected 2 (E_GAIN), got " + _partials[i].Graphs[j].YAxis.ToString(), "Error");
                        return false;
                    }
                    if (j == 2 && _partials[i].Graphs[j].XAxis != 3)
                    {
                        MessageBox.Show("Incorrect X axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                            + "\nExpected 1 (E_ACCELERATOR), got " + _partials[i].Graphs[j].XAxis.ToString(), "Error");
                        return false;
                    }
                    if (j == 2 && _partials[i].Graphs[j].YAxis != 1)
                    {
                        MessageBox.Show("Incorrect Y axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                            + "\nExpected 1 (E_GAIN), got " + _partials[i].Graphs[j].YAxis.ToString(), "Error");
                        return false;
                    }

                    // Game hardcodes the number of points in Pitch/RPM graph as 2
                    if (j == 0 && _partials[i].Graphs[j].Points.Count != 2)
                    {
                        MessageBox.Show("Incorrect number of points in Pitch/RPM graph for partial at index " + i.ToString()
                            + "\nExpected 2, got " + _partials[i].Graphs[j].Points.Count.ToString());
                        return false;
                    }

                    // Game requires there to be more than 1 point in each graph
                    if (_partials[i].Graphs[j].Points.Count < 2)
                    {
                        MessageBox.Show("Not enough points in graph " + j.ToString() + " in partial " + i.ToString()
                            + "\nAt least 2 required, but only " + _partials[i].Graphs[j].Points.Count.ToString() + " present", "Error");
                        return false;
                    }
                }
            }

            return true;
        }

        private Dictionary<UInt32, String> GetRegistryWaveStringMap(BundleEntry entry)
        {
            // Only works on 32-bit platforms. (Not PS4/XB1/NX)
            
            MemoryStream ms = new(entry.EntryBlocks[0].Data);
            BinaryReader2 br = new(ms, entry.Console);
            Dictionary<UInt32, String> dict = new();

            UInt32 entityCount = br.ReadUInt32();
            UInt32 entityCapacity = br.ReadUInt32();
            UInt32 entityDataSize = br.ReadUInt32();
            UInt32 entityDataEnd = br.ReadUInt32();
            UInt32 stringTableSize = br.ReadUInt32();
            UInt32 stringTableEnd = br.ReadUInt32();
            UInt32 nameHashMask = br.ReadUInt32();
            List<UInt32> entityPointers = new((int)entityCount);
            for (int i = 0; i < entityCapacity; ++i)
            {
                UInt32 ptr = br.ReadUInt32();
                if (ptr != 0)
                    entityPointers.Add(ptr);
            }
            for (int i = 0; i < entityCount; ++i)
            {
                br.BaseStream.Position = entityPointers[i];
                UInt32 nameHash = br.ReadUInt32();
                UInt32 typeNameHash = br.ReadUInt32();
                if (typeNameHash != 0x511A448B) // ~ContentSpec~
                    continue;
                UInt32 contentTypeHash = br.ReadUInt32();
                if (contentTypeHash != 0x7CCDA2E7) // ~GenericRwacWaveContent::SK_WAVE_DATA_CONTENT_TYPE~
                    continue;
                UInt16 pathLength = br.ReadUInt16();
                Byte loadMethod = br.ReadByte();
                Byte loadTime = br.ReadByte();
                String name = br.ReadLenString(pathLength);
                if (name.Contains(".GinsuFile"))
                    continue;
                if (!dict.TryAdd(nameHash, name))
                    MessageBox.Show("Failed to add name: \"" + name + "\", 0x" + nameHash.ToString("X8"));
            }

            return dict;
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
            bw.Write(_partials.Count);

            // Write partials without graphs pointers
            int numPriorGraphs = 0;
            int numPriorPoints = 0;
            for (int i = 0; i < _partials.Count; ++i)
            {
                bw.BaseStream.Position = partialsPointer + i * (is64Bit ? 0x10 : 0xC);
                _partials[i].Write(bw, is64Bit, partialsPointer, _partials.Count, ref numPriorGraphs, ref numPriorPoints);
            }

            for (int i = 0; i < 2; ++i) // No idea why, but they write 16 null bytes after everything, and it's consistent
                bw.Write((UInt64)0);
            UInt32 resourceLength = (UInt32)bw.BaseStream.Position;
            bw.Align(0x10);
            bw.BaseStream.Position = is64Bit ? 0x14 : 0x10;
            bw.Write(resourceLength);

            bw.Flush();
            byte[] data = ms.ToArray();
            bw.Close();
            ms.Close();

            entry.EntryBlocks[0].Data = data;
            entry.Dirty = true;

            return true;
        }
    }

    public class Partial
    {
        private UInt32 _waveNameHash = 0; // Name hash
        internal UInt32 WaveNameHash { get => _waveNameHash; }
        private String _waveName;
        public String WaveName { get => _waveName; }

        private List<Graph> _graphs;
        public List<Graph> Graphs { get => _graphs; }

        public Partial()
        {
            _graphs = [];
        }

        public Partial(BinaryReader2 br, bool is64Bit)
        {
            _graphs = [];

            // Position differs between 32-bit and 64-bit
            Int64 graphsPointer = 0;
            if (is64Bit)
            {
                graphsPointer = br.ReadInt64();
                _waveNameHash = br.ReadUInt32();
            }
            else
            {
                _waveNameHash = br.ReadUInt32();
                graphsPointer = br.ReadInt32();
            }
            Byte numGraphs = br.ReadByte();

            for (int i = 0; i < numGraphs; ++i)
            {
                br.BaseStream.Position = graphsPointer + i * (is64Bit ? 0x10 : 8);
                _graphs.Add(new Graph(br, is64Bit));
            }
        }

        public void Write(BinaryWriter2 bw, bool is64Bit, Int64 partialsPointer, int numPartials, ref int numPriorGraphs, ref int numPriorPoints)
        {
            Int64 graphsPointer = partialsPointer;
            if (is64Bit)
            {
                graphsPointer += numPartials * 0x10 + numPriorGraphs * 0x10 + numPriorPoints * 8;
                bw.Write(graphsPointer);
                bw.Write(_waveNameHash);
            }
            else
            {
                graphsPointer += numPartials * 0xC + numPriorGraphs * 8 + numPriorPoints * 8;
                bw.Write(_waveNameHash);
                bw.Write((Int32)graphsPointer);
            }
            bw.Write((Byte)_graphs.Count);
            bw.WriteUniquePadding(3);

            bw.BaseStream.Position = graphsPointer;
            int numPriorPointsInGraph = 0;
            for (int i = 0; i < _graphs.Count; ++i)
            {
                bw.BaseStream.Position = graphsPointer + i * (is64Bit ? 0x10 : 8);
                _graphs[i].Write(bw, is64Bit, graphsPointer, _graphs.Count, ref numPriorPoints, ref numPriorPointsInGraph);
                numPriorGraphs++;
            }
        }

        public void Clear()
        {
            _graphs.Clear();
        }

        internal void SetWaveNameAsHash()
        {
            _waveName = "0x" + _waveNameHash.ToString("X8");
        }

        internal void SetWaveName(string waveName)
        {
            if (waveName.StartsWith("gamedb://"))
            {
                var subStrStart = waveName.LastIndexOf('/') + 1;
                var subStrLen = waveName.IndexOf(".WaveFile") - subStrStart;
                String shortWaveName = waveName.Substring(subStrStart, subStrLen);
                _waveName = shortWaveName;
            }
            else
            {
                _waveName = waveName;
            }
        }
    }

    public class Graph
    {
        private SByte _xAxis = 0;
        internal SByte XAxis { get => _xAxis; }
        private SByte _yAxis = 0;
        internal SByte YAxis { get => _yAxis; }

        private List<Point> _points;
        public List<Point> Points { get => _points; }

        public Graph()
        {
            _points = [];
        }

        public Graph(BinaryReader2 br, bool is64Bit)
        {
            _points = [];
            
            Int64 pointsPointer = is64Bit ? br.ReadInt64() : br.ReadInt32();
            Byte numPoints = br.ReadByte();
            _xAxis = br.ReadSByte();
            _yAxis = br.ReadSByte();

            for (int i = 0; i < numPoints; ++i)
            {
                br.BaseStream.Position = pointsPointer + i * 8;
                _points.Add(new Point(br));
            }
        }

        public void Clear()
        {
            _points.Clear();
        }

        public void Write(BinaryWriter2 bw, bool is64Bit, Int64 graphsPointer, int numGraphs, ref int numPriorPoints, ref int numPriorPointsInGraph)
        {
            Int64 pointsPointer = graphsPointer + numGraphs * (is64Bit ? 0x10 : 8) + numPriorPointsInGraph * 8;
            if (is64Bit)
                bw.Write(pointsPointer);
            else
                bw.Write((Int32)pointsPointer);
            bw.Write((Byte)_points.Count);
            bw.Write(_xAxis);
            bw.Write(_yAxis);
            if (is64Bit)
                bw.WriteUniquePadding(5);
            else
                bw.WriteUniquePadding(1);

            bw.BaseStream.Position = pointsPointer;
            for (int i = 0; i < _points.Count; ++i)
            {
                _points[i].Write(bw);
                numPriorPoints++;
                numPriorPointsInGraph++;
            }
        }
    }

    public class Point
    {
        private float _x = 0;
        public float X { get => _x; set => _x = value; }

        private float _y = 0;
        public float Y { get => _y; set => _y = value; }

        public Point(BinaryReader2 br)
        {
            _x = br.ReadSingle();
            _y = br.ReadSingle();
        }

        public void Write(BinaryWriter2 bw)
        {
            bw.Write(_x);
            bw.Write(_y);
        }
    }
}
