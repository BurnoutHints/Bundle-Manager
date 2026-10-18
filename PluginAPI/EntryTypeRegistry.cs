using BundleFormat;
using System;
using System.Collections.Generic;

namespace PluginAPI;

public static class EntryTypeRegistry
{
    private static Dictionary<EntryType, IEntryData> _handlers = [];

    public static void Register(EntryType type, IEntryData handler)
    {
        if (IsRegistered(type))
        {
            Console.WriteLine("WARN: Handler for " + type.ToString() + " already registered!");
            return;
        }
        _handlers.Add(type, handler);
    }

    public static void Unregister(EntryType type)
    {
        if (!IsRegistered(type))
            return;
        _handlers.Remove(type);
    }

    public static IEntryData GetHandler(EntryType type)
    {
        if (!IsRegistered(type))
            return null;

        return _handlers[type];
    }

    public static bool IsRegistered(EntryType type)
    {
        return _handlers.ContainsKey(type);
    }
}
