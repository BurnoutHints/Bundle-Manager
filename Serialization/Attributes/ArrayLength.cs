namespace Serialization.Attributes;

// In C++, arrays are fixed-size. Their datatype is, for example, int[3].
// In C#, arrays are also fixed-size, but a single type, for example, int[].
// Their size is set upon instantiation.
// It is therefore not possible to retrieve the length via reflection. This is a
// problem for Bundle Manager's reflection-based serialization.
// This attribute is used to annotate the array with size information.
[AttributeUsage(AttributeTargets.Field)]
public class ArrayLengthAttribute : Attribute
{
	public int[] Length { get; }
	public ArrayLengthAttribute(params int[] length)
	{
		Length = length;
	}
}
