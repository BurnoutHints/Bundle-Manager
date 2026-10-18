using System;
using System.ComponentModel;
using System.Globalization;

namespace BaseHandlers;

// Shows a hex string for uint and ulong values, accepts hex with or without 0x, or decimal
public class HexConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is uint or ulong)
            return $"0x{value:X8}";
        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string s)
            return base.ConvertFrom(context, culture, value);

        s = s.Trim();
        bool hex = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        ulong result = hex ? ulong.Parse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture) : ulong.Parse(s, CultureInfo.InvariantCulture);
        return context?.PropertyDescriptor?.PropertyType == typeof(uint) ? (object)checked((uint)result) : result;
    }
}
