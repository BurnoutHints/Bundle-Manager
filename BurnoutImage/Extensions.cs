namespace BurnoutImage;

internal static class Extensions
{
    internal static bool Matches(this byte[] self, byte[] other)
    {
        if (self == null || other == null)
            return false;
        if (self.Length != other.Length)
            return false;

        for (int i = 0; i < self.Length; i++)
        {
            if (self[i] != other[i])
                return false;
        }

        return true;
    }
}
