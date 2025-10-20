using Information.Enums;

namespace Information;

public record GameDetails
{
	public required Game Game { get; init; }
	public required Platform Platform { get; init; }
	public required GameVersion Version { get; init; }
}
