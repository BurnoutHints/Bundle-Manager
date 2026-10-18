using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using BundleFormat;
using BundleUtilities;
using MathLib;
using ModelViewer;
using ModelViewer.SceneData;
using OpenTK.Mathematics;
using PluginAPI;

namespace WorldCollisionHandler;

public class PolygonSoupProperty
{
    public uint UnknownProperty;
    public byte[] Indices;
    public byte[] UnknownBytes;

    public PolygonSoupProperty()
    {
        Indices = new byte[4];
        UnknownBytes = new byte[4];
    }

    public PolygonSoupProperty Copy()
    {
        PolygonSoupProperty result = new();

        result.UnknownProperty = UnknownProperty;
        for (int i = 0; i < Indices.Length; i++)
        {
            result.Indices[i] = Indices[i];
        }
        for (int i = 0; i < UnknownBytes.Length; i++)
        {
            result.UnknownBytes[i] = UnknownBytes[i];
        }

        return result;
    }

    public static PolygonSoupProperty Read(BinaryReader2 br)
    {
        PolygonSoupProperty result = new();

        result.UnknownProperty = br.ReadUInt32();

        for (int i = 0; i < result.Indices.Length; i++)
        {
            result.Indices[i] = br.ReadByte();
        }

        for (int i = 0; i < result.UnknownBytes.Length; i++)
        {
            result.UnknownBytes[i] = br.ReadByte();
        }

        return result;
    }

    public void Write(BinaryWriter2 bw, bool x64)
    {
        // Cap to 0x9D64 for PC
        ushort unknownProperty1 = (ushort)(UnknownProperty & 0xFFFF);

        // Patch values that are too high (from console) with Schembri Pass
        if (!x64 && unknownProperty1 > 0x9D64 && unknownProperty1 != 0xFFFF)
        {
            // 0x8316, 0x9531,  0x9007, 0x8511, 0x8316, 0xFFFF
            unknownProperty1 = 0x8316;
        }

        // Patch values that are too high(from console) with 0xFFFF
        //if (unknownProperty1 > 0x9D64)
        //    unknownProperty1 = 0xFFFF;

        ushort unknownProperty2 = (ushort) ((UnknownProperty >> 16) & 0xFFFF);

        // Remove wreck surfaces - TODO: tmp
        //unknownProperty2 &= unchecked((ushort)~0x4000);

        uint unknownProperty = (uint)((unknownProperty2 << 16) | unknownProperty1);

        bw.Write(unknownProperty);

        for (int i = 0; i < Indices.Length; i++)
        {
            bw.Write(Indices[i]);
        }

        for (int i = 0; i < UnknownBytes.Length; i++)
        {
            bw.Write(UnknownBytes[i]);
        }
    }

    public override string ToString()
    {
        return "Prop: " + UnknownProperty.ToString("X8");
    }
}

public class PolygonSoupBoundingBox
{
    public BoxF Box;
    public int Terminator;

    public PolygonSoupBoundingBox(BoxF box, int terminator)
    {
        Box = box;
        Terminator = terminator;
    }

    public override string ToString()
    {
        return Box.ToString() + ", " + Terminator;
    }
}

public class PolygonSoupChunk
{
    public MathLib.Vector3I Position;
    public float Scale;
    public byte QuadCount;
    public byte Unknown10;
    public short Unknown11;

    public List<Vector3S> PointList;
    public List<PolygonSoupProperty> PropertyList;

    public PolygonSoupChunk()
    {
        PointList = [];
        PropertyList = [];
    }

    public static PolygonSoupChunk Read(BinaryReader2 br, bool x64)
    {
        PolygonSoupChunk result = new();
        long ReadPtr() => x64 ? (long)br.ReadUInt64() : br.ReadUInt32();

        result.Position = br.ReadVector3I();
        result.Scale = br.ReadSingle();
        long propertyListStart = ReadPtr();
        long pointListStart = ReadPtr();
        br.ReadInt16(); // Length
        byte propertyListCount = br.ReadByte();
        result.QuadCount = br.ReadByte();
        int pointCount = br.ReadByte();
        result.Unknown10 = br.ReadByte();
        result.Unknown11 = br.ReadInt16();

        br.BaseStream.Position = pointListStart;
        for (int i = 0; i < pointCount; i++)
        {
            result.PointList.Add(br.ReadVector3S());
        }

        br.BaseStream.Position = propertyListStart;

        for (int i = 0; i < propertyListCount; i++)
        {
            result.PropertyList.Add(PolygonSoupProperty.Read(br));
        }

        return result;
    }

