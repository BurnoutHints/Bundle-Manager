using Serialization.Enums;

namespace Serialization.Attributes;

// Defines whether to do a deep copy, shallow copy, or custom copy on duplicate.
// The default is to do a deep copy.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class CloneBehaviorAttribute : Attribute
{
	public CloneMode Mode { get; }
	public CloneBehaviorAttribute(CloneMode mode)
	{
		Mode = mode;
	}
}
