using OpenTK.Mathematics;

namespace ModelViewer;

public class StaticCamera : ICamera
{
    public Matrix4 LookAtMatrix { get; }

    public StaticCamera()
    {
        Vector3 position = new(0, 0, 0);
        LookAtMatrix = Matrix4.LookAt(position, -Vector3.UnitZ, Vector3.UnitY);
    }

    public StaticCamera(Vector3 position, Vector3 target)
    {
        LookAtMatrix = Matrix4.LookAt(position, target, Vector3.UnitY);
    }

    public void Update()
    {
        
    }
}