    public void Write(BinaryWriter2 bw, bool x64)
    {
        void WritePtr(long ptr) { if (x64) bw.Write((ulong)ptr); else bw.Write((uint)ptr); }

        long chunkStartPtr = bw.BaseStream.Position;
        bw.Write(Position);
        bw.Write(Scale);
        long propListStartPtr = bw.BaseStream.Position;
        WritePtr(0);
        long pointListStartPtr = bw.BaseStream.Position;
        WritePtr(0);
        long lengthPtr = bw.BaseStream.Position;
        bw.Write((ushort) 0);
        bw.Write((byte)PropertyList.Count);
        bw.Write(QuadCount);
        bw.Write((byte)PointList.Count);
        bw.Write(Unknown10);
        bw.Write(Unknown11);
        
        long cPos = bw.BaseStream.Position;
        bw.BaseStream.Position = pointListStartPtr;
        WritePtr(cPos);
        bw.BaseStream.Position = cPos;

        for (int i = 0; i < PointList.Count; i++)
        {
            bw.Write(PointList[i]);
        }

        bw.BaseStream.Position = (16 * ((bw.BaseStream.Position + 15) / 16));
        cPos = bw.BaseStream.Position;
        bw.BaseStream.Position = propListStartPtr;
        WritePtr(cPos);
        bw.BaseStream.Position = cPos;

        for (int i = 0; i < PropertyList.Count; i++)
        {
            PropertyList[i].Write(bw, x64);
        }
        cPos = bw.BaseStream.Position;
        bw.BaseStream.Position = lengthPtr;
        bw.Write((short)(bw.BaseStream.Length - chunkStartPtr));
        bw.BaseStream.Position = cPos;

    }

    public Mesh BuildMesh(MathLib.Vector3I pos, float scale)
    {
        Mesh mesh = new();
        mesh.Faces = [];

        for (int i = 0; i < PropertyList.Count; i++)
        {
            MeshFace face = new();
            PolygonSoupProperty property = PropertyList[i];
            face.Indices.Add(property.Indices[0]);
            face.Indices.Add(property.Indices[1]);
            face.Indices.Add(property.Indices[2]);

            ushort unknownProperty1 = (ushort) (property.UnknownProperty & 0xFFFF);
            ushort unknownProperty2 = (ushort) ((property.UnknownProperty >> 16)); // & 0xFFFF);

            string unknownBytes = property.UnknownBytes[0].ToString("X2") + "_" +
                                  property.UnknownBytes[1].ToString("X2") + "_" +
                                  property.UnknownBytes[2].ToString("X2") + "_" +
                                  property.UnknownBytes[3].ToString("X2");
            face.Material = new Material(
                unknownProperty1.ToString("X4") + "_" + unknownProperty2.ToString("X4") + "_" + unknownBytes,
                Color.White);

            mesh.Faces.Add(face);
            
            if (property.Indices[3] != 0xFF)
            {
                MeshFace face2 = new();
                face2.Material = face.Material;
                face2.Indices.Add(property.Indices[3]);
                face2.Indices.Add(property.Indices[2]);
                face2.Indices.Add(property.Indices[1]);
                mesh.Faces.Add(face2);
            }
        }


        List<Vector3S> points = PointList;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3S vert = points[i];
            mesh.Vertices.Add(new Vector3((vert.X + pos.X) * scale, (vert.Y + pos.Y) * scale, (vert.Z + pos.Z) * scale));
        }

        return mesh;
    }

    public override string ToString()
    {
        return "Pos: " + Position + ", Scale: " + Scale + ", PointCount: " + PointList.Count;
    }
}

public class PolygonSoupList : IEntryData
{
    public Vector3 Min;
    public int Unknown4;
    public Vector3 Max;
    public int Unknown8;
    //public uint ChunkPointerStart;
    //public uint BoxListStart;
    //public int ChunkCount;
    //public uint FileSize;

    //public List<uint> ChunkPointers;
    public List<PolygonSoupBoundingBox> BoundingBoxes;
    public List<PolygonSoupChunk> Chunks;

    public PolygonSoupList()
    {
        //ChunkPointers = new List<uint>();
        BoundingBoxes = [];
        Chunks = [];
    }

