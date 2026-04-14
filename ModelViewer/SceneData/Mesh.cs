using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace ModelViewer.SceneData
{
    public struct TexturedVertex
    {
        public static readonly int SizeInBytes = Vector3.SizeInBytes + Vector2.SizeInBytes;

        public Vector3 Pos;
        public Vector2 UV;

        public TexturedVertex(Vector3 pos, Vector2 uv)
        {
            Pos = pos;
            UV = uv;
        }
    }

    public class MeshFace
    {
        public List<uint> Indices;
        public Material Material;

        public MeshFace()
        {
            Indices = new List<uint>();
        }
    }

    public class Mesh
    {
        private int _vertexArray;
        private int _indexArray;
        private int _texture;

        public Material Material { get; set; }

        public List<Vector3> Vertices { get; set; }
        public List<Vector3> Normals { get; set; }
        public List<Vector3> Normals2 { get; set; }
        public List<uint> Indices { get; set; }
        private List<uint> TempIndices;
        public List<MeshFace> Faces { get; set; }
        public List<Vector2> UV1 { get; set; }
        public List<Vector2> UV2 { get; set; }
        public int ID1 { get; set; }
        public int ID2 { get; set; }

        //public Dictionary<uint, Material> Materials { get; set; }

        public Mesh()
        {
            Vertices = new List<Vector3>();
            Normals = new List<Vector3>();
            Normals2 = new List<Vector3>();
            Indices = new List<uint>();
            UV1 = new List<Vector2>();
            UV2 = new List<Vector2>();
            ID1 = 0;
            ID2 = 0;

            //Materials = new Dictionary<uint, Material>();
        }

        public Mesh Copy()
        {
            Mesh result = new Mesh();

            result.Material = Material;

            result.Vertices = new List<Vector3>(Vertices);
            result.Normals = new List<Vector3>(Normals);
            result.Normals2 = new List<Vector3>(Normals2);
            result.Indices = new List<uint>(Indices);
            result.UV1 = new List<Vector2>(UV1);
            result.UV2 = new List<Vector2>(UV2);
            result.ID1 = ID1;
            result.ID2 = ID2;

            if (Faces != null)
                result.Faces = new List<MeshFace>(Faces);

            return result;
        }

        public Mesh Merged(Mesh mesh)
        {
            Mesh result = Copy();

            for (int i = 0; i < mesh.Indices.Count; i++)
            {
                // TODO: Verify
                result.Indices.Add((uint)result.Vertices.Count + mesh.Indices[i]);
            }

            result.Vertices.AddRange(mesh.Vertices);

            // TODO: Merge Normals, and UVs correclty for indices
            // TODO: Merge Materials somehow (make texture atlas?)

            return result;
        }

        public Mesh Transformed(Matrix4 transform)
        {
            Vector3 scale = transform.ExtractScale();
            Vector3 translation = transform.ExtractTranslation();
            Quaternion rotation = transform.ExtractRotation();

            Mesh result = Copy();

            result.Vertices.Clear();

            for (int i = 0; i < Vertices.Count; i++)
            {
                Vector3 v = Vertices[i];
                v *= scale;
                v = Vector3.Transform(v, rotation);
                v += translation;
                result.Vertices.Add(v);
            }

            return result;
        }

        public bool InitGraphics()
        {
            /*List<TexturedVertex> vertices = new List<TexturedVertex>();

            for (int i = 0; i < Vertices.Count; i++)
            {
                Vector3 pos = Vertices[i];
                Vector2 uv = Vector2.Zero;
                if (i < UV1.Count)
                    uv = UV1[i];
                vertices.Add(new TexturedVertex(pos, uv));
            }

            // Vertices
            _vertexArray = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexArray);
            GL.BufferData(BufferTarget.ArrayBuffer, TexturedVertex.SizeInBytes * vertices.Count, vertices.ToArray(), BufferUsageHint.StaticDraw);
            
            // Indices
            _indexArray = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexArray);
            GL.BufferData(BufferTarget.ElementArrayBuffer, sizeof(uint) * Indices.Count, Indices.ToArray(), BufferUsageHint.StaticDraw);
            */

            List<uint> indices = Indices;

            if (Faces != null)
            {
                indices = new List<uint>();
                foreach (MeshFace face in Faces)
                {
                    /*for (int i = 2; i < face.Indices.Count; i++)
                    {
                        if ((i - 2) % 2 == 0)
                        {
                            indices.Add(face.Indices[i] - 0);
                            indices.Add(face.Indices[i] - 1);
                            indices.Add(face.Indices[i] - 2);
                        }
                        else
                        {
                            indices.Add(face.Indices[i] - 2);
                            indices.Add(face.Indices[i] - 1);
                            indices.Add(face.Indices[i] - 0);
                        }
                    }*/

                    indices.AddRange(face.Indices);
                }
            }

            List<TexturedVertex> vertices = new List<TexturedVertex>();
            if (indices.Count == 0)
            {
                for (int i = 0; i < Vertices.Count; i++)
                {
                    indices.Add((uint)i);
                }
            }

            List<uint> indices2 = indices;
            /*List<uint> indices2 = new List<uint>();

            // Triangulate Faces
            for (int i = 2; i < indices.Count; i++)
            {
                if ((i - 2) % 2 == 0)
                {
                    indices2.Add(indices[i] - 0);
                    indices2.Add(indices[i] - 1);
                    indices2.Add(indices[i] - 2);
                }
                else
                {
                    indices2.Add(indices[i] - 2);
                    indices2.Add(indices[i] - 1);
                    indices2.Add(indices[i] - 0);
                }
            }*/

            for (int i = 0; i < indices2.Count; i++)
            {
                if (indices2[i] >= Vertices.Count)
                {
                    //MessageBox.Show("Index too large: " + indices[i].ToString("X2") + " >= " + Vertices.Count.ToString("X8"));
                    continue;
                }
                Vector3 pos = Vertices[(int)indices2[i]];
                //Vector2 uv = new Vector2();
                //if (UV1.Count < indices[i])
                //    uv = UV1[(int) indices[i]];
                Vector2 uv1;
                if (indices2[i] < UV1.Count)
                    uv1 = UV1[(int)indices2[i]];
                else
                    uv1 = new Vector2(0, 0);

                Vector2 uv = new Vector2(uv1.X, uv1.Y);

                vertices.Add(new TexturedVertex(pos, uv));
            }

            TempIndices = new List<uint>(indices2);

            _vertexArray = GL.GenVertexArray();
            int buffer = GL.GenBuffer();
            GL.BindVertexArray(_vertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);

            GL.NamedBufferStorage(buffer, TexturedVertex.SizeInBytes*vertices.Count, vertices.ToArray(), BufferStorageFlags.MapWriteBit);

            GL.VertexArrayAttribBinding(_vertexArray, 0, 0);
            GL.EnableVertexArrayAttrib(_vertexArray, 0);
            GL.VertexArrayAttribFormat(_vertexArray, 0, 3, VertexAttribType.Float, false, 0);

            GL.VertexArrayAttribBinding(_vertexArray, 1, 0);
            GL.EnableVertexArrayAttrib(_vertexArray, 1);
            GL.VertexArrayAttribFormat(_vertexArray, 1, 2, VertexAttribType.Float, false, Vector3.SizeInBytes);

            GL.VertexArrayVertexBuffer(_vertexArray, 0, buffer, IntPtr.Zero, TexturedVertex.SizeInBytes);



            //_indexArray = GL.GenBuffer();
            //GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexArray);
            //GL.NamedBufferStorage(_indexArray, sizeof(uint) * Indices.Count, Indices.ToArray(), BufferStorageFlags.MapWriteBit);

            if (Material?.DiffuseMap != null)
            {
                try
                {
                    int width = Material.DiffuseMap.Width;
                    int height = Material.DiffuseMap.Height;

                    _texture = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, _texture);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Bgra,
                        PixelType.UnsignedByte, IntPtr.Zero);
                    Bitmap bitmap = new Bitmap(Material.DiffuseMap);
                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgra,
                        PixelType.UnsignedByte, data.Scan0);

                    bitmap.UnlockBits(data);

                    bitmap.Dispose();

                    GL.BindTexture(TextureTarget.Texture2D, 0);

                    GL.Enable(EnableCap.Texture2D);
                } catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message + "\n\n" + ex.StackTrace);
                }
            }
            else
            {
                int width = 16;
                int height = 16;

                _texture = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Bgra,
                    PixelType.UnsignedByte, IntPtr.Zero);
                Bitmap bitmap = new Bitmap(width, height);
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        bitmap.SetPixel(x, y, Color.DarkGray);

                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgra,
                    PixelType.UnsignedByte, data.Scan0);

                bitmap.UnlockBits(data);

                bitmap.Dispose();

                GL.BindTexture(TextureTarget.Texture2D, 0);

                GL.Enable(EnableCap.Texture2D);
            }

            return true;
        }

        public void Update()
        {
            
        }

        public void Render(ICamera camera, Matrix4 transform)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            //GL.Uniform1(21, 0);
            
            Matrix4 translation = Matrix4.CreateTranslation(transform.ExtractTranslation());
            Matrix4 rotation = Matrix4.CreateFromQuaternion(transform.ExtractRotation());
            Matrix4 scale = Matrix4.CreateScale(transform.ExtractScale());

            Matrix4 modelView = rotation * scale * translation;
            if (camera != null)
                modelView *= camera.LookAtMatrix;

            GL.UniformMatrix4(21, false, ref modelView);

            GL.BindVertexArray(_vertexArray);
            //GL.PointSize(4.0f);
            if (TempIndices != null)
                GL.DrawArrays(PrimitiveType.Triangles, 0, TempIndices.Count);
            else
                GL.DrawArrays(PrimitiveType.Triangles, 0, Indices.Count);

            /*GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.IndexArray);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, TexturedVertex.SizeInBytes, IntPtr.Zero);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, TexturedVertex.SizeInBytes, Vector3.SizeInBytes);

            GL.VertexAttrib3(0, transform.ExtractTranslation());
            GL.VertexPointer(5, VertexPointerType.Float, TexturedVertex.SizeInBytes, 0);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexArray);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexArray);
            GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);

            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(0);

            GL.DisableClientState(ArrayCap.IndexArray);
            GL.DisableClientState(ArrayCap.VertexArray);*/
        }

        public void Dispose()
        {
            GL.DeleteVertexArrays(1, ref _vertexArray);
        }
    }
}
