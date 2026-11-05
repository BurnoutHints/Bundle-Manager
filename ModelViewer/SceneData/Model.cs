using System.Collections.Generic;
using OpenTK.Mathematics;

namespace ModelViewer.SceneData;

public class Model
{
    internal List<Mesh> meshes;

    public int MeshCount => meshes.Count;

    public Model(params Mesh[] meshes)
    {
        meshes = [.. meshes];
    }

    public Model Copy()
    {
        Model result = new();

        foreach (Mesh mesh in meshes)
        {
            result.meshes.Add(mesh.Copy());
        }

        return result;
    }

    public void AddMesh(Mesh mesh)
    {
        meshes.Add(mesh);
    }

    public void RemoveMesh(int index)
    {
        meshes.RemoveAt(index);
    }

    public void RemoveMesh(Mesh mesh)
    {
        meshes.Remove(mesh);
    }

    public bool ContainsMesh(Mesh mesh)
    {
        return meshes.Contains(mesh);
    }

    public Model Transformed(Matrix4 transform)
    {
        Model result = new();

        foreach (Mesh mesh in meshes)
        {
            result.meshes.Add(mesh.Transformed(transform));
        }

        return result;
    }

    public Mesh MergeMeshes()
    {
        Mesh result = new();

        foreach (Mesh mesh in meshes)
        {
            result = result.Merged(mesh);
        }

        return result;
    }

    public bool InitGraphics()
    {
        foreach (Mesh mesh in meshes)
            mesh.InitGraphics();

        return true;
    }

    public void Update()
    {
        foreach (Mesh mesh in meshes)
            mesh.Update();
    }

    public void Render(ICamera camera, Matrix4 transform)
    {
        foreach (Mesh mesh in meshes)
            mesh.Render(camera, transform);
    }

    public void Dispose()
    {
        foreach (Mesh mesh in meshes)
            mesh.Dispose();
    }
}
