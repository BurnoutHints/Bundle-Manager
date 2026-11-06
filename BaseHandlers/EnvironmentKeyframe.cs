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

// Names, groups and ranges are from the game's environment tweak menu (BrnWorld::Sky::DebugComponent), which
// doesn't cover bloom, vignette or cloud movement. Those descriptions come from how the game uses the values.
public class BloomData
{
    [DisplayName("Luminance"), FieldDescription("Brightness of the bloom. The game's default is 0.98.")]
    public float mfLuminance { get; set; }

    [DisplayName("Threshold"), FieldDescription("The game's default is 0.77.")]
    public float mfThreshold { get; set; }

    [DisplayName("Scale"), FieldDescription("Per channel bloom scale.")]
    public Vector4 mv4Scale { get; set; }
}

public class VignetteData
{
    [DisplayName("Angle"), FieldDescription("Rotation of the vignette.")]
    public float mfAngle { get; set; }

    [DisplayName("Sharpness"), FieldDescription("Hardness of the vignette's gradient. The game's default is 0.33.", -1, 1)]
    public float mfSharpness { get; set; }

    [DisplayName("Amount"), FieldDescription("Size of the vignette on each axis.")]
    public Vector2 mv2Amount { get; set; }

    [DisplayName("Centre"), FieldDescription("Centre of the vignette on screen.")]
    public Vector2 mv2Centre { get; set; }

    [DisplayName("Inner colour"), FieldDescription("Colour at the centre of the vignette.")]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector4 mv4InnerColour { get; set; }

    [DisplayName("Outer colour"), FieldDescription("Colour at the edge of the vignette.")]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector4 mv4OuterColour { get; set; }
}

public class TintData
{
    [DisplayName("Colour cube"), FieldDescription("ID of the imported Colour Cube resource, the colour grading this keyframe uses."), TypeConverter(typeof(HexConverter))]
    public ulong mpColourCube { get; set; }
}

