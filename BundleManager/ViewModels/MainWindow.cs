using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using BundleManager.Models;
using BundleManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Configuration;
using Information;
using Information.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BundleManager.ViewModels;

public partial class MainWindow : ObservableObject
{
	private bool isClosing;

	public ObservableCollection<FileSystemEntryNode> GameDirectoryContents { get; } = [];
	[ObservableProperty]
	private FileSystemEntryNode? selectedNode;

	public ObservableCollection<FileTab> FileTabs { get; } = [];
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanSaveAs))]
	[NotifyCanExecuteChangedFor(nameof(SaveFileCommand))]
	private FileTab? selectedFileTab;
	public bool CanSave { get => SelectedFileTab?.IsDirty ?? false; }
	public bool CanSaveAs { get => SelectedFileTab != null; }

	[ObservableProperty]
	private GridLength treeColumnWidth = new(250, GridUnitType.Pixel);

	private readonly FileSystemWatcher watcher = new()
	{
		IncludeSubdirectories = true,
		NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
	};

	public MainWindow()
	{
		
	}

	[RelayCommand]
	private async Task OnClosing(WindowClosingEventArgs e)
	{
		if (isClosing)
			return;
		
		e.Cancel = true;

		for (int i = FileTabs.Count - 1; i >= 0; i--)
		{
			var ret = await CloseFile(FileTabs[i]);
			if (ret == 0)
				return;
		}

		isClosing = true;

		watcher.Dispose();

		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			desktop.MainWindow!.Close();
	}

	[RelayCommand]
	private void OnLoaded()
	{
		watcher.Created += OnAddTreeNode;
		watcher.Deleted += OnRemoveTreeNode;
		watcher.Renamed += RenameTreeNode;
		if (!string.IsNullOrWhiteSpace(UserConfig.Config.GameLocation))
		{
			watcher.Path = UserConfig.Config.GameLocation;
			watcher.EnableRaisingEvents = true;
		}
	}

	public void NotifyCanSaveChanged()
	{
		OnPropertyChanged(nameof(CanSave));
		SaveFileCommand.NotifyCanExecuteChanged();
	}

	// Run after configuration has been set. Cannot go in constructor
	public void Setup()
	{
		DirectoryInfo gameDirInfo = new(UserConfig.Config.GameLocation);
		foreach (DirectoryInfo dirInfo in gameDirInfo.GetDirectories())
			GameDirectoryContents.Add(new FileSystemEntryNode(dirInfo.FullName));
		foreach (FileInfo fileInfo in gameDirInfo.GetFiles())
			GameDirectoryContents.Add(new FileSystemEntryNode(fileInfo.FullName));
	}

	public Type GetPrimaryFileTypeViewType()
	{
		FileType primaryFileType = GameInfo.Info.Where(g => g.Id == UserConfig.Config.Game).First()
			.FileTypes.Where(f => f.IsPrimaryFileType == true).First().Id;
		if (primaryFileType == FileType.Bundle)
			return typeof(Views.BundleFile);
		throw new Exception("Primary file type has not been assigned a view.");
	}

	public Type GetPrimaryFileTypeViewModelType()
	{
		FileType primaryFileType = GameInfo.Info.Where(g => g.Id == UserConfig.Config.Game).First()
			.FileTypes.Where(f => f.IsPrimaryFileType == true).First().Id;
		if (primaryFileType == FileType.Bundle)
			return typeof(BundleFile);
		throw new Exception("Primary file type has not been assigned a view model.");
	}

	public void ResetTree()
	{
		GameDirectoryContents.Clear();
		Setup();
		watcher.Path = UserConfig.Config.GameLocation;
		watcher.EnableRaisingEvents = true;
	}

	private void OnAddTreeNode(object sender, FileSystemEventArgs e)
	{
		Dispatcher.UIThread.Post(async () =>
		{
			await Task.Delay(5000);
			AddTreeNode(e.FullPath);
		});
	}

	private void AddTreeNode(string path)
	{
		path = Path.GetFullPath(path);
		string? parentDir = Path.GetDirectoryName(path);
		if (string.IsNullOrEmpty(parentDir))
			return;

		if (Path.GetFullPath(parentDir).TrimEnd(['\\','/']).Equals(Path.GetFullPath(watcher.Path).TrimEnd(['\\', '/']), StringComparison.Ordinal))
		{
			InsertTreeNode(GameDirectoryContents, new FileSystemEntryNode(path));
			return;
		}

		var parentNode = FindTreeNode(parentDir, GameDirectoryContents);
		if (parentNode == null)
			return;

		InsertTreeNode(parentNode.Entries, new FileSystemEntryNode(path));
	}

	private void OnRemoveTreeNode(object sender, FileSystemEventArgs e)
	{
		Dispatcher.UIThread.Post(() =>
		{
			RemoveTreeNode(e.FullPath);
		});
	}

	private void RemoveTreeNode(string path)
	{
		path = Path.GetFullPath(path);
		string? parentDir = Path.GetDirectoryName(path);
		if (string.IsNullOrEmpty(parentDir))
			return;

		if (Path.GetFullPath(parentDir).TrimEnd(['\\', '/']).Equals(Path.GetFullPath(watcher.Path).TrimEnd(['\\', '/']), StringComparison.Ordinal))
		{
			var nodeInRootToRemove = GameDirectoryContents.FirstOrDefault(n => Path.GetFullPath(n.Info.FullName) == path);
			if (nodeInRootToRemove != null)
				GameDirectoryContents.Remove(nodeInRootToRemove);
		}

		var parentNode = FindTreeNode(parentDir, GameDirectoryContents);
		if (parentNode == null)
			return;

		var nodeToRemove = parentNode.Entries.FirstOrDefault(n => Path.GetFullPath(n.Info.FullName) == path);
		if (nodeToRemove != null)
			parentNode.Entries.Remove(nodeToRemove);
	}

	private void RenameTreeNode(object sender, RenamedEventArgs e)
	{
		Dispatcher.UIThread.Post(() =>
		{
			RemoveTreeNode(e.OldFullPath);
			AddTreeNode(e.FullPath);
		});
	}

	private static void InsertTreeNode(ObservableCollection<FileSystemEntryNode> collection, FileSystemEntryNode newNode)
	{
		bool isDir = newNode.IsDirectory;
		string newName = newNode.Info.Name;

		for (int i = 0; i < collection.Count; i++)
		{
			var existing = collection[i];

			if (isDir && !existing.IsDirectory)
			{
				collection.Insert(i, newNode);
				return;
			}

			if (existing.IsDirectory == isDir)
			{
				int cmp = string.Compare(existing.Info.Name, newName, StringComparison.OrdinalIgnoreCase);
				if (cmp > 0)
				{
					collection.Insert(i, newNode);
					return;
				}
			}
		}

		collection.Add(newNode);
	}

	private FileSystemEntryNode? FindTreeNode(string path, ObservableCollection<FileSystemEntryNode> collection)
	{
		foreach (var node in collection)
		{
			if (node.Info.FullName.Equals(path, StringComparison.OrdinalIgnoreCase))
				return node;

			if (node.IsDirectory)
			{
				var result = FindTreeNode(path, node.Entries);
				if (result != null)
					return result;
			}
		}
		return null;
	}

	[RelayCommand]
	private void ExitApplication()
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			desktop.MainWindow!.Close();
	}

	[RelayCommand]
	private async Task OpenOptionsWindow()
	{
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			await dialogService.OpenDialog<Views.OptionsWindow, bool>(new OptionsWindow(this), desktop.MainWindow!);
	}
	
	[RelayCommand]
	private async Task OpenAboutWindow()
	{
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			await dialogService.OpenDialog<Views.AboutWindow, bool>(new AboutWindow(), desktop.MainWindow!);
	}

	private async Task OpenMessageWindow(string messageText)
	{
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			await dialogService.OpenDialog<Views.MessageWindow, bool>(new ViewModels.MessageWindow(messageText), desktop.MainWindow!);
	}

	[RelayCommand]
	private async Task OpenBundle()
	{
		try
		{
			var fileFolderService = App.Current?.Services?.GetService<IFileFolderService>()
				?? throw new Exception("Missing file/folder service instance.");
			var file = await fileFolderService.OpenFileAsync("Select bundle file");
			if (file == null)
				return;
			FileSystemEntryNode node = new(Uri.UnescapeDataString(file.Path.AbsolutePath));
			foreach (FileTab fileTab in FileTabs)
			{
				if (node.Info.FullName == fileTab.Node.Info.FullName)
				{
					SelectedFileTab = fileTab;
					return;
				}
			}
			var bundleFileView = new Views.BundleFile();
			FileTab newTab = new(node, bundleFileView, this);
			BundleFile bundleFileViewModel = new(newTab);
			if (!bundleFileViewModel.Bundle.LoadSucceeded)
				return;
			bundleFileView.DataContext = bundleFileViewModel;
			FileTabs.Add(newTab);
			SelectedFileTab = newTab;
		}
		catch
		{
			throw;
		}
	}

	[RelayCommand]
	private async Task OpenFile()
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var dialogService = App.Current?.Services?.GetService<DialogService>()
				?? throw new Exception("Missing dialog service instance.");
			var fileTypeInfo = GameInfo.Info.Where(g => g.Id == UserConfig.Config.Game).First().FileTypes;
			await dialogService.OpenDialog<Views.OpenFileWindow, bool>(new OpenFileWindow(FileTabs, SelectedFileTab, fileTypeInfo), desktop.MainWindow!);
		}
	}

	[RelayCommand]
	private void OpenFileFromTree()
	{
		if (SelectedNode == null || SelectedNode.IsDirectory)
			return;
		foreach (FileTab fileTab in FileTabs)
		{
			if (SelectedNode.Info.FullName == fileTab.Node.Info.FullName)
			{
				SelectedFileTab = fileTab;
				return;
			}
		}
		// If the game does not have a primary file type, there would have to be a robust
		// way of detecting which file type is being dealt with so the correct View and
		// ViewModel is used.
		// Since all games supported by Bundle Manager use a single container, this is out
		// of scope and not implemented. However, the framework for it is present.
		var view = (UserControl?)Activator.CreateInstance(GetPrimaryFileTypeViewType())
			?? throw new Exception("Primary view type was null.");
		FileTab newTab = new(SelectedNode, view, this);
		var viewModel = (ObservableObject?)Activator.CreateInstance(GetPrimaryFileTypeViewModelType(), newTab)
			?? throw new Exception("Primary view model type was null.");
		view.DataContext = viewModel;
		if (GetPrimaryFileTypeViewModelType() == typeof(BundleFile))
			if (!((BundleFile)viewModel).Bundle.LoadSucceeded)
				return;
		FileTabs.Add(newTab);
		SelectedFileTab = newTab;
	}

	[RelayCommand(CanExecute = nameof(CanSave))]
	private async Task<bool> SaveFile(FileTab? tab)
	{
		if (tab == null)
			return false;

		if (tab.Content.DataContext is BundleFile bundleFile)
		{
			bool saveSucceeded = true;
			foreach (var resourceTab in bundleFile.ResourceTabs)
			{
				if (resourceTab.ContentContainer.Content.DataContext is EditorViewModelBase editorViewModel)
				{
					if (editorViewModel.ContentContainer.IsDirty)
					{
						saveSucceeded = await editorViewModel.SaveResource();
						if (!saveSucceeded)
							break;
					}
				}
			}
			if (!saveSucceeded)
				return false;
			if (!bundleFile.SaveBundle())
			{
				_ = OpenMessageWindow("Failed to save bundle.");
				return false;
			}
			return true;
		}
		return false;
	}

	[RelayCommand]
	private async Task SaveFileAs(FileTab? tab)
	{
		if (tab == null)
			return;

		try
		{
			var fileFolderService = App.Current?.Services?.GetService<IFileFolderService>()
				?? throw new Exception("Missing file/folder service instance.");
			var file = await fileFolderService.SaveFileAsync("Save as");
			if (file == null)
				return;
			var oldNode = tab.Node;
			string newPath = Uri.UnescapeDataString(file.Path.AbsolutePath);
			File.Create(newPath).Close();
			tab.Node = new FileSystemEntryNode(newPath);
			bool saveSucceeded = await SaveFile(tab);
			if (!saveSucceeded)
				tab.Node = oldNode;
		}
		catch
		{
			throw;
		}
	}

	[RelayCommand]
	private async Task<int> CloseFile(FileTab? tab)
	{
		if (tab == null)
			return 1;

		if (tab.Content.DataContext is BundleFile bundleFile
			&& Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var dialogService = App.Current?.Services?.GetService<DialogService>()
				?? throw new Exception("Missing dialog service instance.");
			if (bundleFile.Tab.IsDirty)
			{
				bool saveSucceeded;
				var ret = await dialogService.OpenDialog<Views.YesNoDialog, int>(
					new YesNoDialog($"File {tab.Node.Info.Name} was changed. Save changes?", "Save changes?"), desktop.MainWindow!);
				if (ret == 0)
					return 0;
				else if (ret == 1)
				{
					saveSucceeded = await SaveFile(tab);
					if (!saveSucceeded)
						return 0;
				}
			}
			else
			{
				bool isAnyResourceDirty = false;
				foreach (var resourceTab in bundleFile.ResourceTabs)
				{
					if (resourceTab.ContentContainer.Content.DataContext is EditorViewModelBase editorViewModel)
					{
						if (editorViewModel.ContentContainer.IsDirty)
							isAnyResourceDirty = true;
					}
				}
				if (isAnyResourceDirty)
				{
					bool saveSucceeded;
					var ret = await dialogService.OpenDialog<Views.YesNoDialog, int>(
						new YesNoDialog($"File {tab.Node.Info.Name} was changed. Save changes?", "Save changes?"), desktop.MainWindow!);
					if (ret == 0)
						return 0;
					else if (ret == 1)
					{
						saveSucceeded = await SaveFile(tab);
						if (!saveSucceeded)
							return 0;
					}
				}
			}
		}

		if (FileTabs.Count == 1)
			SelectedFileTab = null;
		else if (SelectedFileTab == tab)
		{
			var tabIndex = FileTabs.IndexOf(tab);
			if (tabIndex == FileTabs.Count - 1 && tabIndex != 0)
				SelectedFileTab = FileTabs[tabIndex - 1];
			else
				SelectedFileTab = FileTabs[tabIndex + 1];
		}
		FileTabs.Remove(tab);

		return 1;
	}
}
