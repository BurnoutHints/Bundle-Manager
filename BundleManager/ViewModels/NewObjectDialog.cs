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
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BundleManager.ViewModels;

public partial class NewObjectDialog : ObservableObject
{
	private readonly PropertyGrid propertyGridViewModel;
	private readonly Views.PropertyGrid propertyGridView = new();
	public StackableViewContainer ContentContainer { get; set; }

	public NewObjectDialog(object topLevelObject, object obj, IResource resource)
	{
		ContentContainer = new StackableViewContainer(propertyGridView, this, false);
		propertyGridViewModel = new(topLevelObject, obj, resource, ContentContainer);
		propertyGridView.DataContext = propertyGridViewModel;
	}

	public NewObjectDialog(object topLevelObject, object obj,
		Stream stream, FileConfig config)
	{
		ContentContainer = new StackableViewContainer(propertyGridView, this, false);
		propertyGridViewModel = new(topLevelObject, obj, stream, config, ContentContainer);
		propertyGridView.DataContext = propertyGridViewModel;
	}

	[RelayCommand]
	public async Task AcceptNewObjectDialog()
	{
		string? error = Validator.ValidateRequiredProperties(propertyGridViewModel.Type, propertyGridViewModel.Object);
		if (error != null)
		{
			await OpenMessageWindow(error);
			return;
		}

		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		dialogService.CloseDialog(this, true);
	}

	[RelayCommand]
	public void CloseNewObjectDialog()
	{

		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		dialogService.CloseDialog(this, false);
	}

	private async Task OpenMessageWindow(string messageText)
	{

		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			await dialogService.OpenDialog<Views.MessageWindow, bool>(new ViewModels.MessageWindow(messageText), desktop.MainWindow!);
	}
}
