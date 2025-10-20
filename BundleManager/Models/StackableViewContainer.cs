using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace BundleManager.Models;

public partial class StackableViewContainer : ObservableObject
{
	public object ParentViewModel { get; }
	public Stack<UserControl> ViewStack { get; } = [];
	[ObservableProperty]
	private UserControl content;
	[ObservableProperty]
	private bool isDirty;
	public bool CanSave { get; }

	public StackableViewContainer(UserControl initialContent, object parentViewModel, bool canSave = true)
	{
		Content = initialContent;
		ParentViewModel = parentViewModel;
		CanSave = canSave;
		ViewStack.Push(Content);
	}
}
