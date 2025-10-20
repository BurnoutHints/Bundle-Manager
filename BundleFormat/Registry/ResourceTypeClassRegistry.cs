using Information;
using Information.Enums;

namespace BundleFormat.Registry;

public record ResourceTypeClassRegistry
{
	public static Dictionary<ResourceType, UniqueStructure[]> Registry { get; } = new()
	{
		[ResourceType.ProgressionData] =
		[
			new UniqueStructure
			{
				TopLevelClass = typeof(ResourceTypes.ProgressionData.BP_10.ProgressionData),
				ApplicableGames =
				[
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.PlayStation3, Version = GameVersion.BP_10 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.PlayStation3, Version = GameVersion.BP_13 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.Xbox360, Version = GameVersion.BP_10 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.Xbox360, Version = GameVersion.BP_13 }
				]
			},
			new UniqueStructure
			{
				TopLevelClass = typeof(ResourceTypes.ProgressionData.BP_14.ProgressionData),
				ApplicableGames =
				[
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.PC, Version = GameVersion.BP_16 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.PC, Version = GameVersion.BP_17 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.PlayStation3, Version = GameVersion.BP_14 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.PlayStation3, Version = GameVersion.BP_16 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.PlayStation3, Version = GameVersion.BP_17 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.PlayStation3, Version = GameVersion.BP_18 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.Xbox360, Version = GameVersion.BP_14 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.Xbox360, Version = GameVersion.BP_16 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.Xbox360, Version = GameVersion.BP_17 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.Xbox360, Version = GameVersion.BP_18 }
				]
			},
			new UniqueStructure
			{
				TopLevelClass = typeof(ResourceTypes.ProgressionData.BP_19.ProgressionData),
				ApplicableGames =
				[
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.PlayStation3, Version = GameVersion.BP_19 },
					new GameDetails { Game = Game.BurnoutParadise, Platform = Platform.Xbox360, Version = GameVersion.BP_19 },
					new GameDetails { Game = Game.BurnoutParadiseRemastered, Platform = Platform.PlayStation4, Version = GameVersion.BPR_10 },
					new GameDetails { Game = Game.BurnoutParadiseRemastered, Platform = Platform.XboxOne, Version = GameVersion.BPR_10 }
				]
			},
			new UniqueStructure
			{
				TopLevelClass = typeof(ResourceTypes.ProgressionData.BPR.ProgressionData),
				ApplicableGames =
				[
					new GameDetails { Game = Game.BurnoutParadiseRemastered, Platform = Platform.PC, Version = GameVersion.BPR_10 },
					new GameDetails { Game = Game.BurnoutParadiseRemastered, Platform = Platform.Switch, Version = GameVersion.BPR_10 }
				]
			}
		]
	};

	public static Dictionary<ResourceTypeV2, UniqueStructure[]> RegistryV2 { get; } = new()
	{

	};
}
