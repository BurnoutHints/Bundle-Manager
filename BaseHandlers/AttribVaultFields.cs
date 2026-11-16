using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using BundleFormat;

namespace BaseHandlers;

internal static class AttribFields
{
    public const string Text = "EA::Reflection::Text", RefSpec = "Attrib::RefSpec";

    public static int? ArrayData(AttribSchema.Field field) => field.IsArray
        ? Math.Max(8, 1 << field.Align)
        : null;

    public static IEnumerable<PropertyDescriptor> For(AttribCollection c)
    {
        yield return new AttribProperty("ID", "ID", "Collection", "The collection's name: a GameDB node ID when its key hashes back to one, otherwise its key.", typeof(string), () => c.ID, null);
        yield return new AttribProperty("Key", "Key", "Collection", "Lookup8 hash of the collection's name.", typeof(ulong), () => c.Key, null, new HexConverter());
        yield return new AttribProperty("Parent", "Parent", "Collection", "Attributes this collection doesn't have come from its parent.", typeof(AttribCollection),
            () => Reference(c.Vault, c.Class, c.ParentKey), null, new AttribRefConverter(c.Vault, () => c.Class));

        if (c.Schema == null)
        {
            if (c.LayoutSlot >= 0)
                yield return Raw(c, "Layout", "Fields", "This class's layout isn't in the schema, so it's shown as bytes.", 0, c.Segments[0].Length);
        }
        else if (PropMappings(c) is PropertyDescriptor mappings)
            yield return mappings;
        else
            foreach (AttribSchema.Field field in c.Schema.Fields.Where(f => f.InLayout))
                yield return Make(c, field, field.Offset, 1, c.LayoutSlot + field.Offset, "Fields");

        for (int i = 0; i < c.Items.Count; i++)
        {
            AttribCollection.Item item = c.Items[i];
            int length = (i + 1 < c.Items.Count
                ? c.Items[i + 1].At
                : c.Bytes.Length) - item.At;
            yield return c.FieldOf(item) is AttribSchema.Field field
                ? Make(c, field, item.At, item.Base, item.Slot, "Attributes")
                : Raw(c, $"0x{item.Key:X16}", "Attributes", "An attribute the schema doesn't know, shown as bytes.", item.At, length);
        }
    }

    public static IEnumerable<string> Uses(AttribCollection c, object target)
    {
        static bool refers(object owner, PropertyDescriptor property, object target) => property.GetValue(owner) switch
        {
            AttribView view => view.Children.Any(child => refers(view, child, target)),
            object value => value == target,
            null => false
        };

        return For(c).Where(p => refers(c, p, target)).Select(p => p.Name);
    }

    private static object Reference(AttribVault vault, ulong classKey, ulong key) => key == 0
        ? null
        : (object)vault.Find(classKey, key) ?? $"0x{key:X16} (not in this vault)";

