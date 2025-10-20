using Information;
using System;

namespace BundleManager.Registry;

internal class CustomView
{
	public required Type ViewType { get; init; }
	public required Type ViewModelType { get; init; }
	public required GameDetails[] ApplicableGames { get; init; }
}
