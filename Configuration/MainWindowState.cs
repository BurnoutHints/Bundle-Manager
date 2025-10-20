using System.Text;
using YamlDotNet.Serialization;

namespace Configuration;

public class MainWindowState
{
	public int X { get; set; }
	public int Y { get; set; }
	public double Width { get; set; }
	public double Height { get; set; }
	public bool IsMaximized { get; set; }
	public double TreeColumnWidth { get; set; }

	public static MainWindowState State { get; set; } = new();
	public static FileInfo WindowStateFileInfo { get; }
	private const string appFolderName = "Bundle Manager Neo";
	private const string windowStateFileName = "window.yaml";

	static MainWindowState()
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string appDirectory = Path.Combine(localAppData, appFolderName);
		Directory.CreateDirectory(appDirectory);
		string windowStateFilePath = Path.Combine(appDirectory, windowStateFileName);
		WindowStateFileInfo = new(windowStateFilePath);
	}

	public static MainWindowState? Load()
	{
		if (!WindowStateFileInfo.Exists)
			return null;
		var yamlDeserializer = new DeserializerBuilder().Build();
		string yaml = File.ReadAllText(WindowStateFileInfo.FullName, Encoding.UTF8);
		return yamlDeserializer.Deserialize<MainWindowState>(yaml);
	}

	public void Save()
	{
		var yamlSerializer = new SerializerBuilder().Build();
		string yaml = yamlSerializer.Serialize(this);
		File.WriteAllText(WindowStateFileInfo.FullName, yaml, Encoding.UTF8);
	}
}
