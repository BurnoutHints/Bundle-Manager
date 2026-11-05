using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using static BaseHandlers.Traffic;

namespace BaseHandlers;

public static class TrafficLabels
{
    private static bool installed;

    private static readonly Dictionary<string, string> names = new()
    {
        ["mPvs"] = "Visibility grid",
        ["maHulls"] = "Road areas",
        ["maSections"] = "Road sections",
        ["maSectionSpans"] = "Traffic capacities",
        ["mSpan"] = "Traffic capacity",
        ["maRungs"] = "Lane points",
        ["maFlowTypes"] = "Vehicle mixes",
        ["mFlowType"] = "Vehicle mix",
        ["maHullPvs"] = "Visibility cells",
        ["maVehicleTypes"] = "Vehicle types",
        ["maJunctions"] = "Junctions",
        ["maLightTriggerStartData"] = "Start layouts",
        ["mOfflineStartData"] = "Offline start layout",
        ["mOnlineStartData"] = "Online start layout",
        ["mBikeStartData"] = "Bike start layout",
        ["muVehiclesPerMinute"] = "Vehicles per minute",
        ["mfSpeed"] = "Speed (game units)",
        ["mfLength"] = "Length (game units)",
        ["muTurnLeftProb"] = "Left turn chance (raw)",
        ["muTurnRightProb"] = "Right turn chance (raw)",
        ["muChangeLeftProb"] = "Change left chance (raw)",
        ["muChangeRightProb"] = "Change right chance (raw)",
        ["mForwardStraightOn"] = "Forward: straight",
        ["mForwardLeft"] = "Forward: left",
        ["mForwardRight"] = "Forward: right",
        ["mBackwardStraightOn"] = "Backward: straight",
        ["mBackwardLeft"] = "Backward: left",
        ["mBackwardRight"] = "Backward: right",
        ["muMaxVehicles"] = "Maximum vehicles",
        ["mfMaxVehicleRecip"] = "Reciprocal vehicle limit",
        ["mVehicleId"] = "Vehicle CgsID",
        ["mOurStartRung"] = "Start point on this section",
        ["mTheirStartRung"] = "Start point on neighbour",
        ["mStartRung"] = "First lane point",
        ["mEndRung"] = "Last lane point",
        ["muParamFixed"] = "Position (fixed point)",
        ["mGridMin"] = "Grid origin",
        ["muNumCells_X"] = "Cells across (X)",
        ["muNumCells_Z"] = "Cells across (Z)",
        ["mRecipCellSize"] = "Reciprocal cell size",
        ["muDataVersion"] = "Data version",
        ["maCoronas"] = "Light glows",
        ["muCumulativeProb"] = "Cumulative chance (raw)",
        ["mExistsAtAllChance"] = "Spawn chance (raw)",
        ["mPosAndYRotation"] = "Position and Y rotation",
        ["mPosPlusYRot"] = "Position and Y rotation",
        ["mID"] = "ID",
        ["muID"] = "ID",
        ["mId"] = "ID"
    };

    private static readonly Dictionary<string, string> help = new()
    {
        ["mfSpeed"] = "Target speed for traffic using this road section, in game units.",
        ["muVehiclesPerMinute"] = "Traffic flow rate for this section, in vehicles per minute.",
        ["mFlowType"] = "The vehicle mix used here. Choose a mix, then use Go to reference to edit its vehicles and chances.",
        ["mSpan"] = "The shared traffic capacity that limits how many vehicles can occupy these sections. Required.",
        ["mVehicleType"] = "Choose the vehicle type from the named list.",
        ["mVehicleId"] = "Decoded vehicle asset ID. Enter a CgsID or a hexadecimal value starting with 0x. Raw ID shows the stored number.",
        ["mAsset"] = "Vehicle resource used by this vehicle type. Required.",
        ["mTraits"] = "Driving behaviour used by this vehicle type. Required.",
        ["mOnlineStartData"] = "Start layout for online events. None uses the offline layout.",
        ["mBikeStartData"] = "Start layout for bike events. Only data version 45 saves this field.",
        ["mPosAndYRotation"] = "X, Y and Z give the position; W gives rotation about the Y axis.",
        ["mPosPlusYRot"] = "X, Y and Z give the position; W gives rotation about the Y axis."
    };

