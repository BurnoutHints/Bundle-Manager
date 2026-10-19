using System.Collections.Generic;
using System.Threading;

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

    private static readonly Dictionary<string, BundleArchive> Archives = [];
    private static readonly Lock ArchivesLock = new();

    public static BundleArchive GetArchive(string path)
    {
        lock (ArchivesLock)
        {
            if (!Archives.TryGetValue(path, out BundleArchive archive))
                Archives[path] = archive = BundleArchive.Read(path);
            return archive;
        }
    }

    public static void ClearArchives()
    {
        lock (ArchivesLock)
            Archives.Clear();
    }

    public static BundleEntry GetEntryByID(ulong entryID)
    {
        string file = GetFileByEntryID(entryID);
        return file == null ? null : GetArchive(file)?.GetEntryByID(entryID);
    }

    public static string GetRelativePath(string path)
    {
        string file = path.Replace('\\', '/').Replace(CurrentPath.Replace('\\', '/'), "");

        if (file.StartsWith("/"))
            file = file[1..];

        return file;
    }
}
