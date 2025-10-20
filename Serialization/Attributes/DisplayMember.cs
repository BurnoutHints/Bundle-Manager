namespace Serialization.Attributes;

// Specifies which member to use to populate the list of options when a property
// uses the FromArray attribute that targets an array of this type.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class DisplayMemberAttribute : Attribute
{
	public string MemberName { get; }
	public DisplayMemberAttribute(string memberName)
	{
		MemberName = memberName;
	}
}
