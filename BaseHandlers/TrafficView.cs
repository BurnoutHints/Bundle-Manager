using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using OpenTK.Mathematics;
using static BaseHandlers.Traffic;
using NVector4 = System.Numerics.Vector4;
using NVector3 = System.Numerics.Vector3;
using Vertex = BaseHandlers.TrafficWorldGeometry.Vertex;

namespace BaseHandlers;

// 3D visualization of traffic lanes, junctions, and triggers
public class TrafficView : SceneView
{
    private static readonly string[] layerNames = ["Lanes", "Junctions", "Light triggers", "Traffic lights", "Static vehicles", "Start positions", "PVS grid"];
    private const int Lanes = 0, Junctions = 1, Triggers = 2, Lights = 3, Statics = 4, Starts = 5, Grid = 6;

    private static readonly uint highlightColour = Colour(255, 230, 0), forwardColour = Colour(80, 255, 80), backwardColour = Colour(255, 90, 60),
        neighbourColour = Colour(0, 220, 255);

    private readonly Traffic data;

    private Vector3 moveOrigin;
    private float moveHeight;
    private bool moving, moved;

    private readonly CheckBox moveMode = new()
    {
        Text = "Move selection",
        AutoSize = true,
        Appearance = Appearance.Button,
        Enabled = false
    };

    public bool IsMoving => moving;

    public event Func<bool> MoveStarting;

    public event Action<NVector3> MovePreview;

    public event Action<bool> MoveFinished;

    public TrafficView(Traffic initialData) : base(layerNames, "PVS grid")
    {
        data = initialData;
        moveMode.CheckedChanged += (_, _) =>
        {
            CancelMove();
            Canvas.Cursor = moveMode.Checked
                ? Cursors.SizeAll
                : Cursors.Default;
        };
        AddToolbarControl(moveMode);
        Tips.SetToolTip(moveMode, "Drag to move the selection on X/Z. Shift+drag also moves it. Esc cancels. Use Move… to change height.");
        Canvas.MouseCaptureChanged += (_, _) =>
        {
            if (moving && !Canvas.Capture)
                CancelMove();
        };

        Rebuild();
        FrameAll();
    }