    private static string Describe(AttribSchema.Field field) =>
        $"{field.Label}: {field.Type}{(field.IsArray
            ? $"[{field.Max}]"
            : "")}{(field.InLayout
        ? $", at 0x{field.Offset:X} in the layout"
        : "")}.";

    private static PropertyDescriptor Make(AttribCollection c, AttribSchema.Field field, int at, int @base, long slot, string category)
    {
        string name = field.Label, display = field.Name == null
            ? name
            : TrafficLabels.Name(field.Name);

        if (ArrayData(field) is not int data)
            return Value(c, field, at, @base, slot, name, display, category, Describe(field));

        if (at + 8 > c.Bytes.Length)
            return Info(name, display, category, "(doesn't fit)");

        AttribSchema.Field element = field with
        {
            Flags = field.Flags & ~1
        };

        int capacity = (int)c.Get(at, 2), size = Math.Max(1, (int)c.Get(at + 4, 2));
        int fits = Math.Min(capacity, (c.Bytes.Length - at - data) / size);
        AttribProperty count = new("Count", "Count", null, $"How many items are in use, up to {fits}.", typeof(ushort), () => (ushort)c.Get(at + 2, 2), value =>
        {
            ushort n = Convert.ToUInt16(value, CultureInfo.InvariantCulture);

            if (n > fits)
                throw new ArgumentOutOfRangeException(null, $"At most {fits} items fit here.");

            c.Set(at + 2, 2, n);
        });

        AttribView view()
        {
            PropertyDescriptor[] items = [.. Enumerable.Range(0, Math.Min((int)c.Get(at + 2, 2), fits))
                .Select(i => Value(c, element, at + data + i * size, @base, slot + data + i * size, $"[{i}]", $"[{i}]", null, Describe(field)))];
            return new(() => Summary(items, c, 8), [count, .. items], items.All(i => i.PropertyType.IsPrimitive));
        }

        return new AttribProperty(name, display, category, Describe(field) + " Expand it to change the items or how many there are.", typeof(AttribView), view, _ => { });
    }

    // The prop material table is parallel arrays (prop IDs, material indices) plus a count, so show it as one list of props
    private static PropertyDescriptor PropMappings(AttribCollection c)
    {
        AttribSchema.Field field(string name) => c.Schema.Fields.FirstOrDefault(f => f.Name == name && f.InLayout && f.IsArray == (name != "MappingCount"));

        if (c.ClassName != "proptomaterialmappings" || field("CgsIds") is not { Size: 8 } ids || field("MappingCount") is not { Size: 4 } count || field("MaterialIndices") is not { Size: 1 } materials)
            return null;

        // The game asserts there are fewer than 500
        int idsAt = ids.Offset + ArrayData(ids).Value, materialsAt = materials.Offset + ArrayData(materials).Value, most = Math.Min(499, Math.Min(ids.Max, materials.Max));
        AttribProperty number = new("Count", "Count", null, $"How many props are mapped, up to {most}. Raising it adds rows at the end; lowering it drops the last ones.", typeof(int),
            () => (int)c.Get(count.Offset, 4), value =>
            {
                int n = Convert.ToInt32(value, CultureInfo.InvariantCulture);

                if (n < 0 || n > most)
                    throw new ArgumentOutOfRangeException(null, $"Between 0 and {most} props fit.");

                c.Set(count.Offset, 4, (uint)n);
            });

        AttribView view()
        {
            Dictionary<string, string> names = MaterialNames(c.Vault);
            PropertyDescriptor[] rows = [.. Enumerable.Range(0, Math.Clamp((int)c.Get(count.Offset, 4), 0, most)).Select(i =>
            {
                ulong id = c.Get(idsAt + i * 8, 8);
                string prop = AttribIDConverter.ShortName(id);
                PropertyDescriptor material = Scalar(c, typeof(int), materialsAt + i, 1, "Material", "Material", null, "The prop material (named in the props crash bin list). Props crash bins choose sounds by it.", names);
                PropertyDescriptor[] parts = [Scalar(c, typeof(ulong), idsAt + i * 8, 8, "Prop", "Prop", null, "The prop's PropScene resource ID, as PropPhysics lists it. 0 maps nothing."), material];
                AttribProperty row = new($"[{i}]", prop ?? (id == 0
                    ? $"[{i}] (none)"
                    : $"0x{id:X}"), null, "Pick this prop's material, or expand it to change the prop.", typeof(AttribView),
                    () => new AttribView(() => material.Converter.ConvertToString(material.GetValue(null)), parts, false, material), value =>
                    {
                        if (value is not AttribView)
                            material.SetValue(null, value);
                    });
                return (row, named: prop != null);
            }).OrderBy(r => !r.named).ThenBy(r => r.row.DisplayName, StringComparer.OrdinalIgnoreCase).Select(r => r.row)];
            return new(() => $"{rows.Length} props", [number, .. rows], false);
        }

        return new AttribProperty("Mappings", "Mappings", "Fields", "Which material each prop uses for crash sounds. Expand it, then pick a material next to a prop; raise Count to add a prop.",
            typeof(AttribView), view, _ => { });
    }

    // Prop material index -> name, from the vault's props crash bin list
    private static Dictionary<string, string> MaterialNames(AttribVault vault)
    {
        AttribCollection list = vault.Collections.FirstOrDefault(x => x.ClassName == "propscrashbinlist");
        return list != null && For(list).FirstOrDefault(p => p.Name == "mMaterialNames")?.GetValue(list) is AttribView names
            ? names.Children.Skip(1).Select((p, i) => (i, text: p.GetValue(names) as string)).Where(n => !string.IsNullOrEmpty(n.text))
                .ToDictionary(n => n.i.ToString(CultureInfo.InvariantCulture), n => n.text)
            : [];
    }

    private static string Summary(PropertyDescriptor[] items, object owner, int most) => items.Length == 0
        ? "None"
        : string.Join(", ", items.Take(most).Select(i => i.Converter.ConvertToString(null, CultureInfo.CurrentCulture, i.GetValue(owner)))) + (items.Length > most
            ? $", … ({items.Length})"
            : "");

    private static PropertyDescriptor Value(AttribCollection c, AttribSchema.Field field, int at, int @base, long slot, string name, string display, string category, string description)
    {
        string type = field.Type;
        int size = field.Size;

        if (at + size > c.Bytes.Length)
            return Info(name, display, category, "(doesn't fit)");

        // 64-bit builds have 8-byte string pointers
        if (type == Text && size is 4 or 8)
        {
            int index = c.TextIndex(@base, slot);
            return new AttribProperty(name, display, category, description, typeof(string), () => index < 0
                ? null
                : c.Texts[index],
                index < 0 || c.TextSlots[index].Original == null
                    ? null
                    : value => c.Texts[index] = (string)value ?? "");
        }

        if (type == RefSpec && size == 24)
            return new AttribProperty(name, display, category, description + " Choose a collection, or Go to reference.", typeof(AttribCollection),
                () => Reference(c.Vault, c.Get(at, 8), c.Get(at + 8, 8)), value =>
                {
                    AttribCollection target = value as AttribCollection;
                    c.Set(at, 8, target?.Class ?? 0);
                    c.Set(at + 8, 8, target?.Key ?? 0);
                }, new AttribRefConverter(c.Vault, () => c.Get(at, 8)));

        if (type == "Attrib::Types::Matrix" && size == 64)
        {
            AttribSchema.Field row = field with
            {
                Type = "Attrib::Types::Vector4",
                Size = 16
            };

            PropertyDescriptor[] rows = [.. Enumerable.Range(0, 4).Select(i => Value(c, row, at + i * 16, @base, slot + i * 16, $"Row{i + 1}", $"Row {i + 1}", null, description))];
            return new AttribProperty(name, display, category, description, typeof(AttribView), () => new AttribView(() => Summary(rows, c, 4).Replace(", ", " | "), rows, false), _ => { });
        }

        if (Components(type) is string[] parts)
        {
            PropertyDescriptor[] floats = [.. parts.Select((p, i) => Scalar(c, typeof(float), at + i * 4, 4, p, p, null, description))];
            return new AttribProperty(name, display, category, description + " Type the values separated by commas, or expand it.", typeof(AttribView), () => new AttribView(() => Summary(floats, c, 4), floats, true), _ => { });
        }

        if (Scalar(type, size) is Type clr)
            return Scalar(c, clr, at, size, name, display, category, description, type.StartsWith("AttribSys::Enums::")
                ? AttribSchema.Instance.Enums.GetValueOrDefault(type[(type.LastIndexOf(':') + 1)..])
                : null);

        return Raw(c, name, category, description + " Shown as bytes.", at, size);
    }

    private static string[] Components(string type) => type switch
    {
        "Attrib::Types::Vector2" or "Attrib::Types::RwVector2" => ["X", "Y"],
        "Attrib::Types::Vector3" or "Attrib::Types::RwVector3" => ["X", "Y", "Z"],
        "Attrib::Types::Vector4" or "Attrib::Types::Quaternion" => ["X", "Y", "Z", "W"],
        "Attrib::Types::FloatColour" or "Attrib::Types::ShaderColour" => ["R", "G", "B", "A"],
        _ => null
    };

    private static Type Scalar(string type, int size) => (type, size) switch
    {
        ("EA::Reflection::Bool", 1) => typeof(bool),
        ("EA::Reflection::Int8", 1) => typeof(sbyte),
        ("EA::Reflection::UInt8" or "EA::Reflection::Char", 1) => typeof(byte),
        ("EA::Reflection::Int16", 2) => typeof(short),
        ("EA::Reflection::UInt16", 2) => typeof(ushort),
        ("EA::Reflection::Int32", 4) => typeof(int),
        ("EA::Reflection::UInt32" or "Attrib::Types::Symbol" or "Attrib::Types::Colour" or "EA::Reflection::Reference", 4) => typeof(uint),
        ("EA::Reflection::Int64" or "EA::Reflection::UInt64" or "Attrib::Key" or "Attrib::Blob" or "EA::Reflection::Type" or "EA::Reflection::KeyValueAttrib", 8) => typeof(ulong),
        ("EA::Reflection::Float", 4) => typeof(float),
        ("EA::Reflection::Double", 8) => typeof(double),
        (_, 4) when type.StartsWith("AttribSys::Enums::") => typeof(int),
        _ => null
    };

    private static PropertyDescriptor Scalar(AttribCollection c, Type type, int at, int size, string name, string display, string category, string description, Dictionary<string, string> names = null)
    {
        object read()
        {
            ulong bits = c.Get(at, size);
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => bits != 0,
                TypeCode.SByte => (sbyte)bits,
                TypeCode.Byte => (byte)bits,
                TypeCode.Int16 => (short)bits,
                TypeCode.UInt16 => (ushort)bits,
                TypeCode.Int32 => (int)bits,
                TypeCode.UInt32 => (uint)bits,
                TypeCode.Single => BitConverter.Int32BitsToSingle((int)bits),
                TypeCode.Double => BitConverter.Int64BitsToDouble((long)bits),
                _ => bits
            };
        }

        void write(object value) => c.Set(at, size, value switch
        {
            bool b => b
                ? 1UL
                : 0,
            float f => (uint)BitConverter.SingleToInt32Bits(f),
            double d => (ulong)BitConverter.DoubleToInt64Bits(d),
            ulong u => u,
            _ => unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture))
        });

        TypeConverter converter = names != null
            ? new AttribEnumConverter(names)
            : type == typeof(ulong)
                ? new AttribIDConverter()
                : type == typeof(uint)
                    ? new HexConverter()
                    : null;
        return new AttribProperty(name, display, category, description, type, read, write, converter);
    }

    private static PropertyDescriptor Raw(AttribCollection c, string name, string category, string description, int at, int length) =>
        new AttribProperty(name, name, category, description, typeof(string), () => Convert.ToHexString(c.Bytes, at, length), value =>
        {
            byte[] bytes = Convert.FromHexString(((string)value ?? "").Replace(" ", ""));

            if (bytes.Length != length)
                throw new ArgumentException($"Enter exactly {length} bytes as hex.");

            bytes.CopyTo(c.Bytes, at);
        });

    private static PropertyDescriptor Info(string name, string display, string category, string text) =>
        new AttribProperty(name, display, category, text, typeof(string), () => text, null);
}

