using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static BaseHandlers.Traffic;

namespace BaseHandlers;

public sealed class TrafficCopy
{
    private readonly Traffic data;
    private readonly Hull sourceArea;
    private readonly object selectedCopy;
    private readonly bool areaSelection;
    private bool committed;

    public Hull Contents { get; }

    public TrafficTranslation Translation { get; }

    public Hull SourceArea => sourceArea;

    public bool IsAreaCopy => areaSelection;

    public static bool CanCopy(object selection) => selection switch
    {
        Hull or Section or JunctionLogicBox or LightTrigger or LightTriggerStartData or TrafficLight or StaticTrafficVehicle => true,
        IList list => list.Count > 0 && list.Cast<object>().All(item => item is Section or JunctionLogicBox or LightTrigger or LightTriggerStartData or TrafficLight or StaticTrafficVehicle),
        _ => false
    };

    public static Hull AreaOf(Traffic data, object selection) =>
        data.maHulls.FirstOrDefault(hull => ReferenceEquals(hull, selection) || ObjectGraph.Nodes(hull).Any(node => ReferenceEquals(node.Value, selection)));

    public TrafficCopy(Traffic initialData, object selection)
    {
        data = initialData;
        sourceArea = AreaOf(initialData, selection);

        if (!CanCopy(selection) || sourceArea == null)
            throw new ArgumentException("Select a road area, road section, light, trigger, start layout or static vehicle to duplicate.");

        areaSelection = selection is Hull;
        Hull package = areaSelection
            ? sourceArea
            : new();
        object[] items = selection is IList list
            ? [.. list.Cast<object>()]
            : [selection];

        if (!areaSelection)
        {
            foreach (object item in items)
                Add(item);

            Dictionary<StopLine, Section> stopOwners = [];

            foreach (Section section in data.maHulls.SelectMany(hull => hull.maSections))
                foreach (StopLine stop in section.maStopLines)
                    stopOwners.TryAdd(stop, section);

            foreach (JunctionLogicBox junction in package.maJunctions)
            {
                Add(junction.mOfflineStartData);
                Add(junction.mOnlineStartData);
                Add(junction.mBikeStartData);

                foreach (TrafficLightController controller in junction.maTrafficLightControllers)
                {
                    foreach (TrafficLight light in controller.maTrafficLights)
                        Add(light);

                    foreach (StopLine stop in controller.maStopLines)
                        if (stopOwners.TryGetValue(stop, out Section owner))
                            Add(owner);
                }
            }

            foreach (Section section in package.maSections)
                if (section.mSpan != null && !package.maSectionSpans.Contains(section.mSpan))
                    package.maSectionSpans.Add(section.mSpan);

            HashSet<FlowType> visited = [];

            foreach (FlowType flow in package.maSections.Select(section => section.mFlowType).Concat(package.maStaticTrafficVehicles.Select(vehicle => vehicle.mFlowType)))
                AddAssets(flow);

            void AddAssets(FlowType flow)
            {
                if (flow == null || !visited.Add(flow))
                    return;

                foreach (FlowVehicleType item in flow.maVehicleTypes)
                {
                    VehicleType vehicle = item.mVehicleType;

                    if (vehicle?.mAsset != null && !package.maVehicleAssets.Contains(vehicle.mAsset))
                        package.maVehicleAssets.Add(vehicle.mAsset);

                    AddAssets(vehicle?.mTrailerFlowType);
                }
            }
        }

        Contents = (Hull)ObjectTreeEditor.Clone(package);
        selectedCopy = selection switch
        {
            Section => Contents.maSections[0],
            JunctionLogicBox => Contents.maJunctions[0],
            LightTrigger => Contents.maLightTriggers[0],
            LightTriggerStartData => Contents.maLightTriggerStartData[0],
            TrafficLight => Contents.maTrafficLights[0],
            StaticTrafficVehicle => Contents.maStaticTrafficVehicles[0],
            IList => FirstCopied(items[0]),
            _ => Contents
        };
        Translation = new(Contents);

        void Add(object item)
        {
            switch (item)
            {
                case Section section when !package.maSections.Contains(section):
                    package.maSections.Add(section);
                    break;
                case JunctionLogicBox junction when !package.maJunctions.Contains(junction):
                    package.maJunctions.Add(junction);
                    break;
                case LightTrigger trigger when !package.maLightTriggers.Contains(trigger):
                    package.maLightTriggers.Add(trigger);
                    Add(trigger.mJunction);
                    break;
                case LightTriggerStartData start when !package.maLightTriggerStartData.Contains(start):
                    package.maLightTriggerStartData.Add(start);
                    break;
                case TrafficLight light when !package.maTrafficLights.Contains(light):
                    package.maTrafficLights.Add(light);
                    break;
                case StaticTrafficVehicle vehicle when !package.maStaticTrafficVehicles.Contains(vehicle):
                    package.maStaticTrafficVehicles.Add(vehicle);
                    break;
            }
        }
    }