    protected override void PointerDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && (moveMode.Checked || (ModifierKeys & Keys.Shift) != 0) && TrafficTranslation.CanMove(Selected))
        {
            TrafficTranslation geometry = new(Selected);

            if (geometry.Count > 0 && GroundPoint(e.Location, geometry.Center.Y) is Vector3 origin && MoveStarting?.Invoke() == true)
            {
                moveOrigin = origin;
                moveHeight = geometry.Center.Y;
                moving = true;
                moved = false;
                Canvas.Capture = true;
            }
        }
    }

    protected override bool PointerDragged(MouseEventArgs e)
    {
        if (!moving)
            return false;

        if (!moved && Math.Abs(e.X - DownMouse.X) + Math.Abs(e.Y - DownMouse.Y) < 4)
            return true;

        if (GroundPoint(e.Location, moveHeight) is Vector3 position)
        {
            Vector3 offset = position - moveOrigin;
            MovePreview?.Invoke(new(offset.X, 0, offset.Z));
            moved = true;
        }

        return true;
    }

    protected override bool PointerReleased(MouseEventArgs e)
    {
        if (!moving)
            return false;

        bool changed = moved;
        FinishMove(changed);

        if (!changed)
            Pick(e.Location);

        return true;
    }

    public override void Highlight(object selection)
    {
        CancelMove();
        moveMode.Enabled = TrafficTranslation.CanMove(selection);

        if (!moveMode.Enabled)
            moveMode.Checked = false;

        base.Highlight(selection);
    }

    public override bool CancelMove()
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
        Canvas.Capture = false;
        MoveFinished?.Invoke(accept);
    }

    private static uint HullColour(int hull) => DistinctColour(hull);

    private static Vector3 At(NVector4 v) => new(v.X, v.Y, v.Z);

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

    protected override void BuildScene()
    {
        for (int h = 0; h < data.maHulls.Count; h++)
        {
            Hull hull = data.maHulls[h];
            uint colour = HullColour(h);

            foreach (Section section in hull.maSections)
            {
                Lane(Layer(Lanes), section, colour, true);
                object[] path = [data.maHulls, hull, hull.maSections, section];

                foreach (LaneRung rung in section.maRungs)
                    AddPickable(Middle(rung), Lanes, path);

                for (int i = 1; i < section.maRungs.Count; i++)
                {
                    LaneRung a = section.maRungs[i - 1], b = section.maRungs[i];

                    if (a.maPoints?.Length != 2 || b.maPoints?.Length != 2)
                        continue;

                    for (int side = 0; side < 2; side++)
                        AddSegment(At(a.maPoints[side]), At(b.maPoints[side]), Lanes, path);

                    AddSegment(Middle(a), Middle(b), Lanes, path);
                }
            }

            foreach (JunctionLogicBox junction in hull.maJunctions)
            {
                Cross(Layer(Junctions), At(junction.mPosition), 4, Colour(255, 255, 255));
                AddPickable(At(junction.mPosition), Junctions, [data.maHulls, hull, hull.maJunctions, junction]);
            }

            foreach (LightTrigger trigger in hull.maLightTriggers)
            {
                Trigger(Layer(Triggers), trigger, Colour(255, 150, 40));
                AddPickable(At(trigger.mPosPlusYRot), Triggers, [data.maHulls, hull, hull.maLightTriggers, trigger]);
            }

            foreach (TrafficLight light in hull.maTrafficLights)
            {
                TrafficLightMarker(Layer(Lights), light, Colour(255, 60, 60));
                AddPickable(At(light.mPosAndYRotation) + Vector3.UnitY * 3, Lights, [data.maHulls, hull, hull.maTrafficLights, light]);
            }

            foreach (StaticTrafficVehicle vehicle in hull.maStaticTrafficVehicles)
            {
                Vehicle(Layer(Statics), vehicle, Colour(80, 220, 255));

                if (vehicle.mTransform?.Length == 4)
                    AddPickable(At(vehicle.mTransform[3]), Statics, [data.maHulls, hull, hull.maStaticTrafficVehicles, vehicle]);
            }

            foreach (LightTriggerStartData startData in hull.maLightTriggerStartData)
            {
                foreach (StartingPosition start in startData.maStartingPositions)
                {
                    Arrow(Layer(Starts), start, Colour(255, 80, 255));
                    AddPickable(At(start.mPosition), Starts, [data.maHulls, hull, hull.maLightTriggerStartData, startData, startData.maStartingPositions, start]);
                }
            }
        }

        for (int cell = 0; cell < data.mPvs.maHullPvs.Count; cell++)
            PVSCellOutline(Layer(Grid), cell, Colour(110, 110, 120));
    }

    private Hull HullHolding(Func<Hull, bool> holds) => data.maHulls.FirstOrDefault(holds);

    protected override void AddHighlight(object item, int depth)
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
                    Lane(HighlightLines, section, highlightColour, false);
                foreach (JunctionLogicBox junction in hull.maJunctions)
                    Cross(HighlightLines, At(junction.mPosition), 6, highlightColour);
                foreach (LightTrigger trigger in hull.maLightTriggers)
                    Trigger(HighlightLines, trigger, highlightColour);
                foreach (TrafficLight light in hull.maTrafficLights)
                    TrafficLightMarker(HighlightLines, light, highlightColour);
                foreach (StaticTrafficVehicle vehicle in hull.maStaticTrafficVehicles)
                    Vehicle(HighlightLines, vehicle, highlightColour);
                foreach (StartingPosition start in hull.maLightTriggerStartData.SelectMany(layout => layout.maStartingPositions))
                    Arrow(HighlightLines, start, highlightColour);
                return;
            case Section section:
                Lane(HighlightLines, section, highlightColour, false);
                if (depth > 0)
                    return;
                Vector3 sectionEnd = section.maRungs.Count > 0
                    ? Middle(section.maRungs[^1])
                    : Vector3.Zero, sectionStart = section.maRungs.Count > 0
                    ? Middle(section.maRungs[0])
                    : Vector3.Zero;
                foreach (Section next in section.Forward.Where(s => s?.maRungs.Count > 0))
                {
                    Lane(HighlightLines, next, forwardColour, false);
                    Line(HighlightLines, sectionEnd, Middle(next.maRungs[0]), forwardColour);
                }
                foreach (Section previous in section.Backward.Where(s => s?.maRungs.Count > 0))
                {
                    Lane(HighlightLines, previous, backwardColour, false);
                    Line(HighlightLines, sectionStart, Middle(previous.maRungs[^1]), backwardColour);
                }
                foreach (Neighbour neighbour in section.maLeftNeighbours.Concat(section.maRightNeighbours).Where(n => n.mSection != null))
                    Lane(HighlightLines, neighbour.mSection, neighbourColour, false);
                return;
            case LaneRung rung:
                if (rung.maPoints?.Length != 2)
                    return;
                Line(HighlightLines, At(rung.maPoints[0]), At(rung.maPoints[1]), highlightColour);
                Line(HighlightLines, Middle(rung), Middle(rung) + Vector3.UnitY * 4, highlightColour);
                return;
            case Neighbour neighbour:
                if (neighbour.mSection != null)
                    Lane(HighlightLines, neighbour.mSection, neighbourColour, false);
                AddHighlight(neighbour.mOurStartRung, depth + 1);
                AddHighlight(neighbour.mTheirStartRung, depth + 1);
                return;
            case SectionSpan span:
                foreach (Section section in HullHolding(h => h.maSectionSpans.Contains(span))?.maSections.Where(s => s.mSpan == span) ?? [])
                    Lane(HighlightLines, section, highlightColour, false);
                return;
            case StopLine line:
                if (data.maHulls.SelectMany(h => h.maSections).FirstOrDefault(s => s.maStopLines.Contains(line)) is Section stopSection)
                    Lane(HighlightLines, stopSection, highlightColour, false);
                return;
            case JunctionLogicBox junction:
                Cross(HighlightLines, At(junction.mPosition), 6, highlightColour);
                foreach (LightTrigger trigger in HullHolding(h => h.maJunctions.Contains(junction))?.maLightTriggers.Where(t => t.mJunction == junction) ?? [])
                    Trigger(HighlightLines, trigger, highlightColour);
                foreach (TrafficLightController controller in junction.maTrafficLightControllers)
                    AddHighlight(controller, depth + 1);
                foreach (LightTriggerStartData startData in (LightTriggerStartData[])[junction.mOfflineStartData, junction.mOnlineStartData, junction.mBikeStartData])
                    AddHighlight(startData, depth + 1);
                return;
            case TrafficLightController controller:
                foreach (TrafficLight light in controller.maTrafficLights)
                    TrafficLightMarker(HighlightLines, light, highlightColour);
                return;
            case LightTrigger trigger:
                Trigger(HighlightLines, trigger, highlightColour);
                return;
            case LightTriggerStartData startData:
                foreach (StartingPosition start in startData.maStartingPositions)
                    Arrow(HighlightLines, start, highlightColour);
                return;
            case StartingPosition start:
                Arrow(HighlightLines, start, highlightColour);
                return;
            case TrafficLight light:
                TrafficLightMarker(HighlightLines, light, highlightColour);
                return;
            case TrafficLightType currentType:
                foreach (TrafficLight light in data.maHulls.SelectMany(h => h.maTrafficLights).Where(l => l.mType == currentType))
                    TrafficLightMarker(HighlightLines, light, highlightColour);
                return;
            case StaticTrafficVehicle vehicle:
                Vehicle(HighlightLines, vehicle, highlightColour);
                return;
            case KillZone zone:
                foreach (KillZoneRegion region in zone.maRegions)
                    AddHighlight(region, depth + 1);
                return;
            case KillZoneRegion region:
                if (region.mSection != null)
                    Lane(HighlightLines, region.mSection, highlightColour, false, Math.Max(region.mSection.maRungs.IndexOf(region.mStartRung), 0), region.mSection.maRungs.IndexOf(region.mEndRung) is >= 0 and int last
                        ? last
                        : int.MaxValue);
                return;
            case FlowType flowType:
                foreach (Section section in data.maHulls.SelectMany(h => h.maSections).Where(s => s.mFlowType == flowType))
                    Lane(HighlightLines, section, highlightColour, false);
                return;
            case PVSCell cell:
                PVSCellOutline(HighlightLines, data.mPvs.maHullPvs.IndexOf(cell), highlightColour);
                foreach (Hull visible in cell.maHulls)
                    foreach (Section section in visible.maSections)
                        Lane(HighlightLines, section, neighbourColour, false);
                return;
        }
    }
}
