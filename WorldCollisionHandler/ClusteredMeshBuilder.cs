using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace WorldCollisionHandler;

public readonly record struct CollisionTriangle(Vector3 A, Vector3 B, Vector3 C, ushort GroupTag, ushort MaterialTag);

// Builds CgsPhysics::CollisionMeshData (aggregate volume over clustered mesh).
// https://burnout.wiki/wiki/Clustered_Mesh/Development
public static class ClusteredMeshBuilder
{
    private const float Granularity = 0.01f;
    private const int MaxClusterVertices = 255, MaxLeafUnits = 16, MaxTreelessUnits = 8, MaxUnitData = 0xF000;
    private const int MeshAt = 0x80, HeaderSize = 0x60, TreeHeaderSize = 0x30, BranchSize = 0x20;
    private const byte TypeTriangle = 1, TypeQuad = 2, FlagEdges = 0x20, FlagGroup = 0x40, FlagSurface = 0x80;
    private const byte EdgeUnmatched = 0x80, EdgeConvex = 0x20, EdgeAngleZero = 26;
    private const uint Branch = 0xFFFFFFFF;
    private const double MaxCreaseForQuad = 0.05;

    private readonly record struct Point(int X, int Y, int Z)
    {
        public Vector3 World => new(X * Granularity, Y * Granularity, Z * Granularity);
    }

    private sealed class Unit
    {
        public Point[] Points;
        public ushort Group, Material;
        public byte[] Edges;
        public Vector3 Low, High, Centre;
        public int Cluster, Offset;
    }

    private sealed class Node
    {
        public int Parent, Axis;
        public readonly uint[] Content = new uint[2], Index = new uint[2];
        public readonly int[] Leaf = new int[2];
        public readonly float[] Extents = new float[2];
    }

    private static Point Snap(Vector3 v) => new((int)Math.Round(v.X / (double)Granularity), (int)Math.Round(v.Y / (double)Granularity), (int)Math.Round(v.Z / (double)Granularity));

    private static Vector3 Normal(Point a, Point b, Point c) => Vector3.Normalize(Vector3.Cross(b.World - a.World, c.World - a.World));

    private static List<Unit> MakeUnits(IReadOnlyList<CollisionTriangle> triangles)
    {
        List<(Point[] P, ushort G, ushort M)> faces = [];

        foreach (CollisionTriangle t in triangles)
        {
            Point a = Snap(t.A), b = Snap(t.B), c = Snap(t.C);

            if (a == b || b == c || a == c || Vector3.Cross(b.World - a.World, c.World - a.World).LengthSquared() < 1e-10f)
                continue;

            faces.Add(([a, b, c], t.GroupTag, t.MaterialTag));
        }

        Dictionary<(Point, Point), int> edges = [];

        for (int i = 0; i < faces.Count; i++)
            for (int e = 0; e < 3; e++)
                edges[(faces[i].P[e], faces[i].P[(e + 1) % 3])] = i;

        bool[] used = new bool[faces.Count];
        List<Unit> units = [];

        for (int i = 0; i < faces.Count; i++)
        {
            if (used[i])
                continue;

            used[i] = true;
            Point[] p = faces[i].P;
            Point[] points = p;

            for (int r = 0; r < 3 && points == p; r++)
            {
                Point a0 = p[r], a1 = p[(r + 1) % 3], a2 = p[(r + 2) % 3];

                if (!edges.TryGetValue((a2, a1), out int j) || used[j] || faces[j].G != faces[i].G || faces[j].M != faces[i].M)
                    continue;

                Point d = faces[j].P.First(q => q != a1 && q != a2);

                if (d == a0 || 1 - Vector3.Dot(Normal(a0, a1, a2), Normal(d, a2, a1)) > MaxCreaseForQuad)
                    continue;

                used[j] = true;
                points = [a0, a1, a2, d];
            }

            units.Add(new()
            {
                Points = points,
                Group = faces[i].G,
                Material = faces[i].M
            });
        }

        foreach (Unit u in units)
        {
            u.Low = new(float.MaxValue);
            u.High = new(float.MinValue);

            foreach (Point q in u.Points)
            {
                u.Low = Vector3.Min(u.Low, q.World);
                u.High = Vector3.Max(u.High, q.World);
            }

            u.Centre = (u.Low + u.High) / 2;
        }

        return units;
    }