    public static string Name(string raw)
    {
        if (names.TryGetValue(raw, out string name))
            return name;

        string text = Regex.Replace(raw, "^[mM](?:[aufiex]|[au][fi])?(?=[A-Z])", "");
        text = Regex.Replace(text.Replace('_', ' '), "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        return text.Length == 0
            ? raw
            : char.ToUpper(text[0]) + text[1..].ToLowerInvariant().Replace(" id", " ID").Replace(" pvs", " visibility");
    }

    public static void Install()
    {
        if (installed)
            return;

        installed = true;

        foreach (Type currentType in typeof(Traffic).GetNestedTypes().Where(t => t.IsClass).Append(typeof(Traffic)))
            TypeDescriptor.AddProvider(new Provider(TypeDescriptor.GetProvider(currentType)), currentType);
    }

    private sealed class Provider(TypeDescriptionProvider parent) : TypeDescriptionProvider(parent)
    {
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance) => new Descriptor(base.GetTypeDescriptor(objectType, instance), objectType);
    }

    private sealed class Descriptor(ICustomTypeDescriptor parent, Type type) : CustomTypeDescriptor(parent)
    {
        public override PropertyDescriptorCollection GetProperties() => GetProperties(null);

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            List<PropertyDescriptor> properties = [.. base.GetProperties(attributes).Cast<PropertyDescriptor>().Select(p => (PropertyDescriptor)new Field(p))];

            if (type == typeof(FlowType))
                properties.Add(new MixField());

            if (type == typeof(VehicleAsset))
                properties.Add(new RawVehicleIDField());

