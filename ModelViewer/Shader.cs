using System;
using System.Diagnostics;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace ModelViewer;

public class Shader
{
    private readonly string _vertex;
    private readonly string _fragment;

    public int ShaderID
    {
        get;
        private set;
    }

    public Shader(string vertex, string fragment)
    {
        ShaderID = -1;
        _vertex = vertex;
        _fragment = fragment;
    }

    public Shader(string name)
    {
        ShaderID = -1;
        _vertex = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Shaders/" + name + "V.glsl"));
        _fragment = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Shaders/" + name + "P.glsl"));
    }

    public int Compile()
    {
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, _vertex);
        GL.CompileShader(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, _fragment);
        GL.CompileShader(fragmentShader);

        ShaderID = GL.CreateProgram();
        GL.AttachShader(ShaderID, vertexShader);
        GL.AttachShader(ShaderID, fragmentShader);
        GL.LinkProgram(ShaderID);
        string info = GL.GetProgramInfoLog(ShaderID);
        if (!string.IsNullOrEmpty(info))
            Debug.WriteLine($"GL.LinkProgram had info log: {info}");

        GL.DetachShader(ShaderID, vertexShader);
        GL.DetachShader(ShaderID, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        return ShaderID;
    }

    public void Bind()
    {
        GL.UseProgram(ShaderID);
    }

    public void Dispose()
    {
        ShaderID = -1;
        GL.DeleteProgram(ShaderID);
    }
}
