using BundleFormat;
using PluginAPI;

namespace LuaList
{

    public class LuaListPlugin : IPlugin
    {
        public string Id => "lualistplugin";
        public string Name => "Lua List Resource Handler";
        
        public static void Init()
        {
            EntryTypeRegistry.Register(EntryType.LUAList, new LuaList());
        }
    }
}
