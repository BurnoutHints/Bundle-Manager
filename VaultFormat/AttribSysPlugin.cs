using PluginAPI;

namespace VaultFormat;

public class AttribSysPlugin : IPlugin
{
    public string ID => "attribsysplugin";

    public string Name => "AttribSys Resource Handler";

    // Vaults are handled by BaseHandlers.AttribVault, which reads every class from the game's schema
    public static void Init()
    {
    }
}
