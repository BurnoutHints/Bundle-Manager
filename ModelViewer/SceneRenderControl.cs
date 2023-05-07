using System;
using System.ComponentModel;
using System.Windows.Forms;
using ModelViewer.SceneData;

namespace ModelViewer
{
    public partial class SceneRenderControl : UserControl
    {
        private GraphicsScene _graphicsScene;
        
        public Scene Scene
        {
            get => _graphicsScene?.Scene;
            set => InitScene(value);
        }

        private readonly bool _designMode;

        private bool _sceneChanged;

        public SceneRenderControl()
        {
            InitializeComponent();

            _designMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;

            _sceneChanged = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!_designMode)
            {
                _graphicsScene?.Resize(glcMain.Width, glcMain.Height);
            }
        }

        private void InitScene(Scene scene)
        {
            if (!_designMode)
            {
                if (_graphicsScene != null)
                {
                    _graphicsScene.FrameRendered -= GraphicsSceneOnFrameRendered;
                    _graphicsScene.Cleanup();
                    _graphicsScene = null;
                }
                _graphicsScene = new GraphicsScene(scene, glcMain.Width, glcMain.Height);
                _graphicsScene.FrameRendered += GraphicsSceneOnFrameRendered;

                tmrUpdate.Enabled = true;

                _sceneChanged = true;
            }
        }

        private void GraphicsSceneOnFrameRendered()
        {
            glcMain.SwapBuffers();
        }

        public void ExportCollada14()
        {
            if (Scene == null)
                return;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Collada Document|*.dae|All Files|*.*";
            DialogResult result = sfd.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(sfd.FileName))
                    return;

                Scene.ExportCollada14(sfd.FileName);
            }
        }

        public void ExportWavefrontObj()
        {
            if (Scene == null)
                return;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Wavefront OBJ|*.obj|All Files|*.*";
            DialogResult result = sfd.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(sfd.FileName))
                    return;

                Scene.ExportWavefrontObj(sfd.FileName);
            }
        }

        private void glcMain_Paint(object sender, PaintEventArgs e)
        {
            if (!_designMode)
            {
                if (_sceneChanged)
                {
                    _graphicsScene?.Init();
                    _sceneChanged = false;
                }
                _graphicsScene?.Render();
            }
        }

        public new void Dispose()
        {
            if (!_designMode)
            {
                tmrUpdate.Enabled = false;
                _graphicsScene?.Cleanup();
            }
            base.Dispose();
        }

        private int _updateCount;
        private void tmrUpdate_Tick(object sender, EventArgs e)
        {
            if (!_designMode)
            {
                if (_updateCount < 2)
                    tmrUpdate.Interval = 17;
                else
                {
                    tmrUpdate.Interval = 16;
                    _updateCount = 0;
                }
                _updateCount++;

                _graphicsScene?.Update();
                glcMain.Refresh();
            }
        }
    }
}
