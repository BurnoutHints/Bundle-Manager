using System;
using System.Collections.Generic;

namespace PluginAPI;

public static class PluginCommandRegistry
{
    private static Dictionary<string, PluginCommand> commands = [];

    public static PluginCommand[] Commands => [.. commands.Values];

    public static void Register(string id, string text, PluginCommand.OnUse use, PluginCommand.OnCheckConditions checkConditions = null)
    {
        Register(new(id, text, use, checkConditions));
    }

    public static void Register(PluginCommand command)
    {
        if (IsRegistered(command.ID))
        {
            Console.WriteLine("WARN: Command " + command.ID.ToString() + " already registered!");
            return;
        }

        commands.Add(command.ID, command);
    }

    public static void Unregister(string id)
    {
        if (!IsRegistered(id))
            return;

        commands.Remove(id);
    }

    public static PluginCommand GetCommand(string id)
    {
        if (!IsRegistered(id))
            return default;

        return commands[id];
    }

    public static bool IsRegistered(string id)
    {
        return commands.ContainsKey(id);
    }
}
