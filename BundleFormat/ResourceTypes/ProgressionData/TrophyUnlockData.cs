using BundleFormat.Types;

namespace BundleFormat.ResourceTypes.ProgressionData;

internal class TrophyUnlockData
{
	internal enum UnlockTrigger : ushort
	{
		None,
		CompleteAllStunts,
		CompleteAllJumps,
		CompleteAllSmashes,
		CompleteAllTakedowns,
		CompleteAllCrashes,
		CompleteAllTimeRoadRules,
		CompleteAllShowtimeRoadRules,
		CompleteAllRoadRules,
		CompleteAllJunctionEvents,
		CompleteAllOnlineChallenges,
		CompleteAllRaces,
		CompleteAllRoadRages,
		CompleteAllBurningRoutes,
		CompleteAllEliminators,
		CompleteAllMarkedMans,
		CompleteAllStuntRuns,
		FindAllGasStations,
		FindAllJunkyards,
		FindAllPaintShops,
		FindAllAutoRepairs,
		FindAllDriveThrus,
		NumMedals,
		NumRoadRules,
		NumTimeRoadRules,
		NumCrashRoadRules,
		NumNormalTakedowns,
		NumSignatureTakedowns,
		NumJumps,
		NumSmashes,
		NumBillboards,
		NumOnlineVerticalTakedowns,
		NumPercentageParallelParkOnline,
		NumOfEachOnlineEventComplete,
		NumMugshotsCollected
	}
	
	internal uint numberTrophyUnlock;
	internal ushort unlockType;
	internal CgsId carUnlockId = new();

	public uint NumberTrophyUnlock { get => numberTrophyUnlock; set => numberTrophyUnlock = value; }
	public UnlockTrigger UnlockType { get => (UnlockTrigger)unlockType; set => unlockType = (ushort)value; }
	public CgsId CarUnlockId { get => carUnlockId; set => carUnlockId = value; }
}
