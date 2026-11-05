namespace PluginAPI;

public interface IPlugin
{
    string ID { get; }

    string Name { get; }

    public static abstract void Init();
}
