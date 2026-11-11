using BaseHandlers;
using BundleFormat;
using MathLib;
using WorldCollisionHandler;
using OVector3 = OpenTK.Mathematics.Vector3;

internal static partial class Program
{
    private static void CheckWorldVisualization()
    {
        TrafficWorldGeometry geometry = null;

        foreach (BundlePlatform platform in (BundlePlatform[])[BundlePlatform.PC, BundlePlatform.PCx64, BundlePlatform.PS3])
        {
            BundleArchive archive = WorldArchive(platform);
            byte[] before = [.. archive.Entries[0].GetPrimaryWithoutImports()];
            geometry = TrafficWorldGeometry.FromArchive(archive, "worldcol.bin");
            Check(geometry.TriangleCount == 2, $"{platform} collision quads become two visualization triangles");
            Check(geometry.Vertices[0].Position == new OVector3(50, 10, 100) && geometry.Vertices[1].Position == new OVector3(55, 10, 100),
                $"{platform} world visualization retains chunk origin and scale");
            Equal(archive.Entries[0].GetPrimaryWithoutImports(), before, $"{platform} visualization leaves collision bytes unchanged");
            Check(!archive.Dirty, $"{platform} world loading does not mark the collision bundle as edited");
        }

        string path = Path.Combine(output, "worldcol.bin");
        WorldArchive(BundlePlatform.PC).Write(path);
        byte[] diskBefore = File.ReadAllBytes(path);
        geometry = TrafficWorldGeometry.Load(path);
        Check(geometry.TriangleCount == 2 && geometry.SourcePath == path, "Loading worldcol.bin reads the collision bundle from disk");
        Equal(File.ReadAllBytes(path), diskBefore, "Loading world geometry never rewrites the source file");

        BundleEntry clustered = Entry(ClusteredWorldBytes());
        clustered.Archive.BNDVersion = 1;
        clustered.Type = EntryType.CollisionMeshData;
        TrafficWorldGeometry clusteredGeometry = TrafficWorldGeometry.FromArchive(clustered.Archive, "worldcol.bin");
        Check(clusteredGeometry.TriangleCount == 1 && clusteredGeometry.Vertices[0].Position == new OVector3(1, 2, 3),
            "BND1 clustered collision meshes use their original world coordinates");

        bool rejected = false;

        try
        {
            TrafficWorldGeometry.FromArchive(new(), "wrong.bin");
        }
        catch (InvalidDataException)
        {
            rejected = true;
        }

        Check(rejected, "A bundle without collision resources reports that a different file is needed");
        rejected = false;

        try
        {
            TrafficWorldGeometry.Load(path, cancel: new(true));
        }
        catch (OperationCanceledException)
        {
            rejected = true;
        }

        Check(rejected, "Cancelling a world load stops before it reads the bundle");

        Traffic source = Sample();
        BundleEntry target = Entry(source.Serialize(false, 4));
        using TrafficEditor editor = new(source, target, false);
        TrafficView view = Field<TrafficView>(editor, "view");
        ObjectTreeEditor tree = Field<ObjectTreeEditor>(editor, "tree");
        OVector3 camera = Field<OVector3>(view, "target");
        float distance = Field<float>(view, "distance");
        byte[] trafficBefore = [.. target.GetPrimaryWithoutImports()];
        view.SetWorldGeometry(geometry);
        Check(view.HasWorldGeometry && Field<CheckBox>(view, "worldLayer").Checked, "Loaded world geometry is enabled as a traffic viewport layer");
        Check(Field<OVector3>(view, "target") == camera && Field<float>(view, "distance") == distance, "Loading world geometry preserves the traffic camera");
        Field<CheckBox>(view, "worldLayer").Checked = false;
        Check(view.HasWorldGeometry, "Hiding the world layer retains its loaded geometry");
        Check(!tree.Modified && !target.Dirty && target.GetPrimaryWithoutImports().SequenceEqual(trafficBefore),
            "World visualization does not create traffic edits or history entries");
        view.Rebuild();
        Check(view.HasWorldGeometry, "Traffic rebuilds keep the world backdrop loaded");
        view.SetWorldGeometry(null);
        Check(!view.HasWorldGeometry && !Field<CheckBox>(view, "worldLayer").Checked && !tree.Modified,
            "Removing the world backdrop leaves the traffic draft unchanged");
    }

    private static BundleArchive WorldArchive(BundlePlatform platform)
    {
        BundleArchive archive = new()
        {
            Platform = platform,
            BNDVersion = 2,
            Version = 2
        };

        BundleEntry entry = archive.CreateEntry(0x1234, EntryType.PolygonSoupList, "trk_col_0", [], 0);
        archive.Entries.Add(entry);
        PolygonSoupChunk chunk = new()
        {
            Position = new(100, 20, 200),
            Scale = 0.5f,
            QuadCount = 1,
            PointList = [new(0, 0, 0), new(10, 0, 0), new(0, 0, 10), new(10, 0, 10)],
            PropertyList = [new()
            {
                Indices = [0, 1, 2, 3]
            }]
        };

        PolygonSoupList polygons = new()
        {
            Chunks = [chunk],
            BoundingBoxes = [new(new BoxF(new(50, 10, 100), new(55, 10, 105)), 0)]
        };

        polygons.Write(entry);
        archive.Dirty = false;
        return archive;
    }

    private static byte[] ClusteredWorldBytes()
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        stream.SetLength(0x200);
        writer.Write(0x40u);
        stream.Position = 0x84;
        writer.Write(0x100u);
        stream.Position = 0x134;
        writer.Write(0x60u);
        writer.Write(1u);
        stream.Position = 0x148;
        writer.Write(1f);
        stream.Position = 0x157;
        writer.Write((byte)1);
        writer.Write((byte)1);
        stream.Position = 0x160;
        writer.Write(0x80u);
        stream.Position = 0x180;
        writer.Write((ushort)1);
        writer.Write((ushort)4);
        writer.Write((ushort)3);
        writer.Write((ushort)0);
        writer.Write((byte)3);
        writer.Write((byte)0);
        writer.Write((byte)0);
        stream.Position = 0x190;

        foreach (OVector3 point in (OVector3[])[new(1, 2, 3), new(5, 2, 3), new(1, 2, 7)])
        {
            writer.Write(point.X);
            writer.Write(point.Y);
            writer.Write(point.Z);
            writer.Write(0f);
        }

        writer.Write((byte)1);
        writer.Write((byte)0);
        writer.Write((byte)1);
        writer.Write((byte)2);
        return stream.ToArray();
    }
}
