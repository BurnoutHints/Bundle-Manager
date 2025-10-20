using Information.Enums;

namespace Serialization.Attributes;

// Sometimes, the length of an array varies by platform, as is the case in the
// original Bundle format. These arrays must be annotated with per-platform size
// information so they can be correctly de/serialized.
// See the description of ArrayLength for why this cannot be determined via an
// instantiated array.
[AttributeUsage(AttributeTargets.Field)]
public class PlatformSpecificArrayLengthAttribute : Attribute
{
	private readonly Dictionary<Platform, int> lengths = [];

	public PlatformSpecificArrayLengthAttribute(params object[] platformLengthPairs)
	{
		if (platformLengthPairs.Length % 2 != 0)
			throw new Exception("Platform-to-array-length pairs are incomplete.");

		for (int i = 0; i < platformLengthPairs.Length; i += 2)
		{
			Platform platform = (Platform)platformLengthPairs[i];
			int length = (int)platformLengthPairs[i + 1];
			lengths[platform] = length;
		}
	}
	
	public int GetLength(Platform platform)
	{
		if (lengths.TryGetValue(platform, out int length))
			return length;
		throw new Exception($"{platform} has no array length set.");
	}
}
