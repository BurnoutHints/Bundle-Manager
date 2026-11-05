using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static BaseHandlers.Traffic;

namespace BaseHandlers;

public sealed class TrafficTranslation
{
    private sealed record Position(Vector4 Original, Action<Vector4> Write);

    private readonly List<Position> positions = [];

    public int Count => positions.Count;

    public Vector3 Minimum { get; private set; }

    public Vector3 Maximum { get; private set; }

    public Vector3 Center => Minimum / 2 + Maximum / 2;

    public static bool CanMove(object selection) => selection switch
    {
        Hull or Section or LaneRung or JunctionLogicBox or LightTrigger or LightTriggerStartData or StartingPosition or TrafficLight or StaticTrafficVehicle => true,
        IList list => list.Cast<object>().Any(CanMove),
        _ => false
    };

    public TrafficTranslation(object selection)
    {
        if (!CanMove(selection))
            return;

        foreach (ObjectGraph.Node node in ObjectGraph.Nodes(selection))
        {
            switch (node.Value)
            {
                case LaneRung rung:
                    for (int index = 0; index < (rung.maPoints?.Length ?? 0); index++)
                    {
                        int point = index;
                        Add(rung.maPoints[point], value => rung.maPoints[point] = value);
                    }
                    break;
                case JunctionLogicBox junction:
                    Add(junction.mPosition, value => junction.mPosition = value);
                    break;
                case LightTrigger trigger:
                    Add(trigger.mPosPlusYRot, value => trigger.mPosPlusYRot = value);
                    break;
                case StartingPosition start:
                    Add(start.mPosition, value => start.mPosition = value);
                    break;
                case TrafficLight light:
                    Add(light.mPosAndYRotation, value => light.mPosAndYRotation = value);
                    break;
                case StaticTrafficVehicle vehicle when vehicle.mTransform?.Length == 4:
                    Add(vehicle.mTransform[3], value => vehicle.mTransform[3] = value);
                    break;
            }
        }

        Minimum = positions.Count == 0
            ? Vector3.Zero
            : new(float.MaxValue);
        Maximum = positions.Count == 0
            ? Vector3.Zero
            : new(float.MinValue);

        foreach (Position position in positions)
        {
            Vector3 point = new(position.Original.X, position.Original.Y, position.Original.Z);
            Minimum = Vector3.Min(Minimum, point);
            Maximum = Vector3.Max(Maximum, point);
        }
    }

    private void Add(Vector4 position, Action<Vector4> write) => positions.Add(new(position, write));

    public void Apply(Vector3 offset)
    {
        if (!float.IsFinite(offset.X) || !float.IsFinite(offset.Y) || !float.IsFinite(offset.Z))
            throw new ArgumentException("The movement offset must contain finite coordinates.");

        if (offset == Vector3.Zero)
        {
            Restore();
            return;
        }

        Vector4 delta = new(offset, 0);

        foreach (Position position in positions)
            if (!Finite(position.Original + delta))
                throw new ArgumentException("The movement would put a position outside the supported range.");

        foreach (Position position in positions)
            position.Write(position.Original + delta);
    }

    public void Restore()
    {
        foreach (Position position in positions)
            position.Write(position.Original);
    }

    private static bool Finite(Vector4 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}

public static class TrafficVisibility
{
    public static string Label(Traffic data, int index)
    {
        uint width = data.mPvs.muNumCells_X;
        string coordinates = width == 0
            ? index.ToString()
            : $"{index % width}, {index / width}";
        return $"Cell {coordinates} — {data.mPvs.maHullPvs[index].maHulls.Count}/8 road areas";
    }

    public static int[] CellsForArea(Traffic data, Hull area) =>
        [.. Enumerable.Range(0, data.mPvs.maHullPvs.Count).Where(i => data.mPvs.maHullPvs[i].maHulls.Contains(area))];

    public static int[] CellsAt(Traffic data, Vector3 minimum, Vector3 maximum)
    {
        PVS grid = data.mPvs;

        if (grid.muNumCells_X == 0 || grid.muNumCells_Z == 0 || grid.mCellSize.X <= 0 || grid.mCellSize.Z <= 0 ||
            !float.IsFinite(grid.mCellSize.X) || !float.IsFinite(grid.mCellSize.Z))
            return [];

        List<int> cells = [];

        for (int index = 0; index < grid.maHullPvs.Count; index++)
        {
            float x = grid.mGridMin.X + index % grid.muNumCells_X * grid.mCellSize.X;
            float z = grid.mGridMin.Z + index / grid.muNumCells_X * grid.mCellSize.Z;

            if (maximum.X >= x && minimum.X < x + grid.mCellSize.X && maximum.Z >= z && minimum.Z < z + grid.mCellSize.Z)
                cells.Add(index);
        }

        return [.. cells];
    }

