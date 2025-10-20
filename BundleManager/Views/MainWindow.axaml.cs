using Avalonia.Controls;
using Configuration;
using System;

namespace BundleManager.Views;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
		RestoreWindowState();
	}

	private void RestoreWindowState()
	{
		var windowState = MainWindowState.Load();
		if (windowState == null)
		{
			Width = 1200;
			Height = 675;
			return;
		}

		Position = new Avalonia.PixelPoint(windowState.X, windowState.Y);
		Width = windowState.Width;
		Height = windowState.Height;
		if (windowState.IsMaximized)
			WindowState = WindowState.Maximized;
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		MainWindowState.State.X = Position.X;
		MainWindowState.State.Y = Position.Y;
		MainWindowState.State.Width = Width;
		MainWindowState.State.Height = Height;
		MainWindowState.State.IsMaximized = WindowState == WindowState.Maximized;

		var grid = this.FindControl<Grid>("TreeContentGrid");
		MainWindowState.State.TreeColumnWidth = grid!.ColumnDefinitions[0].Width.Value;

		MainWindowState.State.Save();
		base.OnClosing(e);
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		if (DataContext is ViewModels.MainWindow viewModel)
		{
			var windowState = Configuration.MainWindowState.Load();
			if (windowState == null)
				return;
			viewModel.TreeColumnWidth = new GridLength(windowState.TreeColumnWidth, GridUnitType.Pixel);
		}
		base.OnDataContextChanged(e);
	}
}
