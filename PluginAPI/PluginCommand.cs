using BundleFormat;
using System.Windows.Forms;

namespace PluginAPI;

public struct PluginCommand
{
    public delegate void OnUse(IWin32Window window, BundleArchive archive);
    public delegate bool OnCheckConditions(BundleArchive archive);

    public string ID;
    public string Text;
    public OnUse Use;
    public OnCheckConditions CheckConditions;

    public PluginCommand(string id, string text, OnUse use, OnCheckConditions checkConditions = null)
    {
        ID = id;
        Text = text;
        CheckConditions = checkConditions;
        Use = use;
    }
}
