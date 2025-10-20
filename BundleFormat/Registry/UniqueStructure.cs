namespace Information;

public record UniqueStructure
{
	public required Type TopLevelClass { get; init; }
	public required GameDetails[] ApplicableGames { get; init; }
}
