using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BundleFormat.Interfaces;
using BundleManager.Models;
using BundleManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serialization;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace BundleManager.ViewModels;

public partial class EditorViewModelBase : ObservableObject
{
	public object TopLevelObject { get; }
	public Type TopLevelType { get; }
	public object Object { get; }
	public Type Type { get; }
	public IResource? Resource { get; }
	public Stream PrimaryStream { get; }
	public Stream? BodyStream { get; }
	public FileConfig Config { get; }
	public StackableViewContainer ContentContainer { get; }

	public EditorViewModelBase(object topLevelObject, object obj,
		IResource resource, StackableViewContainer contentContainer)
	{
		TopLevelObject = topLevelObject;
		TopLevelType = topLevelObject.GetType();
		Object = obj;
		Type = obj.GetType();
		PrimaryStream = resource.Memory;
		BodyStream = resource.BodyMemory;
		Config = resource.Config;
		ContentContainer = contentContainer;
		Resource = resource;
	}

	public EditorViewModelBase(object topLevelObject, object obj, Stream stream,
		FileConfig config, StackableViewContainer contentContainer)
	{
		TopLevelObject = topLevelObject;
		TopLevelType = topLevelObject.GetType();
		Object = obj;
		Type = obj.GetType();
		PrimaryStream = stream;
		Config = config;
		ContentContainer = contentContainer;
	}

	[RelayCommand]
	private async Task CloseView()
	{
		string? error = Validator.ValidateRequiredProperties(Type, Object);
		if (error != null)
		{
			await OpenMessageWindow(error);
			return;
		}
		ContentContainer.ViewStack.Pop();
		ContentContainer.Content = ContentContainer.ViewStack.Peek();
	}

	[RelayCommand]
	public async Task<bool> SaveResource()
	{
		string? error = Validator.ValidateRequiredProperties(Type, Object);
		if (error != null)
		{
			await OpenMessageWindow(error);
			return false;
		}

		var serializeMethod = TopLevelType.GetMethod("Serialize", BindingFlags.Static | BindingFlags.Public);
		serializeMethod!.Invoke(null, [TopLevelObject, Resource]);

		ContentContainer.IsDirty = false;
		if (ContentContainer.ParentViewModel is BundleFile bundleFile)
			bundleFile.Tab.IsDirty = false;
		return true;
	}

	[RelayCommand]
	private void OnViewObjectList(PropertyDetails propertyDetails)
	{
		var newObjectListView = new Views.ObjectList();
		ContentContainer.ViewStack.Push(newObjectListView);
		if (Resource == null)
		{
			var newObjectListViewModel = new ObjectList(TopLevelObject, propertyDetails.Value,
				PrimaryStream, Config, ContentContainer);
			newObjectListView.DataContext = newObjectListViewModel;
			ContentContainer.Content = newObjectListView;
		}
		else
		{
			var newObjectListViewModel = new ObjectList(TopLevelObject, propertyDetails.Value,
				Resource, ContentContainer);
			newObjectListView.DataContext = newObjectListViewModel;
			ContentContainer.Content = newObjectListView;
		}
	}

	[RelayCommand]
	private void OnViewObject(PropertyDetails propertyDetails)
	{
		var newPropertyGridView = new Views.PropertyGrid();
		ContentContainer.ViewStack.Push(newPropertyGridView);
		if (Resource == null)
		{
			var newPropertyGridViewModel = new PropertyGrid(TopLevelObject, propertyDetails.Value,
				PrimaryStream, Config, ContentContainer);
			newPropertyGridView.DataContext = newPropertyGridViewModel;
			ContentContainer.Content = newPropertyGridView;
		}
		else
		{
			var newPropertyGridViewModel = new PropertyGrid(TopLevelObject, propertyDetails.Value,
				Resource, ContentContainer);
			newPropertyGridView.DataContext = newPropertyGridViewModel;
			ContentContainer.Content = newPropertyGridView;
		}
	}

	[RelayCommand]
	private void OnViewSimpleList(PropertyDetails propertyDetails)
	{
		var newSimpleListView = new Views.SimpleList();
		ContentContainer.ViewStack.Push(newSimpleListView);
		if (Resource == null)
		{
			var newSimpleListViewModel = new SimpleList(TopLevelObject, propertyDetails.Value,
				PrimaryStream, Config, ContentContainer);
			newSimpleListView.DataContext = newSimpleListViewModel;
			ContentContainer.Content = newSimpleListView;
		}
		else
		{
			var newSimpleListViewModel = new SimpleList(TopLevelObject, propertyDetails.Value,
				Resource, ContentContainer);
			newSimpleListView.DataContext = newSimpleListViewModel;
			ContentContainer.Content = newSimpleListView;
		}
	}

	protected async Task OpenMessageWindow(string messageText)
	{
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			await dialogService.OpenDialog<Views.MessageWindow, bool>(new ViewModels.MessageWindow(messageText), desktop.MainWindow!);
	}
}
