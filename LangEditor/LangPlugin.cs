using BundleFormat;
using PluginAPI;

namespace LangEditor
{
    public class LangPlugin : IPlugin
    {
        public string Id => "langplugin";
        public string Name => "Language Resource Handler";

        public static void Init()
        {
            EntryTypeRegistry.Register(EntryType.Language, new Language());
        }
    }
}
