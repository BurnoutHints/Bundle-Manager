using System.Drawing;

namespace ModelViewer;

public static class GraphicsUtil
{
    public static float[] GetTextureData(Bitmap bitmap)
    {
        float[] r = new float[bitmap.Width * bitmap.Height * 4];
        int index = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                r[index++] = pixel.R / 255.0f;
                r[index++] = pixel.G / 255.0f;
                r[index++] = pixel.B / 255.0f;
                r[index++] = pixel.A / 255.0f;
            }
        }

        return r;
    }
}
