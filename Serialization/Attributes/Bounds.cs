namespace Serialization.Attributes;

// Causes upper and lower bounds to be applied to a NumericUpDown.
[AttributeUsage(AttributeTargets.Property)]
public class BoundsAttribute : Attribute
{
	public int Min { get; }
	public int Max { get; }
	public BoundsAttribute(int min, int max)
	{
		Min = min;
		Max = max;
	}
}
