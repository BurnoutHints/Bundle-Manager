using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Numerics;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers.EnvironmentSettings;

// BrnWorld::EnvironmentSettings::Keyframe, see https://burnout.wiki/wiki/Environment_Keyframe
// Colours are floats where 1 is full brightness, lighting goes brighter.

[TypeConverter(typeof(ExpandableObjectConverter))]
public class BloomData
{
    public float mfLuminance { get; set; }
    public float mfThreshold { get; set; }
    public Vector4 mv4Scale { get; set; }

    public override string ToString() => "BloomData";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class VignetteData
{
    public float mfAngle { get; set; }
    public float mfSharpness { get; set; }
    public Vector2 mv2Amount { get; set; }
    public Vector2 mv2Centre { get; set; }

    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector4 mv4InnerColour { get; set; }

    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector4 mv4OuterColour { get; set; }

    public override string ToString() => "VignetteData";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class TintData
{
    [Description("ID of the imported Colour Cube resource."), TypeConverter(typeof(HexConverter))]
    public ulong mpColourCube { get; set; }

    public override string ToString() => $"0x{mpColourCube:X8}";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class ScatteringData
{
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3SkyTopColour { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3SkyHorColour { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3SkySunColour { get; set; }
    public float mfSkyHorPow { get; set; }
    public float mfSkySunPow { get; set; }
    public float mfSkyDrk { get; set; }
    public float mfSkyHorBleedScl { get; set; }
    public float mfSkyHorBleedPow { get; set; }
    public float mfSkySunBleedPow { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3ScattTopColour { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3ScattHorColour { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3ScattSunColour { get; set; }
    public float mfScattHorPow { get; set; }
    public float mfScattSunPow { get; set; }
    public float mfScattDrk { get; set; }
    public float mfScattHorBleedScl { get; set; }
    public float mfScattHorBleedPow { get; set; }
    public float mfScattSunBleedPow { get; set; }
    public float[] mafScattDist { get; set; } = new float[2];
    public float mfScattPow { get; set; }
    public float mfScattCap { get; set; }

    public override string ToString() => "ScatteringData";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class LightingData
{
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3KeyLightColour { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3SpecularColour { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3KeyFillColour { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3ShadowFillColour { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3RightFillColour { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3LeftFillColour { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3UpFillColour { get; set; }
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3DownFillColour { get; set; }
    public float mfAmbientIrradianceScale { get; set; }

    public override string ToString() => "LightingData";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class CloudsData
{
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3[] mav3LayerLiteColour { get; set; } = new Vector3[2];
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3[] mav3LayerDarkColour { get; set; } = new Vector3[2];
    public float[] mafLayerDensity { get; set; } = new float[2];
    public float[] mafLayerFeathering { get; set; } = new float[2];
    public float[] mafLayerOpacity { get; set; } = new float[2];
    public float[] mafLayerSpeed { get; set; } = new float[2];
    public float[] mafLayerScale { get; set; } = new float[2];
    public float mfDirectionAngle { get; set; }

    public override string ToString() => "CloudsData";
}

public class EnvironmentKeyframe : IEntryData
{
    private const int Size = 0x240;
    private const int ColourCubeOffset = 0x80; // TintData::mpColourCube, filled by the import

    [ReadOnly(true)]
    public uint muVersion { get; set; } = 8;
    public BloomData mBloomData { get; set; } = new();
    public VignetteData mVignetteData { get; set; } = new();
    public TintData mTintData { get; set; } = new();
    public ScatteringData mScatteringData { get; set; } = new();
    public LightingData mLightingData { get; set; } = new();
    public CloudsData mCloudsData { get; set; } = new();

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        using MemoryStream ms = entry.MakeStream();
        using BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

        if (ms.Length < Size)
            throw new ReadFailedError($"Environment keyframe is 0x{ms.Length:X} bytes, expected at least 0x{Size:X}.");
        muVersion = br.ReadUInt32();
        if (muVersion != 8)
            throw new ReadFailedError($"Unsupported environment keyframe version {muVersion}, expected 8.");

        // Vector2 and Vector3 take 16 bytes
        Vector2 ReadVector2()
        {
            Vector2 v = new(br.ReadSingle(), br.ReadSingle());
            br.BaseStream.Position += 8;
            return v;
        }
        Vector3 ReadVector3()
        {
            Vector3 v = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            br.BaseStream.Position += 4;
            return v;
        }
        Vector4 ReadVector4() => new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        float[] ReadFloats() => [br.ReadSingle(), br.ReadSingle()];

        br.BaseStream.Position = 0x10;
        mBloomData = new() { mfLuminance = br.ReadSingle(), mfThreshold = br.ReadSingle() };
        br.BaseStream.Position = 0x20;
        mBloomData.mv4Scale = ReadVector4();

        mVignetteData = new() { mfAngle = br.ReadSingle(), mfSharpness = br.ReadSingle() };
        br.BaseStream.Position = 0x40;
        mVignetteData.mv2Amount = ReadVector2();
        mVignetteData.mv2Centre = ReadVector2();
        mVignetteData.mv4InnerColour = ReadVector4();
        mVignetteData.mv4OuterColour = ReadVector4();

        mTintData = new() { mpColourCube = entry.GetDependencies().FirstOrDefault(d => d.EntryPointerOffset == ColourCubeOffset).EntryID };

        br.BaseStream.Position = 0x90;
        ScatteringData s = mScatteringData = new();
        s.mv3SkyTopColour = ReadVector3();
        s.mv3SkyHorColour = ReadVector3();
        s.mv3SkySunColour = ReadVector3();
        s.mfSkyHorPow = br.ReadSingle();
        s.mfSkySunPow = br.ReadSingle();
        s.mfSkyDrk = br.ReadSingle();
        s.mfSkyHorBleedScl = br.ReadSingle();
        s.mfSkyHorBleedPow = br.ReadSingle();
        s.mfSkySunBleedPow = br.ReadSingle();
        br.BaseStream.Position = 0xE0;
        s.mv3ScattTopColour = ReadVector3();
        s.mv3ScattHorColour = ReadVector3();
        s.mv3ScattSunColour = ReadVector3();
        s.mfScattHorPow = br.ReadSingle();
        s.mfScattSunPow = br.ReadSingle();
        s.mfScattDrk = br.ReadSingle();
        s.mfScattHorBleedScl = br.ReadSingle();
        s.mfScattHorBleedPow = br.ReadSingle();
        s.mfScattSunBleedPow = br.ReadSingle();
        s.mafScattDist = ReadFloats();
        s.mfScattPow = br.ReadSingle();
        s.mfScattCap = br.ReadSingle();

        br.BaseStream.Position = 0x140;
        LightingData l = mLightingData = new();
        l.mv3KeyLightColour = ReadVector3();
        l.mv3SpecularColour = ReadVector3();
        l.mv3KeyFillColour = ReadVector3();
        l.mv3ShadowFillColour = ReadVector3();
        l.mv3RightFillColour = ReadVector3();
        l.mv3LeftFillColour = ReadVector3();
        l.mv3UpFillColour = ReadVector3();
        l.mv3DownFillColour = ReadVector3();
        l.mfAmbientIrradianceScale = br.ReadSingle();

        br.BaseStream.Position = 0x1D0;
        mCloudsData = new()
        {
            mav3LayerLiteColour = [ReadVector3(), ReadVector3()],
            mav3LayerDarkColour = [ReadVector3(), ReadVector3()],
            mafLayerDensity = ReadFloats(),
            mafLayerFeathering = ReadFloats(),
            mafLayerOpacity = ReadFloats(),
            mafLayerSpeed = ReadFloats(),
            mafLayerScale = ReadFloats(),
            mfDirectionAngle = br.ReadSingle()
        };

        return true;
    }

    // The resource with these values written over its current data, which keeps its padding and import table
    public byte[] Serialize(BundleEntry entry)
    {
        byte[] data = (byte[])entry.EntryBlocks[0].Data.Clone();
        using BinaryWriter2 bw = new(new MemoryStream(data));
        bw.BigEndian = entry.Console;

        void WriteVector2(Vector2 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
            bw.BaseStream.Position += 8;
        }
        void WriteVector3(Vector3 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
            bw.Write(v.Z);
            bw.BaseStream.Position += 4;
        }
        void WriteVector4(Vector4 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
            bw.Write(v.Z);
            bw.Write(v.W);
        }
        void WriteArray<T>(T[] values, Action<T> write, string name)
        {
            if (values.Length != 2)
                throw new InvalidDataException($"{name} has {values.Length} entries, it needs 2.");
            foreach (T value in values)
                write(value);
        }

        bw.Write(muVersion);

        bw.BaseStream.Position = 0x10;
        bw.Write(mBloomData.mfLuminance);
        bw.Write(mBloomData.mfThreshold);
        bw.BaseStream.Position = 0x20;
        WriteVector4(mBloomData.mv4Scale);

        bw.Write(mVignetteData.mfAngle);
        bw.Write(mVignetteData.mfSharpness);
        bw.BaseStream.Position = 0x40;
        WriteVector2(mVignetteData.mv2Amount);
        WriteVector2(mVignetteData.mv2Centre);
        WriteVector4(mVignetteData.mv4InnerColour);
        WriteVector4(mVignetteData.mv4OuterColour);

        bw.BaseStream.Position = 0x90;
        ScatteringData s = mScatteringData;
        WriteVector3(s.mv3SkyTopColour);
        WriteVector3(s.mv3SkyHorColour);
        WriteVector3(s.mv3SkySunColour);
        bw.Write(s.mfSkyHorPow);
        bw.Write(s.mfSkySunPow);
        bw.Write(s.mfSkyDrk);
        bw.Write(s.mfSkyHorBleedScl);
        bw.Write(s.mfSkyHorBleedPow);
        bw.Write(s.mfSkySunBleedPow);
        bw.BaseStream.Position = 0xE0;
        WriteVector3(s.mv3ScattTopColour);
        WriteVector3(s.mv3ScattHorColour);
        WriteVector3(s.mv3ScattSunColour);
        bw.Write(s.mfScattHorPow);
        bw.Write(s.mfScattSunPow);
        bw.Write(s.mfScattDrk);
        bw.Write(s.mfScattHorBleedScl);
        bw.Write(s.mfScattHorBleedPow);
        bw.Write(s.mfScattSunBleedPow);
        WriteArray(s.mafScattDist, bw.Write, nameof(s.mafScattDist));
        bw.Write(s.mfScattPow);
        bw.Write(s.mfScattCap);

        bw.BaseStream.Position = 0x140;
        LightingData l = mLightingData;
        WriteVector3(l.mv3KeyLightColour);
        WriteVector3(l.mv3SpecularColour);
        WriteVector3(l.mv3KeyFillColour);
        WriteVector3(l.mv3ShadowFillColour);
        WriteVector3(l.mv3RightFillColour);
        WriteVector3(l.mv3LeftFillColour);
        WriteVector3(l.mv3UpFillColour);
        WriteVector3(l.mv3DownFillColour);
        bw.Write(l.mfAmbientIrradianceScale);

        bw.BaseStream.Position = 0x1D0;
        CloudsData c = mCloudsData;
        WriteArray(c.mav3LayerLiteColour, WriteVector3, nameof(c.mav3LayerLiteColour));
        WriteArray(c.mav3LayerDarkColour, WriteVector3, nameof(c.mav3LayerDarkColour));
        WriteArray(c.mafLayerDensity, bw.Write, nameof(c.mafLayerDensity));
        WriteArray(c.mafLayerFeathering, bw.Write, nameof(c.mafLayerFeathering));
        WriteArray(c.mafLayerOpacity, bw.Write, nameof(c.mafLayerOpacity));
        WriteArray(c.mafLayerSpeed, bw.Write, nameof(c.mafLayerSpeed));
        WriteArray(c.mafLayerScale, bw.Write, nameof(c.mafLayerScale));
        bw.Write(c.mfDirectionAngle);

        // The colour cube is the import at the TintData pointer
        List<BundleDependency> imports = entry.GetDependencies();
        int colourCube = imports.FindIndex(d => d.EntryPointerOffset == ColourCubeOffset);
        if (colourCube >= 0)
        {
            bw.BaseStream.Position = entry.DependenciesListOffset + colourCube * Dependency.Size;
            bw.Write(mTintData.mpColourCube);
        }
        else if (mTintData.mpColourCube != 0)
            throw new InvalidDataException("This keyframe has no colour cube import to change.");

        return data;
    }

    public bool Write(BundleEntry entry)
    {
        entry.EntryBlocks[0].Data = Serialize(entry);
        entry.Dirty = true;
        return true;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.BrnEnvironmentKeyframe;
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        return new PropertyGridEditor("Environment Keyframe Editor", this, () => Serialize(entry), () => Write(entry));
    }
}
