namespace Serialization.Attributes;

// Use this to add tooltip text to properties.
[AttributeUsage(AttributeTargets.Property)]
public class TooltipAttribute : Attribute
{
	public string Text { get; }
	
	public TooltipAttribute(string text)
	{
		Text = text;
	}
}
