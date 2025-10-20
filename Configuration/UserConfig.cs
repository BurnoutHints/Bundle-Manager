using Information.Enums;
using System.Text;
using YamlDotNet.Serialization;

namespace Configuration;

public class UserConfig
{
	public uint ConfigVersion { get; set; } = 1;
	public string GameLocation { get; set; } = string.Empty;
	public Game Game { get; set; } = Game.Invalid;
	public Platform Platform { get; set; } = Platform.Invalid;
	public GameVersion Version { get; set; } = GameVersion.Invalid;
	public string Theme { get; set; } = string.Empty;

	public static UserConfig Config { get; set; } = new();
	public static FileInfo ConfigFileInfo { get; }
	private const string appFolderName = "Bundle Manager Neo";
	private const string configFileName = "config.yaml";

	static UserConfig()
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string appDirectory = Path.Combine(localAppData, appFolderName);
		Directory.CreateDirectory(appDirectory);
		string configFilePath = Path.Combine(appDirectory, configFileName);
		ConfigFileInfo = new(configFilePath);
	}

	public static UserConfig? Load()
	{
		if (!ConfigFileInfo.Exists)
			return null;
		var yamlDeserializer = new DeserializerBuilder().Build();
		string yaml = File.ReadAllText(ConfigFileInfo.FullName, Encoding.UTF8);
		return yamlDeserializer.Deserialize<UserConfig>(yaml);
	}

	public void Save()
	{
		var yamlSerializer = new SerializerBuilder().Build();
		string yaml = yamlSerializer.Serialize(this);
		File.WriteAllText(ConfigFileInfo.FullName, yaml, Encoding.UTF8);
	}
}