    // (From, To, Face) edge layout; quad edges ordered 01, 13, 20, 32
    private static (Point From, Point To, int Face)[] StoredEdges(Point[] p) => p.Length == 3
        ? [(p[0], p[1], 0), (p[1], p[2], 0), (p[2], p[0], 0)]
        : [(p[0], p[1], 0), (p[1], p[3], 1), (p[2], p[0], 0), (p[3], p[2], 1)];

    private static (Point A, Point B, Point C) Face(Point[] p, int face) => face == 0
        ? (p[0], p[1], p[2])
        : (p[3], p[2], p[1]);

    private static (Point, Point) EdgeKey(Point a, Point b) => (a.X, a.Y, a.Z).CompareTo((b.X, b.Y, b.Z)) < 0
        ? (a, b)
        : (b, a);

    private static byte AngleBits(double cosine)
    {
        double gap = 1 - cosine;

        if (gap < 1e-6)
            return EdgeAngleZero;

        if (cosine < -0.9)
            return 0;

        return (byte)Math.Clamp((int)Math.Round(-Math.Log2(Math.Min(gap, 1)) + 1.75), 1, 25);
    }

    private static void ComputeEdges(List<Unit> units)
    {
        Dictionary<(Point, Point), List<(Unit Owner, int Face, Point Opposite)>> byEdge = [];

        foreach (Unit u in units)
        {
            foreach ((Point from, Point to, int face) in StoredEdges(u.Points))
            {
                (Point a, Point b, Point c) = Face(u.Points, face);
                Point opposite = a != from && a != to
                    ? a
                    : b != from && b != to
                        ? b
                        : c;

                if (!byEdge.TryGetValue(EdgeKey(from, to), out List<(Unit Owner, int Face, Point Opposite)> list))
                    byEdge[EdgeKey(from, to)] = list = [];

                list.Add((u, face, opposite));
            }
        }

        foreach (Unit u in units)
        {
            (Point From, Point To, int Face)[] stored = StoredEdges(u.Points);
            u.Edges = new byte[stored.Length];

            for (int e = 0; e < stored.Length; e++)
            {
                (Point from, Point to, int face) = stored[e];
                (Point a, Point b, Point c) = Face(u.Points, face);
                Vector3 normal = Normal(a, b, c);
                (Unit Owner, int Face, Point Opposite) other = byEdge[EdgeKey(from, to)].FirstOrDefault(n => n.Owner != u);

                if (other.Owner == null)
                {
                    u.Edges[e] = EdgeUnmatched | EdgeConvex;
                    continue;
                }

                (Point oa, Point ob, Point oc) = Face(other.Owner.Points, other.Face);
                double cosine = Vector3.Dot(normal, Normal(oa, ob, oc));
                bool convex = Vector3.Dot(other.Opposite.World - from.World, normal) < 0;
                u.Edges[e] = (byte)(AngleBits(cosine) | (convex
                    ? EdgeConvex
                    : 0));
            }
        }
    }