internal sealed class AttribProperty(string name, string display, string category, string description, Type type, Func<object> get, Action<object> set, TypeConverter converter = null)
    : PropertyDescriptor(name, [new DisplayNameAttribute(display), category == null
        ? CategoryAttribute.Default
        : new CategoryAttribute(category), new DescriptionAttribute(description)])
{
    public override Type ComponentType => typeof(object);

    public override Type PropertyType => type;

    public override bool IsReadOnly => set == null;

    public override TypeConverter Converter => converter ?? base.Converter;

    public override object GetValue(object component) => get();

    public override void SetValue(object component, object value)
    {
        set(value);
        OnValueChanged(component, EventArgs.Empty);
    }

    public override bool CanResetValue(object component) => false;

    public override void ResetValue(object component) { }

    public override bool ShouldSerializeValue(object component) => false;
}

// A value made of other values (vector, matrix, array), expanded to edit its parts in place
[TypeConverter(typeof(AttribViewConverter))]
public sealed class AttribView(Func<string> summary, PropertyDescriptor[] children, bool parsable, PropertyDescriptor primary = null)
{
    internal PropertyDescriptor[] Children => children;

    // Number lists can be typed as "1, 2, 3"
    internal bool Parsable => parsable;

    // The part typed or picked when the view itself is edited (a prop mapping's material)
    internal PropertyDescriptor Primary => primary;

