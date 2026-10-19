using System.Collections.Generic;

namespace BundleFormat;

public static class BundleCache
{
    public static string CurrentPath;
    public static List<string> Paths = [];
    public static Dictionary<ulong, int> Files = [];
    public static Dictionary<ulong, EntryInfo> EntryInfos = [];

    public static string GetFileByEntryID(ulong entryID)
    {
        if (!Files.ContainsKey(entryID))
            return null;
        return Paths[Files[entryID]];
    }

    public static EntryInfo GetInfoByEntryID(ulong entryID)
    {
        if (!EntryInfos.ContainsKey(entryID))
            return null;
        return EntryInfos[entryID];
    }

    public static DebugInfo GetEntryDebugInfoByID(ulong entryID)
    {
        if (!EntryInfos.ContainsKey(entryID))
            return default;
        return EntryInfos[entryID].DebugInfo;
    }

    public static string GetEntryNameByID(ulong entryID)
    {
        string name = GetEntryDebugInfoByID(entryID).Name;
        return string.IsNullOrWhiteSpace(name) ? ResourceDB.Lookup(entryID) : name;
    }

    public static string GetRelativePath(string path)
    {
        string file = path.Replace('\\', '/').Replace(CurrentPath.Replace('\\', '/'), "");

        if (file.StartsWith("/"))
            file = file[1..];

        return file;
    }
}
