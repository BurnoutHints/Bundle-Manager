using System.Diagnostics;

namespace BundleUtilities;

public static class BitScan
{
    private const ulong DeBruijnSequence = 0x37E84A99DAE458F;

    private static readonly int[] MultiplyDeBruijnBitPosition =
    [
    0, 1, 17, 2, 18, 50, 3, 57,
    47, 19, 22, 51, 29, 4, 33, 58,
    15, 48, 20, 27, 25, 23, 52, 41,
    54, 30, 38, 5, 43, 34, 59, 8,
    63, 16, 49, 56, 46, 21, 28, 32,
    14, 26, 24, 40, 53, 37, 42, 7,
    62, 55, 45, 31, 13, 39, 36, 6,
    61, 44, 12, 35, 60, 11, 10, 9,
];

    /// <summary>
    /// Search the mask data from least significant bit (LSB) to the most significant bit (MSB) for a set bit (1)
    /// using De Bruijn sequence approach. Warning: Will return zero for b = 0.
    /// </summary>
    /// <param name="b">Target number.</param>
    /// <returns>Zero-based position of LSB (from right to left).</returns>
    public static int BitScanForward(ulong b)
    {
        Debug.Assert(b > 0, "Target number should not be zero");
        return MultiplyDeBruijnBitPosition[((ulong)((long)b & -(long)b) * DeBruijnSequence) >> 58];
    }

    public static int BitScanReverse(uint mask)
    {
        int i;
        for (i = 31; (mask >> i) == 0 && i >= 0; i--) ;
        return i;
    }
}
