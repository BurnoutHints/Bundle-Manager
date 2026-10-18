using System.Windows.Forms;

namespace PluginAPI;

public interface IEntryEditor
{
    DialogResult ShowDialog(IWin32Window owner);
}
