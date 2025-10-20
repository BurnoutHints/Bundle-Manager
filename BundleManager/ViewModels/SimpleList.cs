using BundleFormat.Interfaces;
using BundleManager.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Configuration;
using Serialization.Interfaces;
using Serialization.Types;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;

namespace BundleManager.ViewModels;

public partial class SimpleList : EditorViewModelBase
{
	public ObservableCollection<SimpleTypeDetails> SimpleObjects { get; } = [];
	public Type SimpleType { get; }
	public IList SimpleObjectsList { get; }
	public bool IsChild => ContentContainer.ViewStack.Count > 1;
	public int Capacity { get; }
	public bool IsVariableLength { get; }
	public bool DisplayAddRemoveItems { get; }
	[ObservableProperty]
	private bool canAddItems;
	[ObservableProperty]
	private bool canRemoveItems;

	public SimpleList(object topLevelObject, object obj,
		IResource resource, StackableViewContainer contentContainer)
		: base(topLevelObject, obj, resource, contentContainer)
	{
		SimpleType = Type.GetGenericArguments()[0];
		var listRefType = typeof(CollectionDescriptor<>).MakeGenericType(SimpleType);
		SimpleObjectsList = (IList)listRefType.GetProperty("Collection")!.GetValue(obj)!;

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

		for (int i = 0; i < SimpleObjectsList.Count; i++)
			SimpleObjects.Add(new SimpleTypeDetails(SimpleObjectsList, SimpleObjects, SimpleType));

		UpdateAddRemove();
	}

	public SimpleList(object topLevelObject, object obj, Stream stream,
		FileConfig config, StackableViewContainer contentContainer)
		: base(topLevelObject, obj, stream, config, contentContainer)
	{
		SimpleType = Type.GetGenericArguments()[0];
		var listRefType = typeof(CollectionDescriptor<>).MakeGenericType(SimpleType);
		SimpleObjectsList = (IList)listRefType.GetProperty("Collection")!.GetValue(obj)!;

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

		for (int i = 0; i < SimpleObjectsList.Count; i++)
			SimpleObjects.Add(new SimpleTypeDetails(SimpleObjectsList, SimpleObjects, SimpleType));

		UpdateAddRemove();
	}

	[RelayCommand]
	private void AddItem()
	{
		object? val = Activator.CreateInstance(SimpleType);
		SimpleObjectsList.Add(val);
		SimpleObjects.Add(new SimpleTypeDetails(SimpleObjectsList, SimpleObjects, SimpleType));
		UpdateAddRemove();
		ContentContainer.IsDirty = true;
		if (ContentContainer.ParentViewModel is BundleFile bundleFile)
			bundleFile.Tab.IsDirty = true;
	}

	[RelayCommand]
	private void RemoveItem(SimpleTypeDetails details)
	{
		int index = SimpleObjects.IndexOf(details);
		SimpleObjectsList.RemoveAt(index);
		SimpleObjects.RemoveAt(index);
		UpdateAddRemove();
		ContentContainer.IsDirty = true;
		if (ContentContainer.ParentViewModel is BundleFile bundleFile)
			bundleFile.Tab.IsDirty = true;
	}

	[RelayCommand]
	private void MoveItemUp(SimpleTypeDetails details)
	{
		int originalIndex = SimpleObjects.IndexOf(details);
		if (originalIndex <= 0)
			return;
		int newIndex = originalIndex - 1;

		var value = SimpleObjectsList[originalIndex];
		SimpleObjectsList.RemoveAt(originalIndex);
		SimpleObjectsList.Insert(newIndex, value);

		SimpleObjects.Move(originalIndex, newIndex);

		ContentContainer.IsDirty = true;
		if (ContentContainer.ParentViewModel is BundleFile bundleFile)
			bundleFile.Tab.IsDirty = true;
	}

	[RelayCommand]
	private void MoveItemDown(SimpleTypeDetails details)
	{
		int originalIndex = SimpleObjects.IndexOf(details);
		if (originalIndex >= SimpleObjectsList.Count - 1)
			return;
		int newIndex = originalIndex + 1;

		var value = SimpleObjectsList[originalIndex];
		SimpleObjectsList.RemoveAt(originalIndex);
		SimpleObjectsList.Insert(newIndex, value);

		SimpleObjects.Move(originalIndex, newIndex);

		ContentContainer.IsDirty = true;
		if (ContentContainer.ParentViewModel is BundleFile bundleFile)
			bundleFile.Tab.IsDirty = true;
	}

	private void UpdateAddRemove()
	{
		if (IsVariableLength)
			CanAddItems = true;
		else
			CanAddItems = SimpleObjectsList.Count < Capacity;
		CanRemoveItems = SimpleObjectsList.Count > 0;
	}
}
