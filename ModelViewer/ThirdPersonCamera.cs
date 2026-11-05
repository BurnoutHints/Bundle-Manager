using ModelViewer.SceneData;
using OpenTK.Mathematics;

namespace ModelViewer;

public class ThirdPersonCamera : ICamera
{
    public Matrix4 LookAtMatrix { get; private set; }

    private readonly SceneObject target;
    private readonly Vector3 offset;

    public ThirdPersonCamera(SceneObject target) : this(target, Vector3.Zero)
    {

    }

    public ThirdPersonCamera(SceneObject initialTarget, Vector3 initialOffset)
    {
        target = initialTarget;
        offset = initialOffset;
    }

    public void Update()
    {
        LookAtMatrix = Matrix4.LookAt(target.Position + (offset * target.Direction), target.Position,
            Vector3.UnitY);
    }
}
