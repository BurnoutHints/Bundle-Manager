using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

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

// Edits a list or array of numbers as "1, 2, 3", the collection editor can't change number items
public class NumberListConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is IEnumerable items)
            return string.Join(", ", items.Cast<object>());
        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string s || context?.PropertyDescriptor == null)
            return base.ConvertFrom(context, culture, value);

        Type type = context.PropertyDescriptor.PropertyType;
        Type elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
        string[] parts = s.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
        Array result = Array.CreateInstance(elementType, parts.Length);
        for (int i = 0; i < parts.Length; i++)
            result.SetValue(Convert.ChangeType(parts[i], elementType, CultureInfo.InvariantCulture), i);
        return type.IsArray ? result : Activator.CreateInstance(type, result);
    }
}

// Shows an array as its elements, "1, 2, 3 | 4, 5, 6", and still expands to edit them
public class ArrayTextConverter : ArrayConverter
{
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Array items)
            return string.Join(" | ", items.Cast<object>().Select(item => TypeDescriptor.GetConverter(item).ConvertToString(context, culture, item)));
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
