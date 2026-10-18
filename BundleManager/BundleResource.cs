using System.Windows.Forms;
using BundleFormat;
using BundleUtilities;

namespace BundleManager;

public abstract class BundleResource
{
    protected BundleEntry Entry;

    public EntryType Type => Entry.Type;

    protected BundleResource(BundleEntry entry, ILoader loader = null)
    {
        Entry = entry;
        _init(loader);
    }

    private void _init(ILoader loader)
    {
        Read(loader);
    }

    protected abstract void Read(ILoader loader = null);
    protected abstract void Write(ILoader loader = null);
    public abstract void ShowPreview(IWin32Window parent = null);
}
