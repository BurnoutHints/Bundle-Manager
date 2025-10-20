using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.VisualTree;
using BundleManager.Converters;
using BundleManager.Models;
using BundleManager.ViewModels;
using System;

namespace BundleManager.DataTemplates;

public class SimpleTemplateSelector : IDataTemplate
{
	public Control? Build(object? param)
	{
		if (param == null || param is not SimpleTypeDetails details)
			return null;
		var elementType = details.Value?.GetType() ?? details.GetType();

		TemplatedControl? control = null;

		if (elementType.IsPrimitive)
		{
			decimal minValue = decimal.MinValue;
			decimal maxValue = decimal.MaxValue;
			decimal increment = 1m;

			if (elementType == typeof(sbyte))
			{
				minValue = sbyte.MinValue;
				maxValue = sbyte.MaxValue;
			}
			else if (elementType == typeof(byte))
			{
				minValue = byte.MinValue;
				maxValue = byte.MaxValue;
			}
			else if (elementType == typeof(short))
			{
				minValue = short.MinValue;
				maxValue = short.MaxValue;
			}
			else if (elementType == typeof(ushort))
			{
				minValue = ushort.MinValue;
				maxValue = ushort.MaxValue;
			}
			else if (elementType == typeof(int))
			{
				minValue = int.MinValue;
				maxValue = int.MaxValue;
			}
			else if (elementType == typeof(uint))
			{
				minValue = uint.MinValue;
				maxValue = uint.MaxValue;
			}
			else if (elementType == typeof(long))
			{
				minValue = long.MinValue;
				maxValue = long.MaxValue;
			}
			else if (elementType == typeof(ulong))
			{
				minValue = ulong.MinValue;
				maxValue = ulong.MaxValue;
			}
			else if (elementType == typeof(float)
				|| elementType == typeof(double))
			{
				// Decimal can't actually hold the min/max float/double values.
				// The converter will bring any out-of-bounds values within decimal range.
				increment = 0.001m;
			}
			else
			{
				throw new Exception("Unsupported primitive type used.");
			}

			control = new NumericUpDown
			{
				[!NumericUpDown.ValueProperty] = new Binding("Value", BindingMode.TwoWay)
				{
					Converter = DecimalValueConverter.Instance,
					ConverterParameter = elementType
				},
				Minimum = minValue,
				Maximum = maxValue,
				Increment = increment,
				MinWidth = 150,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Margin = new Thickness(1)
			};
		}

		// Boolean
		else if (elementType == typeof(bool))
			control = new CheckBox
			{
				[!CheckBox.IsCheckedProperty] = new Binding("Value", BindingMode.TwoWay),
				HorizontalAlignment = HorizontalAlignment.Center
			};

		// String
		else if (elementType == typeof(string))
			control = new TextBox
			{
				[!TextBox.TextProperty] = new Binding("Value", BindingMode.TwoWay),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Margin = new Thickness(1),
				MinWidth = 150
			};

		else if (elementType == typeof(BundleFormat.Types.CgsId))
			control = new TextBox
			{
				[!TextBox.TextProperty] = new Binding("Value.Id", BindingMode.TwoWay),
				MaxLength = 12,
				Width = 190,
				HorizontalAlignment = HorizontalAlignment.Center,
				Margin = new Thickness(1)
			};

		if (control == null)
			throw new Exception($"Unsupported simple type {elementType.FullName}");

		EventHandler<AvaloniaPropertyChangedEventArgs>? handler = (s, e) =>
		{
			if (e.Property != TextBox.TextProperty
				&& e.Property != NumericUpDown.ValueProperty
				&& e.Property != CheckBox.IsCheckedProperty)
				return;

			if (control.FindAncestorOfType<UserControl>()!.DataContext is EditorViewModelBase viewModel)
			{
				viewModel.ContentContainer.IsDirty = true;
				if (viewModel.ContentContainer.ParentViewModel is BundleFile bundleFile)
					bundleFile.Tab.IsDirty = true;
			}
		};

		control.AttachedToVisualTree += (s, e) => control.PropertyChanged += handler;
		control.DetachedFromVisualTree += (s, e) => control.PropertyChanged -= handler;

		return control;
	}

	public bool Match(object? data)
	{
		return data is SimpleTypeDetails;
	}
}