    public static Vector3 CellCenter(Traffic data, int index)
    {
        ValidateCells(data, [index]);
        PVS grid = data.mPvs;

        if (grid.muNumCells_X == 0 || grid.muNumCells_Z == 0 || grid.mCellSize.X <= 0 || grid.mCellSize.Z <= 0 ||
            !float.IsFinite(grid.mCellSize.X) || !float.IsFinite(grid.mCellSize.Z) ||
            !float.IsFinite(grid.mGridMin.X) || !float.IsFinite(grid.mGridMin.Z))
            throw new InvalidOperationException("Set valid visibility grid dimensions and cell sizes before placing items in a cell.");

        Vector3 center = new(grid.mGridMin.X + (index % grid.muNumCells_X + 0.5f) * grid.mCellSize.X,
            grid.mGridMin.Y, grid.mGridMin.Z + (index / grid.muNumCells_X + 0.5f) * grid.mCellSize.Z);

        if (!float.IsFinite(center.X) || !float.IsFinite(center.Z))
            throw new InvalidOperationException("This visibility cell is outside the supported coordinate range.");

        return center;
    }

    public static void ValidateArea(Traffic data, Hull area, IEnumerable<int> selectedCells)
    {
        int[] cells = [.. selectedCells.Distinct()];
        ValidateCells(data, cells);

        foreach (int index in cells)
        {
            List<Hull> areas = data.mPvs.maHullPvs[index].maHulls;

            if (!areas.Contains(area) && areas.Count >= 8)
                throw new InvalidOperationException($"{Label(data, index)} is full. Each visibility cell can contain at most 8 road areas.");
        }
    }

    public static void SetArea(Traffic data, Hull area, IEnumerable<int> selectedCells)
    {
        HashSet<int> selected = [.. selectedCells];
        ValidateArea(data, area, selected);

        if (!data.maHulls.Contains(area))
            throw new InvalidOperationException("Choose a road area from this traffic resource.");

        for (int index = 0; index < data.mPvs.maHullPvs.Count; index++)
        {
            List<Hull> areas = data.mPvs.maHullPvs[index].maHulls;

            if (!selected.Contains(index))
                areas.RemoveAll(item => ReferenceEquals(item, area));
            else if (!areas.Contains(area))
                areas.Add(area);
        }
    }

    public static void CopyCells(Traffic data, int source, IEnumerable<int> destinations, bool replace)
    {
        foreach ((int index, List<Hull> areas) in PrepareCellCopies(data, source, destinations, replace))
            data.mPvs.maHullPvs[index].maHulls = areas;
    }

    public static void ValidateCopyCells(Traffic data, int source, IEnumerable<int> destinations, bool replace) =>
        PrepareCellCopies(data, source, destinations, replace);

    private static Dictionary<int, List<Hull>> PrepareCellCopies(Traffic data, int source, IEnumerable<int> destinations, bool replace)
    {
        int[] targets = [.. destinations.Distinct().Where(index => index != source)];
        ValidateCells(data, [source, .. targets]);
        List<Hull> original = data.mPvs.maHullPvs[source].maHulls;
        Dictionary<int, List<Hull>> changes = [];

        foreach (int index in targets)
        {
            List<Hull> areas = replace
                ? [.. original]
                : [.. data.mPvs.maHullPvs[index].maHulls.Union(original)];

            if (areas.Count > 8)
                throw new InvalidOperationException($"{Label(data, index)} would exceed the limit of 8 road areas. Choose another cell or replace its existing areas.");

            changes.Add(index, areas);
        }

        return changes;
    }

    private static void ValidateCells(Traffic data, IEnumerable<int> cells)
    {
        foreach (int index in cells)
            if (index < 0 || index >= data.mPvs.maHullPvs.Count)
                throw new ArgumentOutOfRangeException(nameof(cells), "Choose an existing visibility cell.");
    }
}
