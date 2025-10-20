namespace Serialization.Attributes;

// Marks a property as requiring a value. If no value is entered, the given
// error message will be displayed in a message box.
[AttributeUsage(AttributeTargets.Property)]
public class RequiresValueAttribute : Attribute
{
	public string ErrorMessage { get; }
	public RequiresValueAttribute(string errorMessage)
	{
		ErrorMessage = errorMessage;
	}
}
