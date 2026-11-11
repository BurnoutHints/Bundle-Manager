using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BundleFormat;
using OpenTK.Mathematics;
using static BaseHandlers.PropPhysics;
using NVector4 = System.Numerics.Vector4;
using Vertex = BaseHandlers.TrafficWorldGeometry.Vertex;

namespace BaseHandlers;

public sealed class PropPhysicsEditor : ResourceEditor
{
    private readonly PropPhysics data;

    public PropPhysicsEditor(PropPhysics source, BundleEntry initialEntry) : base(initialEntry, "Prop physics", "prop physics")
    {
        data = (PropPhysics)ObjectTreeEditor.Clone(source);
        TrafficLabels.Install();
        TrafficLabels.Development = data.Development;
        Disposed += (_, _) => TrafficLabels.Development = false;
        PropPhysicsView view = new(data)
        {
            Dock = DockStyle.Fill
        };

        Initialize(data, "PropPhysics", view);
        Tree.SearchPrompt = "Search prop names, models or masses";
        Tree.CollectionLimit = (_, list) => list switch
        {
            nameof(PropPhysics.maPropTypes) => 500,
            nameof(PropTypeData.maCollisionVolumes) => 63,
            _ => byte.MaxValue
        };

        // Referenced by index in external resources
        Tree.PositionsFixed = (_, list) => list is nameof(PropPhysics.maPropTypes) or nameof(PropTypeData.maParts);
    }

    protected override List<TrafficRules.Issue> FindIssues() => [.. data.Problems()];

    protected override void WriteResource() => data.Write(ResourceEntry);
}

// Visualizes prop collision volumes in a grid layout
public sealed class PropPhysicsView : SceneView
{
    private static readonly string[] layerNames = ["Prop volumes", "Part volumes", "Joints", "Centre of mass", "Bounding spheres"];
    private const int Volumes = 0, Parts = 1, Joints = 2, Masses = 3, Spheres = 4;
    private const int RingSteps = 24;

    private static readonly uint highlightColour = Colour(255, 230, 0), volumeColour = Colour(80, 220, 255), jointColour = Colour(255, 150, 40),
        massColour = Colour(255, 80, 255), sphereColour = Colour(110, 110, 120);

    private readonly PropPhysics data;
    private readonly Dictionary<PropTypeData, Vector3> origins = [];
    private PropTypeData framed;

    protected override float MinimumFrameDistance => 2;

    public PropPhysicsView(PropPhysics initialData) : base(layerNames, "Bounding spheres")
    {
        data = initialData;
        Rebuild();
        FrameAll();
    }

    private static Vector3 At(NVector4 v) => new(v.X, v.Y, v.Z);

    private static float Reach(Volume volume, NVector4 offset) => volume.mTransform?.Length == 4
        ? (At(volume.mTransform[3]) + At(offset)).Length + volume.mExtent.Length() + volume.mfRadius
        : 0;

    // Pre-1.0 sphere radii are placeholders; compute reach from volume extents
    private static float Reach(PropTypeData type) => Math.Max(Math.Max(type.mfSphereRadius, 1), type.maCollisionVolumes.Select(v => Reach(v, default))
        .Concat(type.maParts.SelectMany(p => p.maCollisionVolumes.Select(v => Reach(v, p.mOffset)))).DefaultIfEmpty(0).Max()) + 1;

    private void Lay()
    {
        origins.Clear();
        float width = MathF.Sqrt(data.maPropTypes.Sum(t => 4 * Reach(t) * Reach(t))), x = 0, z = 0, depth = 0;

        foreach (PropTypeData type in data.maPropTypes)
        {
            float reach = Reach(type);

            if (x > 0 && x + reach * 2 > width)
            {
                x = 0;
                z += depth;
                depth = 0;
            }

            origins[type] = new(x + reach, 0, z + reach);
            x += reach * 2;
            depth = Math.Max(depth, reach * 2);
        }
    }

    private static void Ring(List<Vertex> to, Vector3 centre, Vector3 a, Vector3 b, float radius, uint colour)
    {
        Vector3 at(int step) => centre + (a * MathF.Cos(step * MathF.Tau / RingSteps) + b * MathF.Sin(step * MathF.Tau / RingSteps)) * radius;

        for (int i = 0; i < RingSteps; i++)
            Line(to, at(i), at(i + 1), colour);
    }

