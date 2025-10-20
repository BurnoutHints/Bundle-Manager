using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BundleFormat.Interfaces;
using BundleFormat.Registry;
using BundleManager.Services;
using Configuration;
using Information;
using Information.Enums;
using Microsoft.Extensions.DependencyInjection;
using Serialization.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BundleManager.Models;

public partial class BundleResource
{
	public IResource Resource { get; }
	public Type? Type { get; }
	public object? Object { get; }
	public ObservableCollection<PropertyDetails> Properties { get; } = [];
	public BundleResourceTab ResourceTab { get; }
	public bool LoadSucceeded { get; } = false;
	
	public BundleResource(IResource resource, BundleResourceTab resourceTab)
	{
		Resource = resource;
		ResourceTab = resourceTab;
		Type = LoadResourceType(Resource);
		if (Type == null)
			return;
		var deserializeMethod = Type.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static);
		Object = deserializeMethod!.Invoke(null, [ Type, Resource ])!;
		foreach (var property in Type.GetProperties())
			Properties.Add(new PropertyDetails(Object, Object, property));

		LoadSucceeded = true;
	}

	private Type? LoadResourceType(IResource resource)
	{
		Type? modelType = null;
		UniqueStructure[]? uniqueStructures;
		bool gotUniqueStructures;
		if (Resource.Config.Game.Id == Game.Black2
			|| Resource.Config.Game.Id == Game.BurnoutParadise
			|| Resource.Config.Game.Id == Game.BurnoutParadiseRemastered)
			gotUniqueStructures = ResourceTypeClassRegistry.Registry.TryGetValue((ResourceType)resource.Type, out uniqueStructures);
		else
			gotUniqueStructures = ResourceTypeClassRegistry.RegistryV2.TryGetValue((ResourceTypeV2)resource.Type, out uniqueStructures);
		if (!gotUniqueStructures)
		{
			_ = OpenMessageWindow($"Resource type {resource.TypeName} is unsupported.");
			return null;
		}
		var configuredGameDetails = new GameDetails
		{
			Game = Resource.Config.Game.Id,
			Platform = Resource.Config.Platform.Id,
			Version = Resource.Config.Version.Id
		};
		foreach (var uniqueStructure in uniqueStructures!)
		{
			foreach (var gameDetails in uniqueStructure.ApplicableGames)
			{
				if (configuredGameDetails == gameDetails)
					modelType = uniqueStructure.TopLevelClass;
			}
		}
		if (modelType == null)
		{
			string messageText = $"Unsupported {resource.TypeName} variant: "
				+ $"{configuredGameDetails.Game}, {configuredGameDetails.Platform}, {configuredGameDetails.Version}";
			_ = OpenMessageWindow(messageText);
			return null;
		}
		if (!modelType.GetInterfaces().Contains(typeof(ISerializableObject))
			&& !modelType.GetInterfaces().Contains(typeof(ISerializableResourceType)))
		{
			string messageText = $"Type {modelType.FullName} does not implement ISerializableObject or ISerializableResource.";
			_ = OpenMessageWindow(messageText);
			return null;
		}
		return modelType;
	}

	private async Task OpenMessageWindow(string messageText)
	{
		var dialogService = App.Current?.Services?.GetService<DialogService>()
			?? throw new Exception("Missing dialog service instance.");
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			await dialogService.OpenDialog<Views.MessageWindow, bool>(new ViewModels.MessageWindow(messageText), desktop.MainWindow!);
	}
}
