using YamlDotNet.Serialization;

namespace Information;

public static class ResourceInfo
{
	private const string resourceIdsFileName = "ResourceIds.yaml";
	public static Dictionary<ulong, string> Ids { get; }

	static ResourceInfo()
	{
		var yamlDeserializer = new DeserializerBuilder().Build();
		Ids = yamlDeserializer.Deserialize<Dictionary<ulong, string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, resourceIdsFileName)));
	}
}
