using Information.Enums;

namespace Information;

public record PlatformInfo
{
	public required Platform Id { get; init; }
	public required string Name { get; init; }
	public required Endianness Endianness { get; init; }
	public required bool Is64Bit { get; init; }
	public int DefaultVersionIndex { get; init; }
	public required VersionInfo[] Versions { get; init; }
}
