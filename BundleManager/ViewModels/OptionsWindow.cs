using Avalonia;
using Avalonia.Styling;
using BundleManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Configuration;
using Information;
using Information.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BundleManager.ViewModels;

public partial class OptionsWindow : ObservableObject
{
	private readonly MainWindow mainWindowViewModel;
	[ObservableProperty]
	private string gameLocation = string.Empty;
	[ObservableProperty]
	private GameInfo.Entry? selectedGame;
	[ObservableProperty]
	private PlatformInfo? selectedPlatform;
	[ObservableProperty]
	private VersionInfo? selectedVersion;
	[ObservableProperty]
	private int selectedThemeIndex;
	[ObservableProperty]
	private string warningText = string.Empty;
	[ObservableProperty]
	private bool isSavingEnabled = false;

	public string[] Themes { get; } =
	[
		"Use system setting",
		"Dark",
		"Light"
	];

	// Must exist for preview to work
	public OptionsWindow()
	{
		LoadUserConfig();
		mainWindowViewModel = new();
	}

	public OptionsWindow(MainWindow mainWindow)
	{
		LoadUserConfig();
		mainWindowViewModel = mainWindow;
	}

	[RelayCommand]
	public async Task ChooseGameLocation()
	{
		try
		{
			var fileFolderService = App.Current?.Services?.GetService<IFileFolderService>()
				?? throw new Exception("Missing file/folder service instance.");
			var folder = await fileFolderService.PickFolderAsync("Select game location");
			if (folder == null)
				return;
			GameLocation = Uri.UnescapeDataString(folder.Path.AbsolutePath);
		}
		catch
		{
			throw;
		}
	}

	[RelayCommand]
	public void SetSelectedTheme()
	{
		if (Themes[SelectedThemeIndex] == "Dark")
			Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
		else if (Themes[SelectedThemeIndex] == "Light")
			Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
		else
			Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
	}

	public void LoadUserConfig()
	{
		if (UserConfig.Config.GameLocation == string.Empty
			|| UserConfig.Config.Game == Game.Invalid
			|| UserConfig.Config.Platform == Platform.Invalid
			|| UserConfig.Config.Version == GameVersion.Invalid
			|| UserConfig.Config.Theme == string.Empty)
			return;
		GameLocation = UserConfig.Config.GameLocation;
		SelectedGame = GameInfo.Info.Where(g => UserConfig.Config.Game == g.Id).First();
		SelectedPlatform = SelectedGame.Platforms.Where(p => UserConfig.Config.Platform == p.Id).First();
		Avalonia.Threading.Dispatcher.UIThread.Post(() =>
		{
			SelectedVersion = SelectedPlatform.Versions.Where(v => UserConfig.Config.Version == v.Id).First();
		}, Avalonia.Threading.DispatcherPriority.Background);
		SelectedThemeIndex = Array.FindIndex(Themes, t => t == UserConfig.Config.Theme);
	}

	[RelayCommand]
	public void SaveUserSettings()
	{
		bool updateTree = UserConfig.Config.GameLocation != GameLocation;
		UserConfig.Config.GameLocation = GameLocation;
		UserConfig.Config.Game = SelectedGame!.Id;
		UserConfig.Config.Platform = SelectedPlatform!.Id;
		UserConfig.Config.Version = SelectedVersion!.Id;
		UserConfig.Config.Theme = Themes[SelectedThemeIndex];
		UserConfig.Config.Save();
		if (updateTree)
			mainWindowViewModel.ResetTree();
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		dialogService.CloseDialog(this, true);
	}

	[RelayCommand]
	public void CloseOptionsWindow()
	{
		// Themes are dynamically set. Reset if a new one was applied but not saved.
		if (UserConfig.Config.Theme != string.Empty
			&& Themes[SelectedThemeIndex] != UserConfig.Config.Theme)
		{
			SelectedThemeIndex = Array.FindIndex(Themes, t => t == UserConfig.Config.Theme);
			SetSelectedTheme();
		}
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		dialogService.CloseDialog(this, false);
	}

	partial void OnGameLocationChanged(string value)
	{
		EnableOrDisableSaveButton();
		if (SelectedPlatform != null)
			SetGameLocationWarningText();
	}

	partial void OnSelectedGameChanged(GameInfo.Entry? value)
	{
		EnableOrDisableSaveButton();
	}

