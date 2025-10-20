using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BundleFormat;
using BundleManager.Models;
using BundleManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BundleManager.ViewModels;

public partial class BundleFile : ObservableObject
{
	public ObservableCollection<BundleResourceTab> ResourceTabs { get; } = [];
	[ObservableProperty]
	private BundleResourceTab? selectedResourceTab;
	public FileTab Tab { get; }
	public Bundle Bundle { get; }

	public BundleFile(FileTab tab)
	{
		Tab = tab;
		Bundle = new(Tab.Node.Info.FullName, Tab.Config);
		if (!Bundle.LoadSucceeded)
		{
			_ = OpenMessageWindow("Failed to load bundle.");
			return;
		}
		var resourceListViewModel = new BundleResourceList(this);
		var resourceListView = new Views.BundleResourceList()
		{
			DataContext = resourceListViewModel
		};
		var resourceListTab = new BundleResourceTab
		{
			Id = "Bundle",
			ContentContainer = new StackableViewContainer(resourceListView, this, false)
		};
		ResourceTabs.Add(resourceListTab);
		SelectedResourceTab = resourceListTab;
	}

	public bool SaveBundle()
	{
		Bundle.FilePath = Tab.Node.Info.FullName;
		if (!Bundle.Save())
			return false;
		Tab.IsDirty = false;
		return true;
	}

	[RelayCommand]
	private async Task OpenMessageWindow(string messageText)
	{
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			await dialogService.OpenDialog<Views.MessageWindow, bool>(new MessageWindow(messageText), desktop.MainWindow!);
	}

	[RelayCommand]
	private async Task SaveResource(BundleResourceTab? tab)
	{
		if (tab == null)
			return;
		if (tab.ContentContainer.Content.DataContext is EditorViewModelBase editorViewModel
			&& Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var dialogService = App.Current?.Services?.GetService<DialogService>()
				?? throw new Exception("Missing dialog service instance.");
			bool saveSucceeded = await editorViewModel.SaveResource();
			if (saveSucceeded)
				Tab.IsDirty = true;
			else
				_ = OpenMessageWindow("Failed to save resource.");
		}
	}

	[RelayCommand]
	private async Task CloseResource(BundleResourceTab? tab)
	{
		if (tab == null)
			return;

		if (tab.ContentContainer.Content.DataContext is EditorViewModelBase editorViewModel
			&& Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			if (editorViewModel.ContentContainer.IsDirty)
			{
				var dialogService = App.Current?.Services?.GetService<DialogService>()
					?? throw new Exception("Missing dialog service instance.");
				var ret = await dialogService.OpenDialog<Views.YesNoDialog, int>(
					new YesNoDialog($"Save changes to resource {SelectedResourceTab!.Id}?", "Save changes?"), desktop.MainWindow!);
				if (ret == 0)
					return;
				else if (ret == 1)
				{
					bool saveSucceeded = await editorViewModel.SaveResource();
					if (saveSucceeded)
						Tab.IsDirty = true;
					else
						return;
				}
			}
		}

		if (SelectedResourceTab == tab)
		{
			var tabIndex = ResourceTabs.IndexOf(tab);
			if (tabIndex == ResourceTabs.Count - 1)
				SelectedResourceTab = ResourceTabs[tabIndex - 1];
			else
				SelectedResourceTab = ResourceTabs[tabIndex + 1];
		}
		ResourceTabs.Remove(tab);
	}
}