    public override string ToString() => summary();
}

internal sealed class AttribViewConverter : TypeConverter
{
    private static AttribView View(ITypeDescriptorContext context) => context?.PropertyDescriptor?.GetValue(context.Instance) as AttribView;

    public override bool GetPropertiesSupported(ITypeDescriptorContext context) => true;

    public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) =>
        new(((AttribView)value).Children);

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
        sourceType == typeof(string) && View(context) is { Parsable: true } or { Primary: not null };

    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => View(context)?.Primary?.Converter.GetStandardValuesSupported() == true;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => new(View(context)?.Primary?.Converter.GetStandardValues());

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => value switch
    {
        AttribView view when destinationType == typeof(string) => view.ToString(),
        not AttribView and not null when View(context)?.Primary is PropertyDescriptor primary => primary.Converter.ConvertTo(null, culture, value, destinationType),
        _ => base.ConvertTo(context, culture, value, destinationType)
    };

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (View(context)?.Primary is PropertyDescriptor primary)
            return primary.Converter.ConvertFrom(null, culture, value);

        if (value is not string text || View(context) is not AttribView view)
            return base.ConvertFrom(context, culture, value);

        PropertyDescriptor[] items = [.. view.Children.Where(c => c.Name != "Count")];
        string[] parts = text.Split(',', StringSplitOptions.TrimEntries);

        if (parts.Length != items.Length)
            throw new FormatException($"Enter {items.Length} values separated by commas, or expand this to change how many there are.");

        object[] values = [.. items.Select((item, i) => item.Converter.ConvertFromString(null, culture, parts[i]))];

        for (int i = 0; i < items.Length; i++)
            items[i].SetValue(view, values[i]);

        return view;
    }
}

