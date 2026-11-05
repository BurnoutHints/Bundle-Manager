using BundleFormat;
using PluginAPI;

namespace WheelList;

public class WheelListPlugin : IPlugin
{
    public string ID => "wheellistplugin";

    public string Name => "WheelList Resource Handler";

    public static void Init()
    {
        EntryTypeRegistry.Register(EntryType.WheelList, new WheelListData());
    }
}
