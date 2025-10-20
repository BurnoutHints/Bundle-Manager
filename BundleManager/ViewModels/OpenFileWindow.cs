using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BundleManager.Models;
using BundleManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Information;
using Information.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BundleManager.ViewModels;

public partial class OpenFileWindow : ObservableObject
{
	[ObservableProperty]
	private FileTypeInfo[] fileTypes;
	[ObservableProperty]
	private FileTypeInfo selectedFileType;
	private ObservableCollection<FileTab> FileTabs { get; set; }
	private FileTab? selectedTab;
	[ObservableProperty]
	private string fileLocation = string.Empty;
	[ObservableProperty]
	private bool isOpenEnabled = false;

	public OpenFileWindow(ObservableCollection<FileTab> tabs, FileTab? selectedTab, FileTypeInfo[] fileTypeInfo)
	{
		FileTabs = tabs;
		this.selectedTab = selectedTab;
		FileTypes = fileTypeInfo;
		SelectedFileType = fileTypeInfo[0];
	}

	[RelayCommand]
	private async Task ChooseFile()
	{
		try
		{
			var fileFolderService = App.Current?.Services?.GetService<IFileFolderService>()
				?? throw new Exception("Missing file/folder service instance.");
			var file = await fileFolderService.OpenFileAsync("Select bundle file");
			if (file == null)
				return;
			FileLocation = Uri.UnescapeDataString(file.Path.AbsolutePath);
			IsOpenEnabled = true;
		}
		catch
		{
			throw;
		}
	}

	[RelayCommand]
	private void OpenFile()
	{
		FileSystemEntryNode node = new(FileLocation);
		foreach (FileTab fileTab in FileTabs)
		{
			if (node.Info.FullName == fileTab.Node.Info.FullName)
			{
				selectedTab = fileTab;
				return;
			}
		}
		if (SelectedFileType.Id == FileType.Bundle
			&& Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var bundleFileView = new Views.BundleFile();
			FileTab newTab = new(node, bundleFileView, (MainWindow)desktop.MainWindow!.DataContext!);
			BundleFile bundleFileViewModel = new(newTab);
			if (!bundleFileViewModel.Bundle.LoadSucceeded)
				return;
			bundleFileView.DataContext = bundleFileViewModel;
			FileTabs.Add(newTab);
			selectedTab = newTab;
		}
		else if (SelectedFileType.Id == FileType.StoreBin)
		{

		}
		else
			return;

		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		dialogService.CloseDialog(this, true);
	}

	[RelayCommand]
	private void CancelOpenFile()
	{

		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		dialogService.CloseDialog(this, false);
	}
}
