using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using BundleFormat;
using static BaseHandlers.Traffic;

namespace BaseHandlers;

public static class TrafficRules
{
    public sealed record Issue(object Item, string Property, string Message)
    {
        public override string ToString() => Message;
    }

    public static int CollectionLimit(object owner, string property) => (owner, property) switch
    {
        (PVSCell, _) => 8,
        (Hull, "maVehicleAssets") => 16,
        (TrafficLightController, "maTrafficLights") => 2,
        (TrafficLightController, "maStopLines") => 6,
        (LightTriggerStartData, "maStartingPositions") => 8,
        (LightTriggerStartData, "maDestinations") => 16,
        (JunctionLogicBox, "maStates") => 16,
        (JunctionLogicBox, "maTrafficLightControllers") => 8,
        (Traffic, "maHulls" or "maFlowTypes" or "maVehicleTypes" or "maKillZones" or "maTrafficLightTypes") => 65535,
        (PVS, _) => int.MaxValue,
        _ => 255
    };

    private static bool Required(object owner, string property) => (owner, property) switch
    {
        (Section, "mSpan") or (VehicleType, "mAsset" or "mTraits") or (FlowVehicleType, "mVehicleType") or
        (Neighbour, "mSection" or "mOurStartRung" or "mTheirStartRung") or
        (KillZoneRegion, "mSection" or "mStartRung" or "mEndRung") or (TrafficLight, "mType") or (LightTrigger, "mJunction") => true,
        _ => false
    };

    public static List<Issue> Validate(Traffic data, BundleEntry entry)
    {
        List<Issue> issues = [];
        List<ObjectGraph.Node> nodes = ObjectGraph.Nodes(data);
        HashSet<object> owned = new(nodes.Select(n => n.Value), ReferenceEqualityComparer.Instance);
        Dictionary<object, Hull> areas = new(ReferenceEqualityComparer.Instance);
        Dictionary<LaneRung, Section> lanes = [];
        Dictionary<Neighbour, Section> neighbours = [];

        foreach (Hull hull in data.maHulls)
        {
            foreach (ObjectGraph.Node node in ObjectGraph.Nodes(hull))
                areas.TryAdd(node.Value, hull);

            foreach (Section section in hull.maSections)
            {
                foreach (LaneRung rung in section.maRungs)
                    lanes.TryAdd(rung, section);

                foreach (Neighbour neighbour in section.maLeftNeighbours.Concat(section.maRightNeighbours))
                    neighbours.TryAdd(neighbour, section);
            }
        }

        bool validReference(object owner, string property, object target)
        {
            if (target == null || !owned.Contains(target))
                return false;

            if (target is LaneRung rung)
            {
                Section expected = owner switch
                {
                    Neighbour neighbour when property == nameof(Neighbour.mTheirStartRung) => neighbour.mSection,
                    Neighbour neighbour => neighbours.GetValueOrDefault(neighbour),
                    KillZoneRegion region => region.mSection,
                    _ => null
                };
                return expected != null && lanes.GetValueOrDefault(rung) == expected;
            }

            if (target is SectionSpan or JunctionLogicBox or LightTriggerStartData or TrafficLight || target is Section && owner is Neighbour)
                return areas.TryGetValue(owner, out Hull area) && areas.GetValueOrDefault(target) == area;

            return true;
        }

        foreach (ObjectGraph.Node node in nodes.Where(n => n.Value is not IList))
        {
            object currentOwner = node.Value;

            foreach (PropertyInfo currentProperty in ObjectGraph.Properties(currentOwner))
            {
                object value = currentProperty.GetValue(currentOwner);

                if (value is IList list && value is not Array && list.Count > CollectionLimit(currentOwner, currentProperty.Name))
                    issues.Add(new(currentOwner, currentProperty.Name, $"{node.Label} / {TrafficLabels.Name(currentProperty.Name)}: maximum {CollectionLimit(currentOwner, currentProperty.Name)} items; currently {list.Count}."));

                bool reference = currentProperty.GetCustomAttributes(typeof(TypeConverterAttribute), true).Cast<TypeConverterAttribute>()
                                    .Any(a => a.ConverterTypeName.Contains(nameof(TrafficReferenceConverter)) || a.ConverterTypeName.Contains(nameof(TrafficReferenceListConverter)));

                if (!reference)
                    continue;

                if (value == null)
                {
                    if (Required(currentOwner, currentProperty.Name))
                        issues.Add(new(currentOwner, currentProperty.Name, node.Label + " / " + TrafficLabels.Name(currentProperty.Name) + ": choose an item."));

                    continue;
                }

                foreach (object currentTarget in value is IList references
                    ? references.Cast<object>()
                    : [value])
                    if (!validReference(currentOwner, currentProperty.Name, currentTarget))
                        issues.Add(new(currentOwner, currentProperty.Name, node.Label + " / " + TrafficLabels.Name(currentProperty.Name) + ": choose a valid reference."));
            }

            if (currentOwner is FlowType flow)
            {
                int last = 0;

                foreach (FlowVehicleType item in flow.maVehicleTypes)
                {
                    if (item.muCumulativeProb < last)
                    {
                        issues.Add(new(flow, "VehicleMix", node.Label + ": edit Vehicle mix to repair the chances."));
                        break;
                    }

                    last = item.muCumulativeProb;
                }
            }

            if (currentOwner is LaneRung rung && rung.maPoints?.Length != 2)
                issues.Add(new(rung, "maPoints", node.Label + ": a lane point needs exactly two endpoints."));
        }

        if ((long)data.mPvs.muNumCells_X * data.mPvs.muNumCells_Z != data.mPvs.maHullPvs.Count)
            issues.Add(new(data.mPvs, "muNumCells_X", "Visibility grid: cell count must equal cells across X × cells across Z."));

        if (issues.Count == 0)
        {
            try
            {
                data.Serialize(entry);
            }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentException or OverflowException)
            {
                issues.Add(new(data, null, ex.Message));
            }
        }

        return issues;
    }
}
