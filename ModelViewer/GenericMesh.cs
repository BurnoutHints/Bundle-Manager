using System.Collections.Generic;
using ModelViewer.SceneData;
using OpenTK.Mathematics;

namespace ModelViewer;

public class Face
{
    public List<uint> Indices;
    public List<uint> UVIndices;
    public Material Material;

    public Face(Material material = null)
    {
        Indices = [];
        UVIndices = [];
        Material = material;
    }

    public Face(uint[] indices, uint[] uvIndices = null, Material material = null) : this(material)
    {
        Indices.AddRange(indices);
        if (uvIndices != null)
            UVIndices.AddRange(uvIndices);
    }
}

public class GenericMesh
{
    public string Name;
    public Dictionary<uint, Vector3> Vertices;
    public Dictionary<uint, Vector2> UVs;
    public List<Face> Faces;

    public GenericMesh()
    {
        Name = "";
        Vertices = [];
        UVs = [];
        Faces = [];
    }

    public GenericMesh(string name) : this()
    {
        Name = name;
    }
}
