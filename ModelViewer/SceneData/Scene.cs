using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using OpenTK.Mathematics;

namespace ModelViewer.SceneData;

public class Scene
{
    public Vector3 Center { get; private set; }
    public float HorizSize { get; private set; }
    public float VertSize { get; private set; }

    internal Dictionary<string, SceneObject> SceneObjects;
    
    public int ObjectCount => SceneObjects.Count;

    public Scene()
    {
        SceneObjects = [];

        Center = Vector3.Zero;
        HorizSize = 3.0f;
        VertSize = 1.0f;
    }

    public void ClearObjects()
    {
        SceneObjects.Clear();
    }

    public void AddObject(SceneObject obj)
    {
        while (SceneObjects.ContainsKey(obj.Name))
        {
            obj.Name += "_";
        }
        SceneObjects.Add(obj.Name, obj);

        UpdateSizing();
    }

    public void RemoveObject(string name)
    {
        SceneObjects.Remove(name);
    }

    public bool ContainsObject(string name)
    {
        return SceneObjects.ContainsKey(name);
    }

    public bool ContainsObject(SceneObject obj)
    {
        return SceneObjects.ContainsValue(obj);
    }

    public SceneObject MergeSceneObjects()
    {
        Model mergedModel = new();
        foreach (SceneObject obj in SceneObjects.Values)
        {
            mergedModel.Meshes.Add(obj.MergeMeshes());
        }

        Model resultModel = new(mergedModel.MergeMeshes());
        
        return new SceneObject("Merged", resultModel);
    }

    public List<Material> GetAllMaterials()
    {
        List<Material> result = [];

        foreach (SceneObject obj in SceneObjects.Values)
        {
            foreach (Mesh mesh in obj.Model.Meshes)
            {
                Material material = mesh.Material;
                
                if (material != null && !result.Contains(material))
                    result.Add(material);

                if (material == null && mesh.Faces != null)
                {
                    foreach (MeshFace face in mesh.Faces)
                    {
                        Material mat = face.Material;

                        if (mat != null && !result.Contains(mat))
                            result.Add(mat);
                    }
                }
            }
        }

        return result;
    }

    public void ExportCollada14(string path)
    {
        // TODO: Implement Collada Export
    }

