using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using BundleFormat;
using BundleUtilities;
using ModelViewer.SceneData;
using OpenTK.Mathematics;
using WorldCollisionHandler;

namespace BaseHandlers;

public sealed class TrafficWorldGeometry
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Vertex(Vector3 position, uint colour)
    {
        public readonly Vector3 Position = position;
        public readonly uint Colour = colour;
    }

    private static readonly Vector3 lightDirection = new Vector3(0.3f, 1, 0.4f).Normalized();

    public string SourcePath { get; }

    public Vertex[] Vertices { get; }

    public int TriangleCount => Vertices.Length / 3;

    private TrafficWorldGeometry(string path, Vertex[] vertices)
    {
        SourcePath = path;
        Vertices = vertices;
    }

    public static TrafficWorldGeometry Load(string path, ILoader loader = null, CancellationToken cancel = default)
    {
        cancel.ThrowIfCancellationRequested();
        loader?.SetStatus("Reading world collision bundle…");
        BundleArchive archive = BundleArchive.Open(path);
        return FromArchive(archive, path, loader, cancel);
    }

    public static TrafficWorldGeometry FromArchive(BundleArchive archive, string path, ILoader loader = null, CancellationToken cancel = default)
    {
        cancel.ThrowIfCancellationRequested();
        BundleEntry[] entries = [.. archive.Entries.Where(entry => entry.Type is EntryType.PolygonSoupList or EntryType.CollisionMeshData)];

        if (entries.Length == 0)
            throw new InvalidDataException("This bundle contains no supported world collision resources. Choose worldcol.bin for this map.");

        List<Vertex> vertices = [];

        for (int index = 0; index < entries.Length; index++)
        {
            cancel.ThrowIfCancellationRequested();
            loader?.SetStatus($"Building world geometry ({index + 1} of {entries.Length})…");
            BundleEntry entry = entries[index];

            if (entry.Type == EntryType.PolygonSoupList)
            {
                PolygonSoupList polygons = new();
                polygons.Read(entry);

                foreach (PolygonSoupChunk chunk in polygons.Chunks)
                {
                    cancel.ThrowIfCancellationRequested();
                    Vector3[] points = [.. chunk.PointList.Select(point => new Vector3(
                        ((float)chunk.Position.X + point.X) * chunk.Scale,
                        ((float)chunk.Position.Y + point.Y) * chunk.Scale,
                        ((float)chunk.Position.Z + point.Z) * chunk.Scale))];

                    foreach (PolygonSoupProperty polygon in chunk.PropertyList)
                    {
                        AddTriangle(vertices, points, polygon.Indices[0], polygon.Indices[1], polygon.Indices[2]);

                        if (polygon.Indices[3] != 0xFF)
                            AddTriangle(vertices, points, polygon.Indices[3], polygon.Indices[2], polygon.Indices[1]);
                    }
                }
            }
            else
            {
                ClusteredMesh mesh = new();
                mesh.Read(entry);

                foreach (Mesh cluster in mesh.Clusters)
                {
                    cancel.ThrowIfCancellationRequested();

                    foreach (MeshFace face in cluster.Faces)
                        for (int triangle = 1; triangle < face.Indices.Count - 1; triangle++)
                            AddTriangle(vertices, cluster.Vertices, face.Indices[0], face.Indices[triangle], face.Indices[triangle + 1]);
                }
            }

            loader?.SetProgress((index + 1) * 100 / entries.Length);
        }

        cancel.ThrowIfCancellationRequested();

        if (vertices.Count == 0)
            throw new InvalidDataException("This bundle contains no visible collision surfaces.");

        return new(path, [.. vertices]);
    }

    private static void AddTriangle(List<Vertex> vertices, IReadOnlyList<Vector3> points, uint first, uint second, uint third)
    {
        if (first >= points.Count || second >= points.Count || third >= points.Count)
            throw new InvalidDataException("A collision surface refers to a missing vertex.");

        Vector3 a = points[(int)first], b = points[(int)second], c = points[(int)third];

        if (!Finite(a) || !Finite(b) || !Finite(c))
            throw new InvalidDataException("A collision surface contains invalid coordinates.");

        Vector3 normal = Vector3.Cross(b - a, c - a);

        if (!Finite(normal))
            throw new InvalidDataException("A collision surface is outside the supported coordinate range.");

        if (normal.LengthSquared < 0.00000001f)
            return;

        int shade = (int)(55 + 45 * Math.Abs(Vector3.Dot(normal.Normalized(), lightDirection)));
        uint colour = (uint)(shade | shade << 8 | shade << 16 | 255 << 24);
        vertices.Add(new(a, colour));
        vertices.Add(new(b, colour));
        vertices.Add(new(c, colour));
    }

    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
