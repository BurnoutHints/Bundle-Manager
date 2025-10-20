using Avalonia.Controls;
using BundleFormat.Interfaces;
using BundleManager.Models;
using BundleManager.Registry;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Information;
using Information.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BundleManager.ViewModels;

public partial class BundleResourceList : ObservableObject
{
	private readonly BundleFile parent;
	public List<IResource> Resources
	{
		get => parent.Bundle.Resources;
	}
	public IResource? SelectedResource { get; set; }
	public ObservableCollection<BundleResourceTab> ResourceTabs
	{
		get => parent.ResourceTabs;
	}
	public BundleResourceTab SelectedTab
	{
		get => parent.SelectedResourceTab!;
		set => parent.SelectedResourceTab = value;
	}
	public FileTab FileTab
	{
		get => parent.Tab;
	}
	public bool IsMultiStream { get; }

	public BundleResourceList(BundleFile parent)
	{
		this.parent = parent;
		IsMultiStream = Resources.Any(r => r.StreamIndex != 0);
	}

	[RelayCommand]
	private void OpenResource()
	{
		if (SelectedResource == null
			|| ResourceTabs.Any(t => t.Id.Equals(SelectedResource.Name)))
			return;
		BundleResource bundleResource = new(SelectedResource, SelectedTab);
		if (!bundleResource.LoadSucceeded)
			return;

		CustomView[]? customViews;
		bool gotCustomViews;
		if (FileTab.Config.Game.Id == Game.Black2
			|| FileTab.Config.Game.Id == Game.BurnoutParadise
			|| FileTab.Config.Game.Id == Game.BurnoutParadiseRemastered)
			gotCustomViews = ResourceTypeViewRegistry.Registry.TryGetValue((ResourceType)bundleResource.Resource.Type, out customViews);
		else
			gotCustomViews = ResourceTypeViewRegistry.RegistryV2.TryGetValue((ResourceTypeV2)bundleResource.Resource.Type, out customViews);

		if (!gotCustomViews)
		{
			SetDefaultView(bundleResource);
			return;
		}

		GetSpecificCustomView(customViews, out var viewType, out var viewModelType);
		if (viewType == null || viewModelType == null)
			return;
		SetCustomView(bundleResource, viewType, viewModelType);
	}

	private void SetDefaultView(BundleResource bundleResource)
	{
		Views.PropertyGrid propertyGridView = new();
		var newTab = new BundleResourceTab
		{
			Id = SelectedResource!.Name,
			ContentContainer = new StackableViewContainer(propertyGridView, parent)
		};
		PropertyGrid propertyGridViewModel = new(bundleResource.Object!, bundleResource.Object!,
			bundleResource.Resource, newTab.ContentContainer);
		propertyGridView.DataContext = propertyGridViewModel;
		ResourceTabs.Add(newTab);
		SelectedTab = newTab;
	}

	private void GetSpecificCustomView(CustomView[]? customViews, out Type? viewType, out Type? viewModelType)
	{
		var configuredGameDetails = new GameDetails
		{
			Game = parent.Tab.Config.Game.Id,
			Platform = parent.Tab.Config.Platform.Id,
			Version = parent.Tab.Config.Version.Id
		};
		Type? view = null;
		Type? viewModel = null;
		foreach (var customView in customViews!)
		{
			foreach (var gameDetails in customView.ApplicableGames)
			{
				if (configuredGameDetails == gameDetails)
				{
					view = customView.ViewType;
					viewModel = customView.ViewModelType;
				}
			}
		}
		viewType = view;
		viewModelType = viewModel;
	}

	private void SetCustomView(BundleResource bundleResource, Type viewType, Type viewModelType)
	{
		var customControlView = (UserControl?)Activator.CreateInstance(viewType);
		if (customControlView == null)
			return;
		var newTab = new BundleResourceTab
		{
			Id = SelectedResource!.Name,
			ContentContainer = new StackableViewContainer(customControlView, parent)
		};
		var customControlViewModel = Activator.CreateInstance(viewModelType,
			[ bundleResource.Object!, bundleResource.Object!, bundleResource.Resource, newTab.ContentContainer ]);
		if (customControlViewModel == null)
			return;
		customControlView.DataContext = customControlViewModel;
		ResourceTabs.Add(newTab);
		SelectedTab = newTab;
	}
}
