using OpenTK.Mathematics;

namespace MathLib;

public class BoxF
{
    public Vector3 Min;
    public Vector3 Max;

    public BoxF()
    {
        Min = Vector3.Zero;
        Max = Vector3.One;
    }

    public BoxF(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public override string ToString()
    {
        return "(" + Min.ToString() + ", " + Max.ToString() + ")";
    }
}
