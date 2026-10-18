using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace BundleUtilities;

// Writes YAML scalars, and reads documents with YamlDotNet. Read errors are FormatExceptions that
// start with the line number.
public static class Yaml
{
    public static string Quote(string value)
    {
        StringBuilder sb = new("\"");
        foreach (char c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\r': sb.Append("\\r"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    // Line and paragraph separators and a BOM must not appear raw either
                    if (char.IsControl(c) || c is (char)0x2028 or (char)0x2029 or (char)0xFEFF)
                        sb.Append("\\u").Append(((int)c).ToString("X4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }

    // Plain when no YAML reader could take it for another type (YAML 1.1 reads 0x10 as a number and
    // yes/no/on/off as booleans), otherwise quoted
    public static string Key(string key)
    {
        bool plain = key.Length != 0 && char.IsAsciiLetter(key[0]) && key.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')
            && key.ToLowerInvariant() is not ("y" or "n" or "yes" or "no" or "on" or "off" or "true" or "false" or "null");
        return plain ? key : Quote(key);
    }

    // Shortest round-trippable form, with YAML's spellings for the special values
    public static string Float(float value) => float.IsFinite(value) ? FloatText(value.ToString(CultureInfo.InvariantCulture)) : Special(value);

    public static string Float(double value) => double.IsFinite(value) ? FloatText(value.ToString(CultureInfo.InvariantCulture)) : Special(value);

    private static string Special(double value) => double.IsNaN(value) ? ".nan" : value > 0 ? ".inf" : "-.inf";

    // A decimal point keeps YAML from reading the value back as an int: 1 -> 1.0, 1E+20 -> 1.0E+20
    private static string FloatText(string text)
    {
        int exponent = text.IndexOf('E');
        string mantissa = exponent < 0 ? text : text[..exponent];
        return mantissa.Contains('.') ? text : mantissa + ".0" + text[mantissa.Length..];
    }

    // The first document's root, or an empty mapping for an empty file
    public static YamlNode Parse(string text)
    {
        YamlStream stream = new();
        try
        {
            stream.Load(new StringReader(text));
        }
        catch (YamlException ex)
        {
            // YamlDotNet prefixes the message with the positions, which Error adds back as a line number
            string message = ex.InnerException is YamlException inner ? inner.Message : ex.Message;
            int prefixEnd = message.IndexOf("): ", StringComparison.Ordinal);
            throw Error(ex.Start.Line, prefixEnd < 0 ? message : message[(prefixEnd + 3)..]);
        }
        catch (InvalidOperationException) // YamlDotNet throws this instead of a YamlException at an unclosed [ at the end
        {
            throw new FormatException("The file ended inside a [ ] list or quote that is never closed.");
        }
        return stream.Documents.Count == 0 ? new YamlMappingNode() : stream.Documents[0].RootNode;
    }

    public static int Line(YamlNode node) => (int)node.Start.Line;

    public static FormatException Error(long line, string message) => new($"Line {line}: {message}");

    public static bool IsNull(YamlScalarNode scalar)
    {
        return scalar.Style is ScalarStyle.Plain or ScalarStyle.Any && scalar.Tag.IsEmpty
            && (scalar.Value ?? "") is "" or "~" or "null" or "Null" or "NULL";
    }

    public static double ParseFloat(YamlScalarNode scalar)
    {
        string text = scalar.Value;
        switch (text.ToLowerInvariant())
        {
            case ".nan": return double.NaN;
            case ".inf": case "+.inf": return double.PositiveInfinity;
            case "-.inf": return double.NegativeInfinity;
        }
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            throw Error(Line(scalar), $"\"{text}\" is not a number.");
        return value;
    }

    // Decimal, or hexadecimal with a 0x prefix
    public static ulong ParseUnsigned(YamlScalarNode scalar)
    {
        string text = scalar.Value;
        bool ok = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? ulong.TryParse(text[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong value)
            : ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        if (!ok)
            throw Error(Line(scalar), $"\"{text}\" is not a whole number.");
        return value;
    }
}
