using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using ModelViewer.SceneData;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

namespace ModelViewer
{
    public class GraphicsScene
    {
        private Shader _shader;

        private ICamera _camera;
        private Matrix4 _projection;

        public delegate void OnFrameRendered();
        public event OnFrameRendered FrameRendered;

        public int Width { get; set; }
        public int Height { get; set; }

        public bool CanRender { get; set; }

        public Scene Scene { get; }

        public GraphicsScene(Scene scene, int width, int height)
        {
            Scene = scene;
            Width = width;
            Height = height;
        }

        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        private void CreateProjection()
        {
            float aspectRatio = (float)Width / Height;
            _projection =
                Matrix4.CreatePerspectiveFieldOfView(60.0f * ((float) Math.PI / 180.0f), aspectRatio, 0.1f, 4000.0f);
        }

        public void Init()
        {
            if (Scene == null)
                return;

            CreateProjection();
            
            try
            {
                _shader = new Shader("standard");
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);

                CanRender = false;
                return;
            }
            _shader.Compile();

            if (!Scene.InitGraphics())
                return;

            _camera = new OrbitCamera(Scene);
            //_camera = new StaticCamera(new Vector3(0, 2, -5), new Vector3(0, 0, 0));

            GL.ClearColor(Color.CornflowerBlue);
            GL.ClearDepth(1.0f);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            //glShadeModel(GL_SMOOTH);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            CanRender = true;
        }

        public void Update()
        {
            if (!CanRender)
                return;

            _camera?.Update();
            Scene?.Update();

            // TODO: Implement
        }

        public void Render()
        {
            GL.Viewport(0, 0, Width, Height);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (CanRender)
            {
                _shader.Bind();

                GL.UniformMatrix4(20, false, ref _projection);

                Scene?.Render(_camera);
            }

            GL.Flush();

            FrameRendered?.Invoke();
        }

        public void Cleanup()
        {
            if (CanRender)
            {
                Scene?.Dispose();
                _shader.Dispose();
            }
        }
    }
}
