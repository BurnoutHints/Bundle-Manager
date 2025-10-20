using Information.Enums;

namespace Information;

public record FileTypeInfo
{
	public required FileType Id { get; init; }
	public required string Name { get; init; }
	public bool IsPrimaryFileType { get; init; } = false;
}
