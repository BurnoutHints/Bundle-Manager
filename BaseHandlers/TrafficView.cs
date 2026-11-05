using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ModelViewer;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using static BaseHandlers.Traffic;
using NVector4 = System.Numerics.Vector4;
using NVector3 = System.Numerics.Vector3;
using Vertex = BaseHandlers.TrafficWorldGeometry.Vertex;

namespace BaseHandlers;

// The traffic data in 3D: lanes coloured by hull and brightening in their direction, with the selection highlighted
public class TrafficView : UserControl
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

    private static readonly string[] layerNames = ["Lanes", "Junctions", "Light triggers", "Traffic lights", "Static vehicles", "Start positions", "PVS grid"];
    private const int Lanes = 0, Junctions = 1, Triggers = 2, Lights = 3, Statics = 4, Starts = 5, Grid = 6;

    private static readonly uint highlightColour = Colour(255, 230, 0), forwardColour = Colour(80, 255, 80), backwardColour = Colour(255, 90, 60),
        neighbourColour = Colour(0, 220, 255);

    private readonly Traffic data;

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

    private readonly CheckBox[] layerBoxes;
    private readonly List<Vertex>[] layers = [.. layerNames.Select(_ => new List<Vertex>())];
    private readonly List<Vertex> highlight = [];
    private readonly List<(Vector3 Position, int Layer, object[] Path)> pickables = [];
    private readonly List<(Vector3 Start, Vector3 End, object[] Path)> laneSegments = [];
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
    private Vector3 moveOrigin;
    private float moveHeight;
    private bool moving, moved;
    private readonly ToolTip placementTips = new();

    private readonly CheckBox moveMode = new()
    {
        Text = "Move selection",
        AutoSize = true,
        Appearance = Appearance.Button,
        Enabled = false
    };

    public bool IsMoving => moving;

    public bool HasWorldGeometry => world != null;

    public event Func<bool> MoveStarting;

    public event Action<NVector3> MovePreview;

    public event Action<bool> MoveFinished;

    // The objects and lists on the way to what was clicked, from the root
    public event Action<object[]> Picked;

    public TrafficView(Traffic initialData)
    {
        data = initialData;
        FlowLayoutPanel bar = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new(3, 0, 0, 0)
        };

        layerBoxes = [.. layerNames.Select(name => new CheckBox()
        {
            Text = name,
            AutoSize = true,
            Checked = name != "PVS grid"
        })];

        foreach (CheckBox box in layerBoxes)
            box.CheckedChanged += (_, _) => gl.Invalidate();

        bar.Controls.AddRange(layerBoxes);
        bar.Controls.Add(worldLayer);
        worldLayer.CheckedChanged += (_, _) => gl.Invalidate();
        Button fit = new()
        {
            Text = "Fit all",
            AutoSize = true
        };

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

        moveMode.CheckedChanged += (_, _) =>
        {
            CancelMove();
            gl.Cursor = moveMode.Checked
                ? Cursors.SizeAll
                : Cursors.Default;
        };
        bar.Controls.Add(moveMode);
        placementTips.SetToolTip(moveMode, "Drag to move the selection on X/Z. Shift+drag also moves it. Esc cancels. Use Move… to change height.");

        fit.Click += (_, _) =>
        {
            Frame(layers[Lanes]);
            gl.Invalidate();
        };
        frame.Click += (_, _) =>
        {
            Frame(highlight);
            gl.Invalidate();
        };
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

            if (e.Button == MouseButtons.Left && (moveMode.Checked || (ModifierKeys & Keys.Shift) != 0) && TrafficTranslation.CanMove(selected))
            {
                TrafficTranslation geometry = new(selected);

                if (geometry.Count > 0 && GroundPoint(e.Location, geometry.Center.Y) is Vector3 origin && MoveStarting?.Invoke() == true)
                {
                    moveOrigin = origin;
                    moveHeight = geometry.Center.Y;
                    moving = true;
                    moved = false;
                    gl.Capture = true;
                }
            }
        };
        gl.KeyDown += (_, e) =>
        {
            CancelMove();

            if (e.KeyCode == Keys.Home)
                Frame(layers[Lanes]);
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
            if (moving)
            {
                bool changed = moved;
                FinishMove(changed);

                if (!changed)
                    Pick(e.Location);

                return;
            }

            if (e.Button == MouseButtons.Left && Math.Abs(e.X - downMouse.X) + Math.Abs(e.Y - downMouse.Y) < 4)
                Pick(e.Location);
        };
        gl.MouseWheel += (_, e) =>
        {
            CancelMove();
            distance = Math.Clamp(distance * MathF.Pow(0.85f, e.Delta / 120f), 2, 20000);
            gl.Invalidate();
        };
        gl.MouseCaptureChanged += (_, _) =>
        {
            if (moving && !gl.Capture)
                CancelMove();
        };

        Build();
        Frame(layers[Lanes]);
    }

    private void TryShowViewport()
    {
        // GLFW cannot create a window without a monitor (for example a disconnected remote desktop).
        // Keep the GL child hidden until this check, before WinForms can create its native handle.
        try
        {
            GLFWProvider.EnsureInitialized();
            OpenTK.Windowing.GraphicsLibraryFramework.GLFW.PollEvents();

            if (Monitors.GetMonitors().Count == 0)
            {
                unavailableText.Text = "The 3D view needs a connected display. You can continue editing traffic properties. Reconnect the display, then choose Retry 3D view.";
                return;
            }

            gl.Visible = true;
            unavailable.Visible = false;
        }
        catch (Exception ex) when (ex is InvalidOperationException or DllNotFoundException or System.ComponentModel.Win32Exception)
        {
            gl.Visible = false;
            unavailable.Visible = true;
            unavailableText.Text = "The 3D view is unavailable. You can continue editing traffic properties.\n\n" + ex.Message;
        }
    }

    public void Rebuild()
    {
        Build();
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
        placementTips.SetToolTip(worldLayer, geometry == null
            ? ""
            : $"{geometry.SourcePath}\n{geometry.TriangleCount:N0} collision triangles. Visualization only.");
        gl.Invalidate();
    }

    // Selection and property edits preserve the camera; framing is an explicit command.
    public void Highlight(object selection)
    {
        CancelMove();
        selected = selection;
        moveMode.Enabled = TrafficTranslation.CanMove(selection);

        if (!moveMode.Enabled)
            moveMode.Checked = false;

        BuildHighlight();
        gl.Invalidate();
    }

    public void FrameSelection()
    {
        Frame(highlight);
        gl.Invalidate();
    }

    public bool CancelMove()
    {
        if (!moving)
            return false;

        FinishMove(false);
        return true;
    }

    public bool ExitMoveMode()
    {
        bool active = CancelMove() || moveMode.Checked;
        moveMode.Checked = false;
        return active;
    }

    private void FinishMove(bool accept)
    {
        moving = false;
        gl.Capture = false;
        MoveFinished?.Invoke(accept);
    }

    private static uint Colour(int r, int g, int b, int a = 255) => (uint)(r | g << 8 | b << 16 | a << 24);

    private static uint Scale(uint colour, float by) =>
        Colour((int)((colour & 0xFF) * by), (int)((colour >> 8 & 0xFF) * by), (int)((colour >> 16 & 0xFF) * by));

    private static uint HullColour(int hull)
    {
        Color colour = ColorFromHue(hull * 0.618034f % 1);
        return Colour(colour.R, colour.G, colour.B);
    }

    private static Color ColorFromHue(float hue)
    {
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
        return Color.FromArgb((int)(90 + 165 * r), (int)(90 + 165 * g), (int)(90 + 165 * b));
    }

    private static Vector3 At(NVector4 v) => new(v.X, v.Y, v.Z);

    private static void Line(List<Vertex> to, Vector3 a, Vector3 b, uint colour)
    {
        to.Add(new(a, colour));
        to.Add(new(b, colour));
    }

    private static void Cross(List<Vertex> to, Vector3 at, float size, uint colour)
    {
        Line(to, at - Vector3.UnitX * size, at + Vector3.UnitX * size, colour);
        Line(to, at - Vector3.UnitZ * size, at + Vector3.UnitZ * size, colour);
        Line(to, at, at + Vector3.UnitY * size * 2, colour);
    }

    private static readonly int[] signs = [-1, 1];

    // A box from its eight corners, given as a function of -1 or 1 on each axis
    private static void Box(List<Vertex> to, Func<int, int, int, Vector3> corner, uint colour)
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

    private static Vector3 Rotated(Vector3 offset, float yRotation) =>
        new(offset.X * MathF.Cos(yRotation) + offset.Z * MathF.Sin(yRotation), offset.Y, -offset.X * MathF.Sin(yRotation) + offset.Z * MathF.Cos(yRotation));

    private static void Lane(List<Vertex> to, Section section, uint colour, bool shade, int from = 0, int until = int.MaxValue)
    {
        List<LaneRung> rungs = section.maRungs;
        until = Math.Min(until, rungs.Count - 1);

        for (int i = from; i <= until; i++)
        {
            if (rungs[i].maPoints?.Length != 2)
                continue;

            uint shaded = shade
                ? Scale(colour, 0.4f + 0.6f * i / Math.Max(rungs.Count - 1, 1))
                : colour;

            if (i == from || i == until || !shade)
                Line(to, At(rungs[i].maPoints[0]), At(rungs[i].maPoints[1]), shaded);

            if (i < until && rungs[i + 1].maPoints?.Length == 2)
            {
                Line(to, At(rungs[i].maPoints[0]), At(rungs[i + 1].maPoints[0]), shaded);
                Line(to, At(rungs[i].maPoints[1]), At(rungs[i + 1].maPoints[1]), shaded);
            }
        }
    }

    private static Vector3 Middle(LaneRung rung) => rung.maPoints?.Length == 2
        ? (At(rung.maPoints[0]) + At(rung.maPoints[1])) / 2
        : Vector3.Zero;

    private static void Trigger(List<Vertex> to, LightTrigger trigger, uint colour)
    {
        Vector3 centre = At(trigger.mPosPlusYRot), half = At(trigger.mDimensions) / 2;
        Box(to, (x, y, z) => centre + Rotated(new(x * half.X, y * half.Y, z * half.Z), trigger.mPosPlusYRot.W), colour);
    }

    private static void TrafficLightMarker(List<Vertex> to, TrafficLight light, uint colour)
    {
        Vector3 at = At(light.mPosAndYRotation);
        Line(to, at, at + Vector3.UnitY * 6, colour);
        Line(to, at + Vector3.UnitY * 6, at + Vector3.UnitY * 6 + Rotated(Vector3.UnitZ * 2, light.mPosAndYRotation.W), colour);
    }

    private static void Vehicle(List<Vertex> to, StaticTrafficVehicle vehicle, uint colour)
    {
        NVector4[] m = vehicle.mTransform;

        if (m?.Length != 4)
            return;

        Box(to, (x, y, z) => At(m[0] * x * 1f + m[1] * (y + 1) * 0.75f + m[2] * z * 2.2f + m[3]), colour);
    }

    private static void Arrow(List<Vertex> to, StartingPosition start, uint colour)
    {
        Vector3 at = At(start.mPosition), direction = At(start.mDirection);

        if (direction.LengthSquared < 1e-6f)
            direction = Vector3.UnitZ;

        direction = direction.Normalized() * 4;
        Vector3 side = Vector3.Cross(direction, Vector3.UnitY).Normalized();
        Line(to, at, at + direction, colour);
        Line(to, at + direction, at + direction * 0.6f + side, colour);
        Line(to, at + direction, at + direction * 0.6f - side, colour);
    }

    private void PVSCellOutline(List<Vertex> to, int cell, uint colour)
    {
        PVS pvs = data.mPvs;

        if (pvs.muNumCells_X == 0)
            return;

        float x = pvs.mGridMin.X + cell % pvs.muNumCells_X * pvs.mCellSize.X, z = pvs.mGridMin.Z + cell / pvs.muNumCells_X * pvs.mCellSize.Z, y = pvs.mGridMin.Y;
        Vector3 a = new(x, y, z), b = new(x + pvs.mCellSize.X, y, z), c = new(x + pvs.mCellSize.X, y, z + pvs.mCellSize.Z), d = new(x, y, z + pvs.mCellSize.Z);
        Line(to, a, b, colour);
        Line(to, b, c, colour);
        Line(to, c, d, colour);
        Line(to, d, a, colour);
    }

    private void Build()
    {
        foreach (List<Vertex> layer in layers)
            layer.Clear();

        pickables.Clear();
        laneSegments.Clear();

        for (int h = 0; h < data.maHulls.Count; h++)
        {
            Hull hull = data.maHulls[h];
            uint colour = HullColour(h);

            foreach (Section section in hull.maSections)
            {
                Lane(layers[Lanes], section, colour, true);
                object[] path = [data.maHulls, hull, hull.maSections, section];

                foreach (LaneRung rung in section.maRungs)
                    pickables.Add((Middle(rung), Lanes, path: path));

                for (int i = 1; i < section.maRungs.Count; i++)
                {
                    LaneRung a = section.maRungs[i - 1], b = section.maRungs[i];

                    if (a.maPoints?.Length != 2 || b.maPoints?.Length != 2)
                        continue;

                    for (int side = 0; side < 2; side++)
                        laneSegments.Add((At(a.maPoints[side]), At(b.maPoints[side]), path: path));

                    laneSegments.Add((Middle(a), Middle(b), path: path));
                }
            }

            foreach (JunctionLogicBox junction in hull.maJunctions)
            {
                Cross(layers[Junctions], At(junction.mPosition), 4, Colour(255, 255, 255));
                pickables.Add((At(junction.mPosition), Junctions, [data.maHulls, hull, hull.maJunctions, junction]));
            }

            foreach (LightTrigger trigger in hull.maLightTriggers)
            {
                Trigger(layers[Triggers], trigger, Colour(255, 150, 40));
                pickables.Add((At(trigger.mPosPlusYRot), Triggers, [data.maHulls, hull, hull.maLightTriggers, trigger]));
            }

            foreach (TrafficLight light in hull.maTrafficLights)
            {
                TrafficLightMarker(layers[Lights], light, Colour(255, 60, 60));
                pickables.Add((At(light.mPosAndYRotation) + Vector3.UnitY * 3, Lights, [data.maHulls, hull, hull.maTrafficLights, light]));
            }

            foreach (StaticTrafficVehicle vehicle in hull.maStaticTrafficVehicles)
            {
                Vehicle(layers[Statics], vehicle, Colour(80, 220, 255));

                if (vehicle.mTransform?.Length == 4)
                    pickables.Add((At(vehicle.mTransform[3]), Statics, [data.maHulls, hull, hull.maStaticTrafficVehicles, vehicle]));
            }

            foreach (LightTriggerStartData startData in hull.maLightTriggerStartData)
            {
                foreach (StartingPosition start in startData.maStartingPositions)
                {
                    Arrow(layers[Starts], start, Colour(255, 80, 255));
                    pickables.Add((At(start.mPosition), Starts, [data.maHulls, hull, hull.maLightTriggerStartData, startData, startData.maStartingPositions, start]));
                }
            }
        }

        for (int cell = 0; cell < data.mPvs.maHullPvs.Count; cell++)
            PVSCellOutline(layers[Grid], cell, Colour(110, 110, 120));

        layersChanged = true;
    }

    private void BuildHighlight()
    {
        highlight.Clear();

        if (selected != null)
            AddHighlight(selected, 0);

        highlightChanged = true;
    }

    private Hull HullHolding(Func<Hull, bool> holds) => data.maHulls.FirstOrDefault(holds);

    private void AddHighlight(object item, int depth)
    {
        switch (item)
        {
            case Traffic or null:
                return;
            case IList list:
                foreach (object entry in list)
                    AddHighlight(entry, depth);
                return;
            case Hull hull:
                foreach (Section section in hull.maSections)
                    Lane(highlight, section, highlightColour, false);
                foreach (JunctionLogicBox junction in hull.maJunctions)
                    Cross(highlight, At(junction.mPosition), 6, highlightColour);
                foreach (LightTrigger trigger in hull.maLightTriggers)
                    Trigger(highlight, trigger, highlightColour);
                foreach (TrafficLight light in hull.maTrafficLights)
                    TrafficLightMarker(highlight, light, highlightColour);
                foreach (StaticTrafficVehicle vehicle in hull.maStaticTrafficVehicles)
                    Vehicle(highlight, vehicle, highlightColour);
                foreach (StartingPosition start in hull.maLightTriggerStartData.SelectMany(layout => layout.maStartingPositions))
                    Arrow(highlight, start, highlightColour);
                return;
            case Section section:
                Lane(highlight, section, highlightColour, false);
                if (depth > 0)
                    return;
                Vector3 sectionEnd = section.maRungs.Count > 0
                    ? Middle(section.maRungs[^1])
                    : Vector3.Zero, sectionStart = section.maRungs.Count > 0
                    ? Middle(section.maRungs[0])
                    : Vector3.Zero;
                foreach (Section next in section.Forward.Where(s => s?.maRungs.Count > 0))
                {
                    Lane(highlight, next, forwardColour, false);
                    Line(highlight, sectionEnd, Middle(next.maRungs[0]), forwardColour);
                }
                foreach (Section previous in section.Backward.Where(s => s?.maRungs.Count > 0))
                {
                    Lane(highlight, previous, backwardColour, false);
                    Line(highlight, sectionStart, Middle(previous.maRungs[^1]), backwardColour);
                }
                foreach (Neighbour neighbour in section.maLeftNeighbours.Concat(section.maRightNeighbours).Where(n => n.mSection != null))
                    Lane(highlight, neighbour.mSection, neighbourColour, false);
                return;
            case LaneRung rung:
                if (rung.maPoints?.Length != 2)
                    return;
                Line(highlight, At(rung.maPoints[0]), At(rung.maPoints[1]), highlightColour);
                Line(highlight, Middle(rung), Middle(rung) + Vector3.UnitY * 4, highlightColour);
                return;
            case Neighbour neighbour:
                if (neighbour.mSection != null)
                    Lane(highlight, neighbour.mSection, neighbourColour, false);
                AddHighlight(neighbour.mOurStartRung, depth + 1);
                AddHighlight(neighbour.mTheirStartRung, depth + 1);
                return;
            case SectionSpan span:
                foreach (Section section in HullHolding(h => h.maSectionSpans.Contains(span))?.maSections.Where(s => s.mSpan == span) ?? [])
                    Lane(highlight, section, highlightColour, false);
                return;
            case StopLine line:
                if (data.maHulls.SelectMany(h => h.maSections).FirstOrDefault(s => s.maStopLines.Contains(line)) is Section stopSection)
                    Lane(highlight, stopSection, highlightColour, false);
                return;
            case JunctionLogicBox junction:
                Cross(highlight, At(junction.mPosition), 6, highlightColour);
                foreach (LightTrigger trigger in HullHolding(h => h.maJunctions.Contains(junction))?.maLightTriggers.Where(t => t.mJunction == junction) ?? [])
                    Trigger(highlight, trigger, highlightColour);
                foreach (TrafficLightController controller in junction.maTrafficLightControllers)
                    AddHighlight(controller, depth + 1);
                foreach (LightTriggerStartData startData in (LightTriggerStartData[])[junction.mOfflineStartData, junction.mOnlineStartData, junction.mBikeStartData])
                    AddHighlight(startData, depth + 1);
                return;
            case TrafficLightController controller:
                foreach (TrafficLight light in controller.maTrafficLights)
                    TrafficLightMarker(highlight, light, highlightColour);
                return;
            case LightTrigger trigger:
                Trigger(highlight, trigger, highlightColour);
                return;
            case LightTriggerStartData startData:
                foreach (StartingPosition start in startData.maStartingPositions)
                    Arrow(highlight, start, highlightColour);
                return;
            case StartingPosition start:
                Arrow(highlight, start, highlightColour);
                return;
            case TrafficLight light:
                TrafficLightMarker(highlight, light, highlightColour);
                return;
            case TrafficLightType currentType:
                foreach (TrafficLight light in data.maHulls.SelectMany(h => h.maTrafficLights).Where(l => l.mType == currentType))
                    TrafficLightMarker(highlight, light, highlightColour);
                return;
            case StaticTrafficVehicle vehicle:
                Vehicle(highlight, vehicle, highlightColour);
                return;
            case KillZone zone:
                foreach (KillZoneRegion region in zone.maRegions)
                    AddHighlight(region, depth + 1);
                return;
            case KillZoneRegion region:
                if (region.mSection != null)
                    Lane(highlight, region.mSection, highlightColour, false, Math.Max(region.mSection.maRungs.IndexOf(region.mStartRung), 0), region.mSection.maRungs.IndexOf(region.mEndRung) is >= 0 and int last
                        ? last
                        : int.MaxValue);
                return;
            case FlowType flowType:
                foreach (Section section in data.maHulls.SelectMany(h => h.maSections).Where(s => s.mFlowType == flowType))
                    Lane(highlight, section, highlightColour, false);
                return;
            case PVSCell cell:
                PVSCellOutline(highlight, data.mPvs.maHullPvs.IndexOf(cell), highlightColour);
                foreach (Hull visible in cell.maHulls)
                    foreach (Section section in visible.maSections)
                        Lane(highlight, section, neighbourColour, false);
                return;
        }
    }

    // Looks at what's given from the current angle, from far enough away to see all of it
    private void Frame(List<Vertex> vertices)
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
        distance = Math.Clamp((max - min).Length * 1.1f, 25, 20000);
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

        if (moving)
        {
            if (!moved && Math.Abs(e.X - downMouse.X) + Math.Abs(e.Y - downMouse.Y) < 4)
                return;

            if (GroundPoint(e.Location, moveHeight) is Vector3 position)
            {
                Vector3 offset = position - moveOrigin;
                MovePreview?.Invoke(new(offset.X, 0, offset.Z));
                moved = true;
            }

            return;
        }

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

    private Vector3? GroundPoint(Point position, float height)
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

    private void Pick(Point at)
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

        if (layerBoxes[Lanes].Checked)
        {
            foreach ((Vector3 laneStart, Vector3 laneEnd, object[] path) in laneSegments)
            {
                Vector4 a = new Vector4(laneStart, 1) * viewProjection, b = new Vector4(laneEnd, 1) * viewProjection;

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

        // The selection shows through everything in front of it
        GL.Disable(EnableCap.DepthTest);
        highlightBatch.Draw();
        gl.SwapBuffers();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            placementTips.Dispose();

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
