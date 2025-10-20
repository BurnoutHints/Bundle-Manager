namespace Serialization.Attributes;

// Specifies a maximum length for the user to input in a text box. This is
// usually used on a property whose backing field is annotated with ArrayLength.
[AttributeUsage(AttributeTargets.Property)]
public class MaxStringLengthAttribute : Attribute
{
	public int MaxLength { get; }
	public MaxStringLengthAttribute(int maxLength)
	{
		MaxLength = maxLength;
	}
}
