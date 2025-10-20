using Information.Enums;

namespace Information;

public record GameInfo
{
	public record Entry
	{
		public required Game Id { get; init; }
		public required string Name { get; init; }
		public required FileTypeInfo[] FileTypes { get; init; }
		public required PlatformInfo[] Platforms { get; init; }
	}

	public static Entry[] Info { get; } =
	[
		new Entry
		{
			Id = Game.Black2,
			Name = "Black 2",
			FileTypes =
			[
				new FileTypeInfo { Id = FileType.Bundle, Name = "Bundle", IsPrimaryFileType = true }
			],
			Platforms =
			[
				new PlatformInfo
				{
					Id = Platform.Xbox360,
					Name = "Xbox 360",
					Endianness = Endianness.Big,
					Is64Bit = false,
					DefaultVersionIndex = 0,
					Versions =
					[
						new VersionInfo { Id = GameVersion.Black2_Dev_20060629, Name = "2006-06-29 build" },
						new VersionInfo { Id = GameVersion.Black2_Dev_20070213, Name = "2007-02-13 \"2007-02-08_10-29\" build" }
					]
				}
			]
		},
		new Entry
		{
			Id = Game.BurnoutParadise,
			Name = "Burnout Paradise",
			FileTypes =
			[
				new FileTypeInfo { Id = FileType.Bundle, Name = "Bundle", IsPrimaryFileType = true },
				//new FileTypeInfo { Id = FileType.Savegame, Name = "Savegame" },
				//new FileTypeInfo { Id = FileType.StoreBin, Name = "STORE.BIN" }
			],
			Platforms =
			[
				// Major version notes:
				// 1.3=Cagney update
				// 1.4=Bikes update, PSN release
				// 1.6=Major revision, Ultimate Box release, party DLC
				// 1.7=Premium DLC
				// 1.8=Cops and robbers DLC
				// 1.9=Island DLC
				// PC 1.1.0.0 is equivalent to 1.6
				new PlatformInfo
				{
					Id = Platform.PC,
					Name = "PC",
					Endianness = Endianness.Little,
					Is64Bit = false,
					DefaultVersionIndex = 1,
					Versions =
					[
						new VersionInfo { Id = GameVersion.BP_16, Name = "1.0.0.x" }, // 1.0.0.1=bugfixes
						new VersionInfo { Id = GameVersion.BP_17, Name = "1.1.0.0" }
					]
				},
				new PlatformInfo
				{
					Id = Platform.PlayStation3,
					Name = "PlayStation 3",
					Endianness = Endianness.Big,
					Is64Bit = false,
					DefaultVersionIndex = 6,
					Versions =
					[
						new VersionInfo { Id = GameVersion.BP_10, Name = "1.0-1.2" }, // 1.1,1.2=bugfixes
						new VersionInfo { Id = GameVersion.BP_13, Name = "1.3" },
						new VersionInfo { Id = GameVersion.BP_14, Name = "1.4-1.5" }, // 1.5=trophies+bugfixes
						new VersionInfo { Id = GameVersion.BP_16, Name = "1.6" },
						new VersionInfo { Id = GameVersion.BP_17, Name = "1.7" },
						new VersionInfo { Id = GameVersion.BP_18, Name = "1.8" },
						new VersionInfo { Id = GameVersion.BP_19, Name = "1.9" },
						new VersionInfo { Id = GameVersion.BP_PS3_Dev_20070831, Name = "2007-08-31 build" },
						new VersionInfo { Id = GameVersion.BP_PS3_Dev_20070904, Name = "2007-09-04 build" },
						new VersionInfo { Id = GameVersion.BP_PS3_Dev_20070928, Name = "2007-09-28 \"B5SClean58803\" build" },
						new VersionInfo { Id = GameVersion.BP_PS3_Dev_20071003, Name = "2007-10-03 \"B5SClean59580\" build" },
						new VersionInfo { Id = GameVersion.BP_PS3_Dev_20071010, Name = "2007-10-10 \"B5SClean69036-10 October07 Matt Benson\" build" },
						new VersionInfo { Id = GameVersion.BP_PS3_Dev_20071012, Name = "2007-10-12 \"2007-10-12-0125_61591_67243\" build" },
						new VersionInfo { Id = GameVersion.BP_PS3_Dev_20071021, Name = "2007-10-21 \"B5 Final\" build" }
					]
				},
				new PlatformInfo
				{
					Id = Platform.Xbox360,
					Name = "Xbox 360",
					Endianness = Endianness.Big,
					Is64Bit = false,
					DefaultVersionIndex = 6,
					Versions =
					[
						new VersionInfo { Id = GameVersion.BP_10, Name = "1.0-1.2" }, // 1.1,1.2=bugfixes
						new VersionInfo { Id = GameVersion.BP_13, Name = "1.3" },
						new VersionInfo { Id = GameVersion.BP_14, Name = "1.4" },
						new VersionInfo { Id = GameVersion.BP_16, Name = "1.6" },
						new VersionInfo { Id = GameVersion.BP_17, Name = "1.7" },
						new VersionInfo { Id = GameVersion.BP_18, Name = "1.8" },
						new VersionInfo { Id = GameVersion.BP_19, Name = "1.9" },
						new VersionInfo { Id = GameVersion.BP_X360_Dev_20061113, Name = "2006-11-13 \"FranchiseBuild\" build" },
						new VersionInfo { Id = GameVersion.BP_X360_Dev_20070124, Name = "2007-01-24 build" },
						new VersionInfo { Id = GameVersion.BP_X360_Dev_20070222, Name = "2007-02-22 build" },
						new VersionInfo { Id = GameVersion.BP_X360_Dev_20080130, Name = "2008-01-30 \"Burnout_tcartwright\" build" }
					]
				}
			]
		},
		new Entry
		{
			Id = Game.BurnoutParadiseRemastered,
			Name = "Burnout Paradise Remastered",
			FileTypes =
			[
				new FileTypeInfo { Id = FileType.Bundle, Name = "Bundle", IsPrimaryFileType = true },
				//new FileTypeInfo { Id = FileType.Savegame, Name = "Savegame" },
				//new FileTypeInfo { Id = FileType.StoreBin, Name = "STORE.BIN" }
			],
			Platforms =
			[
				// Major version notes: N/A (no major updates)
				new PlatformInfo
				{
					Id = Platform.PC,
					Name = "PC",
					Endianness = Endianness.Little,
					Is64Bit = false,
					Versions =
					[
						new VersionInfo { Id = GameVersion.BPR_10, Name = "1.0.0.0" }
					]
				},
				new PlatformInfo
				{
					Id = Platform.PlayStation4,
					Name = "PlayStation 4",
					Endianness = Endianness.Little,
					Is64Bit = true,
					Versions =
					[
						new VersionInfo { Id = GameVersion.BPR_10, Name = "1.0x" } // 1.01,1.02,1.03=bugfixes
					]
				},
				new PlatformInfo
				{
					Id = Platform.Switch,
					Name = "Switch",
					Endianness = Endianness.Little,
					Is64Bit = true,
					Versions =
					[
						new VersionInfo { Id = GameVersion.BPR_10, Name = "1.0.0" } // v0
					]
				},
				new PlatformInfo
				{
					Id = Platform.XboxOne,
					Name = "Xbox One",
					Endianness = Endianness.Little,
					Is64Bit = true,
					Versions =
					[
						new VersionInfo { Id = GameVersion.BPR_10, Name = "1.0.0.x" } // 1.0.0.2=retail;1.0.0.3,1.0.0.5=bugfixes
					]
				}
			]
		},
		new Entry
		{
			Id = Game.HotPursuit,
			Name = "Need for Speed Hot Pursuit",
			FileTypes =
			[
				new FileTypeInfo { Id = FileType.Bundle, Name = "Bundle", IsPrimaryFileType = true },
				new FileTypeInfo { Id = FileType.Savegame, Name = "Savegame" }
			],
			Platforms =
			[
				// Major version notes:
				// 1.1=Super Sports DLC
				// 1.2=Armed and Dangerous, Lamborghini Untamed, Porsche Unleashed DLC
				// PC 1.0.2.0 adds a few vehicles. Otherwise, DLC is console-exclusive.
				new PlatformInfo
				{
					Id = Platform.PC,
					Name = "PC",
					Endianness = Endianness.Little,
					Is64Bit = false,
					Versions =
					[
						new VersionInfo { Id = GameVersion.HP_10, Name = "1.0.x.x" } // 1.0.1.0,1.0.3.0,1.0.4.0,1.0.5.0=bugfixes
					]
				},
				new PlatformInfo
				{
					Id = Platform.PlayStation3,
					Name = "PlayStation 3",
					Endianness = Endianness.Big,
					Is64Bit = false,
					DefaultVersionIndex = 2,
					Versions =
					[
						new VersionInfo { Id = GameVersion.HP_10, Name = "1.00" },
						new VersionInfo { Id = GameVersion.HP_11, Name = "1.01" },
						new VersionInfo { Id = GameVersion.HP_12, Name = "1.02" },
						new VersionInfo { Id = GameVersion.HP_PS3_Dev_20100827, Name = "2010-08-27 \"Alaska_PRODUCTION\" build" },
						new VersionInfo { Id = GameVersion.HP_PS3_Dev_20100906, Name = "2010-09-06 build" }
					]
				},
				new PlatformInfo
				{
					Id = Platform.Xbox360,
					Name = "Xbox 360",
					Endianness = Endianness.Big,
					Is64Bit = false,
					DefaultVersionIndex = 2,
					Versions =
					[
						new VersionInfo { Id = GameVersion.HP_10, Name = "1.0" },
						new VersionInfo { Id = GameVersion.HP_11, Name = "1.1" },
						new VersionInfo { Id = GameVersion.HP_12, Name = "1.2" },
						new VersionInfo { Id = GameVersion.HP_X360_Dev_20100902, Name = "2010-09-02 \"AK_SHOWTIME\" build" }
					]
				}
			]
		},
		new Entry
		{
			Id = Game.HotPursuitRemastered,
			Name = "Need for Speed Hot Pursuit Remastered",
			FileTypes =
			[
				new FileTypeInfo { Id = FileType.Bundle, Name = "Bundle", IsPrimaryFileType = true },
				//new FileTypeInfo { Id = FileType.Savegame, Name = "Savegame" }
			],
			Platforms =
			[
				// Major version notes:
				// Patch 2 adds a wrap editor.
				new PlatformInfo
				{
					Name = "PC",
					Id = Platform.PC,
					Endianness = Endianness.Little,
					Is64Bit = true,
					DefaultVersionIndex = 1,
					Versions =
					[
						// No version numbers available (always marked as 1.0.0.0)
						new VersionInfo { Id = GameVersion.HPR_10, Name = "Original/Patch 1" }, // Patch 1=bugfixes. Likely had a launch patch too
						new VersionInfo { Id = GameVersion.HPR_P2, Name = "Patch 2" }
					]
				},
				new PlatformInfo
				{
					Id = Platform.PlayStation4,
					Name = "PlayStation 4",
					Endianness = Endianness.Little,
					Is64Bit = true,
					DefaultVersionIndex = 1,
					Versions =
					[
						new VersionInfo { Id = GameVersion.HPR_10, Name = "1.00-1.02" }, // 1.01,1.02=bugfixes
						new VersionInfo { Id = GameVersion.HPR_P2, Name = "1.03" }
					]
				},
				new PlatformInfo
				{
					Id = Platform.Switch,
					Name = "Switch",
					Endianness = Endianness.Little,
					Is64Bit = true,
					DefaultVersionIndex = 1,
					Versions =
					[
						// TODO: Retrieve client-side version numbers if available
						new VersionInfo { Id = GameVersion.HPR_10, Name = "v0-v2" }, // v1,v2=bugfixes
						new VersionInfo { Id = GameVersion.HPR_P2, Name = "v3" }
					]
				},
				new PlatformInfo
				{
					Id = Platform.XboxOne,
					Name = "Xbox One",
					Endianness = Endianness.Little,
					Is64Bit = true,
					DefaultVersionIndex = 1,
					Versions =
					[
						// TODO: Retrieve version numbers
						new VersionInfo { Id = GameVersion.HPR_10, Name = "Original/Patch 1" }, // Patch 1=bugfixes. Likely had a launch patch too
						new VersionInfo { Id = GameVersion.HPR_P2, Name = "Patch 2" }
					]
				}
			]
		},
		new Entry
		{
			Id = Game.MostWanted,
			Name = "Need for Speed Most Wanted",
			FileTypes =
			[
				new FileTypeInfo { Id = FileType.Bundle, Name = "Bundle", IsPrimaryFileType = true },
				//new FileTypeInfo { Id = FileType.Savegame, Name = "Savegame" }
			],
			Platforms =
			[
				// Major version notes:
				// Update 1=Ultimate Speed Pack
				// Update 2=Terminal Velocity, Movie Legends, NFS Heroes DLC
				// Vita was a second-class citizen and has no DLC.
				// Wii U was also second-class, released late and only has Update 1.
				new PlatformInfo
				{
					Id = Platform.PC,
					Name = "PC",
					Endianness = Endianness.Little,
					Is64Bit = false,
					DefaultVersionIndex = 2,
					Versions =
					[
						new VersionInfo { Id = GameVersion.MW_10, Name = "1.0.0.0-1.2.0.0" }, // 1.1.0.0,1.2.0.0=bugfixes
						new VersionInfo { Id = GameVersion.MW_MU1, Name = "1.3.0.0-1.4.0.0" }, // 1.4.0.0=bugfixes
						new VersionInfo { Id = GameVersion.MW_MU2, Name = "1.5.0.0" }
					]
				},
				new PlatformInfo
				{
					Id = Platform.PlayStation3,
					Name = "PlayStation 3",
					Endianness = Endianness.Big,
					Is64Bit = false,
					DefaultVersionIndex = 2,
					Versions =
					[
						new VersionInfo { Id = GameVersion.MW_10, Name = "1.00-1.01" }, // 1.01=bugfixes
						new VersionInfo { Id = GameVersion.MW_MU1, Name = "1.02" },
						new VersionInfo { Id = GameVersion.MW_MU2, Name = "1.03" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20110916, Name = "2011-09-16 \"Hawaii_pmaguire\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20111124, Name = "2011-11-24 \"HAWAII_MAIN\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20111220, Name = "2011-12-20 \"Hawaii_MAIN\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20111221, Name = "2011-12-21 \"Hawaii_Jmcclean\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120124, Name = "2012-01-24 \"Hawaii_BenS\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120127, Name = "2012-01-27 \"Hawaii_HHH\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120222, Name = "2012-02-22 \"anything\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120227, Name = "2012-02-27 \"a\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120301, Name = "2012-03-01 \"test\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120308, Name = "2012-03-08 \"s\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120320, Name = "2012-03-20 \"HAWAII_MAIN_PC\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120420, Name = "2012-04-20 \"fgh\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120425, Name = "2012-04-25 \"Hawaii_aveal\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120519, Name = "2012-05-19 \"Hawaii_MAIN_HHH\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120525, Name = "2012-05-25 \"Hawaii_andydavidson\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120531, Name = "2012-05-31 \"Geldy\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120714, Name = "2012-07-14 \"*sven*\" builds" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120721, Name = "2012-07-21 \"MAIN\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120807, Name = "2012-08-07 \"MAIN\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120811, Name = "2012-08-11 \"Hawaii_GAMESCOM_AutoTest\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120812, Name = "2012-08-12 \"Hawaii_GAMESCOM\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120816, Name = "2012-08-16 \"Hawaii_MAIN\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120820, Name = "2012-08-20 \"HAWAII_MAIN (incomplete)\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120911, Name = "2012-09-11 \"Hawaii_MAIN\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120913, Name = "2012-09-13 \"Hawaii_MAIN\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20120914, Name = "2012-09-14 \"Hawaii_*_AutoTest\" builds" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20121015, Name = "2012-10-15 \"Hawaii_DLC_NEXT\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20121130, Name = "2012-11-30 \"Hawaii_WORLD_INTEGRATION\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20121219, Name = "2012-12-19 \"Hawaii_FINAL_PS3\" build" },
						new VersionInfo { Id = GameVersion.MW_PS3_Dev_20130125, Name = "2013-01-25 build" }
					]
				},
				new PlatformInfo
				{
					Id = Platform.PlayStationVita,
					Name = "PlayStation Vita",
					Endianness = Endianness.Little,
					Is64Bit = false,
					Versions =
					[
						new VersionInfo { Id = GameVersion.MW_10, Name = "1.0x" } // 1.01=likely bugfixes
					]
				},
				new PlatformInfo
				{
					Id = Platform.WiiU,
					Name = "Wii U",
					Endianness = Endianness.Big,
					Is64Bit = false,
					Versions =
					[
						new VersionInfo { Id = GameVersion.MW_10, Name = "1.x.x" } // 1.0.0 (v0), 1.1.0 (v16), 1.2.0 (v32). Unknown content
					]
				},
				new PlatformInfo
				{
					Id = Platform.Xbox360,
					Name = "Xbox 360",
					Endianness = Endianness.Big,
					Is64Bit = false,
					DefaultVersionIndex = 2,
					Versions =
					[
						new VersionInfo { Id = GameVersion.MW_10, Name = "Original" },
						new VersionInfo { Id = GameVersion.MW_MU1, Name = "Multiplayer Update" },
						new VersionInfo { Id = GameVersion.MW_MU2, Name = "Multiplayer Update 2" }
					]
				}
			]
		}
	];
}
