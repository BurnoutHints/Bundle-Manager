using System.Collections.Generic;
using System.Drawing;
using BundleFormat;
using BundleUtilities;
using ModelViewer;
using ModelViewer.SceneData;
using PluginAPI;

namespace WorldCollisionHandler;

// CgsPhysics::CollisionMeshData from early 2007 bundles
// https://burnout.wiki/wiki/Clustered_Mesh/Development
public class ClusteredMesh : IEntryData
{
    private const int UnitTypeQuad = 2;
    private const int UnitTypeTriList = 3;
    private const int UnitFlagNormal = 0x10;
    private const int UnitFlagEdgeAngle = 0x20;
    private const int UnitFlagGroupID = 0x40;
    private const int UnitFlagSurfaceID = 0x80;

    public List<Mesh> Clusters = [];

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        if (entry.Archive?.BNDVersion != 1)
            throw new ReadFailedError("Only BND1 clustered meshes can be read.");

        Clusters.Clear();

        using BinaryReader2 br = new(entry.MakeStream());
        br.BigEndian = entry.Console;

        uint volume = br.ReadUInt32();

        if (volume == 0)
            return true; // No collision

        // Volume
        br.BaseStream.Position = volume + 0x44;
        long mesh = br.ReadUInt32();

        // Pointers are relative to the mesh header
        br.BaseStream.Position = mesh + 0x34;
        uint clusterTable = br.ReadUInt32();
        uint clusterCount = br.ReadUInt32();
        br.BaseStream.Position = mesh + 0x48;
        float granularity = br.ReadSingle();
        br.BaseStream.Position = mesh + 0x52;
        ushort defaultGroupID = br.ReadUInt16();
        ushort defaultSurfaceID = br.ReadUInt16();
        br.BaseStream.Position = mesh + 0x57;
        int groupIDSize = br.ReadByte();
        int surfaceIDSize = br.ReadByte();

        for (int i = 0; i < clusterCount; i++)
        {
            br.BaseStream.Position = mesh + clusterTable + 4 * i;
            long cluster = mesh + br.ReadUInt32();

            br.BaseStream.Position = cluster;
            ushort unitCount = br.ReadUInt16();
            br.ReadUInt16(); // unitDataSize
            ushort unitDataStart = br.ReadUInt16();
            br.ReadUInt16(); // normalStart
            byte vertexCount = br.ReadByte();
            br.ReadByte(); // normalCount
            byte compressionMode = br.ReadByte();

            Mesh result = new()
            {
                Faces = []
            };

            br.BaseStream.Position = cluster + 0x10;

            switch (compressionMode)
            {
                case 0: // Uncompressed vpu::Vector3
                    for (int v = 0; v < vertexCount; v++)
                    {
                        result.Vertices.Add(new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                        br.ReadSingle();
                    }
                    break;
                case 1: // u16 offsets from an s32 base
                    int baseX = br.ReadInt32(), baseY = br.ReadInt32(), baseZ = br.ReadInt32();
                    for (int v = 0; v < vertexCount; v++)
                        result.Vertices.Add(new((baseX + br.ReadUInt16()) * granularity, (baseY + br.ReadUInt16()) * granularity, (baseZ + br.ReadUInt16()) * granularity));
                    break;
                case 2: // s32
                    for (int v = 0; v < vertexCount; v++)
                        result.Vertices.Add(new(br.ReadInt32() * granularity, br.ReadInt32() * granularity, br.ReadInt32() * granularity));
                    break;
                default:
                    throw new ReadFailedError($"Unknown vertex compression mode {compressionMode} in cluster {i}.");
            }

            // Units begin 16 bytes past unitDataStart relative to vertex array
            br.BaseStream.Position = cluster + 0x10 * (unitDataStart + 1);

            for (int u = 0; u < unitCount; u++)
            {
                byte header = br.ReadByte();
                int currentType = header & 0xF;
                int triangleCount = currentType == UnitTypeTriList
                    ? br.ReadByte()
                    : currentType == UnitTypeQuad
                        ? 2
                        : 1;
                byte[] indices = br.ReadBytes(triangleCount + 2);

                if ((header & UnitFlagNormal) != 0)
                    br.ReadByte();

                if ((header & UnitFlagEdgeAngle) != 0)
                    br.ReadBytes(triangleCount + 2);

                // IDs are little-endian across all platforms
                int groupID = (header & UnitFlagGroupID) != 0
                    ? ReadID(br, groupIDSize)
                    : defaultGroupID;
                int surfaceID = (header & UnitFlagSurfaceID) != 0
                    ? ReadID(br, surfaceIDSize)
                    : defaultSurfaceID;

                Material material = new($"{groupID:X4}_{surfaceID:X4}", Color.White);

                // Strip order: (0,1,2), (3,2,1), (2,3,4)...
                for (int t = 0; t < triangleCount; t++)
                {
                    MeshFace face = new()
                    {
                        Material = material
                    };

                    if (t % 2 == 0)
                        face.Indices.AddRange([indices[t], indices[t + 1], indices[t + 2]]);
                    else
                        face.Indices.AddRange([indices[t + 2], indices[t + 1], indices[t]]);

                    result.Faces.Add(face);
                }
            }

            Clusters.Add(result);
        }

        return true;
    }

    private static int ReadID(BinaryReader2 br, int size)
    {
        byte[] bytes = br.ReadBytes(size);
        return size == 2
            ? bytes[0] | (bytes[1] << 8)
            : bytes[0];
    }

    public Scene MakeScene()
    {
        Scene scene = new();

        for (int i = 0; i < Clusters.Count; i++)
            scene.AddObject(new(i.ToString(), new(Clusters[i])));

        return scene;
    }

    public bool Write(BundleEntry entry)
    {
        return false; // Preview only
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.CollisionMeshData;
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        ModelViewerForm viewer = new();
        viewer.Renderer.Scene = MakeScene();
        return viewer;
    }
}
