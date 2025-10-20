using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using BundleFormat.Interfaces;
using BundleManager.Models;
using BundleManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serialization;
using Serialization.Attributes;
using Serialization.Enums;
using Serialization.Interfaces;
using Serialization.Types;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace BundleManager.ViewModels;

public partial class ObjectList : EditorViewModelBase
{
	public Type ElementType { get; }
	public PropertyInfo[] PropertiesInfo { get => Type.GenericTypeArguments[0].GetProperties(); }
	public IList Objects { get; }
	public bool IsChild { get => ContentContainer.ViewStack.Count > 1; }
	public int Capacity { get; }
	public bool IsVariableLength { get; }
	public bool DisplayAddRemoveItems { get; }
	[ObservableProperty]
	private bool canAddItems;
	[ObservableProperty]
	private bool canRemoveItems;

	public ObjectList(object topLevelObject, object obj,
		IResource resource, StackableViewContainer contentContainer)
		: base(topLevelObject, obj, resource, contentContainer)
	{
		ElementType = Type.GetGenericArguments()[0];
		var listRefType = typeof(CollectionDescriptor<>).MakeGenericType(ElementType);
		Objects = (IList)listRefType.GetProperty("Collection")!.GetValue(obj)!;

		Capacity = ((ICollectionDescriptor)obj).Capacity;
		if (Capacity == 0)
			DisplayAddRemoveItems = true;
		else
		{
			var variableElementUsage = ((ICollectionDescriptor)obj).VariableElementUsage;
			if (variableElementUsage)
				DisplayAddRemoveItems = true;
		}
		IsVariableLength = Capacity == 0;

		UpdateAddRemove();
	}

	public ObjectList(object topLevelObject, object obj, Stream stream,
		FileConfig config, StackableViewContainer contentContainer)
		: base(topLevelObject, obj, stream, config, contentContainer)
	{
		ElementType = Type.GetGenericArguments()[0];
		var listRefType = typeof(CollectionDescriptor<>).MakeGenericType(ElementType);
		Objects = (IList)listRefType.GetProperty("Collection")!.GetValue(obj)!;

		Capacity = ((ICollectionDescriptor)obj).Capacity;
		if (Capacity == 0)
			DisplayAddRemoveItems = true;
		else
		{
			var variableElementUsage = ((ICollectionDescriptor)obj).VariableElementUsage;
			if (variableElementUsage)
				DisplayAddRemoveItems = true;
		}
		IsVariableLength = Capacity == 0;

		UpdateAddRemove();
	}

	[RelayCommand]
	private void OnLoaded(RoutedEventArgs args)
	{
		if (args.Source is not DataGrid grid)
			return;

		if (grid.Columns.Count != 0)
			return;

		var propertyTemplateSelector = (IDataTemplate?)grid.FindResource("PropertyTemplateSelector");
		if (propertyTemplateSelector == null)
			return;

		foreach (var property in PropertiesInfo)
		{
			var cellTemplate = new FuncDataTemplate<object>((row, namescope) =>
			{
				var propertyDetails = new PropertyDetails(TopLevelObject, row, property);
				return new ContentPresenter
				{
					Content = propertyDetails,
					ContentTemplate = propertyTemplateSelector
				};
			});

			TextBlock header = new()
			{
				Text = property.Name
			};
			ToolTip.SetTip(header, property.GetCustomAttribute<TooltipAttribute>()?.Text);

			grid.Columns.Add(new DataGridTemplateColumn
			{
				Header = header,
				CellTemplate = cellTemplate
			});
		}
	}