    private object FirstCopied(object source) => source switch
    {
        Section => Contents.maSections[0],
        JunctionLogicBox => Contents.maJunctions[0],
        LightTrigger => Contents.maLightTriggers[0],
        LightTriggerStartData => Contents.maLightTriggerStartData[0],
        TrafficLight => Contents.maTrafficLights[0],
        StaticTrafficVehicle => Contents.maStaticTrafficVehicles[0],
        _ => Contents
    };

    public void Validate(Hull destination, IEnumerable<int> cells, bool keepConnections)
    {
        if (committed)
            throw new InvalidOperationException("This copy has already been inserted.");

        if (destination != null && !data.maHulls.Contains(destination))
            throw new InvalidOperationException("Choose a road area from this traffic resource.");

        if (destination == null && data.maHulls.Count >= TrafficRules.CollectionLimit(data, nameof(Traffic.maHulls)))
            throw new InvalidOperationException("The traffic resource cannot contain another road area.");

        if (keepConnections && !ReferenceEquals(destination, sourceArea))
            throw new InvalidOperationException("Connections to existing roads can be kept only when copying within the same road area.");

        foreach (PropertyInfo property in ObjectGraph.Properties(Contents))
        {
            if (property.GetValue(Contents) is not IList additions)
                continue;

            IList existing = destination == null
                ? null
                : (IList)property.GetValue(destination);
            int count = property.Name == nameof(Hull.maVehicleAssets) && destination != null
                ? (existing?.Cast<object>() ?? []).Union(additions.Cast<object>()).Count()
                : (existing?.Count ?? 0) + additions.Count;
            int limit = TrafficRules.CollectionLimit(Contents, property.Name);

            if (count > limit)
                throw new InvalidOperationException($"The destination would contain {count} {TrafficLabels.Name(property.Name).ToLowerInvariant()}; the limit is {limit}.");
        }

        TrafficVisibility.ValidateArea(data, destination ?? Contents, cells);
    }

    public object Commit(Hull destination, IEnumerable<int> selectedCells, bool keepConnections)
    {
        int[] cells = [.. selectedCells.Distinct()];
        Validate(destination, cells, keepConnections);

        if (!keepConnections)
        {
            HashSet<Section> copied = [.. Contents.maSections];

            foreach (Section section in Contents.maSections)
            {
                section.Forward = [.. section.Forward.Select(target => copied.Contains(target)
                    ? target
                    : null)];
                section.Backward = [.. section.Backward.Select(target => copied.Contains(target)
                    ? target
                    : null)];
                section.maLeftNeighbours.RemoveAll(neighbour => !copied.Contains(neighbour.mSection));
                section.maRightNeighbours.RemoveAll(neighbour => !copied.Contains(neighbour.mSection));
            }
        }

        HashSet<uint> junctionIDs = [.. data.maHulls.SelectMany(area => area.maJunctions).Select(junction => junction.muID)];
        HashSet<uint> lightIDs = [.. data.maHulls.SelectMany(area => area.maTrafficLights).Select(light => light.muInstanceID)];
        uint junctionID = junctionIDs.DefaultIfEmpty(0u).Max(), lightID = lightIDs.DefaultIfEmpty(0u).Max();

        foreach (JunctionLogicBox junction in Contents.maJunctions)
            junction.muID = NextID(junctionIDs, ref junctionID);

        foreach (TrafficLight light in Contents.maTrafficLights)
            light.muInstanceID = NextID(lightIDs, ref lightID);

        Hull targetArea = destination ?? Contents;

        if (destination == null)
            data.maHulls.Add(Contents);
        else
            foreach (PropertyInfo property in ObjectGraph.Properties(Contents))
                if (property.GetValue(Contents) is IList additions && property.GetValue(destination) is IList existing)
                    foreach (object item in additions)
                        if (property.Name != nameof(Hull.maVehicleAssets) || !existing.Contains(item))
                            existing.Add(item);

        TrafficVisibility.SetArea(data, targetArea, cells);
        committed = true;
        return areaSelection
            ? targetArea
            : selectedCopy;
    }

    private static uint NextID(HashSet<uint> used, ref uint value)
    {
        do
        {
            value = value == uint.MaxValue
                ? 1
                : value + 1;
        }
        while (!used.Add(value));

        return value;
    }
}
