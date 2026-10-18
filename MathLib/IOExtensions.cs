using OpenTK.Mathematics;
using System.IO;

namespace MathLib;

public static class IOExtensions
{
    public static Matrix4 ReadMatrix4(this BinaryReader self)
    {
        Matrix4 result = new();
        
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                result[i, j] = self.ReadSingle();
            }
        }
        
        return result;
    }

    public static void Write(this BinaryWriter self, Matrix4 value)
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                self.Write(value[i, j]);
            }
        }
    }

    public static Vector3 ReadVector3F(this BinaryReader self)
    {
        Vector3 result = new();

        result.X = self.ReadSingle();
        result.Y = self.ReadSingle();
        result.Z = self.ReadSingle();

        return result;
    }

    public static void Write(this BinaryWriter self, Vector3 value)
    {
        self.Write(value.X);
        self.Write(value.Y);
        self.Write(value.Z);
    }

    public static Vector3I ReadVector3I(this BinaryReader self)
    {
        Vector3I result = new();

        result.X = self.ReadInt32();
        result.Y = self.ReadInt32();
        result.Z = self.ReadInt32();

        return result;
    }

    public static void Write(this BinaryWriter self, Vector3I value)
    {
        self.Write(value.X);
        self.Write(value.Y);
        self.Write(value.Z);
    }

    public static Vector2I ReadVector2I(this BinaryReader self)
    {
        Vector2I result = new();

        result.X = self.ReadInt32();
        result.Y = self.ReadInt32();

        return result;
    }

    public static void Write(this BinaryWriter self, Vector2I value)
    {
        self.Write(value.X);
        self.Write(value.Y);
    }

    public static Vector3S ReadVector3S(this BinaryReader self)
    {
        Vector3S result = new();

        result.X = self.ReadInt16();
        result.Y = self.ReadInt16();
        result.Z = self.ReadInt16();

        return result;
    }

    public static void Write(this BinaryWriter self, Vector3S value)
    {
        self.Write(value.X);
        self.Write(value.Y);
        self.Write(value.Z);
    }

    public static BoxF ReadBoxF(this BinaryReader self)
    {
        BoxF result = new();

        result.Min = self.ReadVector3F();
        result.Max = self.ReadVector3F();

        return result;
    }

    public static void Write(this BinaryWriter self, BoxF value)
    {
        self.Write(value.Min);
        self.Write(value.Max);
    }

    public static RectF ReadRectF(this BinaryReader self)
    {
        RectF result = new();

        result.X = self.ReadSingle();
        result.Width = self.ReadSingle();
        result.Y = self.ReadSingle();
        result.Height = self.ReadSingle();

        return result;
    }

    public static void Write(this BinaryWriter self, RectF value)
    {
        self.Write(value.X);
        self.Write(value.Width);
        self.Write(value.Y);
        self.Write(value.Height);
    }

    public static Triangle ReadTriangle(this BinaryReader self)
    {
        Triangle result = new();

        result.Index0 = self.ReadInt16();
        result.Index1 = self.ReadInt16();
        result.Index2 = self.ReadInt16();

        return result;
    }

    public static void Write(this BinaryWriter self, Triangle value)
    {
        self.Write(value.Index0);
        self.Write(value.Index1);
        self.Write(value.Index2);
    }

    public static Matrix3x2 ReadMatrix3x2(this BinaryReader self)
    {
        Matrix3x2 result = new();

        // TODO: Verify
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                result[i, j] = self.ReadSingle();
            }
        }

        return result;
    }

    public static void Write(this BinaryWriter self, Matrix3x2 value)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                self.Write(value[i, j]);
            }
        }
    }
}