public class ScatteringData
{
    [Category("Sky colours"), DisplayName("Top colour"), FieldDescription("Colour of the sky overhead.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3SkyTopColour { get; set; }

    [Category("Sky colours"), DisplayName("Horizon colour"), FieldDescription("Colour of the sky at the horizon.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3SkyHorColour { get; set; }

    [Category("Sky colours"), DisplayName("Sun colour"), FieldDescription("Colour of the sky around the sun.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3SkySunColour { get; set; }

    [Category("Sky colours"), DisplayName("Sky shape"), FieldDescription("Shape of the gradient from the horizon to the top.", 0, 10)]
    public float mfSkyHorPow { get; set; }

    [Category("Sky colours"), DisplayName("Sun shape"), FieldDescription("Shape of the glow around the sun.", 0, 50)]
    public float mfSkySunPow { get; set; }

    [Category("Sky colours"), DisplayName("Darkness"), FieldDescription("", 0, 1)]
    public float mfSkyDrk { get; set; }

    [Category("Sky colours"), DisplayName("Horizon bleeding height"), FieldDescription("", 0.01f, 50)]
    public float mfSkyHorBleedScl { get; set; }

    [Category("Sky colours"), DisplayName("Horizon bleeding width"), FieldDescription("", 0.01f, 50)]
    public float mfSkyHorBleedPow { get; set; }

    [Category("Sky colours"), DisplayName("Sun/horizon bleeding balance"), FieldDescription("", 0.01f, 50)]
    public float mfSkySunBleedPow { get; set; }

    [Category("Scattering colours"), DisplayName("Top colour"), FieldDescription("Colour of the haze over distant scenery, looking up.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3ScattTopColour { get; set; }

    [Category("Scattering colours"), DisplayName("Horizon colour"), FieldDescription("Colour of the haze over distant scenery at the horizon.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3ScattHorColour { get; set; }

    [Category("Scattering colours"), DisplayName("Sun colour"), FieldDescription("Colour of the haze over distant scenery towards the sun.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3ScattSunColour { get; set; }

    [Category("Scattering colours"), DisplayName("Sky shape"), FieldDescription("", 0, 10)]
    public float mfScattHorPow { get; set; }

    [Category("Scattering colours"), DisplayName("Sun shape"), FieldDescription("", 0, 50)]
    public float mfScattSunPow { get; set; }

    [Category("Scattering colours"), DisplayName("Darkness"), FieldDescription("", 0, 1)]
    public float mfScattDrk { get; set; }

    [Category("Scattering colours"), DisplayName("Horizon bleeding height"), FieldDescription("", 0.01f, 50)]
    public float mfScattHorBleedScl { get; set; }

    [Category("Scattering colours"), DisplayName("Horizon bleeding width"), FieldDescription("", 0.01f, 50)]
    public float mfScattHorBleedPow { get; set; }

    [Category("Scattering colours"), DisplayName("Sun/horizon bleeding balance"), FieldDescription("", 0.01f, 50)]
    public float mfScattSunBleedPow { get; set; }

    [Category("Scattering"), DisplayName("Distances"), FieldDescription("Scattering distances 0 and 1.", 1, 1000000)]
    [TypeConverter(typeof(NumberListConverter))]
    public float[] mafScattDist { get; set; } = new float[2];

    [Category("Scattering"), DisplayName("Power"), FieldDescription("", 0, 100)]
    public float mfScattPow { get; set; }

    [Category("Scattering"), DisplayName("Cap"), FieldDescription("", 0.01f, 1)]
    public float mfScattCap { get; set; }
}

public class LightingData
{
    [Category("Key light"), DisplayName("Key light colour"), FieldDescription("Colour of the main directional light.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3KeyLightColour { get; set; }

    [Category("Key light"), DisplayName("Specular colour"), FieldDescription("Colour of specular highlights.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3SpecularColour { get; set; }

    [Category("Irradiance rig"), DisplayName("Irradiance scale"), FieldDescription("Scale of the ambient light from the fill colours.", 0, 5)]
    public float mfAmbientIrradianceScale { get; set; }

    [Category("Irradiance rig"), DisplayName("Key fill colour"), FieldDescription("Ambient light from the key light's side.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3KeyFillColour { get; set; }

    [Category("Irradiance rig"), DisplayName("Shadow fill colour"), FieldDescription("Ambient light from the side away from the key light.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3ShadowFillColour { get; set; }

    [Category("Irradiance rig"), DisplayName("Left fill colour"), FieldDescription("Ambient light from the left.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3LeftFillColour { get; set; }

    [Category("Irradiance rig"), DisplayName("Right fill colour"), FieldDescription("Ambient light from the right.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3RightFillColour { get; set; }

    [Category("Irradiance rig"), DisplayName("Up fill colour"), FieldDescription("Ambient light from above.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3UpFillColour { get; set; }

    [Category("Irradiance rig"), DisplayName("Down fill colour"), FieldDescription("Ambient light from below.", 0, 10)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    public Vector3 mv3DownFillColour { get; set; }
}

public class CloudsData
{
    [Category("Layer colours"), DisplayName("Lit colours"), FieldDescription("Lit colour of layers 0 and 1.", 0, 1)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    [TypeConverter(typeof(ArrayTextConverter))]
    public Vector3[] mav3LayerLiteColour { get; set; } = new Vector3[2];

    [Category("Layer colours"), DisplayName("Dark colours"), FieldDescription("Dark colour of layers 0 and 1.", 0, 1)]
    [Editor(typeof(VectorColorEditor), typeof(UITypeEditor))]
    [TypeConverter(typeof(ArrayTextConverter))]
    public Vector3[] mav3LayerDarkColour { get; set; } = new Vector3[2];

    [Category("Layers"), DisplayName("Density"), FieldDescription("Layers 0 and 1.", 0, 1)]
    [TypeConverter(typeof(NumberListConverter))]
    public float[] mafLayerDensity { get; set; } = new float[2];

    [Category("Layers"), DisplayName("Feathering"), FieldDescription("Layers 0 and 1.", 0, 1)]
    [TypeConverter(typeof(NumberListConverter))]
    public float[] mafLayerFeathering { get; set; } = new float[2];

    [Category("Layers"), DisplayName("Opacity"), FieldDescription("Layers 0 and 1.", 0, 1)]
    [TypeConverter(typeof(NumberListConverter))]
    public float[] mafLayerOpacity { get; set; } = new float[2];

    [Category("Layers"), DisplayName("Speed"), FieldDescription("Scroll speed of layers 0 and 1. The game's default is 30.")]
    [TypeConverter(typeof(NumberListConverter))]
    public float[] mafLayerSpeed { get; set; } = new float[2];

    [Category("Layers"), DisplayName("Scale"), FieldDescription("Size of layers 0 and 1. The game's default is 7000.")]
    [TypeConverter(typeof(NumberListConverter))]
    public float[] mafLayerScale { get; set; } = new float[2];

    [Category("Wind"), DisplayName("Direction"), FieldDescription("Direction the clouds move in, in degrees.")]
    public float mfDirectionAngle { get; set; }
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
        Vector2 readVector2()
        {
            Vector2 v = new(br.ReadSingle(), br.ReadSingle());
            br.BaseStream.Position += 8;
            return v;
        }

        Vector3 readVector3()
        {
            Vector3 v = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            br.BaseStream.Position += 4;
            return v;
        }

        Vector4 readVector4() => new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        float[] readFloats() => [br.ReadSingle(), br.ReadSingle()];

        br.BaseStream.Position = 0x10;
        mBloomData = new()
        {
            mfLuminance = br.ReadSingle(),
            mfThreshold = br.ReadSingle()
        };
        br.BaseStream.Position = 0x20;
        mBloomData.mv4Scale = readVector4();

        mVignetteData = new()
        {
            mfAngle = br.ReadSingle(),
            mfSharpness = br.ReadSingle()
        };
        br.BaseStream.Position = 0x40;
        mVignetteData.mv2Amount = readVector2();
        mVignetteData.mv2Centre = readVector2();
        mVignetteData.mv4InnerColour = readVector4();
        mVignetteData.mv4OuterColour = readVector4();

        mTintData = new()
        {
            mpColourCube = entry.GetDependencies().FirstOrDefault(d => d.EntryPointerOffset == ColourCubeOffset).EntryID
        };

        br.BaseStream.Position = 0x90;
        ScatteringData s = mScatteringData = new();
        s.mv3SkyTopColour = readVector3();
        s.mv3SkyHorColour = readVector3();
        s.mv3SkySunColour = readVector3();
        s.mfSkyHorPow = br.ReadSingle();
        s.mfSkySunPow = br.ReadSingle();
        s.mfSkyDrk = br.ReadSingle();
        s.mfSkyHorBleedScl = br.ReadSingle();
        s.mfSkyHorBleedPow = br.ReadSingle();
        s.mfSkySunBleedPow = br.ReadSingle();
        br.BaseStream.Position = 0xE0;
        s.mv3ScattTopColour = readVector3();
        s.mv3ScattHorColour = readVector3();
        s.mv3ScattSunColour = readVector3();
        s.mfScattHorPow = br.ReadSingle();
        s.mfScattSunPow = br.ReadSingle();
        s.mfScattDrk = br.ReadSingle();
        s.mfScattHorBleedScl = br.ReadSingle();
        s.mfScattHorBleedPow = br.ReadSingle();
        s.mfScattSunBleedPow = br.ReadSingle();
        s.mafScattDist = readFloats();
        s.mfScattPow = br.ReadSingle();
        s.mfScattCap = br.ReadSingle();

        br.BaseStream.Position = 0x140;
        LightingData l = mLightingData = new();
        l.mv3KeyLightColour = readVector3();
        l.mv3SpecularColour = readVector3();
        l.mv3KeyFillColour = readVector3();
        l.mv3ShadowFillColour = readVector3();
        l.mv3RightFillColour = readVector3();
        l.mv3LeftFillColour = readVector3();
        l.mv3UpFillColour = readVector3();
        l.mv3DownFillColour = readVector3();
        l.mfAmbientIrradianceScale = br.ReadSingle();

        br.BaseStream.Position = 0x1D0;
        mCloudsData = new()
        {
            mav3LayerLiteColour = [readVector3(), readVector3()],
            mav3LayerDarkColour = [readVector3(), readVector3()],
            mafLayerDensity = readFloats(),
            mafLayerFeathering = readFloats(),
            mafLayerOpacity = readFloats(),
            mafLayerSpeed = readFloats(),
            mafLayerScale = readFloats(),
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

        void writeVector2(Vector2 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
            bw.BaseStream.Position += 8;
        }

        void writeVector3(Vector3 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
            bw.Write(v.Z);
            bw.BaseStream.Position += 4;
        }

        void writeVector4(Vector4 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
            bw.Write(v.Z);
            bw.Write(v.W);
        }

        void writeArray<T>(T[] values, Action<T> write, string name)
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
        writeVector4(mBloomData.mv4Scale);

        bw.Write(mVignetteData.mfAngle);
        bw.Write(mVignetteData.mfSharpness);
        bw.BaseStream.Position = 0x40;
        writeVector2(mVignetteData.mv2Amount);
        writeVector2(mVignetteData.mv2Centre);
        writeVector4(mVignetteData.mv4InnerColour);
        writeVector4(mVignetteData.mv4OuterColour);

        bw.BaseStream.Position = 0x90;
        ScatteringData s = mScatteringData;
        writeVector3(s.mv3SkyTopColour);
        writeVector3(s.mv3SkyHorColour);
        writeVector3(s.mv3SkySunColour);
        bw.Write(s.mfSkyHorPow);
        bw.Write(s.mfSkySunPow);
        bw.Write(s.mfSkyDrk);
        bw.Write(s.mfSkyHorBleedScl);
        bw.Write(s.mfSkyHorBleedPow);
        bw.Write(s.mfSkySunBleedPow);
        bw.BaseStream.Position = 0xE0;
        writeVector3(s.mv3ScattTopColour);
        writeVector3(s.mv3ScattHorColour);
        writeVector3(s.mv3ScattSunColour);
        bw.Write(s.mfScattHorPow);
        bw.Write(s.mfScattSunPow);
        bw.Write(s.mfScattDrk);
        bw.Write(s.mfScattHorBleedScl);
        bw.Write(s.mfScattHorBleedPow);
        bw.Write(s.mfScattSunBleedPow);
        writeArray(s.mafScattDist, bw.Write, nameof(s.mafScattDist));
        bw.Write(s.mfScattPow);
        bw.Write(s.mfScattCap);

        bw.BaseStream.Position = 0x140;
        LightingData l = mLightingData;
        writeVector3(l.mv3KeyLightColour);
        writeVector3(l.mv3SpecularColour);
        writeVector3(l.mv3KeyFillColour);
        writeVector3(l.mv3ShadowFillColour);
        writeVector3(l.mv3RightFillColour);
        writeVector3(l.mv3LeftFillColour);
        writeVector3(l.mv3UpFillColour);
        writeVector3(l.mv3DownFillColour);
        bw.Write(l.mfAmbientIrradianceScale);

        bw.BaseStream.Position = 0x1D0;
        CloudsData c = mCloudsData;
        writeArray(c.mav3LayerLiteColour, writeVector3, nameof(c.mav3LayerLiteColour));
        writeArray(c.mav3LayerDarkColour, writeVector3, nameof(c.mav3LayerDarkColour));
        writeArray(c.mafLayerDensity, bw.Write, nameof(c.mafLayerDensity));
        writeArray(c.mafLayerFeathering, bw.Write, nameof(c.mafLayerFeathering));
        writeArray(c.mafLayerOpacity, bw.Write, nameof(c.mafLayerOpacity));
        writeArray(c.mafLayerSpeed, bw.Write, nameof(c.mafLayerSpeed));
        writeArray(c.mafLayerScale, bw.Write, nameof(c.mafLayerScale));
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
        return new PropertyGridEditor("Environment Keyframe Editor", () => Serialize(entry), () => Write(entry),
            ("Lighting", mLightingData: mLightingData), ("Sky", mScatteringData: mScatteringData), ("Clouds", mCloudsData: mCloudsData),
            ("Bloom", mBloomData: mBloomData), ("Vignette", mVignetteData: mVignetteData), ("Colour Cube", mTintData: mTintData));
    }
}
