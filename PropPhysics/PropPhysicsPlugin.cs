using BundleFormat;
using PluginAPI;

namespace PropPhysics
{
    public class PropPhysicsPlugin : Plugin
    {
        public override void Init()
        {
            EntryTypeRegistry.Register(EntryType.PropPhysics, new PropPhysicsData());
        }

        public override string GetID()
        {
            return "propphysicsplugin";
        }

        public override string GetName()
        {
            return "PropPhysics Resource Handler";
        }
    }
}
