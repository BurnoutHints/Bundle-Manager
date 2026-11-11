using System.Linq;
using BaseHandlers.EnvironmentSettings;
using BaseHandlers.Flapt;
using BundleFormat;
using PluginAPI;

namespace BaseHandlers;

public class BasePlugin : IPlugin
{
    public string ID => "baseplugin";

    public string Name => "Base Resource Handlers";

    public static void Init()
    {
        EntryTypeRegistry.Register(EntryType.Renderable, new Renderable());
        EntryTypeRegistry.Register(EntryType.EntryList, new IDList());

        //EntryTypeRegistry.Register(EntryType.AptData, new AptData());
        EntryTypeRegistry.Register(EntryType.InstanceList, new InstanceList());
        EntryTypeRegistry.Register(EntryType.TrafficData, new Traffic());
        EntryTypeRegistry.Register(EntryType.AISections, new AISections());
        EntryTypeRegistry.Register(EntryType.PropPhysics, new PropPhysics());
        EntryTypeRegistry.Register(EntryType.VFXPropCollection, new VFXPropCollection());
        EntryTypeRegistry.Register(EntryType.ParticleDescriptionCollection, new ParticleDescriptionCollection());
        EntryTypeRegistry.Register(EntryType.TextureNameMap, new TextureNameMap());
        EntryTypeRegistry.Register(EntryType.ParticleDescription, new LionEffect());
        PluginCommandRegistry.Register("export-lef", "Export LION Effects (.lef)...", LionEffect.ExportAll,
            archive => archive.Entries.Any(e => e.Type == EntryType.ParticleDescription));
        EntryTypeRegistry.Register(EntryType.TriggerData, new TriggerData());
        EntryTypeRegistry.Register(EntryType.GraphicsSpec, new GraphicsSpec());
        EntryTypeRegistry.Register(EntryType.ProgressionData, new ProgressionData());
        EntryTypeRegistry.Register(EntryType.StreetData, new StreetData());
        EntryTypeRegistry.Register(EntryType.FlaptFile, new FlaptFile());
        EntryTypeRegistry.Register(EntryType.BrnEnvironmentKeyframe, new EnvironmentKeyframe());
        EntryTypeRegistry.Register(EntryType.BrnEnvironmentTimeLine, new EnvironmentTimeLine());
    }
}