    private static int BuildTree(List<Unit> units, int[] order, int start, int count, int parent, List<Node> nodes, List<(int Node, int Slot, int Start, int Count)> leaves)
    {
        int at = nodes.Count;
        Node node = new()
        {
            Parent = parent
        };

        nodes.Add(node);

        Vector3 low = new(float.MaxValue), high = new(float.MinValue);

        for (int i = start; i < start + count; i++)
        {
            low = Vector3.Min(low, units[order[i]].Centre);
            high = Vector3.Max(high, units[order[i]].Centre);
        }

        Vector3 size = high - low;
        node.Axis = size.X >= size.Y && size.X >= size.Z
            ? 0
            : size.Y >= size.Z
                ? 1
                : 2;

        static float along(Vector3 v, int axis) => axis == 0
            ? v.X
            : axis == 1
                ? v.Y
                : v.Z;

        Array.Sort(order, start, count, Comparer<int>.Create((a, b) => along(units[a].Centre, node.Axis).CompareTo(along(units[b].Centre, node.Axis))));
        int half = count / 2;
        node.Extents[0] = float.MinValue;
        node.Extents[1] = float.MaxValue;

        for (int i = start; i < start + half; i++)
            node.Extents[0] = MathF.Max(node.Extents[0], along(units[order[i]].High, node.Axis));

        for (int i = start + half; i < start + count; i++)
            node.Extents[1] = MathF.Min(node.Extents[1], along(units[order[i]].Low, node.Axis));

        for (int slot = 0; slot < 2; slot++)
        {
            int childStart = slot == 0
                ? start
                : start + half;
            int childCount = slot == 0
                ? half
                : count - half;

            if (childCount <= MaxLeafUnits)
            {
                node.Leaf[slot] = 1;
                leaves.Add((at, slot, childStart, childCount));
                continue;
            }

            node.Content[slot] = Branch;
            node.Index[slot] = (uint)BuildTree(units, order, childStart, childCount, at, nodes, leaves);
        }

        return at;
    }

    private static int UnitSize(Unit u) => 1 + u.Points.Length * 2 + 4;

    private static void Put(List<byte> to, uint value)
    {
        to.Add((byte)(value >> 24));
        to.Add((byte)(value >> 16));
        to.Add((byte)(value >> 8));
        to.Add((byte)value);
    }

    private static void Put(List<byte> to, float value) => Put(to, BitConverter.SingleToUInt32Bits(value));

    private static void Put(List<byte> to, ushort value)
    {
        to.Add((byte)(value >> 8));
        to.Add((byte)value);
    }

    private static void Put(List<byte> to, Vector3 v, float w = 0)
    {
        Put(to, v.X);
        Put(to, v.Y);
        Put(to, v.Z);
        Put(to, w);
    }

    private static void Pad(List<byte> to, int alignment)
    {
        while (to.Count % alignment != 0)
            to.Add(0);
    }

