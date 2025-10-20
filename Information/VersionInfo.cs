using Information.Enums;

namespace Information;

public record VersionInfo
{
	public required GameVersion Id { get; init; }
	public required string Name { get; init; }
}
