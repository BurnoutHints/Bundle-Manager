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
        return string.IsNullOrWhiteSpace(name)
            ? ResourceDB.Lookup(entryID)
            : name;
    }

    private static readonly Dictionary<string, BundleArchive> archives = [];
    private static readonly Lock archivesLock = new();

    public static BundleArchive GetArchive(string path)
    {
        lock (archivesLock)
        {
            if (!archives.TryGetValue(path, out BundleArchive archive))
                archives[path] = archive = BundleArchive.Read(path);

            return archive;
        }
    }

    public static void ClearArchives()
    {
        lock (archivesLock)
            archives.Clear();
    }

    public static BundleEntry GetEntryByID(ulong entryID)
    {
        string currentFile = GetFileByEntryID(entryID);
        return currentFile == null
            ? null
            : GetArchive(currentFile)?.GetEntryByID(entryID);
    }

    public static string GetRelativePath(string path)
    {
        string currentFile = path.Replace('\\', '/').Replace(CurrentPath.Replace('\\', '/'), "");

        if (currentFile.StartsWith("/"))
            currentFile = currentFile[1..];

        return currentFile;
    }
}
