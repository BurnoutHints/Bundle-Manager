using BundleFormat;
using PluginAPI;

namespace VaultFormat
{
    public class AttribSysPlugin : IPlugin
    {
        public string Id => "attribsysplugin";
        public string Name => "AttribSys Resource Handler";

        public static void Init()
        {
            EntryTypeRegistry.Register(EntryType.AttribSysVault, new AttribSys());
        }
    }
}
