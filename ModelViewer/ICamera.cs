using OpenTK.Mathematics;

namespace ModelViewer;

public interface ICamera
{
    Matrix4 LookAtMatrix { get; }
    void Update();
}
