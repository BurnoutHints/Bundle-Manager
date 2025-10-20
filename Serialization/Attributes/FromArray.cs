namespace Serialization.Attributes;

// Specifies that this property (typically an index or instance pointer) refers
// to a targeted external array. In the UI, this will populate a combobox with
// all the options in that array. The DisplayMember attribute is used on the
// target class to set the displayed content of each option.
[AttributeUsage(AttributeTargets.Property)]
public class FromArrayAttribute : Attribute
{
	public string ArrayName { get; }
	public FromArrayAttribute(string arrayName)
	{
		ArrayName = arrayName;
	}
}
