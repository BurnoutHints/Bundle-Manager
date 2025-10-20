using Avalonia;
using BundleFormat.Interfaces;
using BundleManager.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Configuration;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace BundleManager.ViewModels;

public partial class PropertyGrid : EditorViewModelBase
{
	public ObservableCollection<PropertyDetails> Properties { get; } = [];
	public bool IsChild { get => ContentContainer.ViewStack.Count > 1; }
	[ObservableProperty]
	private Vector scrollOffset = new();

	public PropertyGrid(object topLevelObject, object obj,
		IResource resource, StackableViewContainer contentContainer)
		: base(topLevelObject, obj, resource, contentContainer)
	{
		foreach (var property in Type.GetProperties())
			Properties.Add(new PropertyDetails(topLevelObject, obj, property));
	}

	public PropertyGrid(object topLevelObject, object obj, Stream stream,
		FileConfig config, StackableViewContainer contentContainer)
		: base(topLevelObject, obj, stream, config, contentContainer)
	{
		foreach (var property in Type.GetProperties())
			Properties.Add(new PropertyDetails(topLevelObject, obj, property));
	}
}
