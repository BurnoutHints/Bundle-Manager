using Avalonia;
using Avalonia.Controls;
using System.Windows.Input;

namespace BundleManager.Controls;

public partial class CloseableTabItem : UserControl
{
	public static readonly StyledProperty<string> HeaderProperty =
		AvaloniaProperty.Register<CloseableTabItem, string>(nameof(Header), defaultValue: "Test header");
	public string Header
	{
		get => GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	public static readonly StyledProperty<ICommand?> SaveCommandProperty =
		AvaloniaProperty.Register<CloseableTabItem, ICommand?>(nameof(SaveCommand));
	public ICommand? SaveCommand
	{
		get => GetValue(SaveCommandProperty);
		set => SetValue(SaveCommandProperty, value);
	}

	public static readonly StyledProperty<ICommand?> CloseCommandProperty =
		AvaloniaProperty.Register<CloseableTabItem, ICommand?>(nameof(CloseCommand));
	public ICommand? CloseCommand
	{
		get => GetValue(CloseCommandProperty);
		set => SetValue(CloseCommandProperty, value);
	}

	public static readonly StyledProperty<bool> IsSaveButtonEnabledProperty =
		AvaloniaProperty.Register<CloseableTabItem, bool>(nameof(IsSaveButtonEnabled), defaultValue: false);
	public bool IsSaveButtonEnabled
	{
		get => GetValue(IsSaveButtonEnabledProperty);
		set => SetValue(IsSaveButtonEnabledProperty, value);
	}

	public static readonly StyledProperty<bool> IsSaveButtonVisibleProperty =
		AvaloniaProperty.Register<CloseableTabItem, bool>(nameof(IsSaveButtonVisible), defaultValue: true);
	public bool IsSaveButtonVisible
	{
		get => GetValue(IsSaveButtonVisibleProperty);
		set => SetValue(IsSaveButtonVisibleProperty, value);
	}

	public static readonly StyledProperty<bool> IsCloseButtonVisibleProperty =
		AvaloniaProperty.Register<CloseableTabItem, bool>(nameof(IsCloseButtonVisible), defaultValue: true);
	public bool IsCloseButtonVisible
	{
		get => GetValue(IsCloseButtonVisibleProperty);
		set => SetValue(IsCloseButtonVisibleProperty, value);
	}

	public CloseableTabItem()
	{
		InitializeComponent();
	}
}
