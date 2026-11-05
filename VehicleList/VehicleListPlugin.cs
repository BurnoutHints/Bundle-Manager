using BundleFormat;
using PluginAPI;

namespace VehicleList;

public class VehicleListPlugin : IPlugin
{
    public string ID => "vehiclelistplugin";

    public string Name => "VehicleList Resource Handler";

    public static void Init()
    {
        EntryTypeRegistry.Register(EntryType.VehicleList, new VehicleListData());
    }
}
