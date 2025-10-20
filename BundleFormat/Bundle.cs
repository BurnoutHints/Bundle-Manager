using BundleFormat.Interfaces;
using Configuration;
using Information.Enums;

namespace BundleFormat;

public class Bundle
{
	private IBundle instance;
	private FileConfig config;
	private string filePath;
	public string FilePath
	{
		get => filePath;
		set
		{
			filePath = value;
			instance.FilePath = value;
		}
	}
	public bool LoadSucceeded { get; } = false;

	public List<IResource> Resources { get => instance.Resources; }

	public Bundle(string filePath, FileConfig config)
	{
		this.filePath = filePath;
		this.config = config;
		switch (config.Game.Id)
		{
			case Game.Black2:
				instance = new BundleV1.Bundle(filePath, config);
				break;
			case Game.BurnoutParadise:
				if (config.Version.Id == GameVersion.BP_X360_Dev_20061113
					|| config.Version.Id == GameVersion.BP_X360_Dev_20070124
					|| config.Version.Id == GameVersion.BP_X360_Dev_20070222)
					instance = new BundleV1.Bundle(filePath, config);
				else
					instance = new BundleV2.V2.Bundle(filePath, config);
					break;
			case Game.BurnoutParadiseRemastered:
				instance = new BundleV2.V2.Bundle(filePath, config);
				break;
			case Game.HotPursuit:
				instance = new BundleV2.V3.Bundle(filePath, config);
				break;
			case Game.HotPursuitRemastered:
				instance = new BundleV2.V3.Bundle(filePath, config);
				break;
			case Game.MostWanted:
				instance = new BundleV2.V5.Bundle(filePath, config);
				break;
			default:
				throw new Exception("Attempted to create bundle for unimplemented game.");
		}
		LoadSucceeded = instance.Load();
	}

	public bool Save()
	{
		return instance.Save();
	}
}
