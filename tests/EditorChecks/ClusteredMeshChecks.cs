using System.Buffers.Binary;
using System.Numerics;
using WorldCollisionHandler;

internal static partial class Program
{
    private static void CheckClusteredMeshBuilder()
    {
        const int cellCount = 60;
        List<CollisionTriangle> grid = [];

        for (int x = 0; x < cellCount; x++)
        {
            for (int z = 0; z < cellCount; z++)
            {
                Vector3 a = new(x, 2, z), b = new(x, 2, z + 1), c = new(x + 1, 2, z), d = new(x + 1, 2, z + 1);
                ushort group = (ushort)(0x8000 | x / 10), material = (ushort)(0x8000 | z % 3 << 4);
                grid.Add(new(a, b, c, group, material));
                grid.Add(new(c, b, d, group, material));
            }
        }

        byte[] built = ClusteredMeshBuilder.Build(grid);
        Check(ClusteredMeshBuilder.Validate(built) == null, "A built clustered mesh passes the structure check");

        uint word(byte[] b, int at) => BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(at));

        int mesh = (int)word(built, (int)word(built, 0) + 0x44), units = (int)word(built, mesh + 0x40);
        Check(units == cellCount * cellCount, "Triangles sharing an edge and tags become quads");

        ClusteredMesh read = new();
        read.Read(Bnd1Entry(built), null);
        List<string> tags = [.. read.Clusters.SelectMany(c => c.Faces).Select(f => f.Material.Name)];
        Check(tags.Count == grid.Count, "A built clustered mesh keeps every triangle");
        Check(tags.Order().SequenceEqual(grid.Select(t => $"{t.GroupTag:X4}_{t.MaterialTag:X4}").Order()), "A built clustered mesh keeps each triangle's tags");
        Check(read.Clusters.All(c => c.Vertices.All(v => v.Y == 2 || MathF.Abs(v.Y - 2) < 0.011f)), "A built clustered mesh keeps vertex positions to its granularity");

        int matched = 0, open = 0, table = (int)word(built, mesh + 0x34);

        for (int c = 0; c < (int)word(built, mesh + 0x38); c++)
        {
            int cluster = mesh + (int)word(built, mesh + table + c * 4);
            int at = cluster + 16 * (BinaryPrimitives.ReadUInt16BigEndian(built.AsSpan(cluster + 4)) + 1);

            for (int u = 0; u < BinaryPrimitives.ReadUInt16BigEndian(built.AsSpan(cluster)); u++)
            {
                int corners = (built[at] & 15) == 2
                    ? 4
                    : 3;

                for (int e = 0; e < corners; e++)
                {
                    byte edge = built[at + 1 + corners + e];

                    if (edge == 0xA0)
                        open++;
                    else if ((edge & 0x1F) == 26)
                        matched++;
                }

                at += 1 + corners * 2 + 4;
            }
        }

        Check(open >= cellCount * 4, "Edges on the outside of a mesh are marked unmatched");
        Check(matched > 0, "Edges between flat neighbours are marked flat");

        Check(ClusteredMeshBuilder.Build([]) == null, "A mesh with nothing in it isn't built");
        Check(ClusteredMeshBuilder.Build([new(Vector3.One, Vector3.One, Vector3.UnitX, 0x8000, 0x8000)]) == null, "Degenerate triangles are dropped");

        byte[] wide = ClusteredMeshBuilder.Build([new(new(0, 0, 0), new(0, 0, 900), new(900, 0, 0), 0x8000, 0x8000)]);
        Check(ClusteredMeshBuilder.Validate(wide) == null, "A mesh too wide for 16 bit vertices still builds");

        ClusteredMesh wideRead = new();
        wideRead.Read(Bnd1Entry(wide), null);
        Check(wideRead.Clusters[0].Vertices.Any(v => MathF.Abs(v.X - 900) < 0.011f), "Wide meshes keep their vertices");

        byte[] broken = [.. built];
        BinaryPrimitives.WriteUInt32BigEndian(broken.AsSpan(mesh + 0x40), (uint)units + 1);
        Check(ClusteredMeshBuilder.Validate(broken) != null, "The structure check notices a wrong unit count");
    }
}
