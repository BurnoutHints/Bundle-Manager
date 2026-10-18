using OpenTK.Mathematics;

namespace ModelViewer.SceneData;

public class SceneObject
{
    public Model Model { get; set; }
    public Matrix4 Transform { get; set; }
    public string Name { get; set; }

    public Vector3 Position => Transform.ExtractTranslation();
    public Vector3 Scale => Transform.ExtractScale();
    public Quaternion Rotation => Transform.ExtractRotation();
    public Vector3 Direction { get; set; }

    public string ID { get; set; }

    public SceneObject(string name, Model model)
    {
        Name = name;
        Model = model;
        Transform = Matrix4.Identity;

        Direction = Vector3.UnitZ;

        //SetPosition(new Vector3(0, 0, -10.0f));
        //var t = Matrix4.CreateTranslation(0, 0, -4.0f);
        //var r = Matrix4.CreateFromAxisAngle(new Vector3(0.0f, -1.0f, 0.0f), MathHelper.DegreesToRadians(90.0f));
        //Transform = r * t;
    }

    public SceneObject(string name, Model model, Matrix4 transform)
    {
        Name = name;
        Model = model;
        Transform = transform;

        Direction = Vector3.UnitZ;
    }

    public Mesh MergeMeshes()
    {
        return Model.MergeMeshes().Transformed(Transform);
    }

    public Model MergeMeshesToModel()
    {
        return new Model(MergeMeshes());
    }

    public bool InitGraphics()
    {
        Model.InitGraphics();

        return true;
    }

    public void Update()
    {
        Model.Update();
    }

    public void Render(ICamera camera)
    {
        Model.Render(camera, Transform);
    }

    public void Dispose()
    {
        Model.Dispose();
    }

    public void SetPosition(Vector3 pos)
    {
        Matrix4 mat = Matrix4.CreateTranslation(pos);
        Transform = Matrix4.Add(Transform, mat);
    }
}
