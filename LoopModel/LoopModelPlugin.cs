using BundleFormat;
using PluginAPI;

namespace LoopModel
{
    public class LoopModelPlugin : Plugin
    {
        public override void Init()
        {
            EntryTypeRegistry.Register(EntryType.LoopModel, new LoopModelData());
        }

        public override string GetID()
        {
            return "loopmodelplugin";
        }

        public override string GetName()
        {
            return "LoopModel Resource Handler";
        }
    }
}
