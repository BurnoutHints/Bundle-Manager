namespace BundleFormat;

public struct BundleDependency
{
    public ulong EntryID;
    public int Unknown1;
    public int EntryPointerOffset;

    public int EntryIndex;
    public BundleEntry Entry;

    public override string ToString()
    {
        string value = "";

        bool external = Entry == null;

        string extra = "";

        if (Entry == null)
        {
            string currentFile = BundleCache.GetFileByEntryID(EntryID);

            if (!string.IsNullOrEmpty(currentFile))
            {
                extra = ", Path: " + BundleCache.GetRelativePath(currentFile);
                Entry = BundleCache.GetEntryByID(EntryID);
            }
        }

        if (Entry != null && Entry.Type == EntryType.VertexDescriptor)
        {
            VertexDesc desc = VertexDesc.Read(Entry);
            value = ", Attribute Count: " + desc.AttributeCount.ToString("D2");
        }

        string location = external
            ? "External"
            : "Internal";

        string info = "(External)";

        if (Entry != null)
            info = "(" + location + ": " + EntryIndex.ToString("D3") + ", " + Entry.Type + value + extra + ")";

        return "ID: 0x" + EntryID.ToString("X8") + ", PtrOffset: 0x" + EntryPointerOffset.ToString("X8") + " " + info;
    }
}
