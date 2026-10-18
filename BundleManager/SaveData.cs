using System;
using System.Collections.Generic;
using System.IO;

namespace BundleManager;

public static class SaveData
{
    public static string SaveFolder = Environment.GetEnvironmentVariable("APPDATA") + "\\BurnoutStudio\\";
    public static string RecentFilesPath = SaveFolder + "RecentFiles.txt";

    public static List<string> RecentPaths = [];

    private static bool _initialized;

    public static void Init()
    {
        if (_initialized)
            return;
        _initialized = true;

        if (!Directory.Exists(SaveFolder))
            Directory.CreateDirectory(SaveFolder);
    }

    public static void Load()
    {
        Init();

        RecentPaths.Clear();

        if (!File.Exists(RecentFilesPath))
            return;

        Stream s = File.Open(RecentFilesPath, FileMode.Open, FileAccess.Read);
        StreamReader sr = new(s);

        while (!sr.EndOfStream)
        {
            string line = sr.ReadLine();
            if (line != null && line.Trim().Length > 0)
                RecentPaths.Add(line);
        }

        sr.Close();
        s.Close();
    }

    public static void Save()
    {
        Init();

        Stream s = File.Open(RecentFilesPath, FileMode.Create);
        StreamWriter sw = new(s);

        for (int i = 0; i < RecentPaths.Count; i++)
        {
            sw.WriteLine(RecentPaths[i]);
        }

        sw.Flush();
        sw.Close();
        s.Close();
    }

    public static void AddRecentPath(string path)
    {
        Load();
        if (RecentPaths.Contains(path))
            RecentPaths.Remove(path);
        RecentPaths.Add(path);
        if (RecentPaths.Count > 10)
            RecentPaths.RemoveAt(0);
    }
}
