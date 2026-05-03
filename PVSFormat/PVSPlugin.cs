using BundleFormat;
using PluginAPI;

namespace PVSFormat
{
    public class PVSPlugin : IPlugin
    {
        public string Id => "pvsplugin";
        public string Name => "PVS Resource Handler";

        public static void Init()
        {
            EntryTypeRegistry.Register(EntryType.ZoneList, new PVS());
        }
    }
}
