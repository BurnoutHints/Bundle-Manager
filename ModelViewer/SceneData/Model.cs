using System.Collections.Generic;
using OpenTK.Mathematics;

namespace ModelViewer.SceneData;

public class Model
{
    internal List<Mesh> Meshes;

    public int MeshCount => Meshes.Count;

    public Model(params Mesh[] meshes)
    {
        Meshes = [.. meshes];
    }

    public Model Copy()
    {
        Model result = new();

        foreach (Mesh mesh in Meshes)
        {
            result.Meshes.Add(mesh.Copy());
        }

        return result;
    }

    public void AddMesh(Mesh mesh)
    {
        Meshes.Add(mesh);
    }

    public void RemoveMesh(int index)
    {
        Meshes.RemoveAt(index);
    }

    public void RemoveMesh(Mesh mesh)
    {
        Meshes.Remove(mesh);
    }

    public bool ContainsMesh(Mesh mesh)
    {
        return Meshes.Contains(mesh);
    }

    public Model Transformed(Matrix4 transform)
    {
        Model result = new();

        foreach (Mesh mesh in Meshes)
        {
            result.Meshes.Add(mesh.Transformed(transform));
        }

        return result;
    }

    public Mesh MergeMeshes()
    {
        Mesh result = new();
        
        foreach (Mesh mesh in Meshes)
        {
            result = result.Merged(mesh);
        }

        return result;
    }

    public bool InitGraphics()
    {
        foreach (Mesh mesh in Meshes)
            mesh.InitGraphics();
        
        return true;
    }

    public void Update()
    {
        foreach (Mesh mesh in Meshes)
            mesh.Update();
    }

    public void Render(ICamera camera, Matrix4 transform)
    {
        foreach (Mesh mesh in Meshes)
            mesh.Render(camera, transform);
    }

    public void Dispose()
    {
        foreach (Mesh mesh in Meshes)
            mesh.Dispose();
    }
}
