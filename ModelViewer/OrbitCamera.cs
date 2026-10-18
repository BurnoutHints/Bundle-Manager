using System;
using ModelViewer.SceneData;
using OpenTK.Mathematics;

namespace ModelViewer;

public class OrbitCamera : ICamera
{
    public Matrix4 LookAtMatrix { get; private set; }
    private readonly Scene _scene;
    //private readonly Vector3 _target;
    //private readonly float _horizDist;
    //private readonly float _vertDist;
    private float _angle;

    public OrbitCamera(Scene scene, float startAngle = 0)
    {
        //Scene.Center, Scene.HorizSize, Scene.VertSize / 2.0f
        //_target = target;
        //_horizDist = horizDist;
        //_vertDist = vertDist;
        _scene = scene;
        _angle = startAngle;
    }

    public void Update()
    {
        //float xPos = (float)Math.Cos(MathHelper.DegreesToRadians(_angle)) * _horizDist;
        //float yPos = (float)Math.Tan(MathHelper.DegreesToRadians(_angle)) * _horizDist;

        //Vector3 eye = new Vector3(xPos, _horizDist, yPos);
        //eye = new Vector3(0, 2, -5);

        Vector3 target = _scene.Center;
        float horizDist = _scene.HorizSize;
        float vertDist = _scene.VertSize;

        /*if (Math.Abs(vertDist - target.Y) < 1.2f)
        {
            vertDist += 4;
            horizDist -= 5;
        }*/

        Vector3 eye = new((float)-Math.Sin(_angle) * horizDist, vertDist, (float)-Math.Cos(_angle) * horizDist);
        eye += target;

        LookAtMatrix = Matrix4.LookAt(eye, target, Vector3.UnitY);

        _angle += 0.025f;
        if (_angle >= 2 * Math.PI)
            _angle -= 2 * (float)Math.PI;
    }
}