// Picks a collection for a RefSpec, from those of the class it already names (or any, if it's empty)
internal sealed class AttribRefConverter(AttribVault vault, Func<ulong> classKey) : TypeConverter
{
    private const string None = "(none)";

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
        ulong key = classKey();
        return new((object[])[null, .. vault.Collections.Where(c => key == 0 || c.Class == key)]);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
        destinationType == typeof(string)
            ? value switch
            {
                null => None,
                AttribCollection collection => collection.Label,
                _ => value.ToString()
            }
            : base.ConvertTo(context, culture, value, destinationType);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
        value is not string text
            ? base.ConvertFrom(context, culture, value)
            : text is "" or None
                ? null
                : vault.Collections.FirstOrDefault(c => c.Label == text) ?? throw new FormatException($"There's no collection called {text} in this vault.");
}

internal sealed class AttribEnumConverter(Dictionary<string, string> names) : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) =>
        new(names.Keys.Select(k => (object)int.Parse(k, CultureInfo.InvariantCulture)).ToArray());

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
        destinationType == typeof(string) && value is int number
            ? names.GetValueOrDefault(number.ToString(CultureInfo.InvariantCulture)) ?? number.ToString(culture)
            : base.ConvertTo(context, culture, value, destinationType);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
        value is string text
            ? names.FirstOrDefault(n => n.Value == text.Trim()).Key is string key
                ? int.Parse(key, CultureInfo.InvariantCulture)
                : int.Parse(text, culture)
            : base.ConvertFrom(context, culture, value);
}

// 64-bit values are mostly IDs, so show the resource name too when it's known
internal sealed class AttribIDConverter : HexConverter
{
    public static string ShortName(ulong id) => id != 0 && ResourceDB.Lookup(id) is string name
        ? name[(name.LastIndexOf('/') + 1)..].Split('?')[0]
        : null;

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        object text = base.ConvertTo(context, culture, value, destinationType);
        return destinationType == typeof(string) && value is ulong id && ShortName(id) is string name
            ? $"{text} ({name})"
            : text;
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
        base.ConvertFrom(context, culture, value is string text
            ? text.Trim().Split(' ')[0]
            : value);
}
