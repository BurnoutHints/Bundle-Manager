using System.Linq;
using BundleFormat;

namespace BundleManager;

// Deep copy mutable entry state while retaining shared archive reference
internal static class EntryDraft
{
    public static BundleEntry Copy(BundleEntry entry)
    {
        BundleEntry copy = new(entry.Archive);
        Assign(entry, copy);
        copy.Dirty = false;
        return copy;
    }

    public static void Assign(BundleEntry from, BundleEntry to)
    {
        to.ID = from.ID;
        to.Index = from.Index;
        to.Platform = from.Platform;
        to.Type = from.Type;
        to.DebugInfo = from.DebugInfo;
        to.DependenciesListOffset = from.DependenciesListOffset;
        to.DependencyCount = from.DependencyCount;
        to.Dependencies = [.. from.Dependencies];
        to.EntryBlocks = [.. from.EntryBlocks.Select(block => block == null
            ? null
            : new EntryBlock()
            {
                Compressed = block.Compressed,
                CompressedSize = block.CompressedSize,
                UncompressedSize = block.UncompressedSize,
                UncompressedAlignment = block.UncompressedAlignment,
                RawData = block.RawData?.ToArray()
            })];
    }
}
