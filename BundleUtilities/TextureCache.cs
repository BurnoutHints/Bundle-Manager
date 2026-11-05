using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace BundleUtilities;

// Locked, as entries load on a worker thread while MainForm resets the cache on the UI thread
public static class TextureCache
{
    private static readonly Dictionary<ulong, Image> cachedTextures = [];
    private static readonly Lock cacheLock = new();

    public static void ResetCache()
    {
        lock (cacheLock)
        {
            foreach (Image img in cachedTextures.Values)
                img?.Dispose();

            cachedTextures.Clear();
        }
    }

    public static void AddToCache(ulong id, Image image)
    {
        lock (cacheLock)
            cachedTextures[id] = image;
    }

    public static bool Contains(ulong key)
    {
        lock (cacheLock)
            return cachedTextures.ContainsKey(key);
    }

    public static Image GetTexture(ulong key)
    {
        lock (cacheLock)
            return cachedTextures[key];
    }
}