            return new([.. properties]);
        }
    }

    private sealed class Field(PropertyDescriptor inner) : PropertyDescriptor(inner)
    {
        private bool Chance => ComponentType == typeof(FlowVehicleType) && Name == "muCumulativeProb";

        public override string DisplayName => Chance
            ? "Chance (%)"
            : TrafficLabels.Name(Name);

        public override string Category => Name.Contains("Forward") || Name.Contains("Backward") || Name.Contains("Neighbour") || Name.Contains("Rung")
            ? "Connections"
            : Name.Contains("Flow") || Name.Contains("VehiclesPerMinute") || Name.Contains("Speed") || Chance
                ? "Traffic flow"
                : Name.Contains("Position") || Name.Contains("Points") || Name.Contains("Transform") || Name.Contains("Length") || Name.Contains("Dimensions") || Name.Contains("Rot")
                    ? "Geometry"
                    : Name.Contains("flag") || Name.Contains("Recip") || Name.Contains("Fixed") || Name.Contains("Prob") || Name.Contains("Chance")
                        ? "Advanced"
                        : "Properties";

        public override string Description => Chance
            ? "Individual share of this vehicle mix. Select the parent mix and edit Vehicle mix to change the chances."
            : (help.GetValueOrDefault(Name) ?? (Name.Contains("Prob") || Name.Contains("Chance")
                ? "Stored chance value (0–255). Its engine scale is not documented here."
                : inner.Description.StartsWith("From ")
                    ? "Edit " + DisplayName.ToLowerInvariant() + "."
                    : inner.Description)) + $"\nStored field: {Name}.";

        public override bool IsReadOnly => Chance || inner.IsReadOnly;

        public override Type ComponentType => inner.ComponentType;

        public override Type PropertyType => Chance
            ? typeof(decimal)
            : inner.PropertyType;

        public override TypeConverter Converter => Chance
            ? new DecimalConverter()
            : inner.PropertyType.IsEnum
                ? new FriendlyEnumConverter(inner.PropertyType)
                : inner.Converter;

        public override object GetEditor(Type editorBaseType) => editorBaseType == typeof(UITypeEditor) &&
                    (inner.Converter is TrafficReferenceConverter or TrafficReferenceListConverter)
            ? new TrafficReferenceEditor()
            : inner.GetEditor(editorBaseType);

        public override bool CanResetValue(object component) => inner.CanResetValue(component);

        public override object GetValue(object component)
        {
            if (!Chance)
                return inner.GetValue(component);

            FlowVehicleType item = (FlowVehicleType)component;
            FlowType flow = TrafficReferences.Data?.maFlowTypes.FirstOrDefault(f => f.maVehicleTypes.Contains(item));
            int index = flow?.maVehicleTypes.IndexOf(item) ?? -1;
            return Math.Round((item.muCumulativeProb - (index > 0
                ? flow.maVehicleTypes[index - 1].muCumulativeProb
                : 0)) * 100m / 255, 2);
        }

        public override void SetValue(object component, object value) => inner.SetValue(component, value);

        public override void ResetValue(object component) => inner.ResetValue(component);

        public override bool ShouldSerializeValue(object component) => inner.ShouldSerializeValue(component);
    }

    private sealed class RawVehicleIDField() : PropertyDescriptor("RawVehicleID", [])
    {
        public override string DisplayName => "Raw ID";

        public override string Category => "Advanced";

        public override string Description => "Stored 64-bit vehicle asset ID. Edit Vehicle CgsID to change it.";

        public override Type ComponentType => typeof(VehicleAsset);

        public override Type PropertyType => typeof(ulong);

        public override bool IsReadOnly => true;

        public override TypeConverter Converter => new HexConverter();

        public override object GetValue(object component) => ((VehicleAsset)component).mVehicleId;

        public override void SetValue(object component, object value) { }

        public override bool CanResetValue(object component) => false;

        public override void ResetValue(object component) { }

        public override bool ShouldSerializeValue(object component) => false;
    }

    private sealed class MixField() : PropertyDescriptor("VehicleMix", [new EditorAttribute(typeof(TrafficMixEditor), typeof(UITypeEditor))])
    {
        public override string DisplayName => "Vehicle mix…";

        public override string Category => "Traffic flow";

        public override string Description => "Choose vehicles and their individual chances. The editor calculates cumulative values for the game.";

        public override Type ComponentType => typeof(FlowType);

        public override Type PropertyType => typeof(List<FlowVehicleType>);

        public override bool IsReadOnly => false;

        public override TypeConverter Converter => new MixConverter();

        public override object GetValue(object component) => ((FlowType)component).maVehicleTypes;

        public override void SetValue(object component, object value) => ((FlowType)component).maVehicleTypes = (List<FlowVehicleType>)value;

        public override bool CanResetValue(object component) => false;

        public override void ResetValue(object component)
        {
        }

        public override bool ShouldSerializeValue(object component) => false;
    }

    private sealed class MixConverter : TypeConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
            destinationType == typeof(string) && value is List<FlowVehicleType> items
                ? $"{items.Count} vehicle types"
                : base.ConvertTo(context, culture, value, destinationType);
    }

    private sealed class FriendlyEnumConverter(Type type) : EnumConverter(type)
    {
        private string Label(object value) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToString().Split('_').Last().ToLowerInvariant());

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => destinationType == typeof(string) && value != null
            ? Label(value)
            : base.ConvertTo(context, culture, value, destinationType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value is string text
            ? Enum.GetValues(EnumType).Cast<object>().FirstOrDefault(v => string.Equals(Label(v), text, StringComparison.OrdinalIgnoreCase)) ?? base.ConvertFrom(context, culture, value)
            : base.ConvertFrom(context, culture, value);
    }
}
