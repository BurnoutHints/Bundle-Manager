using BundleManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BundleManager.ViewModels;

public partial class YesNoDialog : ObservableObject
{
	public string MessageText { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;

	public YesNoDialog(string messageText, string? title = null)
	{
		MessageText = messageText;
		if (title != null)
			Title = title;
	}

	[RelayCommand]
	public void AcceptYesNoDialog()
	{
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		dialogService.CloseDialog(this, 1);
	}

	[RelayCommand]
	public void CloseYesNoDialog()
	{
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		dialogService.CloseDialog(this, 2);
	}
}
