namespace Serialization.Attributes;

// Informs the de/serializer this field should not be automatically aligned.
[AttributeUsage(AttributeTargets.Field)]
public class UnalignedAttribute : Attribute
{

}
