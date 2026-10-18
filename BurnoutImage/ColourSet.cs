using System;
using BCnEncoder.Shared;

namespace BurnoutImage;

internal class ColourSet
{
    public int Count;
    public Vec3[] Points;
    public float[] Weights;
    public int[] Remap;
    public bool Transparent;

    public ColourSet(byte[] rgba, int mask, CompressionFormat compression)
    {
        Count = 0;
        Transparent = false;

        Points = new Vec3[16];
        Weights = new float[16];
        Remap = new int[16];

        // Check the compression mode for dxt1
        bool isDxt1 = compression == CompressionFormat.Bc1;
        bool weightByAlpha = false;

        // Create the minimal set
        for (int i = 0; i < 16; ++i)
        {
            // Check if this pixel is enabled
            int bit = 1 << i;
            if ((mask & bit) == 0)
            {
                Remap[i] = -1;
                continue;
            }

            // Check for transparent pixels when using dxt1
            if (isDxt1 && rgba[4 * i + 3] < 128)
            {
                Remap[i] = -1;
                Transparent = true;
                continue;
            }

            // Loop over previous points for a match
            for (int j = 0; ; ++j)
            {
                // allocate a new point
                if (j == i)
                {
                    // normalize coordinates to [0,1]
                    float x = (float)rgba[4 * i + 0] / 255.0f;
                    float y = (float)rgba[4 * i + 1] / 255.0f;
                    float z = (float)rgba[4 * i + 2] / 255.0f;

                    // ensure there is always non-zero weight even for zero alpha
                    float w = (float)(rgba[4 * i + 3] + 1) / 256.0f;

                    // add the point
                    Points[Count] = new Vec3(x, y, z);
                    Weights[Count] = (weightByAlpha ? w : 1.0f);
                    Remap[i] = Count;

                    // Advance
                    ++Count;
                    break;
                }

                // check for a match
                int oldbit = 1 << j;
                bool match = ((mask & oldbit) != 0)
                    && (rgba[4 * i + 0] == rgba[4 * j + 0])
                    && (rgba[4 * i + 1] == rgba[4 * j + 1])
                    && (rgba[4 * i + 2] == rgba[4 * j + 2])
                    && (rgba[4 * j + 3] >= 128 || !isDxt1);
                if (match)
                {
                    // get the index of the match
                    int index = Remap[j];

                    // ensure there is always non-zero weight even for zero alpha
                    float w = (float)(rgba[4 * i + 3] + 1) / 256.0f;

                    // map to this point and increase the weight
                    Weights[index] += (weightByAlpha ? w : 1.0f);
                    Remap[i] = index;
                    break;
                }
            }
        }

        // square root the weights
        for (int i = 0; i < Count; ++i)
            Weights[i] = (float)Math.Sqrt(Weights[i]);
    }

    public void RemapIndices(byte[] source, byte[] target)
    {
        for (int i = 0; i < 16; ++i)
        {
            int j = Remap[i];
            if (j == -1)
                target[i] = 3;
            else
                target[i] = source[j];
        }
    }
}
