using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using static BaseHandlers.Traffic;

namespace BaseHandlers;

public static class TrafficReferences
{
    public const string None = "(none)";

    // Active resource instances for reference resolution
    public static Traffic Data;
    public static AISections AIData;
    public static VFXPropCollection VFXData;
    public static Action<object> Preview;
    public static Action RestorePreview;

    private static List<(string Key, object Value)> Indexed<T>(IEnumerable<T> items) => [.. items.Select((item, i) => (i.ToString(), (object)item))];

    public static List<(string Key, object Value)> Candidates(object owner, string property, Type type)
    {
        if (type == typeof(AISections.AISection))
            return Indexed(AIData?.maSections ?? []);

        if (type == typeof(VFXPropCollection.VFXCoronaTypeData))
            return Indexed(VFXData?.mpCoronaTypeDataTable ?? []);

        Traffic data = Data;

        if (data == null)
            return [];

        if (type == typeof(Hull))
            return Indexed(data.maHulls);

        if (type == typeof(FlowType))
            return Indexed(data.maFlowTypes);

        if (type == typeof(VehicleType))
            return Indexed(data.maVehicleTypes);

        if (type == typeof(VehicleAsset))
            return Indexed(data.maVehicleAssets);

        if (type == typeof(VehicleTraits))
            return Indexed(data.maVehicleTraits);

        if (type == typeof(TrafficLightType))
            return Indexed(data.maTrafficLightTypes);

        if (type == typeof(Section))
        {
            // Neighbors must reside within the same hull
            Hull only = owner is Neighbour
                ? HullOf(data, owner)
                : null;
            return [.. data.maHulls.SelectMany((hull, h) => only != null && hull != only
                ? []
                : hull.maSections.Select((section, s) => ($"{h}/{s}", (object)section)))];
        }

        if (type == typeof(StopLine))
        {
            return [.. data.maHulls.SelectMany((hull, h) => hull.maSections.SelectMany((section, s) =>
                section.maStopLines.Select((line, k) => ($"{h}/{s}/{k}", (object)line))))];
        }

        if (type == typeof(LaneRung))
        {
            Section rungsOf = owner switch
            {
                Neighbour neighbour when property == nameof(Neighbour.mTheirStartRung) => neighbour.mSection,
                Neighbour neighbour => data.maHulls.SelectMany(h => h.maSections)
                    .FirstOrDefault(s => s.maLeftNeighbours.Contains(neighbour) || s.maRightNeighbours.Contains(neighbour)),
                KillZoneRegion region => region.mSection,
                _ => null
            };
            return rungsOf == null
                ? []
                : Indexed(rungsOf.maRungs);
        }

        Hull hullOf = HullOf(data, owner);

        if (hullOf == null)
            return [];

        if (type == typeof(SectionSpan))
            return Indexed(hullOf.maSectionSpans);

        if (type == typeof(JunctionLogicBox))
            return Indexed(hullOf.maJunctions);

        if (type == typeof(LightTriggerStartData))
            return Indexed(hullOf.maLightTriggerStartData);

        if (type == typeof(TrafficLight))
            return Indexed(hullOf.maTrafficLights);

        return [];
    }

    private static Hull HullOf(Traffic data, object item) => data.maHulls.FirstOrDefault(hull => item switch
    {
        Section section => hull.maSections.Contains(section),
        Neighbour neighbour => hull.maSections.Any(s => s.maLeftNeighbours.Contains(neighbour) || s.maRightNeighbours.Contains(neighbour)),
        JunctionLogicBox junction => hull.maJunctions.Contains(junction),
        TrafficLightController controller => hull.maJunctions.Any(j => j.maTrafficLightControllers.Contains(controller)),
        LightTrigger trigger => hull.maLightTriggers.Contains(trigger),
        _ => false
    });

    public static string Describe(object value) =>
        value.GetType().GetMethod(nameof(ToString), Type.EmptyTypes)?.DeclaringType == typeof(object)
            ? TrafficLabels.Name(value.GetType().Name)
            : value.ToString();

    // Returns "?" if deleted or out of scope
    public static string Key(object value, object owner, string property, Type type) =>
            Candidates(owner, property, type).FirstOrDefault(c => c.Value == value).Key ?? "?";

    public static string Label(object value, object owner, string property, Type type) =>
        value == null
            ? None
            : $"{Key(value, owner, property, type)}: {Describe(value)}";

    // Resolves from key or full display label
    public static object Resolve(object owner, string property, Type type, string text)
    {
        text = text.Trim();

        if (text.Length == 0 || text == None)
            return null;

        string key = text.Split(':')[0].Trim();

        foreach ((string candidateKey, object value) in Candidates(owner, property, type))
            if (candidateKey == key)
                return value;

        throw new FormatException($"There's no {type.Name} {key} that can be used here.");
    }
}

public class TrafficReferenceConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType != typeof(string))
            return base.ConvertTo(context, culture, value, destinationType);

        if (value == null)
            return TrafficReferences.None;

        return context?.PropertyDescriptor is PropertyDescriptor currentProperty
            ? TrafficReferences.Label(value, context.Instance, currentProperty.Name, currentProperty.PropertyType)
            : TrafficReferences.Describe(value);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
        value is string text && context?.PropertyDescriptor is PropertyDescriptor currentProperty
            ? TrafficReferences.Resolve(context.Instance, currentProperty.Name, currentProperty.PropertyType, text)
            : base.ConvertFrom(context, culture, value);

    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => context?.PropertyDescriptor != null;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
        object[] values = [null, .. TrafficReferences.Candidates(context.Instance, context.PropertyDescriptor.Name, context.PropertyDescriptor.PropertyType).Select(c => c.Value)];
        return new(values);
    }
}

// Formats reference lists as comma-separated keys (e.g. "1, 4, 7")
public class TrafficReferenceListConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => false;

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType != typeof(string) || value is not IEnumerable items)
            return base.ConvertTo(context, culture, value, destinationType);

        Type currentType = value.GetType().GetGenericArguments()[0];
        return string.Join("; ", items.Cast<object>().Select(item => TrafficReferences.Label(item, context?.Instance, context?.PropertyDescriptor?.Name, currentType)));
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string text || context?.PropertyDescriptor is not PropertyDescriptor currentProperty)
            return base.ConvertFrom(context, culture, value);

        Type currentType = currentProperty.PropertyType.GetGenericArguments()[0];
        IList list = (IList)Activator.CreateInstance(currentProperty.PropertyType);

        foreach (string key in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            list.Add(TrafficReferences.Resolve(context.Instance, currentProperty.Name, currentType, key) ?? throw new FormatException("A list can't have none in it."));

        return list;
    }
}
