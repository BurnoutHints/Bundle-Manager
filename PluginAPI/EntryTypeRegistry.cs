using BundleFormat;
using System;
using System.Collections.Generic;

namespace PluginAPI;

public static class EntryTypeRegistry
{
    private static Dictionary<EntryType, IEntryData> handlers = [];

    public static void Register(EntryType type, IEntryData handler)
    {
        if (IsRegistered(type))
        {
            Console.WriteLine("WARN: Handler for " + type.ToString() + " already registered!");
            return;
        }

        handlers.Add(type, handler);
    }

    public static void Unregister(EntryType type)
    {
        if (!IsRegistered(type))
            return;

        handlers.Remove(type);
    }

    public static IEntryData GetHandler(EntryType type)
    {
        if (!IsRegistered(type))
            return null;

        return handlers[type];
    }

    public static bool IsRegistered(EntryType type)
    {
        return handlers.ContainsKey(type);
    }
}
