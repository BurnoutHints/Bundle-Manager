using Serialization.Attributes;
using Serialization.Enums;
using Serialization.Types;

namespace BundleFormat.ResourceTypes.ProgressionData.BP_10;

[CloneBehavior(CloneMode.Shallow)]
internal class EventJunction
{
	internal uint id;
	internal InstancePointer<RaceEventData> offlineEvent = new();
	internal InstancePointer<RaceEventData> onlineEvent = new();
	internal int shotGroup;

	public uint Id { get => id; set => id = value; }
	[RequiresValue("Offline event requires a value to be set.")]
	[FromArray(nameof(ProgressionData.Events))]
	public RaceEventData OfflineEvent { get => offlineEvent.Data!; set => offlineEvent.Data = value; }
	[FromArray(nameof(ProgressionData.Events))]
	public RaceEventData OnlineEvent { get => onlineEvent.Data!; set => onlineEvent.Data = value; }
	public int ShotGroup { get => shotGroup; set => shotGroup = value; }
}
