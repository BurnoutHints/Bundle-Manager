using System.Collections.Generic;
using System.Drawing;

namespace PVSFormat;

public class Polygon
{
    public PointF[] Points
    {
        get;
        set;
    }

    public float MinX
    {
        get
        {
            float min = float.MaxValue;

            foreach (PointF point in Points)
            {
                float current = point.X;

                if (current < min)
                    min = current;
            }

            return min;
        }
    }

    public float MinY
    {
        get
        {
            float min = float.MaxValue;

            foreach (PointF point in Points)
            {
                float current = point.Y;

                if (current < min)
                    min = current;
            }

            return min;
        }
    }

    public float MaxX
    {
        get
        {
            float max = float.MinValue;

            foreach (PointF point in Points)
            {
                float current = point.X;

                if (current > max)
                    max = current;
            }

            return max;
        }
    }

    public float MaxY
    {
        get
        {
            float max = float.MinValue;

            foreach (PointF point in Points)
            {
                float current = point.Y;

                if (current > max)
                    max = current;
            }

            return max;
        }
    }

    public RectangleF Bounds
    {
        get
        {
            return new RectangleF(MinX, MinY, MaxX - MinX, MaxY - MinY);
        }
    }

    public Polygon(params PointF[] points)
    {
        Points = points;
    }

    public Polygon(List<PointF> points)
    {
        Points = [.. points];
    }
}
