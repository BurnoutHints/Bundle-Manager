using System;
using System.ComponentModel;
using System.Globalization;
using BundleUtilities;

namespace BaseHandlers;

public sealed class TrafficCgsIDConverter : TypeConverter
{
    public static string Label(ulong id)
    {
        string decoded = new EncryptedString(id).Value;

        if (decoded.Length > 0 && new EncryptedString(decoded).Encrypted == id)
            return decoded;

        return $"0x{id:X16}";
    }

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
        destinationType == typeof(string) && value is ulong id
            ? Label(id)
            : base.ConvertTo(context, culture, value, destinationType);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string input)
            return base.ConvertFrom(context, culture, value);

        input = input.Trim();

        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ulong.TryParse(input.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong id)
                ? id
                : throw new FormatException("Enter a hexadecimal ID with up to 16 digits after 0x.");

        input = input.ToUpperInvariant();

        if (input.Length is < 1 or > 12)
            throw new FormatException("Enter a CgsID with 1–12 characters, or a hexadecimal ID starting with 0x.");

        foreach (char character in input)
            if (!(character is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-' or '/' or ' '))
                throw new FormatException("CgsIDs use letters A–Z, digits, spaces, underscores, hyphens and slashes.");

        return new EncryptedString(input).Encrypted;
    }
}
