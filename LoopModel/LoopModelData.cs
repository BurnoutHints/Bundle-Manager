using BundleFormat;
using BundleUtilities;
using PluginAPI;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace LoopModel;

public class LoopModelData : IEntryData
{
    private const uint Version = 7;
    private const uint Magic = 0x59444E41;
    private bool is64Bit = false;

    private List<Partial> partials;

    public List<Partial> Partials
    {
        get => partials;
    }

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
        LoopModelEditor loopModel = new()
        {
            Data = this
        };

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

        bool x64 = entry.Platform == BundlePlatform.PCx64;

        uint versionCheck = br.ReadUInt32();

        if (versionCheck != Version)
        {
            MessageBox.Show("Unknown version: expected " + Version.ToString() + ", got " + versionCheck.ToString(), "Failed to read resource", MessageBoxButtons.OK);
            return false;
        }

        uint magicCheck = br.ReadUInt32();

        if (magicCheck != Magic)
        {
            MessageBox.Show("Unknown magic: expected 0x" + Magic.ToString("X8") + ", got 0x" + magicCheck.ToString("X8"), "Failed to read resource", MessageBoxButtons.OK);
            return false;
        }

        uint partialsPointer = br.ReadUInt32();

        // The above is actually 64-bit on some systems, but will never exceed 32-bit storage in practice.
        // Use a hack to make reading/writing cross-platform under the assumption there are no loop models without partials.
        uint numPartials = br.ReadUInt32();

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
            br.BaseStream.Position = partialsPointer + i * (x64
                ? 0x18
                : is64Bit
                    ? 0x10
                    : 0xC);
            partials.Add(new(br, is64Bit, x64));
        }

        if (!CheckGraphErrors())
            return false;

        // Read Registry resource to obtain the wave hashes/strings
        List<BundleEntry> registryResources = entry.Archive.Entries.FindAll(e => e.Type == EntryType.Registry);

        if (registryResources.Count < 1 || registryResources.Count > 1)
        {
            MessageBox.Show("Expected 1 Registry resource, but " + registryResources.Count.ToString() + "were found.\n"
                + "Partial names will fall back to hashes.", "Warning");

            foreach (Partial currentPartial in partials)
                currentPartial.SetWaveNameAsHash();
        }
        else
        {
            Dictionary<uint, string> waveNameDict = GetRegistryWaveStringMap(registryResources[0]);

            foreach (Partial currentPartial2 in partials)
            {
                if (waveNameDict.TryGetValue(currentPartial2.WaveNameHash, out string waveName))
                    currentPartial2.SetWaveName(waveName);
                else
                    currentPartial2.SetWaveNameAsHash(); // Should never be hit
            }
        }

        br.Close();
        ms.Close();
        return true;
    }

    private bool CheckGraphErrors()
    {
        for (int i = 0; i < partials.Count; ++i)
        {
            // Game hardcodes the number of graphs in each partial to be 3
            if (partials[i].Graphs.Count != 3)
            {
                MessageBox.Show("Incorrect number of graphs: expected 3, got " + partials[i].Graphs.Count.ToString()
                    + "\nfor partial at index " + i.ToString(), "Error");
                return false;
            }

            for (int j = 0; j < partials[i].Graphs.Count; ++j)
            {
                // Game hardcodes the axes of each graph
                if (j == 0 && partials[i].Graphs[j].XAxis != 1)
                {
                    MessageBox.Show("Incorrect X axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                        + "\nExpected 1 (E_RPM), got " + partials[i].Graphs[j].XAxis.ToString(), "Error");
                    return false;
                }

                if (j == 0 && partials[i].Graphs[j].YAxis != 2)
                {
                    MessageBox.Show("Incorrect Y axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                        + "\nExpected 2 (E_PITCH), got " + partials[i].Graphs[j].YAxis.ToString(), "Error");
                    return false;
                }

                if (j == 1 && partials[i].Graphs[j].XAxis != 1)
                {
                    MessageBox.Show("Incorrect X axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                        + "\nExpected 1 (E_RPM), got " + partials[i].Graphs[j].XAxis.ToString(), "Error");
                    return false;
                }

                if (j == 1 && partials[i].Graphs[j].YAxis != 1)
                {
                    MessageBox.Show("Incorrect Y axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                        + "\nExpected 2 (E_GAIN), got " + partials[i].Graphs[j].YAxis.ToString(), "Error");
                    return false;
                }

                if (j == 2 && partials[i].Graphs[j].XAxis != 3)
                {
                    MessageBox.Show("Incorrect X axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                        + "\nExpected 1 (E_ACCELERATOR), got " + partials[i].Graphs[j].XAxis.ToString(), "Error");
                    return false;
                }

                if (j == 2 && partials[i].Graphs[j].YAxis != 1)
                {
                    MessageBox.Show("Incorrect Y axis on graph index " + j.ToString() + ", partial index " + i.ToString()
                        + "\nExpected 1 (E_GAIN), got " + partials[i].Graphs[j].YAxis.ToString(), "Error");
                    return false;
                }

                // Game hardcodes the number of points in Pitch/RPM graph as 2
                if (j == 0 && partials[i].Graphs[j].Points.Count != 2)
                {
                    MessageBox.Show("Incorrect number of points in Pitch/RPM graph for partial at index " + i.ToString()
                        + "\nExpected 2, got " + partials[i].Graphs[j].Points.Count.ToString());
                    return false;
                }

                // Game requires there to be more than 1 point in each graph
                if (partials[i].Graphs[j].Points.Count < 2)
                {
                    MessageBox.Show("Not enough points in graph " + j.ToString() + " in partial " + i.ToString()
                        + "\nAt least 2 required, but only " + partials[i].Graphs[j].Points.Count.ToString() + " present", "Error");
                    return false;
                }
            }
        }

        return true;
    }

    private Dictionary<uint, string> GetRegistryWaveStringMap(BundleEntry entry)
    {
        // Only works on 32-bit platforms except for decomp. (Not PS4/XB1/NX)
        MemoryStream ms = new(entry.EntryBlocks[0].Data);
        BinaryReader2 br = new(ms, entry.Console);
        Dictionary<uint, string> dict = [];

        bool x64 = entry.Platform == BundlePlatform.PCx64;

        long ReadPtr() => x64
            ? br.ReadInt64()
            : br.ReadUInt32();

        uint entityCount = br.ReadUInt32();
        uint entityCapacity = br.ReadUInt32();
        long entityDataSize = ReadPtr();
        long entityDataEnd = ReadPtr();
        long stringTableSize = ReadPtr();
        long stringTableEnd = ReadPtr();
        long nameHashMask = ReadPtr();
        List<long> entityPointers = new((int)entityCount);

        for (int i = 0; i < entityCapacity; ++i)
        {
            long ptr = ReadPtr();

            if (ptr != 0)
                entityPointers.Add(ptr);
        }

        for (int i = 0; i < entityCount; ++i)
        {
            br.BaseStream.Position = entityPointers[i];
            uint nameHash = br.ReadUInt32();
            uint typeNameHash = br.ReadUInt32();

            if (typeNameHash != 0x511A448B) // ~ContentSpec~
                continue;

            long contentTypeHash = ReadPtr();

            if (contentTypeHash != 0x7CCDA2E7) // ~GenericRwacWaveContent::SK_WAVE_DATA_CONTENT_TYPE~
                continue;

            ushort pathLength = br.ReadUInt16();
            byte loadMethod = br.ReadByte();
            byte loadTime = br.ReadByte();
            string name = br.ReadLenString(pathLength);

            if (name.Contains(".GinsuFile"))
                continue;

            dict.TryAdd(nameHash, name); // There will be duplicates that don't get added
        }

        return dict;
    }

    public bool Write(BundleEntry entry)
    {
        MemoryStream ms = new();
        BinaryWriter2 bw = new(ms, entry.Console);
        bool x64 = entry.Platform == BundlePlatform.PCx64;

        bw.Write(Version);
        bw.Write(Magic);
        int partialsPointer = is64Bit
            ? 0x18
            : 0x14;

        if (is64Bit)
            bw.Write((long)partialsPointer);
        else
            bw.Write(partialsPointer);

        bw.Write(partials.Count);

        // Write partials without graphs pointers
        int numPriorGraphs = 0;
        int numPriorPoints = 0;

        for (int i = 0; i < partials.Count; ++i)
        {
            bw.BaseStream.Position = partialsPointer + i * (x64
                ? 0x18
                : is64Bit
                    ? 0x10
                    : 0xC);
            partials[i].Write(bw, is64Bit, x64, partialsPointer, partials.Count, ref numPriorGraphs, ref numPriorPoints);
        }

        for (int i = 0; i < 2; ++i) // No idea why, but they write 16 null bytes after everything, and it's consistent
            bw.Write((ulong)0);

        uint resourceLength = (uint)bw.BaseStream.Position;
        bw.Align(0x10);
        bw.BaseStream.Position = is64Bit
            ? 0x14
            : 0x10;
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
    private uint waveNameHash = 0; // Name hash

    internal uint WaveNameHash
    {
        get => waveNameHash;
    }

    private string waveName;

    public string WaveName
    {
        get => waveName;
    }

    private List<Graph> graphs;

    public List<Graph> Graphs
    {
        get => graphs;
    }

    public Partial()
    {
        graphs = [];
    }

    public Partial(BinaryReader2 br, bool is64Bit, bool x64)
    {
        graphs = [];

        // Position differs between 32-bit and 64-bit
        long graphsPointer = 0;

        if (x64)
        {
            waveNameHash = br.ReadUInt32();
            br.BaseStream.Position += 4;
            graphsPointer = br.ReadInt64();
        }
        else if (is64Bit)
        {
            graphsPointer = br.ReadInt64();
            waveNameHash = br.ReadUInt32();
        }
        else
        {
            waveNameHash = br.ReadUInt32();
            graphsPointer = br.ReadInt32();
        }

        byte numGraphs = br.ReadByte();

        for (int i = 0; i < numGraphs; ++i)
        {
            br.BaseStream.Position = graphsPointer + i * (is64Bit
                ? 0x10
                : 8);
            graphs.Add(new(br, is64Bit));
        }
    }

    public void Write(BinaryWriter2 bw, bool is64Bit, bool x64, long partialsPointer, int numPartials, ref int numPriorGraphs, ref int numPriorPoints)
    {
        long graphsPointer = partialsPointer;

        if (x64)
        {
            graphsPointer += numPartials * 0x18 + numPriorGraphs * 0x10 + numPriorPoints * 8;
            bw.Write(waveNameHash);
            bw.Write(0);
            bw.Write(graphsPointer);
        }
        else if (is64Bit)
        {
            graphsPointer += numPartials * 0x10 + numPriorGraphs * 0x10 + numPriorPoints * 8;
            bw.Write(graphsPointer);
            bw.Write(waveNameHash);
        }
        else
        {
            graphsPointer += numPartials * 0xC + numPriorGraphs * 8 + numPriorPoints * 8;
            bw.Write(waveNameHash);
            bw.Write((int)graphsPointer);
        }

        bw.Write((byte)graphs.Count);
        bw.WriteUniquePadding(x64
            ? 7
            : 3);

        bw.BaseStream.Position = graphsPointer;
        int numPriorPointsInGraph = 0;

        for (int i = 0; i < graphs.Count; ++i)
        {
            bw.BaseStream.Position = graphsPointer + i * (is64Bit
                ? 0x10
                : 8);
            graphs[i].Write(bw, is64Bit, graphsPointer, graphs.Count, ref numPriorPoints, ref numPriorPointsInGraph);
            numPriorGraphs++;
        }
    }

    public void Clear()
    {
        graphs.Clear();
    }

    internal void SetWaveNameAsHash()
    {
        waveName = "0x" + waveNameHash.ToString("X8");
    }

    internal void SetWaveName(string newWaveName)
    {
        if (newWaveName.StartsWith("gamedb://"))
        {
            int subStrStart = newWaveName.LastIndexOf('/') + 1;
            int subStrLen = newWaveName.IndexOf(".WaveFile") - subStrStart;
            string shortWaveName = newWaveName.Substring(subStrStart, subStrLen);
            waveName = shortWaveName;
        }
        else
        {
            waveName = newWaveName;
        }
    }
}

public class Graph
{
    private sbyte xAxis = 0;

    internal sbyte XAxis
    {
        get => xAxis;
    }

    private sbyte yAxis = 0;

    internal sbyte YAxis
    {
        get => yAxis;
    }

    private List<Point> points;

    public List<Point> Points
    {
        get => points;
    }

    public Graph()
    {
        points = [];
    }

    public Graph(BinaryReader2 br, bool is64Bit)
    {
        points = [];

        long pointsPointer = is64Bit
            ? br.ReadInt64()
            : br.ReadInt32();
        byte numPoints = br.ReadByte();
        xAxis = br.ReadSByte();
        yAxis = br.ReadSByte();

        for (int i = 0; i < numPoints; ++i)
        {
            br.BaseStream.Position = pointsPointer + i * 8;
            points.Add(new(br));
        }
    }

    public void Clear()
    {
        points.Clear();
    }

    public void Write(BinaryWriter2 bw, bool is64Bit, long graphsPointer, int numGraphs, ref int numPriorPoints, ref int numPriorPointsInGraph)
    {
        long pointsPointer = graphsPointer + numGraphs * (is64Bit
            ? 0x10
            : 8) + numPriorPointsInGraph * 8;

        if (is64Bit)
            bw.Write(pointsPointer);
        else
            bw.Write((int)pointsPointer);

        bw.Write((byte)points.Count);
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
            numPriorPointsInGraph++;
        }
    }
}

public class Point
{
    private float x = 0;

    public float X
    {
        get => x; set => x = value;
    }

    private float y = 0;

    public float Y
    {
        get => y; set => y = value;
    }

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
