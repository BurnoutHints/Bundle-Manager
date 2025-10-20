using Avalonia.Controls;

namespace BundleManager.Views;

public partial class AboutWindow : Window
{
	public AboutWindow()
	{
		InitializeComponent();
		DataContext = new ViewModels.AboutWindow();
	}
}
