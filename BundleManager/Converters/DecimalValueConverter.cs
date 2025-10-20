using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace BundleManager.Converters;

public class DecimalValueConverter : IValueConverter
{
	public static readonly DecimalValueConverter Instance = new();

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null)
			return 0m;

		if (targetType == typeof(float))
		{
			var f = (float)value;
			if (f > (float)decimal.MaxValue)
				return decimal.MaxValue;
			else if (f < (float)decimal.MinValue)
				return decimal.MinValue;
		}
		else if (targetType == typeof(double))
		{
			var d = (double)value;
			if (d > (double)decimal.MaxValue)
				return decimal.MaxValue;
			else if (d < (double)decimal.MinValue)
				return decimal.MinValue;
		}

		try
		{
			return System.Convert.ToDecimal(value);
		}
		catch
		{
			return 0m;
		}
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not decimal dec)
			return Activator.CreateInstance((Type)parameter!);

		var realType = parameter as Type ?? targetType;
		try
		{
			return System.Convert.ChangeType(dec, realType);
		}
		catch
		{
			return Activator.CreateInstance((Type)parameter!);
		}
	}
}
