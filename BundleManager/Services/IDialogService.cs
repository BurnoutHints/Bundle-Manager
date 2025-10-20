using Avalonia.Controls;
using System.Threading.Tasks;

namespace BundleManager.Services;

public interface IDialogService
{
	void CloseDialog(object viewModel, bool result = true);
	Task<TResult?> OpenDialog<TWindow, TResult>(object viewModel, Window owner)
		where TWindow : Window, new();
}
