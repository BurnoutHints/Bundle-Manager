using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace ModelViewer.SceneData;

public class Material
{
    public string Name;
    public Image DiffuseMap { get; set; }
    public Image NormalMap { get; set; }
    public Image SpecularMap { get; set; }

    public Color Color { get; set; }

    public Material(string name, Color color, Image diffuse = null, Image normal = null, Image specular = null)
    {
        Name = name;
        Color = color;
        DiffuseMap = diffuse;
        NormalMap = normal;
        SpecularMap = specular;
    }

    public override bool Equals(object obj)
    {
        if (obj is not Material other)
            return false;

        return other.Name == Name && other.DiffuseMap == DiffuseMap && other.NormalMap == NormalMap &&
               other.SpecularMap == SpecularMap;
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Name != null ? Name.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (DiffuseMap != null ? DiffuseMap.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (NormalMap != null ? NormalMap.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (SpecularMap != null ? SpecularMap.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Color.GetHashCode();
            return hashCode;
        }
    }
}
