using BundleFormat;
using BundleUtilities;

namespace PluginAPI;

public interface IEntryData
{
    bool Read(BundleEntry entry, ILoader loader = null);

    bool Write(BundleEntry entry);

    EntryType GetEntryType(BundleEntry entry);

    IEntryEditor GetEditor(BundleEntry entry);
}