    public void RemoveWreckSurfaces()
    {
        foreach (PolygonSoupChunk chunk in Chunks)
        {
            foreach (PolygonSoupProperty property in chunk.PropertyList)
            {
                ushort unknownProperty1 = (ushort)(property.UnknownProperty & 0xFFFF);

                ushort unknownProperty2 = (ushort)((property.UnknownProperty >> 16) & 0xFFFF);

                // Remove wreck surfaces - TODO: tmp
                unknownProperty2 &= unchecked((ushort)~0x4000);

                uint unknownProperty = (uint)((unknownProperty2 << 16) | unknownProperty1);

                property.UnknownProperty = unknownProperty;
            }
        }
    }

    public Scene MakeScene(ILoader loader = null)
    {
        Scene scene = new();

        int index = 0;
        foreach (PolygonSoupChunk chunk in Chunks)
        {
            string id = index.ToString();

            MathLib.Vector3I pos = chunk.Position;
            float scale = chunk.Scale;
            Model model = new(chunk.BuildMesh(pos, scale));
            SceneObject sceneObject = new(id, model);
            scene.AddObject(sceneObject);
            index++;
        }

        return scene;
    }

    public void ImportObj(string path)
    {
        GenericModel model = OBJImporter.ImportOBJ(path);
        model.SplitByPointCount(255);

        // Verify data before applying
        foreach (GenericMesh mesh in model.Meshes)
        {
            // Get highest point
            uint pointCount = 0;
            foreach (uint key in mesh.Vertices.Keys)
            {
                if (pointCount < key)
                {
                    pointCount = key;
                }
            }
            pointCount++;
            // Is it too big for a byte?
            if (pointCount > 256)
                throw new ReadFailedError("Too many points for mesh: " + mesh.Name + ", " + pointCount + " > 256");
            foreach (Face face in mesh.Faces)
            {
                // Triangulation required for now.
                if (face.Indices.Count > 3)
                    throw new ReadFailedError("Please triangulate your mesh: " + mesh.Name);

                // Material names are required
                if (string.IsNullOrEmpty(face.Material?.Name))
                    throw new ReadFailedError("Invalid Material for mesh: " + mesh.Name);

                // Verify that all data is there
                string[] split = face.Material.Name.Split('_');
                if (split.Length < 7)
                    throw new ReadFailedError("Invalid Material Data: " + face.Material.Name + ", for mesh: " + mesh.Name);

                // Verify that all data can be parsed
                try
                {
                    Utilities.Parse(split[1], true, out ushort _);
                    Utilities.Parse(split[2], true, out ushort _);
                    Utilities.Parse(split[3], true, out byte _);
                    Utilities.Parse(split[4], true, out byte _);
                    Utilities.Parse(split[5], true, out byte _);
                    Utilities.Parse(split[6], true, out byte _);
                }
                catch (NotSupportedException)
                {
                    throw new ReadFailedError("Unable to Parse Material Data: " + face.Material.Name + ", for mesh: " + mesh.Name);
                }
            }
        }

        // Clear existing data
        Chunks.Clear();
        BoundingBoxes.Clear();

        // Global vertices list to calculate the bounding box
        List<Vector3> vertices = [];

        // Generate PolygonSoupChunks and BoundingBoxes
        foreach (GenericMesh mesh in model.Meshes)
        {
            PolygonSoupChunk chunk = new();
            foreach (Face face in mesh.Faces)
            {
                PolygonSoupProperty property = new();

                // Set Indices
                property.Indices[0] = (byte)face.Indices[0];
                property.Indices[1] = (byte)face.Indices[1];
                property.Indices[2] = (byte)face.Indices[2];

                if (face.Indices.Count > 3)
                    property.Indices[3] = (byte) face.Indices[3];
                else 
                    property.Indices[3] = 0xFF;

                // Get data from material name
                string materialName = face.Material.Name;
                string[] split = materialName.Split('_');

                // Parse the values
                Utilities.Parse(split[1], true, out ushort unknownProperty1);
                Utilities.Parse(split[2], true, out ushort unknownProperty2);
                Utilities.Parse(split[3], true, out byte unknownByte1);
                Utilities.Parse(split[4], true, out byte unknownByte2);
                Utilities.Parse(split[5], true, out byte unknownByte3);
                Utilities.Parse(split[6], true, out byte unknownByte4);

                // Combine unknownProperty1 and unknownProperty2
                property.UnknownProperty = (uint) ((unknownProperty2 << 16) | unknownProperty1);

                // Set unknown bytes
                property.UnknownBytes[0] = unknownByte1;
                property.UnknownBytes[1] = unknownByte2;
                property.UnknownBytes[2] = unknownByte3;
                property.UnknownBytes[3] = unknownByte4;

                // Add the property
                chunk.PropertyList.Add(property);
            }

            // Add the vertices to the global vertices list
            vertices.AddRange(mesh.Vertices.Values.ToArray());

            // Get the minimum and maximum point of the mesh
            Vector3 min = MathUtils.MinBounds([.. mesh.Vertices.Values]);
            Vector3 max = MathUtils.MaxBounds([.. mesh.Vertices.Values]);

            // Use the minimum as the position
            Vector3 position = min;

            // Set the scale to something standard
            float scale = 0.015f;
            
            // Get the point count
            uint pointCount = 0;
            foreach (uint key in mesh.Vertices.Keys)
            {
                if (pointCount < key)
                {
                    pointCount = key;
                }
            }
            pointCount++;
            Vector3S[] verts = new Vector3S[pointCount];
            foreach (uint key in mesh.Vertices.Keys)
            {
                // Convert the point to a short and apply position and scale
                verts[key] = new Vector3S((mesh.Vertices[key] - position) / scale);
            }
            chunk.PointList = [.. verts];
            
            // Quads are currently unsupported
            chunk.QuadCount = 0;

            chunk.Scale = scale;

            // Set the position and apply scale
            chunk.Position = new MathLib.Vector3I(position / scale);

            // Add the chunk
            Chunks.Add(chunk);

            // Add the bounding box
            BoundingBoxes.Add(new PolygonSoupBoundingBox(new BoxF(min, max), -1));
        }

        // Calculate Bounding Box
        Min = MathUtils.MinBounds([.. vertices]);
        Max = MathUtils.MaxBounds([.. vertices]);
    }

