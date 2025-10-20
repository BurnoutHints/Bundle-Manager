using Avalonia.Controls;

namespace BundleManager.Views;

public partial class ObjectList : UserControl
{
	public ObjectList()
	{
		InitializeComponent();
	}

    private void LoadingRowCommand(object? sender, Avalonia.Controls.DataGridRowEventArgs e)
    {
    }
}
