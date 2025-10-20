using Information;

namespace Configuration;

public class FileConfig
{
	public required GameInfo.Entry Game { get; set; }
	public required PlatformInfo Platform { get; set; }
	public required VersionInfo Version { get; set; }
}