	[RelayCommand]
	private void OnLoadingRow(DataGridRowEventArgs e)
	{
		e.Row.Header = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = Thickness.Parse("2,0,2,0"),
			Children =
			{
				new Button
				{
					Content = "\u2191",
					Command = MoveItemUpCommand,
					CommandParameter = e.Row.DataContext,
					Padding = Thickness.Parse("7,5,7,5")
				},
				new Button
				{
					Content = "\u2193",
					Command = MoveItemDownCommand,
					CommandParameter = e.Row.DataContext,
					Padding = Thickness.Parse("7,5,7,5")
				},
				new Button
				{
					Content = "\u29C9",
					Command = DuplicateItemCommand,
					CommandParameter = e.Row.DataContext,
					Padding = Thickness.Parse("6,4,6,4")
				},
				new Button
				{
					Content = "-",
					Command = RemoveItemCommand,
					CommandParameter = e.Row.DataContext,
					Padding = Thickness.Parse("10,5,10,5")
				}
			}
		};
	}

	[RelayCommand]
	private async Task AddItem()
	{
		object? item = Activator.CreateInstance(ElementType);
		if (item == null)
			return;
		bool shouldAddObject = await OpenNewObjectDialog(item);
		if (!shouldAddObject)
			return;
		Objects.Add(item);
		UpdateAddRemove();
		ContentContainer.IsDirty = true;
		if (ContentContainer.ParentViewModel is BundleFile bundleFile)
			bundleFile.Tab.IsDirty = true;
	}

	[RelayCommand]
	private void DuplicateItem(object item)
	{
		object? clone;
		Type objectType = item.GetType();
		var cloneBehaviorAttribute = objectType.GetCustomAttribute<CloneBehaviorAttribute>();
		if (cloneBehaviorAttribute != null)
		{
			if (cloneBehaviorAttribute.Mode == CloneMode.Shallow)
				clone = FastCloner.FastCloner.ShallowClone(item);
			else if (cloneBehaviorAttribute.Mode == CloneMode.Custom
				&& objectType is ICloneable cloneable)
				clone = cloneable.Clone();
			else
				clone = FastCloner.FastCloner.DeepClone(item);
		}
		else
			clone = FastCloner.FastCloner.DeepClone(item);

		int index = Objects.IndexOf(item);
		Objects.Insert(index + 1, clone);

		ContentContainer.IsDirty = true;
		if (ContentContainer.ParentViewModel is BundleFile bundleFile)
			bundleFile.Tab.IsDirty = true;
	}

	[RelayCommand]
	private async Task RemoveItem(object item)
	{
		var foundReferences = ReferenceFinder.FindReferences(TopLevelObject, item);
		if (foundReferences.Count > 1)
		{
			string messageText = "Cannot remove an object that is referenced more than once. The following uses were found:\n"
				+ string.Join('\n', foundReferences);
			await OpenMessageWindow(messageText);
			return;
		}
		Objects.Remove(item);
		UpdateAddRemove();
		ContentContainer.IsDirty = true;
		if (ContentContainer.ParentViewModel is BundleFile bundleFile)
			bundleFile.Tab.IsDirty = true;
	}

	[RelayCommand]
	private void MoveItemUp(object item)
	{
		int originalIndex = Objects.IndexOf(item);
		if (originalIndex <= 0)
			return;
		int newIndex = originalIndex - 1;

		var value = Objects[originalIndex];
		Objects.RemoveAt(originalIndex);
		Objects.Insert(newIndex, value);

		ContentContainer.IsDirty = true;
		if (ContentContainer.ParentViewModel is BundleFile bundleFile)
			bundleFile.Tab.IsDirty = true;
	}

	[RelayCommand]
	private void MoveItemDown(object item)
	{
		int originalIndex = Objects.IndexOf(item);
		if (originalIndex >= Objects.Count - 1)
			return;
		int newIndex = originalIndex + 1;

		var value = Objects[originalIndex];
		Objects.RemoveAt(originalIndex);
		Objects.Insert(newIndex, value);

		ContentContainer.IsDirty = true;
		if (ContentContainer.ParentViewModel is BundleFile bundleFile)
			bundleFile.Tab.IsDirty = true;
	}

	private void UpdateAddRemove()
	{
		if (IsVariableLength)
			CanAddItems = true;
		else
			CanAddItems = Objects.Count < Capacity;
		CanRemoveItems = Objects.Count > 0;
	}

	private async Task<bool> OpenNewObjectDialog(object newItem)
	{
		var desktop = (IClassicDesktopStyleApplicationLifetime)Application.Current?.ApplicationLifetime!;
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		if (Resource == null)
		{
			return await dialogService.OpenDialog<Views.NewObjectDialog, bool>(
				new NewObjectDialog(TopLevelObject, newItem, PrimaryStream, Config), desktop.MainWindow!);
		}
		else
		{
			return await dialogService.OpenDialog<Views.NewObjectDialog, bool>(
				new NewObjectDialog(TopLevelObject, newItem, Resource), desktop.MainWindow!);
		}
	}
}
