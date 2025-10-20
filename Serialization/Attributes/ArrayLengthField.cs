namespace Serialization.Attributes;

// Informs the de/serializer of which field holds the number of elements.
// In fixed-length arrays, this should define the field that holds the number
// of used elements. Ensure ArrayLength is also present.
// In pointed-to arrays, this should define the field that holds the total
// number of elements.
[AttributeUsage(AttributeTargets.Field)]
public class ArrayLengthFieldAttribute : Attribute
{
	public string FieldName { get; }
	public ArrayLengthFieldAttribute(string fieldName)
	{
		FieldName = fieldName;
	}
}