    private void Clear()
    {
        Min = default;
        Unknown4 = default;
        Max = default;
        Unknown8 = default;
        BoundingBoxes.Clear();
        Chunks.Clear();
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        Clear();

        MemoryStream ms = entry.MakeStream();
        BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

        bool x64 = entry.Platform == BundlePlatform.PCx64;
        long ReadPtr() => x64 ? (long)br.ReadUInt64() : br.ReadUInt32();

        Min = br.ReadVector3F();
        Unknown4 = br.ReadInt32();
        Max = br.ReadVector3F();
        Unknown8 = br.ReadInt32();
        long chunkPointerStart = ReadPtr();
        long boxListStart = ReadPtr();
        int chunkCount = br.ReadInt32();
        br.ReadUInt32(); // FileSize

        List<long> chunkPointers = [];

        // No Data
        if (chunkCount == 0)
        {
            br.Close();
            ms.Close();
            return true;
        }

        br.BaseStream.Position = chunkPointerStart;

        for (int i = 0; i < chunkCount; i++)
        {
            chunkPointers.Add(ReadPtr());
        }

        for (int i = 0; i < chunkCount; i++)
        {
            // Read Vertically

            long pos = boxListStart + 0x70 * (i / 4) + 4 * (i % 4);

            BoxF boundingBox = new();
            br.BaseStream.Position = pos;
            float minX = br.ReadSingle();
            br.BaseStream.Position += 12;
            float minY = br.ReadSingle();
            br.BaseStream.Position += 12;
            float minZ = br.ReadSingle();

            boundingBox.Min = new Vector3(minX, minY, minZ);

            br.BaseStream.Position += 12;
            float maxX = br.ReadSingle();
            br.BaseStream.Position += 12;
            float maxY = br.ReadSingle();
            br.BaseStream.Position += 12;
            float maxZ = br.ReadSingle();
            br.BaseStream.Position += 12;

            boundingBox.Max = new Vector3(maxX, maxY, maxZ);

            PolygonSoupBoundingBox box = new(boundingBox, br.ReadInt32());

            BoundingBoxes.Add(box);
        }

        for (int i = 0; i < chunkPointers.Count; i++)
        {
            br.BaseStream.Position = chunkPointers[i];

            Chunks.Add(PolygonSoupChunk.Read(br, x64));
        }

        br.Close();
        ms.Close();

        return true;
    }

