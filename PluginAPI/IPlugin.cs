namespace PluginAPI
{
    public interface IPlugin
    {
        string Id { get; }
        string Name { get; }

        public static abstract void Init();
    }
}
