using Serialization.Attributes;

namespace BundleFormat.ResourceTypes.ProgressionData;

internal class EventRacerPersonality
{
	internal float minAggression;
	internal float maxAggression;
	internal float skill;
	internal float speed;

	public float MinAggresion { get => minAggression; set => minAggression = value; }
	public float MaxAggression { get => maxAggression; set => maxAggression = value; }
	public float Skill { get => skill; set => skill = value; }
	public float Speed { get => speed; set => speed = value; }
}
