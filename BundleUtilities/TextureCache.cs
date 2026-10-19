using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace BundleUtilities;

// Locked, as entries load on a worker thread while MainForm resets the cache on the UI thread
public static class TextureCache
{
    private static readonly Dictionary<ulong, Image> _cachedTextures = [];
    private static readonly Lock _lock = new();

    public static void ResetCache()
    {
        lock (_lock)
        {
            foreach (Image img in _cachedTextures.Values)
                img?.Dispose();
            _cachedTextures.Clear();
        }
    }

    public static void AddToCache(ulong id, Image image)
    {
        lock (_lock)
            _cachedTextures[id] = image;
    }

    public static bool Contains(ulong key)
    {
        lock (_lock)
            return _cachedTextures.ContainsKey(key);
    }

    public static Image GetTexture(ulong key)
    {
        lock (_lock)
            return _cachedTextures[key];
    }
}
