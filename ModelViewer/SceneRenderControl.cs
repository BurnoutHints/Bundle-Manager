using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ModelViewer.SceneData;
using OpenTK.GLControl;

namespace ModelViewer;

public partial class SceneRenderControl : UserControl
{
    private GLControl glcMain;
    private GraphicsScene graphicsScene;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Scene Scene
    {
        get => graphicsScene?.Scene;
        set => InitScene(value);
    }

    private readonly bool designMode;

    private bool sceneChanged;

    public SceneRenderControl()
    {
        InitializeComponent();
        designMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        if (!designMode)
            SetupGLControl();

        sceneChanged = true;
    }

    private void SetupGLControl()
    {
        glcMain = new()
        {
            BackColor = Color.Black,
            Dock = DockStyle.Fill,
            Location = new(0, 0),
            Name = "glcMain",
            Size = new(150, 150),
            TabIndex = 1
        };
        glcMain.Paint += new PaintEventHandler(glcMain_Paint);
        Controls.Add(glcMain);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (!designMode)
        {
            graphicsScene?.Resize(glcMain.Width, glcMain.Height);
        }
    }

    private void InitScene(Scene scene)
    {
        if (!designMode)
        {
            graphicsScene?.FrameRendered -= GraphicsSceneOnFrameRendered;
            graphicsScene?.Cleanup();
            graphicsScene = new(scene, glcMain.Width, glcMain.Height);
            graphicsScene.FrameRendered += GraphicsSceneOnFrameRendered;

            tmrUpdate.Enabled = true;

            sceneChanged = true;
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

        SaveFileDialog sfd = new()
        {
            Filter = "Collada Document|*.dae|All Files|*.*"
        };

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

        SaveFileDialog sfd = new()
        {
            Filter = "Wavefront OBJ|*.obj|All Files|*.*"
        };

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
        if (!designMode)
        {
            if (sceneChanged)
            {
                graphicsScene?.Init();
                sceneChanged = false;
            }

            graphicsScene?.Render();
        }
    }

    // From Dispose, while glcMain and its GL context still exist
    private void StopRendering()
    {
        if (designMode)
            return;

        tmrUpdate.Enabled = false;

        if (graphicsScene != null && glcMain.IsHandleCreated)
        {
            glcMain.MakeCurrent();
            graphicsScene.Cleanup();
        }

        graphicsScene = null;
    }

    private int updateCount;

    private void tmrUpdate_Tick(object sender, EventArgs e)
    {
        if (!designMode)
        {
            if (updateCount < 2)
                tmrUpdate.Interval = 17;
            else
            {
                tmrUpdate.Interval = 16;
                updateCount = 0;
            }

            updateCount++;

            graphicsScene?.Update();
            glcMain.Refresh();
        }
    }
}