    private static void Shape(List<Vertex> to, Volume volume, Vector3 origin, uint colour)
    {
        if (volume.mTransform?.Length != 4)
            return;

        Vector3 x = At(volume.mTransform[0]), y = At(volume.mTransform[1]), z = At(volume.mTransform[2]), centre = origin + At(volume.mTransform[3]);
        float radius = volume.mfRadius, height = volume.mExtent.X;

        switch (volume.meType)
        {
            case VolumeType.E_VOLUMETYPE_BOX:
                Box(to, (i, j, k) => centre + x * (i * volume.mExtent.X) + y * (j * volume.mExtent.Y) + z * (k * volume.mExtent.Z), colour);
                break;
            case VolumeType.E_VOLUMETYPE_SPHERE:
                Ring(to, centre, x, y, radius, colour);
                Ring(to, centre, x, z, radius, colour);
                Ring(to, centre, y, z, radius, colour);
                break;
            case VolumeType.E_VOLUMETYPE_CAPSULE or VolumeType.E_VOLUMETYPE_CYLINDER:
                Vector3 top = centre + z * height, bottom = centre - z * height;
                Ring(to, top, x, y, radius, colour);
                Ring(to, bottom, x, y, radius, colour);

                foreach (Vector3 side in (Vector3[])[x, -x, y, -y])
                    Line(to, top + side * radius, bottom + side * radius, colour);

                if (volume.meType == VolumeType.E_VOLUMETYPE_CAPSULE)
                {
                    foreach (Vector3 end in (Vector3[])[top, bottom])
                    {
                        Ring(to, end, x, z, radius, colour);
                        Ring(to, end, y, z, radius, colour);
                    }
                }

                break;
            default:
                Cross(to, centre, 0.25f, colour);
                break;
        }
    }

    private void AddVolumes(List<Volume> volumes, Vector3 origin, int layer, uint colour, object[] path)
    {
        foreach (Volume volume in volumes)
        {
            object[] volumePath = [.. path, volumes, volume];
            int before = Layer(layer).Count;
            Shape(Layer(layer), volume, origin, colour);
            List<Vertex> lines = Layer(layer);

            for (int i = before; i + 1 < lines.Count; i += 2)
                AddSegment(lines[i].Position, lines[i + 1].Position, layer, volumePath);

            if (volume.mTransform?.Length == 4)
                AddPickable(origin + At(volume.mTransform[3]), layer, volumePath);
        }
    }

    protected override void BuildScene()
    {
        Lay();

        foreach (PropTypeData type in data.maPropTypes)
        {
            Vector3 origin = origins[type];
            object[] path = [data.maPropTypes, type];
            AddVolumes(type.maCollisionVolumes, origin, Volumes, volumeColour, path);

            for (int p = 0; p < type.maParts.Count; p++)
            {
                PropPartTypeData part = type.maParts[p];
                AddVolumes(part.maCollisionVolumes, origin + At(part.mOffset), Parts, DistinctColour(p), [.. path, type.maParts, part]);
            }

            if (!data.Development)
                Cross(Layer(Joints), origin + At(type.mJointLocator), 0.2f, jointColour);

            Cross(Layer(Masses), origin + At(type.mCOMOffset), 0.2f, massColour);
            Ring(Layer(Spheres), origin, Vector3.UnitX, Vector3.UnitZ, type.mfSphereRadius, sphereColour);
            Ring(Layer(Spheres), origin, Vector3.UnitX, Vector3.UnitY, type.mfSphereRadius, sphereColour);
            AddPickable(origin, Joints, path);
        }
    }

    private PropTypeData Owner(object item) => item switch
    {
        PropTypeData type => type,
        PropPartTypeData part => data.maPropTypes.FirstOrDefault(t => t.maParts.Contains(part)),
        Volume volume => data.maPropTypes.FirstOrDefault(t => t.maCollisionVolumes.Contains(volume) || t.maParts.Any(p => p.maCollisionVolumes.Contains(volume))),
        _ => null
    };

    public override void Highlight(object selection)
    {
        base.Highlight(selection);
        PropTypeData owner = Owner(selection);

        if (owner != null && owner != framed)
            FrameSelection();

        framed = owner;
    }

    protected override void AddHighlight(object item, int depth)
    {
        switch (item)
        {
            case PropPhysics or null:
                return;
            case IList list:
                foreach (object entry in list)
                    AddHighlight(entry, depth);
                return;
            case PropTypeData type when origins.TryGetValue(type, out Vector3 origin):
                foreach (Volume volume in type.maCollisionVolumes)
                    Shape(HighlightLines, volume, origin, highlightColour);
                foreach (PropPartTypeData part in type.maParts)
                    foreach (Volume volume in part.maCollisionVolumes)
                        Shape(HighlightLines, volume, origin + At(part.mOffset), highlightColour);
                return;
            case PropPartTypeData part when Owner(part) is PropTypeData type && origins.TryGetValue(type, out Vector3 origin):
                foreach (Volume volume in part.maCollisionVolumes)
                    Shape(HighlightLines, volume, origin + At(part.mOffset), highlightColour);
                return;
            case Volume volume when Owner(volume) is PropTypeData type && origins.TryGetValue(type, out Vector3 origin):
                PropPartTypeData holder = type.maParts.FirstOrDefault(p => p.maCollisionVolumes.Contains(volume));
                Shape(HighlightLines, volume, holder == null
                    ? origin
                    : origin + At(holder.mOffset), highlightColour);
                return;
        }
    }
}