    private void ExportMTL(string mtlPath, string materialDir, string materialDirPath, List<Material> materials)
    {
        Stream s1 = File.Open(mtlPath, FileMode.Create);
        StreamWriter sw1 = new(s1);
        for (int i = 0; i < materials.Count; i++)
        {
            Material material = materials[i];
            if (material == null)
                continue;

            string materialFileName = "material_" + material.Name + ".png";
            string materialRelativePathName = materialDir + materialFileName;
            string materialPathName = materialDirPath + materialFileName;

            Image image = material.DiffuseMap;

            float r = material.Color.R / 255.0f;
            float g = material.Color.G / 255.0f;
            float b = material.Color.B / 255.0f;

            sw1.WriteLine("newmtl material_" + material.Name);
            sw1.WriteLine("Kd " + r + " " + g + " " + b);
            if (image != null)
                sw1.WriteLine("map_Kd " + materialRelativePathName);

            sw1.WriteLine();

            if (image != null)
            {
                try
                {
                    image?.Save(materialPathName, ImageFormat.Png);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        sw1.Flush();
        sw1.Close();
        s1.Close();
    }

    public void ExportWavefrontObj(string path)
    {
        List<Material> materials = GetAllMaterials();
        string fullPath = Path.GetFullPath(path).Replace(Path.GetFileName(path), "");
        string mtlPath = fullPath + Path.GetFileNameWithoutExtension(path) + ".mtl";
        string materialDir = Path.GetFileNameWithoutExtension(path) + "_materials\\";
        string materialDirPath = fullPath + "/" + materialDir;

        if (materials.Count > 0)
        {
            bool hasMaterials = false;
            bool hasTextures = false;
            foreach (Material mat in materials)
            {
                if (mat != null)
                {
                    hasMaterials = true;
                    if (mat.DiffuseMap != null || mat.NormalMap != null || mat.SpecularMap != null)
                        hasTextures = true;
                }
            }
            if (hasMaterials)
            {
                if (hasTextures && !Directory.Exists(materialDirPath))
                    Directory.CreateDirectory(materialDirPath);

                ExportMTL(mtlPath, materialDir, materialDirPath, materials);
            }
        }

        try
        {
            Stream s = File.Open(path, FileMode.Create);
            StreamWriter sw = new(s);

            if (materials.Count > 0)
                sw.WriteLine("mtllib " + mtlPath);

            int meshIndex = 0;

            uint usedIndices = 0;
            foreach (SceneObject obj in SceneObjects.Values)
            {
                string name = obj.Name;
                sw.WriteLine("# Model " + name);

                Model model = obj.Model.Transformed(obj.Transform);
                foreach (Mesh mesh in model.Meshes)
                {
                    foreach (Vector3 vertex in mesh.Vertices)
                    {
                        sw.WriteLine("v " + vertex.X + " " + vertex.Y + " " + vertex.Z);
                    }

                    foreach (Vector2 uv1 in mesh.UV1)
                    {
                        sw.WriteLine("vt " + uv1.X + " " + -uv1.Y);
                    }

                    sw.WriteLine();

                    if (string.IsNullOrEmpty(obj.ID))
                        sw.WriteLine("g mesh" + meshIndex);
                    else
                        sw.WriteLine("g mesh" + meshIndex + "_" + obj.ID);

                    if (mesh.Material != null)
                        sw.WriteLine("usemtl material_" + mesh.Material.Name);

                    sw.WriteLine();

                    List<Vector3> verts = mesh.Vertices;

                    if (mesh.Faces != null)
                    {
                        foreach (MeshFace face in mesh.Faces)
                        {
                            if (face.Material != null)
                                sw.WriteLine("usemtl material_" + face.Material.Name);

                            StringBuilder sb = new("f");
                            foreach (uint index in face.Indices)
                            {
                                uint ind = index + 1 + usedIndices;
                                sb.Append(" " + ind + "/" + ind);
                            }
                            sw.WriteLine(sb.ToString());
                        }
                    }
                    else
                    {
                        List<uint> inds = mesh.Indices;

                        if (inds.Count == 0)
                        {
                            for (int i = 0; i < verts.Count; i++)
                            {
                                inds.Add((uint) i);
                            }
                        }

                        //Dictionary<uint, Material> usedMats = new Dictionary<uint, Material>();

                        for (int j = 0; j < inds.Count; j += 3)
                        {
                            if (j + 2 >= inds.Count)
                                break;

                            /*if (mesh.Materials != null && mesh.Materials.ContainsKey(inds[j]))
                            {
                                Material mat = mesh.Materials[inds[j]];
                                if (mat != null)
                                    sw.WriteLine("usemtl material_" + mat.Name);
                            }*/

                            uint v0 = inds[j + 0] + 1 + usedIndices;
                            uint v1 = inds[j + 1] + 1 + usedIndices;
                            uint v2 = inds[j + 2] + 1 + usedIndices;
                            sw.WriteLine("f " + v0 + "/" + v0 + " " + v1 + "/" + v1 + " " + v2 + "/" + v2);
                        }
                    }
                    usedIndices += (uint)verts.Count;

                    sw.WriteLine();

                    meshIndex++;
                }
            }

            sw.Flush();
            sw.Close();
            s.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void UpdateSizing()
    {
        float maxX = float.MinValue;
        float minX = float.MaxValue;
        float maxY = float.MinValue;
        float minY = float.MaxValue;
        float maxZ = float.MinValue;
        float minZ = float.MaxValue;

        //Vector3 closestToCenter = new Vector3(float.NaN);

        //int numVerts = 0;

        foreach (SceneObject obj in SceneObjects.Values)
        {
            foreach (Mesh mesh in obj.Model.Meshes)
            {
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    Vector3 vert = mesh.Vertices[i];

                    maxX = Math.Max(vert.X, maxX);
                    minX = Math.Min(vert.X, minX);
                    maxY = Math.Max(vert.Y, maxY);
                    minY = Math.Min(vert.Y, minY);
                    maxZ = Math.Max(vert.Z, maxZ);
                    minZ = Math.Min(vert.Z, minZ);

                    //numVerts++;
                }
            }
        }

        float midX = (minX + maxX) / 2;
        float midY = (minY + maxY) / 2;
        float midZ = (minZ + maxZ) / 2;

        HorizSize = Math.Max(maxX - minX, maxZ - minZ) / 2 * 1.5f;
        VertSize = (maxY - minY) / 2 * 1.5f;

        Center = new Vector3(midX, midY, midZ);
    }

    public bool InitGraphics()
    {
        foreach (SceneObject obj in SceneObjects.Values)
        {
            if (!obj.InitGraphics())
                return false;
        }

        return true;
    }

    public void Update()
    {
        foreach (SceneObject obj in SceneObjects.Values)
            obj.Update();
    }

    public void Render(ICamera camera)
    {
        foreach (SceneObject obj in SceneObjects.Values)
            obj.Render(camera);
    }

    public void Dispose()
    {
        foreach (SceneObject obj in SceneObjects.Values)
            obj.Dispose();
    }
}
