using System.Collections.Generic;

namespace ModelViewer;

public class GenericModel
{
    public List<GenericMesh> Meshes;

    public GenericModel()
    {
        Meshes = [];
    }

    public void SplitByPointCount(int pointCount)
    {
        //pointCount /= 3;
        /*List<GenericMesh> newMeshes = new List<GenericMesh>();

        List<int> meshesToSplit = new List<int>();

        for (int i = 0; i < Meshes.Count; i++)
        {
            GenericMesh mesh = Meshes[i];
            uint count = 0;
            foreach (uint key in mesh.Vertices.Keys)
            {
                if (count < key)
                {
                    count = key;
                }
            }
            count++;

            if (count > 256)
            {
                meshesToSplit.Add(i);
            }
            else
            {
                newMeshes.Add(mesh);
            }
        }

        foreach (int meshIndex in meshesToSplit)
        {
            GenericMesh mesh = Meshes[meshIndex];

            List<Face> newFaces = new List<Face>();
            
            for (int i = 0; i < mesh.Faces.Count; i += pointCount)
            {
                int remaining = mesh.Faces.Count - i;
                if (remaining < pointCount)
                    newFaces.AddRange(mesh.Faces.GetRange(i, Math.Abs(remaining)));
                else
                    newFaces.AddRange(mesh.Faces.GetRange(i, pointCount));
            }

            for (int i = 0; i < newFaces.Count; i++)
            {
                Face face = newFaces[i];
                GenericMesh newMesh = new GenericMesh();
                for (int j = 0; j < face.Indices.Count; j++)
                {
                    uint oldIndex = face.Indices[j];
                    while (face.Indices[j] > pointCount)
                    {
                        face.Indices[j] -= (uint) pointCount;
                    }
                    Vector3 point = mesh.Vertices[oldIndex];
                    newMesh.Vertices.Add(face.Indices[j], point);
                }
                newMesh.Faces.Add(face);
                newMesh.Name = mesh.Name + "." + i;

                // TODO: Deal with UVs

                newMeshes.Add(newMesh);
            }
        }

        Meshes = newMeshes;*/

        // TODO: Split Meshes by Point Count
    }
}
