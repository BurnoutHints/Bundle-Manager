using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Linq;
using System.Threading.Tasks;

namespace BundleManager.Services;

public class DialogService : IDialogService
{
	public void CloseDialog(object viewModel, bool result)
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var window = desktop.Windows.First(w => w.DataContext == viewModel);
			window?.Close(result);
		}
	}

	public void CloseDialog(object viewModel, int result)
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var window = desktop.Windows.First(w => w.DataContext == viewModel);
			window?.Close(result);
		}
	}

	public async Task<TResult?> OpenDialog<TWindow, TResult>(object viewModel, Window owner)
		where TWindow : Window, new()
	{
		var window = new TWindow
		{
			DataContext = viewModel
		};
		return await window.ShowDialog<TResult>(owner);
	}
}
