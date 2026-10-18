using System;
using System.ComponentModel;

namespace LuaList;

// Included...
// [1] Define TypeConverter class for hex.
// [2] use it when defining a member of a data class using attribute TypeConverter.
// [3] Connect the data class to a PropertyGrid.


// [1] define UInt32HexTypeConverter is-a TypeConverter
public class EncryptedStringConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertFrom(context, sourceType);
        }
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertTo(context, destinationType);
        }
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value.GetType() == typeof(BundleUtilities.EncryptedString))
        {
            return ((BundleUtilities.EncryptedString)value).Value;
        }
        else
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        if (value.GetType() == typeof(string))
        {
            string input = (string)value;
            return new BundleUtilities.EncryptedString(input);
        }
        else
        {
            return base.ConvertFrom(context, culture, value);
        }
    }
}