    // Serializes mesh to binary collision resource, or returns null if empty
    public static byte[] Build(IReadOnlyList<CollisionTriangle> triangles)
    {
        List<Unit> units = MakeUnits(triangles);

        if (units.Count == 0)
            return null;

        ComputeEdges(units);

        int[] order = [.. Enumerable.Range(0, units.Count)];
        List<Node> nodes = [];
        List<(int Node, int Slot, int Start, int Count)> leaves = [];

        if (units.Count > MaxTreelessUnits)
            BuildTree(units, order, 0, units.Count, 0, nodes, leaves);

        // Group units by tree traversal order to maintain leaf contiguity
        List<(int First, int Count, List<Point> Points, Dictionary<Point, int> Index)> clusters = [];
        (int First, int Count, List<Point> Points, Dictionary<Point, int> Index) current = (0, 0, [], []);
        int dataSize = 0;

        for (int i = 0; i < order.Length; i++)
        {
            Unit u = units[order[i]];
            int added = u.Points.Distinct().Count(q => !current.Index.ContainsKey(q));

            if (current.Points.Count + added > MaxClusterVertices || dataSize + UnitSize(u) > MaxUnitData)
            {
                clusters.Add(current);
                current = (i, 0, [], []);
                dataSize = 0;
            }

            foreach (Point q in u.Points)
            {
                if (current.Index.ContainsKey(q))
                    continue;

                current.Index[q] = current.Points.Count;
                current.Points.Add(q);
            }

            u.Cluster = clusters.Count;
            u.Offset = dataSize;
            dataSize += UnitSize(u);
            current.Count++;
        }

        clusters.Add(current);

        foreach ((int node, int slot, int start, int count) in leaves)
        {
            Unit first = units[order[start]];
            nodes[node].Content[slot] = (uint)count;
            nodes[node].Index[slot] = (uint)(first.Cluster << 16 | first.Offset);
        }

        Vector3 low = new(float.MaxValue), high = new(float.MinValue);

        foreach (Unit u in units)
        {
            low = Vector3.Min(low, u.Low);
            high = Vector3.Max(high, u.High);
        }

        Vector3 centre = (low + high) / 2;
        float radius = units.SelectMany(u => u.Points).Max(q => Vector3.Distance(q.World, centre));
        int tagBits = (int)Math.Ceiling(Math.Log2(units.Count + 1));

        int treeAt = HeaderSize, branchesAt = TreeHeaderSize, tableAt = treeAt + TreeHeaderSize + nodes.Count * BranchSize;
        List<byte> body = [];
        List<uint> clusterAt = [];

        for (int c = 0; c < clusters.Count; c++)
        {
            (int first, int count, List<Point> points, _) = clusters[c];

            while ((tableAt + clusters.Count * 4 + body.Count) % 16 != 0)
                body.Add(0);

            clusterAt.Add((uint)(tableAt + clusters.Count * 4 + body.Count));

            int minX = points.Min(q => q.X), minY = points.Min(q => q.Y), minZ = points.Min(q => q.Z);
            bool compressed = points.Max(q => q.X) - minX <= ushort.MaxValue && points.Max(q => q.Y) - minY <= ushort.MaxValue && points.Max(q => q.Z) - minZ <= ushort.MaxValue;
            int vertexBytes = compressed
                ? 12 + points.Count * 6
                : points.Count * 12;
            int unitStart = (vertexBytes + 15) / 16;
            int unitBytes = Enumerable.Range(first, count).Sum(i => UnitSize(units[order[i]]));

            Put(body, (ushort)count);
            Put(body, (ushort)unitBytes);
            Put(body, (ushort)unitStart);
            Put(body, (ushort)unitStart);
            body.Add((byte)points.Count);
            body.Add(0);
            body.Add((byte)(compressed
                ? 1
                : 2));
            body.AddRange(new byte[5]);
            int vertexStart = body.Count;

            if (compressed)
            {
                Put(body, (uint)minX);
                Put(body, (uint)minY);
                Put(body, (uint)minZ);

                foreach (Point q in points)
                {
                    Put(body, (ushort)(q.X - minX));
                    Put(body, (ushort)(q.Y - minY));
                    Put(body, (ushort)(q.Z - minZ));
                }
            }
            else
            {
                foreach (Point q in points)
                {
                    Put(body, (uint)q.X);
                    Put(body, (uint)q.Y);
                    Put(body, (uint)q.Z);
                }
            }

            while (body.Count - vertexStart < unitStart * 16)
                body.Add(0);

            Dictionary<Point, int> index = clusters[c].Index;

            for (int i = first; i < first + count; i++)
            {
                Unit u = units[order[i]];
                body.Add((byte)((u.Points.Length == 4
                    ? TypeQuad
                    : TypeTriangle) | FlagEdges | FlagGroup | FlagSurface));

                foreach (Point q in u.Points)
                    body.Add((byte)index[q]);

                body.AddRange(u.Edges);
                body.Add((byte)u.Group);
                body.Add((byte)(u.Group >> 8));
                body.Add((byte)u.Material);
                body.Add((byte)(u.Material >> 8));
            }

            while ((tableAt + clusters.Count * 4 + body.Count) % 16 != 0)
                body.Add(0);
        }

        uint size = (uint)(tableAt + clusters.Count * 4 + body.Count);
        List<byte> o = [];
        Put(o, (uint)0x20);
        Put(o, 0x460F917Cu);
        Put(o, 0x5A030000u);
        Put(o, 0x30951400u);
        Put(o, centre, radius);

        // Aggregate volume with identity transform
        Put(o, Vector3.UnitX);
        Put(o, Vector3.UnitY);
        Put(o, Vector3.UnitZ);
        Put(o, Vector3.Zero);
        Put(o, 6u);
        Put(o, (uint)MeshAt);
        o.AddRange(new byte[20]);
        Put(o, 1u);

        Put(o, low);
        Put(o, high);
        Put(o, 0x5CF9AB00u);
        Put(o, (uint)tagBits);
        Put(o, (uint)units.Count);
        Put(o, 0u);
        Put(o, (uint)treeAt);
        Put(o, (uint)tableAt);
        Put(o, (uint)clusters.Count);
        Put(o, (uint)clusters.Count);
        Put(o, (uint)units.Count);
        Put(o, (uint)units.Count);
        Put(o, Granularity);
        Put(o, size);
        Put(o, (ushort)0x10);
        Put(o, (ushort)0);
        Put(o, (ushort)0);
        o.Add(0x80);
        o.Add(2);
        o.Add(2);
        o.AddRange(new byte[7]);

        Put(o, (uint)branchesAt);
        Put(o, (uint)nodes.Count);
        Put(o, (uint)units.Count);
        Put(o, 0u);
        Put(o, low);
        Put(o, high);

        foreach (Node n in nodes)
        {
            Put(o, (uint)n.Parent);
            Put(o, (uint)n.Axis);

            for (int slot = 0; slot < 2; slot++)
            {
                Put(o, n.Content[slot]);
                Put(o, n.Index[slot]);
            }

            Put(o, n.Extents[0]);
            Put(o, n.Extents[1]);
        }

        foreach (uint at in clusterAt)
            Put(o, at);

        o.AddRange(body);
        return [.. o];
    }

