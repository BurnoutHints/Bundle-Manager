using BundleManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Reflection;

namespace BundleManager.ViewModels;

public partial class AboutWindow : ObservableObject
{
	public string BuildDate { get; set; } = string.Empty;

	public AboutWindow()
	{
		BuildDate = GetBuildDate(Assembly.GetExecutingAssembly()).ToString("yyyy-MM-dd HH:mm:ss");
	}

	[RelayCommand]
	private void CloseAboutWindow()
	{
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		dialogService.CloseDialog(this, true);
	}

	[AttributeUsage(AttributeTargets.Assembly)]
	internal class BuildDateAttribute : Attribute
	{
		public BuildDateAttribute(string value)
		{
			DateTime = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None);
		}

		public DateTime DateTime { get; }
	}

	private static DateTime GetBuildDate(Assembly assembly)
	{
		var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
		return attribute != null ? attribute.DateTime : default(DateTime);
	}
}
