using BundleUtilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace BaseHandlers;

// Converts an object's public properties to and from YAML: block mappings for objects and lists
// of objects, flow sequences for vectors and lists of scalars. Base class properties come first.
internal static class ObjectYaml
{
    public static string Serialize(object value)
    {
        StringBuilder sb = new();
        WriteObject(sb, value, 0, null);
        return sb.ToString();
    }

    // Sets target's properties from the YAML. Properties the YAML leaves out keep their values, and
    // nothing is changed if the YAML is invalid. Throws FormatException naming the line.
    public static void Populate(object target, string yaml)
    {
        if (Yaml.Parse(yaml) is not YamlMappingNode mapping)
            throw Yaml.Error(1, "Expected \"property: value\" lines.");
        Fill(target, mapping);
    }

    // listPrefix ("  - ") puts the first property on the list item's line
    private static void WriteObject(StringBuilder sb, object obj, int indent, string listPrefix)
    {
        string pad = new(' ', indent);
        bool first = true;
        foreach (PropertyInfo property in Properties(obj.GetType()))
        {
            string key = (first && listPrefix != null ? listPrefix : pad) + property.Name + ":";
            first = false;
            WriteValue(sb, key, property.GetValue(obj), indent);
        }

        if (first) // No properties
            sb.Append(listPrefix ?? pad).AppendLine("{}");
    }

    private static void WriteValue(StringBuilder sb, string key, object value, int indent)
    {
        if (TryScalar(value, out string scalar))
            sb.Append(key).Append(' ').AppendLine(scalar);
        else if (value is IList list)
        {
            List<string> scalars = [];
            foreach (object item in list)
            {
                if (!TryScalar(item, out string itemScalar))
                {
                    scalars = null;
                    break;
                }
                scalars.Add(itemScalar);
            }

            if (scalars != null)
                sb.Append(key).Append(" [").Append(string.Join(", ", scalars)).AppendLine("]");
            else
            {
                sb.AppendLine(key);
                foreach (object item in list)
                    WriteObject(sb, item, indent + 4, new string(' ', indent + 2) + "- ");
            }
        }
        else
        {
            sb.AppendLine(key);
            WriteObject(sb, value, indent + 2, null);
        }
    }

    private static bool TryScalar(object value, out string text)
    {
        text = value switch
        {
            null => "null",
            string s => Yaml.Quote(s),
            bool b => b ? "true" : "false",
            float f => Yaml.Float(f),
            double d => Yaml.Float(d),
            Enum e => e.ToString(),
            CgsID id => "0x" + id.ID.ToString("X16"),
            Vector4 v => $"[{Yaml.Float(v.X)}, {Yaml.Float(v.Y)}, {Yaml.Float(v.Z)}, {Yaml.Float(v.W)}]",
            IFormattable number when value.GetType().IsPrimitive => number.ToString(null, CultureInfo.InvariantCulture),
            _ => null
        };
        return text != null;
    }

    // Converts every value before setting any, so a bad value leaves target untouched
    private static void Fill(object target, YamlMappingNode mapping)
    {
        Dictionary<string, PropertyInfo> properties = Properties(target.GetType()).Where(p => p.CanWrite).ToDictionary(p => p.Name);
        List<(PropertyInfo Property, object Value)> values = [];
        foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
        {
            string key = (entry.Key as YamlScalarNode)?.Value;
            if (key == null || !properties.TryGetValue(key, out PropertyInfo property))
                throw Yaml.Error(Yaml.Line(entry.Key), $"{target.GetType().Name} has no property \"{key}\".");
            values.Add((property, Convert(property.PropertyType, entry.Value, key)));
        }

        foreach ((PropertyInfo property, object value) in values)
            property.SetValue(target, value);
    }

    private static object Convert(Type type, YamlNode node, string name)
    {
        if (type == typeof(Vector4))
        {
            if (node is not YamlSequenceNode { Children.Count: 4 } v || v.Children.Any(i => i is not YamlScalarNode))
                throw Yaml.Error(Yaml.Line(node), $"{name} should be [x, y, z, w].");
            float F(int i) => (float)Yaml.ParseFloat((YamlScalarNode)v.Children[i]);
            return new Vector4(F(0), F(1), F(2), F(3));
        }

        if (typeof(IList).IsAssignableFrom(type))
        {
            IList list = (IList)Activator.CreateInstance(type);
            if (node is YamlScalarNode empty && Yaml.IsNull(empty))
                return list;
            if (node is not YamlSequenceNode sequence)
                throw Yaml.Error(Yaml.Line(node), $"{name} should be a list.");
            Type itemType = type.GetGenericArguments()[0];
            foreach (YamlNode item in sequence.Children)
                list.Add(Convert(itemType, item, name));
            return list;
        }

        if (node is YamlScalarNode scalar)
            return ConvertScalar(type, scalar, name);

        if (node is not YamlMappingNode mapping || type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(CgsID))
            throw Yaml.Error(Yaml.Line(node), $"{name} should be a single value.");
        object obj = Activator.CreateInstance(type);
        Fill(obj, mapping);
        return obj;
    }

    private static object ConvertScalar(Type type, YamlScalarNode scalar, string name)
    {
        int line = Yaml.Line(scalar);
        if (type == typeof(string))
            return Yaml.IsNull(scalar) ? null : scalar.Value;
        if (Yaml.IsNull(scalar))
            throw Yaml.Error(line, $"{name} needs a value.");

        if (type == typeof(CgsID))
            return new CgsID { ID = Yaml.ParseUnsigned(scalar) };
        if (type == typeof(float))
            return (float)Yaml.ParseFloat(scalar);
        if (type == typeof(double))
            return Yaml.ParseFloat(scalar);
        if (type == typeof(bool))
        {
            if (!bool.TryParse(scalar.Value, out bool b))
                throw Yaml.Error(line, $"{name} should be true or false.");
            return b;
        }
        if (type.IsEnum)
        {
            if (!Enum.TryParse(type, scalar.Value, false, out object value))
                throw Yaml.Error(line, $"\"{scalar.Value}\" isn't a {type.Name}. Use one of: {string.Join(", ", Enum.GetNames(type))}.");
            return value;
        }
        if (type.IsPrimitive)
        {
            try
            {
                if (scalar.Value.StartsWith('-'))
                {
                    if (!long.TryParse(scalar.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long signed))
                        throw Yaml.Error(line, $"\"{scalar.Value}\" is not a whole number.");
                    return System.Convert.ChangeType(signed, type, CultureInfo.InvariantCulture);
                }
                return System.Convert.ChangeType(Yaml.ParseUnsigned(scalar), type, CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                throw Yaml.Error(line, $"{scalar.Value} is out of range for {name}.");
            }
        }
        throw Yaml.Error(line, $"{name} should be a list of properties, not a single value.");
    }

    private static IEnumerable<PropertyInfo> Properties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .OrderBy(p => Depth(p.DeclaringType));
    }

    private static int Depth(Type type)
    {
        int depth = 0;
        while ((type = type.BaseType) != null)
            depth++;
        return depth;
    }
}
