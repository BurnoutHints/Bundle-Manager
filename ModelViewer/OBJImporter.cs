using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using BundleUtilities;
using ModelViewer.SceneData;
using OpenTK.Mathematics;

namespace ModelViewer;

public static class OBJImporter
{
    public static GenericModel ImportOBJ(string path)
    {
        Stream s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        GenericModel result = ImportOBJ(s);
        s.Close();

        return result;
    }

    public static GenericModel ImportOBJ(Stream s)
    {
        GenericModel result = new();

        MemoryStream ms = new();
        s.CopyTo(ms);

        ms.Position = 0;

        StreamReader sr = new(ms);

        try
        {
            List<Vector3> globalVertices = [];
            List<Vector2> globalUVs = [];

            int currentMesh = -1;
            uint startIndex = 0;
            uint startUVIndex = 0;
            Material lastMaterial = null;

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                if (string.IsNullOrEmpty(line))
                    continue;

                line = line.Trim();

                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("v "))
                {
                    string[] split = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length < 4)
                        throw new ReadFailedError("Invalid Vertex: " + line);

                    Utilities.Parse(split[1], false, out float x);
                    Utilities.Parse(split[2], false, out float y);
                    Utilities.Parse(split[3], false, out float z);
                    
                    // Add to global vertices as we might not have a mesh yet.
                    globalVertices.Add(new Vector3(x, y, z));
                }
                else if (line.StartsWith("vt "))
                {
                    string[] split = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length < 3)
                        throw new ReadFailedError("Invalid UV: " + line);

                    Utilities.Parse(split[1], false, out float u);
                    Utilities.Parse(split[2], false, out float v);

                    // Add to global uvs as we might not have a mesh yet.
                    globalUVs.Add(new Vector2(u, v));
                }
                else if (line.StartsWith("f "))
                {
                    string[] split = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length < 4)
                        throw new ReadFailedError("Invalid Face: " + line);

                    string[] i1 = split[1].Split(['/'], StringSplitOptions.RemoveEmptyEntries);
                    string[] i2 = split[2].Split(['/'], StringSplitOptions.RemoveEmptyEntries);
                    string[] i3 = split[3].Split(['/'], StringSplitOptions.RemoveEmptyEntries);

                    Utilities.Parse(i1[0], false, out uint v1);
                    Utilities.Parse(i2[0], false, out uint v2);
                    Utilities.Parse(i3[0], false, out uint v3);

                    bool isQuad = false;
                    uint v4 = 0;
                    uint uv4 = 1;
                    if (split.Length > 5)
                    {
                        isQuad = true;
                        string[] i4 = split[5].Split(['/'], StringSplitOptions.RemoveEmptyEntries);
                        Utilities.Parse(i4[0], false, out v4);

                        if (i4.Length > 1)
                            Utilities.Parse(i4[1], false, out uv4);
                    }

                    uint uv1 = 1;
                    uint uv2 = 1;
                    uint uv3 = 1;

                    if (i1.Length > 1 && i2.Length > 1 && i3.Length > 1)
                    {
                        Utilities.Parse(i1[1], false, out uv1);
                        Utilities.Parse(i2[1], false, out uv2);
                        Utilities.Parse(i3[1], false, out uv3);
                    }

                    // OBJ is 1-indexed, so convert it to be 0-indexed
                    v1--;
                    v2--;
                    v3--;
                    if (isQuad)
                        v4--;
                    uv1--;
                    uv2--;
                    uv3--;
                    if (isQuad)
                        uv4--;

                    Face face;
                    if (isQuad)
                    {
                        face = new Face([v1 - startIndex, v2 - startIndex, v3 - startIndex, v4 - startUVIndex],
                            [uv1 - startUVIndex, uv2 - startUVIndex, uv3 - startUVIndex, uv4 - startUVIndex], lastMaterial);
                    }
                    else
                    {
                        face = new Face([v1 - startIndex, v2 - startIndex, v3 - startIndex],
                            [uv1 - startUVIndex, uv2 - startUVIndex, uv3 - startUVIndex], lastMaterial);
                    }

                    // If there is no mesh yet, the file isn't split up into meshes,
                    // so we should just make one here.
                    if (currentMesh == -1)
                    {
                        result.Meshes.Add(new GenericMesh());
                        currentMesh++;
                    }

                    while (globalUVs.Count <= uv1 || globalUVs.Count <= uv2 || globalUVs.Count <= uv3 || (isQuad && globalUVs.Count <= uv4))
                    {
                        globalUVs.Add(new Vector2(0, 0));
                    }

                    if (!result.Meshes[currentMesh].UVs.ContainsKey(face.UVIndices[0]))
                        result.Meshes[currentMesh].UVs.Add(face.UVIndices[0], globalUVs[(int)uv1]);
                    if (!result.Meshes[currentMesh].UVs.ContainsKey(face.UVIndices[1]))
                        result.Meshes[currentMesh].UVs.Add(face.UVIndices[1], globalUVs[(int)uv2]);
                    if (!result.Meshes[currentMesh].UVs.ContainsKey(face.UVIndices[2]))
                        result.Meshes[currentMesh].UVs.Add(face.UVIndices[2], globalUVs[(int)uv3]);
                    if (isQuad && (!result.Meshes[currentMesh].UVs.ContainsKey(face.UVIndices[3])))
                        result.Meshes[currentMesh].UVs.Add(face.UVIndices[3], globalUVs[(int)uv4]);

                    if (!result.Meshes[currentMesh].Vertices.ContainsKey(face.Indices[0]))
                        result.Meshes[currentMesh].Vertices.Add(face.Indices[0], globalVertices[(int)v1]);
                    if (!result.Meshes[currentMesh].Vertices.ContainsKey(face.Indices[1]))
                        result.Meshes[currentMesh].Vertices.Add(face.Indices[1], globalVertices[(int)v2]);
                    if (!result.Meshes[currentMesh].Vertices.ContainsKey(face.Indices[2]))
                        result.Meshes[currentMesh].Vertices.Add(face.Indices[2], globalVertices[(int)v3]);
                    if (isQuad && (!result.Meshes[currentMesh].Vertices.ContainsKey(face.Indices[3])))
                        result.Meshes[currentMesh].Vertices.Add(face.Indices[3], globalVertices[(int)v4]);

                    result.Meshes[currentMesh].Faces.Add(face);
                }
                else if (line.StartsWith("usemtl "))
                {
                    string[] split = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length < 2)
                        throw new ReadFailedError("Invalid Material: " + line);
                    lastMaterial = new Material(split[1], Color.White);
                }
                else if (line.StartsWith("g "))
                {
                    string meshName = "";
                    string[] split = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length >= 2)
                        meshName = split[1];
                    result.Meshes.Add(new GenericMesh(meshName));
                    currentMesh++;

                    startIndex = (uint) globalVertices.Count;
                    startUVIndex = (uint) globalUVs.Count;
                }
            }
        }
        catch (NotSupportedException)
        {
            throw new ReadFailedError("Invalid Values");
        }
        finally
        {
            sr.Close();
            ms.Close();
        }
        
        return result;
    }
}
