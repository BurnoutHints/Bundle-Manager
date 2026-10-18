using ModelViewer.SceneData;
using OpenTK.Mathematics;

namespace ModelViewer;

public class ThirdPersonCamera : ICamera
{
    public Matrix4 LookAtMatrix { get; private set; }
    private readonly SceneObject _target;
    private readonly Vector3 _offset;

    public ThirdPersonCamera(SceneObject target) : this(target, Vector3.Zero)
    {
        
    }

    public ThirdPersonCamera(SceneObject target, Vector3 offset)
    {
        _target = target;
        _offset = offset;
    }

    public void Update()
    {
        LookAtMatrix = Matrix4.LookAt(_target.Position + (_offset * _target.Direction), _target.Position,
            Vector3.UnitY);
    }
}
