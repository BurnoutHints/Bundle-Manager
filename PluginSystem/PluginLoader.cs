using PluginAPI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PluginSystem
{
    public static class PluginLoader
    {
        // Plugin classes. To add an entry here, the plugin's containing project
        // must be added as a reference.
        private static readonly List<Type> registeredPlugins = 
        [
            typeof(BaseHandlers.BasePlugin),
            typeof(LangEditor.LangPlugin),
            typeof(LoopModel.LoopModelPlugin),
            typeof(LuaList.LuaListPlugin),
            typeof(PVSFormat.PVSPlugin),
            typeof(VaultFormat.AttribSysPlugin),
            typeof(VehicleList.VehicleListPlugin),
            typeof(WheelList.WheelListPlugin),
            typeof(WorldCollisionHandler.WorldCollisionPlugin)
        ];

        // Runs each plugin's static initialization method.
        public static void InitializePlugins()
        {
            Console.WriteLine("Initializing plugins...");
            int numPluginsInitialized = 0;
            foreach (Type pluginType in registeredPlugins)
            {
                if (pluginType.IsAssignableTo(typeof(IPlugin)))
                {
                    MethodInfo initInfo = pluginType.GetMethod(nameof(IPlugin.Init));
                    initInfo.Invoke(null, []);
                    numPluginsInitialized++;
                }
                else
                {
                    Console.WriteLine($"{pluginType.AssemblyQualifiedName} is not a valid plugin. Skipping.");
                }
            }
            Console.WriteLine($"Initialized {numPluginsInitialized} plugins.");
        }
    }
}