	partial void OnSelectedPlatformChanged(PlatformInfo? value)
	{
		if (value != null)
		{
			// Neo's first race condition! :D
			// (ComboBox ItemsSource is changed after the new version is selected)
			var newVersion = value.Versions[value.DefaultVersionIndex];
			Avalonia.Threading.Dispatcher.UIThread.Post(() =>
			{
				SelectedVersion = newVersion;
			}, Avalonia.Threading.DispatcherPriority.Background);
		}
		SetGameLocationWarningText();
		EnableOrDisableSaveButton();
	}

	partial void OnSelectedVersionChanged(VersionInfo? value)
	{
		EnableOrDisableSaveButton();
	}

	public void EnableOrDisableSaveButton()
	{
		if (GameLocation == string.Empty || SelectedGame == null || SelectedPlatform == null || SelectedVersion == null)
		{
			IsSavingEnabled = false;
			return;
		}
		DirectoryInfo gameLocationInfo = new(GameLocation);
		if (!gameLocationInfo.Exists)
		{
			IsSavingEnabled = false;
			return;
		}
		IsSavingEnabled = true;
	}

	public void SetGameLocationWarningText()
	{
		if (GameLocation == string.Empty || SelectedGame == null || SelectedPlatform == null)
		{
			WarningText = string.Empty;
			return;
		}
		DirectoryInfo dirInfo = new(GameLocation);
		bool executableFound = false;

		if (SelectedPlatform.Id == Platform.PlayStation3
			|| SelectedPlatform.Id == Platform.PlayStation4
			|| SelectedPlatform.Id == Platform.PlayStationVita)
		{
			// BOOT.BIN exists, but only on PSP, where there are no bundles
			foreach (FileInfo fileInfo in dirInfo.GetFiles())
			{
				if (fileInfo.Name.Equals("EBOOT.BIN", StringComparison.OrdinalIgnoreCase)
					|| fileInfo.Name.EndsWith(".self", StringComparison.OrdinalIgnoreCase)
					|| fileInfo.Name.EndsWith(".elf", StringComparison.OrdinalIgnoreCase))
					executableFound = true;
			}
			if (!executableFound)
				WarningText = "Could not find eboot.bin or a self/elf";
		}
		else if (SelectedPlatform.Id == Platform.Xbox360)
		{
			foreach (FileInfo fileInfo in dirInfo.GetFiles())
			{
				if (fileInfo.Name.EndsWith(".xex", StringComparison.OrdinalIgnoreCase)
					|| fileInfo.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
					executableFound = true;
			}
			if (!executableFound)
				WarningText = "Could not find a xex/exe";
		}
		else if (SelectedPlatform.Id == Platform.PC
			|| SelectedPlatform.Id == Platform.XboxOne)
		{
			string executableName = string.Empty;
			if (SelectedPlatform.Id == Platform.PC)
			{
				switch (SelectedGame.Id)
				{
					case Game.BurnoutParadise:
						executableName = "BurnoutParadise.exe";
						break;
					case Game.BurnoutParadiseRemastered:
						executableName = "BurnoutPR.exe";
						break;
					case Game.HotPursuit:
						executableName = "NFS11.exe";
						break;
					case Game.HotPursuitRemastered:
						executableName = "NFS11Remastered.exe";
						break;
					case Game.MostWanted:
						executableName = "NFS13.exe";
						break;
				}
			}
			else if (SelectedPlatform.Id == Platform.XboxOne)
			{
				switch (SelectedGame.Id)
				{
					case Game.BurnoutParadiseRemastered:
						executableName = "Burnout_External_Xbox_One.exe";
						break;
					case Game.HotPursuitRemastered:
						// TODO: Dump game and get executable name
						WarningText = string.Empty;
						return;
				}
			}
			foreach (FileInfo fileInfo in dirInfo.GetFiles())
			{
				if (fileInfo.Name == executableName)
					executableFound = true;
			}
			if (!executableFound)
				WarningText = string.Concat("Could not find ", executableName);
		}
		else
		{
			// Wii U and Switch use platform-wide naming but cannot be checked as their
			// executables are stored separately from assets
			WarningText = string.Empty;
		}

		if (!executableFound)
			return;
		WarningText = string.Empty;
	}
}
