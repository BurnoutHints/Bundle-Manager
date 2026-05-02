using BundleFormat;
using PluginAPI;

namespace LoopModel
{
    public class LoopModelPlugin : IPlugin
    {
        public string Id => "loopmodelplugin";
        public string Name => "LoopModel Resource Handler";

        public static void Init()
        {
            EntryTypeRegistry.Register(EntryType.LoopModel, new LoopModelData());
        }
    }
}
