using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BundleManager.Services;
using Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace BundleManager;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override async void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			// Avoid duplicate validations from both Avalonia and the CommunityToolkit.
			// More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
			DisableAvaloniaDataAnnotationValidation();

			var mainWindowViewModel = new ViewModels.MainWindow();
			desktop.MainWindow = new Views.MainWindow()
			{
				DataContext = mainWindowViewModel
			};

			var services = new ServiceCollection();
			services.AddSingleton(s => new DialogService());
			services.AddSingleton<IFileFolderService>(s => new FileFolderService(desktop.MainWindow));
			Services = services.BuildServiceProvider();

			var config = UserConfig.Load();
			if (config == null)
			{
				// Should only happen on first startup
				Views.OptionsWindow optionsWindow = new()
				{
					DataContext = new ViewModels.OptionsWindow(mainWindowViewModel)
				};
				desktop.MainWindow.Show(); // Must be shown: throws on showing dialog from non-visible owner
				bool result = await optionsWindow.ShowDialog<bool>(desktop.MainWindow);
				// Now the options window has set up the config, or user closed it
				if (result == false)
				{
					desktop.Shutdown();
					return;
				}
			}
			else
			{
				UserConfig.Config = config;
			}
			mainWindowViewModel.Setup();
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void DisableAvaloniaDataAnnotationValidation()
	{
		// Get an array of plugins to remove
		var dataValidationPluginsToRemove =
			BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

		// remove each entry found
		foreach (var plugin in dataValidationPluginsToRemove)
			BindingPlugins.DataValidators.Remove(plugin);
	}

	public new static App? Current => Application.Current as App;

	public IServiceProvider? Services { get; private set; }
}
