using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ModelViewer;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using Vertex = BaseHandlers.TrafficWorldGeometry.Vertex;

namespace BaseHandlers;

public abstract class SceneView : UserControl
{
    private sealed class Batch
    {
        private int vao, vbo, count;

        public void Upload(List<Vertex> vertices)
        {
            Vertex[] array = [.. vertices];
            Upload(array);
        }

        public void Upload(Vertex[] array)
        {
            if (vao == 0)
            {
                vao = GL.GenVertexArray();
                vbo = GL.GenBuffer();
                GL.BindVertexArray(vao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 16, 0);
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 4, VertexAttribPointerType.UnsignedByte, true, 16, 12);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, array.Length * 16, array, BufferUsageHint.StaticDraw);
            count = array.Length;
        }

        public void Draw(PrimitiveType primitive = PrimitiveType.Lines)
        {
            if (count == 0)
                return;

            GL.BindVertexArray(vao);
            GL.DrawArrays(primitive, 0, count);
        }

        public void Delete()
        {
            GL.DeleteBuffer(vbo);
            GL.DeleteVertexArray(vao);
        }
    }

    private const string VertexShader = """
        #version 330 core
        layout(location = 0) in vec3 position;
        layout(location = 1) in vec4 colour;
        uniform mat4 viewProjection;
        out vec4 vertexColour;
        void main()
        {
            gl_Position = viewProjection * vec4(position, 1.0);
            vertexColour = colour;
        }
        """;

    private const string FragmentShader = """
        #version 330 core
        in vec4 vertexColour;
        out vec4 fragmentColour;
        void main()
        {
            fragmentColour = vertexColour;
        }
        """;

    private readonly GLControl gl = new()
    {
        Dock = DockStyle.Fill,
        Visible = false
    };

    private readonly FlowLayoutPanel unavailable = new()
    {
        Dock = DockStyle.Fill,
        Padding = new(12),
        FlowDirection = FlowDirection.TopDown
    };

    private readonly Label unavailableText = new()
    {
        AutoSize = true,
        Text = "Preparing 3D view…"
    };

    private readonly FlowLayoutPanel bar = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        Padding = new(3, 0, 0, 0)
    };

    private readonly Button fit = new()
    {
        Text = "Fit all",
        AutoSize = true
    };

    private readonly CheckBox[] layerBoxes;
    private readonly List<Vertex>[] layers;
    private readonly List<Vertex> highlight = [];
    private readonly List<(Vector3 Position, int Layer, object[] Path)> pickables = [];
    private readonly List<(Vector3 Start, Vector3 End, int Layer, object[] Path)> segments = [];
    private Batch[] batches;
    private Batch highlightBatch;
    private Shader shader;
    private TrafficWorldGeometry world;
    private Batch worldBatch;
    private bool worldChanged = true;

    private readonly CheckBox worldLayer = new()
    {
        Text = "World geometry",
        AutoSize = true,
        Visible = false
    };

    private int viewProjectionLocation;
    private bool layersChanged = true, highlightChanged = true;
    private object selected;

    private Vector3 target;
    private float distance = 1000, yaw = 0.6f, pitch = 0.9f;
    private Point lastMouse, downMouse;

    protected readonly ToolTip Tips = new();

    protected GLControl Canvas => gl;

    protected Point DownMouse => downMouse;

    protected object Selected => selected;

    protected List<Vertex> HighlightLines => highlight;

    protected virtual float MinimumFrameDistance => 25;

    public bool HasWorldGeometry => world != null;

    // Hierarchy path from root to the picked object
    public event Action<object[]> Picked;

    protected SceneView(string[] layerNames, params string[] hiddenLayers)
    {
        layers = [.. layerNames.Select(_ => new List<Vertex>())];
        layerBoxes = [.. layerNames.Select(name => new CheckBox()
        {
            Text = name,
            AutoSize = true,
            Checked = !hiddenLayers.Contains(name)
        })];

        foreach (CheckBox box in layerBoxes)
            box.CheckedChanged += (_, _) => gl.Invalidate();

        bar.Controls.AddRange(layerBoxes);
        bar.Controls.Add(worldLayer);
        worldLayer.CheckedChanged += (_, _) => gl.Invalidate();
        Button frame = new()
        {
            Text = "Frame selection",
            AutoSize = true
        };

        Button top = new()
        {
            Text = "Top view",
            AutoSize = true
        };

        fit.Click += (_, _) => FrameAll();
        frame.Click += (_, _) => FrameSelection();
        top.Click += (_, _) =>
        {
            yaw = 0;
            pitch = 1.55f;
            gl.Invalidate();
        };
        bar.Controls.AddRange([fit, frame, top]);
        Controls.Add(gl);
        Button retry = new()
        {
            Text = "Retry 3D view",
            AutoSize = true
        };

        retry.Click += (_, _) => TryShowViewport();
        unavailable.Controls.AddRange([unavailableText, retry]);
        unavailable.SizeChanged += (_, _) => unavailableText.MaximumSize = new(Math.Max(120, unavailable.ClientSize.Width - 30), 0);
        Controls.Add(unavailable);
        Controls.Add(bar);
        Load += (_, _) => TryShowViewport();

        gl.Paint += (_, _) => Render();
        gl.Resize += (_, _) => gl.Invalidate();
        gl.MouseDown += (_, e) =>
        {
            gl.Focus();
            lastMouse = downMouse = e.Location;
            PointerDown(e);
        };
        gl.KeyDown += (_, e) =>
        {
            CancelMove();

            if (e.KeyCode == Keys.Home)
                Frame(layers[0]);
            else if (e.KeyCode == Keys.F)
                Frame(highlight);
            else if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus)
                distance = Math.Max(2, distance * 0.85f);
            else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)
                distance = Math.Min(20000, distance / 0.85f);
            else
                return;

            gl.Invalidate();
            e.Handled = true;
        };
        gl.MouseMove += OnDrag;
        gl.MouseUp += (_, e) =>
        {
            if (PointerReleased(e))
                return;

            if (e.Button == MouseButtons.Left && Math.Abs(e.X - downMouse.X) + Math.Abs(e.Y - downMouse.Y) < 4)
                Pick(e.Location);
        };
        gl.MouseWheel += (_, e) =>
        {
            CancelMove();
            distance = Math.Clamp(distance * MathF.Pow(0.85f, e.Delta / 120f), 2, 20000);
            gl.Invalidate();
        };
    }

    // Populates scene layers and pickable elements
    protected abstract void BuildScene();

    // Adds visual representation of the item to HighlightLines
    protected abstract void AddHighlight(object item, int depth);

    protected virtual void PointerDown(MouseEventArgs e)
    {
    }

    // Returns true if drag was handled (suppresses camera rotation)
    protected virtual bool PointerDragged(MouseEventArgs e) => false;

    // Returns true if release was handled (suppresses selection)
    protected virtual bool PointerReleased(MouseEventArgs e) => false;

    public virtual bool CancelMove() => false;

    protected List<Vertex> Layer(int index) => layers[index];

    protected bool LayerVisible(int index) => layerBoxes[index].Checked;

    protected void AddPickable(Vector3 position, int layer, object[] path) => pickables.Add((position, layer, path));

    protected void AddSegment(Vector3 start, Vector3 end, int layer, object[] path) => segments.Add((start, end, layer, path));

    protected void AddToolbarControl(Control control)
    {
        bar.Controls.Add(control);
        bar.Controls.SetChildIndex(control, bar.Controls.GetChildIndex(fit));
    }

    private void TryShowViewport()
    {
        // Keep GLControl hidden until initialized to prevent early native handle creation
        try
        {
            GLFWProvider.EnsureInitialized();
            OpenTK.Windowing.GraphicsLibraryFramework.GLFW.PollEvents();

            if (Monitors.GetMonitors().Count == 0)
            {
                unavailableText.Text = "The 3D view needs a connected display. Reconnect the display, then choose Retry 3D view.";
                return;
            }

            gl.Visible = true;
            unavailable.Visible = false;
        }
        catch (Exception ex) when (ex is InvalidOperationException or DllNotFoundException or System.ComponentModel.Win32Exception)
        {
            gl.Visible = false;
            unavailable.Visible = true;
            unavailableText.Text = "The 3D view is unavailable. You can continue editing properties.\n\n" + ex.Message;
        }
    }

    public void Rebuild()
    {
        foreach (List<Vertex> layer in layers)
            layer.Clear();

        pickables.Clear();
        segments.Clear();
        BuildScene();
        layersChanged = true;
        BuildHighlight();
        gl.Invalidate();
    }

    public void SetWorldGeometry(TrafficWorldGeometry geometry)
    {
        CancelMove();
        world = geometry;
        worldChanged = true;
        worldLayer.Visible = geometry != null;
        worldLayer.Checked = geometry != null;
        Tips.SetToolTip(worldLayer, geometry == null
            ? ""
            : $"{geometry.SourcePath}\n{geometry.TriangleCount:N0} collision triangles. Visualization only.");
        gl.Invalidate();
    }

    public virtual void Highlight(object selection)
    {
        selected = selection;
        BuildHighlight();
        gl.Invalidate();
    }

    public void FrameSelection()
    {
        Frame(highlight);
        gl.Invalidate();
    }

    public void FrameAll()
    {
        Frame(layers[0]);
        gl.Invalidate();
    }

    private void BuildHighlight()
    {
        highlight.Clear();

        if (selected != null)
            AddHighlight(selected, 0);

        highlightChanged = true;
    }

    protected static uint Colour(int r, int g, int b, int a = 255) => (uint)(r | g << 8 | b << 16 | a << 24);

    protected static uint Scale(uint colour, float by) =>
        Colour((int)((colour & 0xFF) * by), (int)((colour >> 8 & 0xFF) * by), (int)((colour >> 16 & 0xFF) * by));

    // Generates distinct colors for sequential indexes using golden ratio hue spacing
    protected static uint DistinctColour(int index)
    {
        float hue = index * 0.618034f % 1;
        float h = hue * 6, x = 1 - Math.Abs(h % 2 - 1);
        (float r, float g, float b) = (int)h switch
        {
            0 => (1, x: x, 0),
            1 => (x: x, 1, 0),
            2 => (0, 1, x: x),
            3 => (0, x: x, 1),
            4 => (x: x, 0, 1),
            _ => (1f, 0f, x: x)
        };
        return Colour((int)(90 + 165 * r), (int)(90 + 165 * g), (int)(90 + 165 * b));
    }

    protected static void Line(List<Vertex> to, Vector3 a, Vector3 b, uint colour)
    {
        to.Add(new(a, colour));
        to.Add(new(b, colour));
    }

    protected static void Cross(List<Vertex> to, Vector3 at, float size, uint colour)
    {
        Line(to, at - Vector3.UnitX * size, at + Vector3.UnitX * size, colour);
        Line(to, at - Vector3.UnitZ * size, at + Vector3.UnitZ * size, colour);
        Line(to, at, at + Vector3.UnitY * size * 2, colour);
    }

    private static readonly int[] signs = [-1, 1];

    // Builds a wireframe box from normalized corner coordinates (-1 to 1)
    protected static void Box(List<Vertex> to, Func<int, int, int, Vector3> corner, uint colour)
    {
        foreach (int a in signs)
        {
            foreach (int b in signs)
            {
                Line(to, corner(-1, a, b), corner(1, a, b), colour);
                Line(to, corner(a, -1, b), corner(a, 1, b), colour);
                Line(to, corner(a, b, -1), corner(a, b, 1), colour);
            }
        }
    }

    // Adjusts camera distance and target to encompass the given vertices
    protected void Frame(List<Vertex> vertices)
    {
        if (vertices.Count == 0)
            return;

        Vector3 min = new(float.MaxValue), max = new(float.MinValue);

        foreach (Vertex vertex in vertices)
        {
            min = Vector3.ComponentMin(min, vertex.Position);
            max = Vector3.ComponentMax(max, vertex.Position);
        }

        target = (min + max) / 2;
        distance = Math.Clamp((max - min).Length * 1.1f, MinimumFrameDistance, 20000);
    }

    private Matrix4 ViewProjection()
    {
        Vector3 eye = target + distance * new Vector3(MathF.Cos(pitch) * MathF.Sin(yaw), MathF.Sin(pitch), MathF.Cos(pitch) * MathF.Cos(yaw));
        Matrix4 view = Matrix4.LookAt(eye, target, Vector3.UnitY);
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(50f), Math.Max(gl.Width, 1) / (float)Math.Max(gl.Height, 1),
            Math.Max(0.5f, distance / 500), distance * 4 + 10000);
        return view * projection;
    }

    private void OnDrag(object sender, MouseEventArgs e)
    {
        int dx = e.X - lastMouse.X, dy = e.Y - lastMouse.Y;
        lastMouse = e.Location;

        if (PointerDragged(e))
            return;

        if (e.Button == MouseButtons.Left)
        {
            yaw -= dx * 0.01f;
            pitch = Math.Clamp(pitch + dy * 0.01f, -1.55f, 1.55f);
        }
        else if (e.Button is MouseButtons.Right or MouseButtons.Middle)
        {
            Vector3 forward = new(MathF.Sin(yaw), 0, MathF.Cos(yaw)), right = Vector3.Cross(Vector3.UnitY, forward);
            target -= (right * dx + forward * dy) * distance * 0.0015f;
        }
        else
        {
            return;
        }

        gl.Invalidate();
    }

    protected Vector3? GroundPoint(Point position, float height)
    {
        float x = 2f * position.X / Math.Max(1, gl.Width) - 1;
        float y = 1 - 2f * position.Y / Math.Max(1, gl.Height);
        Matrix4 inverse = ViewProjection().Inverted();
        Vector4 near = new Vector4(x, y, -1, 1) * inverse;
        Vector4 far = new Vector4(x, y, 1, 1) * inverse;

        if (Math.Abs(near.W) < 0.000001f || Math.Abs(far.W) < 0.000001f)
            return null;

        Vector3 start = near.Xyz / near.W;
        Vector3 direction = far.Xyz / far.W - start;

        if (Math.Abs(direction.Y) < 0.000001f)
            return null;

        float distanceToPlane = (height - start.Y) / direction.Y;
        return distanceToPlane >= 0
            ? start + direction * distanceToPlane
            : null;
    }

    protected void Pick(Point at)
    {
        Matrix4 viewProjection = ViewProjection();
        float best = 12 * 12;
        object[] picked = null;

        foreach ((Vector3 position, int layer, object[] path) in pickables)
        {
            if (!layerBoxes[layer].Checked)
                continue;

            Vector4 clip = new Vector4(position, 1) * viewProjection;

            if (clip.W <= 0)
                continue;

            float x = (clip.X / clip.W + 1) / 2 * gl.Width - at.X, y = (1 - clip.Y / clip.W) / 2 * gl.Height - at.Y;

            if (x * x + y * y < best)
            {
                best = x * x + y * y;
                picked = path;
            }
        }

        foreach ((Vector3 segmentStart, Vector3 segmentEnd, int layer, object[] path) in segments)
        {
            if (!layerBoxes[layer].Checked)
                continue;

            Vector4 a = new Vector4(segmentStart, 1) * viewProjection, b = new Vector4(segmentEnd, 1) * viewProjection;

            if (a.W <= 0 || b.W <= 0)
                continue;

            Vector2 start = new((a.X / a.W + 1) / 2 * gl.Width, (1 - a.Y / a.W) / 2 * gl.Height);
            Vector2 end = new((b.X / b.W + 1) / 2 * gl.Width, (1 - b.Y / b.W) / 2 * gl.Height);
            Vector2 point = new(at.X, at.Y), delta = end - start;
            float t = delta.LengthSquared > 0
                ? Math.Clamp(Vector2.Dot(point - start, delta) / delta.LengthSquared, 0, 1)
                : 0;
            float squared = (point - (start + t * delta)).LengthSquared;

            if (squared < best)
            {
                best = squared;
                picked = path;
            }
        }

        if (picked == null)
            return;

        Picked?.Invoke(picked);
    }

    private void Render()
    {
        gl.MakeCurrent();

        if (shader == null)
        {
            shader = new(VertexShader, FragmentShader);
            viewProjectionLocation = GL.GetUniformLocation(shader.Compile(), "viewProjection");
            batches = [.. layers.Select(_ => new Batch())];
            highlightBatch = new();
            worldBatch = new();
        }

        if (layersChanged)
        {
            for (int i = 0; i < layers.Length; i++)
                batches[i].Upload(layers[i]);

            layersChanged = false;
        }

        if (highlightChanged)
        {
            highlightBatch.Upload(highlight);
            highlightChanged = false;
        }

        if (worldChanged)
        {
            worldBatch.Upload(world?.Vertices ?? []);
            worldChanged = false;
        }

        GL.Viewport(0, 0, gl.Width, gl.Height);
        GL.ClearColor(0.11f, 0.12f, 0.14f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);
        shader.Bind();
        Matrix4 viewProjection = ViewProjection();
        GL.UniformMatrix4(viewProjectionLocation, false, ref viewProjection);

        if (worldLayer.Checked && world != null)
        {
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(1, 1);
            worldBatch.Draw(PrimitiveType.Triangles);
            GL.Disable(EnableCap.PolygonOffsetFill);
        }

        for (int i = 0; i < batches.Length; i++)
            if (layerBoxes[i].Checked)
                batches[i].Draw();

        // Render highlights without depth testing
        GL.Disable(EnableCap.DepthTest);
        highlightBatch.Draw();
        gl.SwapBuffers();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Tips.Dispose();

        if (disposing && shader != null && gl.IsHandleCreated)
        {
            gl.MakeCurrent();

            foreach (Batch batch in batches)
                batch.Delete();

            highlightBatch.Delete();
            worldBatch.Delete();
            GL.DeleteProgram(shader.ShaderID);
            shader = null;
        }

        base.Dispose(disposing);
    }
}