    public bool Write(BundleEntry entry)
    {
        MemoryStream ms = new();
        BinaryWriter2 bw = new(ms);
        bw.BigEndian = entry.Console;

        bool x64 = entry.Platform == BundlePlatform.PCx64;
        void WritePtr(long ptr) { if (x64) bw.Write((ulong)ptr); else bw.Write((uint)ptr); }

        bw.Write(Min);
        bw.Write(Unknown4);
        bw.Write(Max);
        bw.Write(Unknown8);
        long chunkPointerStartPos = bw.BaseStream.Position;
        WritePtr(0);
        long boxListStartPos = bw.BaseStream.Position;
        WritePtr(0);
        bw.Write(Chunks.Count);
        long fileSizePos = bw.BaseStream.Position;
        bw.Write((uint)0);

        uint[] chunkPointers = new uint[Chunks.Count];

        // No Data
        if (Chunks.Count == 0)
        {
            if (x64)
            {
                bw.BaseStream.Position = fileSizePos;
                bw.Write((uint)bw.BaseStream.Length);
                bw.BaseStream.SetLength(0x40);
            }

            bw.Flush();
            byte[] data2 = ms.ToArray();
            bw.Close();
            ms.Close();

            entry.EntryBlocks[0].Data = data2;
            entry.Dirty = true;
            return true;
        }

        long cPos = bw.BaseStream.Position;
        uint chunkPointerStart = (uint)bw.BaseStream.Position;
        bw.BaseStream.Position = chunkPointerStartPos;
        bw.Write((uint)cPos);
        bw.BaseStream.Position = cPos;

        for (int i = 0; i < Chunks.Count; i++)
        {
            WritePtr(0);
        }

        bw.BaseStream.Position = (16 * ((bw.BaseStream.Position + 15) / 16));
        cPos = bw.BaseStream.Position;
        uint boxListStart = (uint)bw.BaseStream.Position;
        bw.BaseStream.Position = boxListStartPos;
        WritePtr(cPos);

        for (int i = 0; i < Chunks.Count; i++)
        {
            // Write Vertically

            long pos = boxListStart + 0x70 * (i / 4) + 4 * (i % 4);

            PolygonSoupBoundingBox box = BoundingBoxes[i];

            bw.BaseStream.Position = pos;
            bw.Write(box.Box.Min.X);
            bw.BaseStream.Position += 12;
            bw.Write(box.Box.Min.Y);
            bw.BaseStream.Position += 12;
            bw.Write(box.Box.Min.Z);

            bw.BaseStream.Position += 12;
            bw.Write(box.Box.Max.X);
            bw.BaseStream.Position += 12;
            bw.Write(box.Box.Max.Y);
            bw.BaseStream.Position += 12;
            bw.Write(box.Box.Max.Z);

            bw.BaseStream.Position += 12;
            bw.Write(box.Terminator);
        }

        bw.BaseStream.Position = (16 * ((bw.BaseStream.Position + 15) / 16));

        if (!x64)
        {
            bw.BaseStream.Position = (128 * ((bw.BaseStream.Position + 127) / 128));

            for (int i = 0; i < Chunks.Count / 4; i++)
            {
                if ((i % 8) % 3 == 0)
                {
                    bw.BaseStream.Position += 256;
                }
                else
                {
                    bw.BaseStream.Position += 384;
                }
            }
        }

        for (int i = 0; i < chunkPointers.Length; i++)
        {
            bw.BaseStream.Position = (128 * ((bw.BaseStream.Position + 127) / 128));

            chunkPointers[i] = (uint)bw.BaseStream.Position;

            Chunks[i].Write(bw, x64);
        }

        if (x64)
        {
            cPos = bw.BaseStream.Position + 0x60;
            bw.BaseStream.SetLength(16 * ((cPos + 15) / 16));
        }
        else
        {
            bw.BaseStream.Position = (16 * ((bw.BaseStream.Position + 15) / 16)) + 0x5F;
            bw.Write((byte)0);
            cPos = bw.BaseStream.Position;
        }

        bw.BaseStream.Position = fileSizePos;
        bw.Write((uint)cPos);

        bw.BaseStream.Position = chunkPointerStart;
        for (int i = 0; i < Chunks.Count; i++)
        {
            WritePtr(chunkPointers[i]);
        }

        bw.Flush();
        byte[] data = ms.ToArray();
        bw.Close();
        ms.Close();

        entry.EntryBlocks[0].Data = data;
        entry.Dirty = true;

        return true;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.PolygonSoupList;
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        WorldColEditor editor = new();
        editor.Poly = this;
        editor.Changed += () =>
        {
            Write(entry);
        };

        return editor;
    }
}
