using BundleFormat;
using PluginAPI;

namespace BaseHandlers
{
    public class BasePlugin : Plugin
    {
        public override void Init()
        {
            EntryTypeRegistry.Register(EntryType.Renderable, new Renderable());
            EntryTypeRegistry.Register(EntryType.EntryList, new IDList());
            //EntryTypeRegistry.Register(EntryType.AptData, new AptData());
            EntryTypeRegistry.Register(EntryType.InstanceList, new InstanceList());
            EntryTypeRegistry.Register(EntryType.TrafficData, new Traffic());
            EntryTypeRegistry.Register(EntryType.TriggerData, new TriggerData());
            EntryTypeRegistry.Register(EntryType.GraphicsSpec, new GraphicsSpec());
            EntryTypeRegistry.Register(EntryType.ProgressionData, new ProgressionData());
            EntryTypeRegistry.Register(EntryType.StreetData, new StreetData());
            EntryTypeRegistry.Register(EntryType.FlaptFile, new FlaptFile());
        }

        public override string GetID()
        {
            return "baseplugin";
        }

        public override string GetName()
        {
            return "Base Resource Handlers";
        }
    }
}
