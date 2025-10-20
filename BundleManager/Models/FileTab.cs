using Avalonia.Controls;
using BundleManager.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Configuration;
using Information;
using System;

namespace BundleManager.Models;

public partial class FileTab : ObservableObject
{
	private MainWindow parent;
	[ObservableProperty]
	private FileSystemEntryNode node;
	public UserControl Content { get; }
	public FileConfig Config { get; }
	[ObservableProperty]
	private bool isDirty;

	public FileTab(FileSystemEntryNode node, UserControl content, MainWindow parent)
	{
		Node = node;
		Content = content;
		this.parent = parent;
		GameInfo.Entry game = Array.Find(GameInfo.Info, g => g.Id == UserConfig.Config.Game)
			?? throw new Exception("Game was null.");
		PlatformInfo platform = Array.Find(game.Platforms, p => p.Id == UserConfig.Config.Platform)
			?? throw new Exception("Platform was null.");
		VersionInfo version = Array.Find(platform.Versions, v => v.Id == UserConfig.Config.Version)
			?? throw new Exception("Version was null.");
		Config = new FileConfig { Game = game, Platform = platform, Version = version };
	}

	partial void OnIsDirtyChanged(bool value)
	{
		parent?.NotifyCanSaveChanged();
	}
}
