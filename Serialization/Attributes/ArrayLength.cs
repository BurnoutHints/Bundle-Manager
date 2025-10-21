namespace Serialization.Attributes;

// Adds fixed-length information to a list.
// Necessary when the original model uses fixed-length arrays because the editor
// is not set up to handle arrays, only lists. This may be removed in future
// versions in favor of another technique.
[AttributeUsage(AttributeTargets.Field)]
public class ArrayLengthAttribute : Attribute
{
	public int[] Length { get; }
	public ArrayLengthAttribute(params int[] length)
	{
		Length = length;
	}
}
