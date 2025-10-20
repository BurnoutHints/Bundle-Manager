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
using Serialization.Attributes;
using Serialization.Types;
using System;
using System.Linq;
using System.Reflection;

namespace BundleManager.DataTemplates;

public class PropertyTemplateSelector : IDataTemplate
{
	public Control? Build(object? param)
	{
		if (param == null)
			return null;
		var details = (PropertyDetails)param;

		Control? control = null;

		if (details.ParentArray != null)
		{
			var itemType = details.ParentArray.GetType().GetGenericArguments().FirstOrDefault()
					?? details.ParentArray.GetType().GetElementType();

			var displayMember = itemType?.GetCustomAttribute<DisplayMemberAttribute>()?.MemberName;

			var grid = new Grid
			{
				ColumnDefinitions = new ColumnDefinitions("*, Auto"),
				ColumnSpacing = 2,
				Margin = new Thickness(1),
				HorizontalAlignment = HorizontalAlignment.Stretch
			};

			ComboBox comboBox;
			
			if (displayMember != null)
			{
				comboBox = new ComboBox
				{
					ItemsSource = details.ParentArray,
					[!SelectingItemsControl.SelectedItemProperty] = new Binding("Value", BindingMode.TwoWay),
					HorizontalAlignment = HorizontalAlignment.Stretch
				};
				comboBox.ItemTemplate = new FuncDataTemplate<object>((item, namescope) =>
					new TextBlock
					{
						[!TextBlock.TextProperty] = new Binding(displayMember!)
					});
			}
			else
			{
				comboBox = new ComboBox
				{
					ItemsSource = Enumerable.Range(0, details.ParentArray.Count).ToList(),
					[!SelectingItemsControl.SelectedItemProperty] = new Binding("Value", BindingMode.TwoWay),
					HorizontalAlignment = HorizontalAlignment.Stretch
				};
			}

			var clearButton = new Button
			{
				Content = "X",
				Padding = new Thickness(4, 0, 4, 0),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			clearButton.Click += (s, e) => comboBox.SelectedItem = null;

			grid.Children.Add(comboBox);
			grid.Children.Add(clearButton);
			Grid.SetColumn(clearButton, 1);

			control = grid;

			EventHandler<AvaloniaPropertyChangedEventArgs>? gridComboBoxHandler = (s, e) =>
			{
				if (e.Property != ComboBox.SelectedItemProperty)
					return;

				if (control.FindAncestorOfType<UserControl>()!.DataContext is EditorViewModelBase viewModel)
				{
					viewModel.ContentContainer.IsDirty = true;
					if (viewModel.ContentContainer.ParentViewModel is BundleFile bundleFile)
						bundleFile.Tab.IsDirty = true;
				}
			};

			control.AttachedToVisualTree += (s, e) => comboBox.PropertyChanged += gridComboBoxHandler;
			control.DetachedFromVisualTree += (s, e) => comboBox.PropertyChanged -= gridComboBoxHandler;

			return control;
		}

		// Integer and floating point primitives.
		// nint/nuint/char are unsupported
		else if (details.Type.IsPrimitive
			&& details.Type != typeof(bool))
		{
			decimal minValue = decimal.MinValue;
			decimal maxValue = decimal.MaxValue;
			decimal increment = 1m;

			if (details.Type == typeof(sbyte))
			{
				minValue = sbyte.MinValue;
				maxValue = sbyte.MaxValue;
			}
			else if (details.Type == typeof(byte))
			{
				minValue = byte.MinValue;
				maxValue = byte.MaxValue;
			}
			else if (details.Type == typeof(short))
			{
				minValue = short.MinValue;
				maxValue = short.MaxValue;
			}
			else if (details.Type == typeof(ushort))
			{
				minValue = ushort.MinValue;
				maxValue = ushort.MaxValue;
			}
			else if (details.Type == typeof(int))
			{
				minValue = int.MinValue;
				maxValue = int.MaxValue;
			}
			else if (details.Type == typeof(uint))
			{
				minValue = uint.MinValue;
				maxValue = uint.MaxValue;
			}
			else if (details.Type == typeof(long))
			{
				minValue = long.MinValue;
				maxValue = long.MaxValue;
			}
			else if (details.Type == typeof(ulong))
			{
				minValue = ulong.MinValue;
				maxValue = ulong.MaxValue;
			}
			else if (details.Type == typeof(float)
				|| details.Type == typeof(double))
			{
				// Decimal can't actually hold the min/max float/double values.
				// The converter will bring any out-of-bounds values within decimal range.
				increment = 0.001m;
			}
			else
			{
				throw new Exception("Unsupported primitive type used.");
			}

			var boundsAttribute = details.Info.GetCustomAttribute<BoundsAttribute>();

			var numericUpDown = new NumericUpDown
			{
				[!NumericUpDown.ValueProperty] = new Binding("Value", BindingMode.TwoWay)
				{
					Converter = DecimalValueConverter.Instance,
					ConverterParameter = details.Type
				},
				Minimum = minValue,
				Maximum = maxValue,
				Increment = increment,
				IsReadOnly = details.IsReadOnly,
				MinWidth = 150,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Margin = new Thickness(1)
			};
			if (boundsAttribute != null)
			{
				numericUpDown.Maximum = boundsAttribute.Max;
				numericUpDown.Minimum = boundsAttribute.Min;
			}
			control = numericUpDown;
		}

		// Boolean
		else if (details.Type == typeof(bool))
			control = new CheckBox
			{
				[!CheckBox.IsCheckedProperty] = new Binding("Value", BindingMode.TwoWay),
				IsEnabled = !details.IsReadOnly,
				HorizontalAlignment = HorizontalAlignment.Center
			};

		// String
		else if (details.Type == typeof(string))
		{
			var maxLengthAttribute = details.Info.GetCustomAttribute<MaxStringLengthAttribute>();
			var textBox = new TextBox
			{
				[!TextBox.TextProperty] = new Binding("Value", BindingMode.TwoWay),
				IsReadOnly = details.IsReadOnly,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Margin = new Thickness(1),
				MinWidth = 150
			};
			if (maxLengthAttribute != null)
				textBox.MaxLength = maxLengthAttribute.MaxLength;
			control = textBox;
		}

		// Enum
		else if (details.Type.IsEnum)
			control = new ComboBox
			{
				ItemsSource = Enum.GetValues(details.Type),
				[!ComboBox.SelectedItemProperty] = new Binding("Value", BindingMode.TwoWay),
				IsEnabled = !details.IsReadOnly,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Margin = new Thickness(1)
			};

		// Lists
		else if (details.Type.IsGenericType &&
			details.Type.GetGenericTypeDefinition() == typeof(CollectionDescriptor<>))
		{
			var elementType = details.Type.GenericTypeArguments[0];

			// Collection of simple types
			if (elementType.IsPrimitive
				|| elementType == typeof(string)
				|| elementType == typeof(BundleFormat.Types.CgsId))
				control = new Button
				{
					Content = "View collection",
					[!Button.CommandProperty] = new Binding
					{
						Path = "DataContext.ViewSimpleListCommand",
						RelativeSource = new RelativeSource
						{
							Mode = RelativeSourceMode.FindAncestor,
							AncestorType = typeof(UserControl)
						},
						Mode = BindingMode.TwoWay
					},
					CommandParameter = details,
					HorizontalAlignment = HorizontalAlignment.Center,
					Margin = new Thickness(1)
				};

			// Collection of objects
			else if (elementType.IsClass)
				control = new Button
				{
					Content = "View collection",
					[!Button.CommandProperty] = new Binding
					{
						Path = "DataContext.ViewObjectListCommand",
						RelativeSource = new RelativeSource
						{
							Mode = RelativeSourceMode.FindAncestor,
							AncestorType = typeof(UserControl)
						},
						Mode = BindingMode.TwoWay
					},
					CommandParameter = details,
					HorizontalAlignment = HorizontalAlignment.Center,
					Margin = new Thickness(1)
				};
		}

		// CgsId
		else if (details.Type == typeof(BundleFormat.Types.CgsId))
			control = new TextBox
			{
				[!TextBox.TextProperty] = new Binding("Value.Id", BindingMode.TwoWay),
				MaxLength = 12,
				Width = 190,
				HorizontalAlignment = HorizontalAlignment.Center,
				Margin = new Thickness(1)
			};

		// Class
		else if (details.Type.IsClass)
			return new Button
			{
				Content = "View object",
				[!Button.CommandProperty] = new Binding
				{
					Path = "DataContext.ViewObjectCommand",
					RelativeSource = new RelativeSource
					{
						Mode = RelativeSourceMode.FindAncestor,
						AncestorType = typeof(UserControl)
					},
					Mode = BindingMode.TwoWay
				},
				CommandParameter = details,
				HorizontalAlignment = HorizontalAlignment.Center,
				Margin = new Thickness(1)
			};

		if (control == null)
			throw new Exception($"Unsupported property type {details.Type.FullName}");

		EventHandler<AvaloniaPropertyChangedEventArgs>? handler = (s, e) =>
		{
			if (e.Property != TextBox.TextProperty
				&& e.Property != NumericUpDown.ValueProperty
				&& e.Property != CheckBox.IsCheckedProperty
				&& e.Property != ComboBox.SelectedItemProperty)
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
		return data is PropertyDetails;
	}
}