    // Validates binary mesh structure; returns error string or null if valid
    public static string Validate(byte[] p)
    {
        uint word(int at) => BinaryPrimitives.ReadUInt32BigEndian(p.AsSpan(at));

        ushort half(int at) => BinaryPrimitives.ReadUInt16BigEndian(p.AsSpan(at));

        float real(int at) => BinaryPrimitives.ReadSingleBigEndian(p.AsSpan(at));

        int whole(int at) => BinaryPrimitives.ReadInt32BigEndian(p.AsSpan(at));

        try
        {
            int volume = (int)word(0), mesh = (int)word(volume + 0x44);

            if (mesh + word(mesh + 0x4C) != p.Length)
                return "The size of the mesh doesn't match the resource.";

            int table = (int)word(mesh + 0x34), clusters = (int)word(mesh + 0x38), units = (int)word(mesh + 0x40), tree = mesh + (int)word(mesh + 0x30);
            float granularity = real(mesh + 0x48);
            int groupSize = p[mesh + 0x57], surfaceSize = p[mesh + 0x58];
            int branches = (int)word(tree + 4), entries = (int)word(tree + 8);

            if (entries != units)
                return $"The tree holds {entries} entries for {units} units.";

            int[] clusterAt = new int[clusters], dataAt = new int[clusters], dataSize = new int[clusters];
            Vector3[][] vertices = new Vector3[clusters][];

            for (int c = 0; c < clusters; c++)
            {
                int at = mesh + (int)word(mesh + table + c * 4);

                if (at % 16 != 0 || at + 16 > p.Length)
                    return $"Cluster {c} is misplaced.";

                int count = p[at + 8], mode = p[at + 10], start = half(at + 4);
                Vector3[] v = new Vector3[count];

                for (int i = 0; i < count; i++)
                {
                    v[i] = mode switch
                    {
                        1 => new(((whole(at + 16) + half(at + 28 + i * 6)) * granularity), ((whole(at + 20) + half(at + 30 + i * 6)) * granularity), ((whole(at + 24) + half(at + 32 + i * 6)) * granularity)),
                        2 => new(whole(at + 16 + i * 12) * granularity, whole(at + 20 + i * 12) * granularity, whole(at + 24 + i * 12) * granularity),
                        _ => new(real(at + 16 + i * 16), real(at + 20 + i * 16), real(at + 24 + i * 16))
                    };
                }

                clusterAt[c] = at;
                dataAt[c] = at + 16 * (start + 1);
                dataSize[c] = half(at + 2);
                vertices[c] = v;

                if (dataAt[c] + dataSize[c] > p.Length)
                    return $"The units of cluster {c} run past the resource.";
            }

            int total = 0;

            bool visit(uint index, uint content, Vector3 low, Vector3 high, ref string error, int depth)
            {
                if (depth > 96)
                {
                    error = "The tree is too deep.";
                    return false;
                }

                if (content == Branch)
                {
                    if (index >= branches)
                    {
                        error = $"Branch index {index} is out of range.";
                        return false;
                    }

                    int n = tree + (int)word(tree) + (int)index * BranchSize;
                    int axis = (int)word(n + 4);
                    float[] extents = [real(n + 24), real(n + 28)];

                    for (int slot = 0; slot < 2; slot++)
                    {
                        Vector3 childLow = low, childHigh = high;

                        if (slot == 0)
                            childHigh = Set(childHigh, axis, MathF.Min(Get(childHigh, axis), extents[0]));
                        else
                            childLow = Set(childLow, axis, MathF.Max(Get(childLow, axis), extents[1]));

                        if (!visit(word(n + 12 + slot * 8), word(n + 8 + slot * 8), childLow, childHigh, ref error, depth + 1))
                            return false;
                    }

                    return true;
                }

                int cluster = (int)(index >> 16), offset = (int)(index & 0xFFFF);

                for (uint u = 0; u < content; u++)
                {
                    if (cluster >= clusters || offset >= dataSize[cluster])
                    {
                        error = $"Leaf units run past cluster {cluster}.";
                        return false;
                    }

                    int at = dataAt[cluster] + offset, type = p[at] & 15, flags = p[at] >> 4;
                    int tris = type == 2
                        ? 2
                        : 1;

                    for (int k = 0; k < tris + 2; k++)
                    {
                        int vertex = p[at + 1 + k];

                        if (vertex >= vertices[cluster].Length)
                        {
                            error = $"A unit in cluster {cluster} uses vertex {vertex}.";
                            return false;
                        }

                        Vector3 point = vertices[cluster][vertex];

                        for (int axis = 0; axis < 3; axis++)
                        {
                            if (Get(point, axis) < Get(low, axis) - 0.02f && low.X > float.MinValue)
                            {
                                error = "A unit lies outside its branch.";
                                return false;
                            }

                            if (Get(point, axis) > Get(high, axis) + 0.02f)
                            {
                                error = "A unit lies outside its branch.";
                                return false;
                            }
                        }
                    }

                    offset += 1 + (tris + 2) + ((flags & 2) != 0
                        ? tris + 2
                        : 0) + ((flags & 4) != 0
                        ? groupSize
                        : 0) + ((flags & 8) != 0
                        ? surfaceSize
                        : 0);
                    total++;

                    if (offset >= dataSize[cluster] && u + 1 < content)
                    {
                        cluster++;
                        offset = 0;
                    }
                }

                return true;
            }

            string problem = null;
            Vector3 lowest = new(float.MinValue), highest = new(float.MaxValue);

            if (branches > 0)
                visit(0, Branch, lowest, highest, ref problem, 0);
            else
                visit(0, (uint)entries, lowest, highest, ref problem, 0);

            if (problem != null)
                return problem;

            return total == units
                ? null
                : $"The tree reaches {total} of {units} units.";
        }
        catch (ArgumentOutOfRangeException)
        {
            return "A pointer runs past the resource.";
        }
    }

    private static float Get(Vector3 v, int axis) => axis == 0
        ? v.X
        : axis == 1
            ? v.Y
            : v.Z;

    private static Vector3 Set(Vector3 v, int axis, float value)
    {
        if (axis == 0)
            v.X = value;
        else if (axis == 1)
            v.Y = value;
        else
            v.Z = value;

        return v;
    }
}
