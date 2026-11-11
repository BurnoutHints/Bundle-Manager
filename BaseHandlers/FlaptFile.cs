using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers.Flapt;

// BrnFlapt::FlaptFile (https://burnout.wiki/wiki/Flapt_File)
[TypeConverter(typeof(ExpandableObjectConverter))]
public class HashedString
{
    private string debugString;

    [DisplayName("Hash"), Description("Language hash of the string."), TypeConverter(typeof(HexConverter))]
    public uint muHash { get; set; }

    [DisplayName("String (DEBUG)"), Description("Changing it also updates the hash, unless the hash was set to something else.")]
    public string mpacDEBUGString
    {
        get => debugString;
        set
        {
            if (value != null && (muHash == 0 || muHash == Hash(debugString ?? "")))
                muHash = Hash(value);

            debugString = value;
        }
    }

    public HashedString()
    {
    }

    public HashedString(uint hash, string initialDebugString)
    {
        muHash = hash;
        debugString = initialDebugString;
    }

    public static uint Hash(string value) => ~(uint)CRC32.HashCRC32B(value);

    public override string ToString() => mpacDEBUGString ?? $"0x{muHash:X8}";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class GUITexture
{
    [Description("ID of the imported Texture resource."), TypeConverter(typeof(HexConverter))]
    public ulong ResourceId { get; set; }

    public override string ToString() => $"0x{ResourceId:X8}";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class GUIVertex
{
    public Vector2 mv2Pos { get; set; }

    [TypeConverter(typeof(ColorConverter)), Editor(typeof(ColorEditor), typeof(UITypeEditor))]
    public Color mv4Colour { get; set; } = Color.White;

    public Vector2 mv2Tex0UV { get; set; }

    public override string ToString() => $"{mv2Pos.X}, {mv2Pos.Y}";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class FontStyle
{
    [DisplayName("Name")]
    public string mpacFontName { get; set; }

    [Category("Appearance"), DisplayName("Colour")]
    [TypeConverter(typeof(ColorConverter)), Editor(typeof(ColorEditor), typeof(UITypeEditor))]
    public Color muColour { get; set; } = Color.White;

    [Category("Appearance"), DisplayName("Font Size")]
    public float mfFontHeight { get; set; }

    public override string ToString() => $"{mpacFontName} - {mfFontHeight}pt - #{muColour.ToArgb():X8}";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class IndexPath
{
    [DisplayName("Depth")]
    public byte muDepth { get; set; }

    [DisplayName("Indices"), Description("Always 32 entries."), TypeConverter(typeof(NumberListConverter))]
    public byte[] mauIndices { get; set; } = new byte[32];

    public override string ToString() => string.Join(".", mauIndices.Take(muDepth));
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class TriggerParameters
{
    [DisplayName("Parameters"), Description("Always 4 entries, unused ones are empty."), Editor(FlaptFile.StringListEditor, typeof(UITypeEditor))]
    public string[] mapcParameters { get; set; } = new string[4];

    public override string ToString() => string.Join(", ", mapcParameters.Where(p => !string.IsNullOrEmpty(p)));
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class FileDebugData
{
    [Editor(FlaptFile.StringListEditor, typeof(UITypeEditor))]
    public List<string> mpapStrings { get; set; } = [];

    public override string ToString() => $"{mpapStrings.Count} strings";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class RenderLayer
{
    public byte mxFlags { get; set; }

    public byte muMovieClipOffset { get; set; }

    public byte muMovieClipCount { get; set; }

    public byte muMeshOffset { get; set; }

    public byte muMeshCount { get; set; }

    public byte muTextFieldOffset { get; set; }

    public byte muTextFieldCount { get; set; }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class RenderLayerKeyFrame
{
    [TypeConverter(typeof(HexConverter))]
    public uint mxEnabledMovieClips { get; set; }

    [TypeConverter(typeof(HexConverter))]
    public uint mxEnabledMeshes { get; set; }

    [TypeConverter(typeof(HexConverter))]
    public uint mxEnabledTextFields { get; set; }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class Transform
{
    public Vector4 mRightUp { get; set; }

    public Vector4 mOriginXYZ { get; set; }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class ColourTransform
{
    public Vector4 mScale { get; set; } = Vector4.One;

    public Vector4 mTranslate { get; set; }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class KeyFrameAnims
{
    [Description("Always 3 entries."), TypeConverter(typeof(NumberListConverter))]
    public uint[] maxPlacedChildren { get; set; } = new uint[3];

    public byte muCommandOffset { get; set; }

    public byte muCommandCount { get; set; }

    [Description("One per transform."), TypeConverter(typeof(NumberListConverter))]
    public List<byte> mpauTransformObjects { get; set; } = [];

    public List<Transform> mpaTransforms { get; set; } = [];

    [Description("One per colour transform."), TypeConverter(typeof(NumberListConverter))]
    public List<byte> mpauColourTransformObjects { get; set; } = [];

    public List<ColourTransform> mpaColourTransforms { get; set; } = [];
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class FScriptCommand
{
    [DisplayName("Command Index")]
    public byte muCommand { get; set; }

    [DisplayName("Parameter 1 (8-bit)")]
    public byte muParam8 { get; set; }

    [DisplayName("Parameter 2 (16-bit)")]
    public ushort muParam16 { get; set; }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class Mesh
{
    [Description("Index into the file's textures, -1 for none.")]
    public sbyte miTextureId { get; set; }

    public byte muNumVerts { get; set; }

    [Description("Index of the first vertex in the file's vertices.")]
    public ushort muVertOffset { get; set; }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class TextField
{
    [Description("Unused in retail, the names are in the movie clip's text field names.")]
    public HashedString mName { get; set; } = new();

    [Description("Index into the file's strings.")]
    public ushort muInitialStringId { get; set; }

    [Category("Appearance"), DisplayName("Font Style"), TypeConverter(typeof(FontStyleIndexConverter))]
    public byte muFontStyleIndex { get; set; }

    public byte mxFlags { get; set; }

    public byte muAlignment { get; set; }

    [Category("Bounds"), DisplayName("Top Left")]
    public Vector2 mTopLeft { get; set; }

    [Category("Bounds"), DisplayName("Bottom Right")]
    public Vector2 mBottomRight { get; set; }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class MovieClip
{
    public byte mxFlags { get; set; }

    [Description("The frame to keyframe map's size, or the keyframe count without one.")]
    public ushort muNumFramesInTimeline => (ushort)(mpauFrameToKeyFrameMap.Count > 0
        ? mpauFrameToKeyFrameMap.Count
        : mpaKeyFrameAnims.Count);

    [Description("Keyframe for each frame. Empty when every frame is a keyframe."), TypeConverter(typeof(NumberListConverter))]
    public List<ushort> mpauFrameToKeyFrameMap { get; set; } = [];

    [Category("Movie"), DisplayName("Render Layers")]
    public List<RenderLayer> mpaRenderLayers { get; set; } = [];

    [Category("Movie"), DisplayName("Render Layer Keyframes"), Description("One per render layer for each keyframe.")]
    public List<RenderLayerKeyFrame> mpaKeyFrames { get; set; } = [];

    [Category("Movie"), Description("One per keyframe.")]
    public List<KeyFrameAnims> mpaKeyFrameAnims { get; set; } = [];

    [Category("Movie"), DisplayName("FScript Commands")]
    public List<FScriptCommand> mpaFScriptStream { get; set; } = [];

    [Category("Movie"), DisplayName("Child Movies"), Description("Indices into the file's movie clips."), TypeConverter(typeof(NumberListConverter))]
    public List<ushort> mpauChildMovieClips { get; set; } = [];

    [Category("Movie"), Description("One per child movie.")]
    public List<HashedString> mpaChildNames { get; set; } = [];

    public List<Mesh> mpaMeshes { get; set; } = [];

    [Category("Text"), DisplayName("Text Fields")]
    public List<TextField> mpaTextFields { get; set; } = [];

    [Category("Text"), Description("One per text field.")]
    public List<HashedString> mpaTextFieldNames { get; set; } = [];

    public List<HashedString> mpaFrameLabels { get; set; } = [];

    [Description("One per frame label."), TypeConverter(typeof(NumberListConverter))]
    public List<ushort> mpauLabelledFrameIds { get; set; } = [];

    [Category("Name"), DisplayName("Name")]
    public string mpcComponentName { get; set; }

    public override string ToString() => mpcComponentName ?? "(unnamed)";
}

public class FlaptFile : IEntryData
{
    // Read by FontStyleIndexConverter, which only sees the text field
    public static FlaptFile Editing;

    // Internal to WinForms, so it can only be named
    internal const string StringListEditor = "System.Windows.Forms.Design.StringCollectionEditor, System.Windows.Forms.Design";

    [Category("info"), ReadOnly(true)]
    public byte muVersion { get; set; } = 12;

    [Category("info"), ReadOnly(true)]
    public uint muSizeInBytes { get; set; }

    [Category("Global Properties"), DisplayName("Seconds Per Frame"), Description("Seconds per frame, for instance 0.03333333 equates to ~30 frames per second.")]
    public float mfTimePerFrame { get; set; } = 1 / 30f;

    [DisplayName("Movie Clips")]
    public List<MovieClip> mpaMovieClips { get; set; } = [];

    [DisplayName("Textures"), Description("Imported textures. The special textures take the slots after these.")]
    public List<GUITexture> mpapTextures { get; set; } = [];

    [DisplayName("Vertices")]
    public List<GUIVertex> mpaVerts { get; set; } = [];

    [DisplayName("Font Styles")]
    public List<FontStyle> mpaFontStyles { get; set; } = [];

    [DisplayName("Component Names")]
    public List<HashedString> mpaComponentNames { get; set; } = [];

    [DisplayName("Component Paths"), Description("One per component name.")]
    public List<IndexPath> mpaComponentPaths { get; set; } = [];

    [DisplayName("Trigger Parameters")]
    public List<TriggerParameters> mpaTriggerParameters { get; set; } = [];

    [DisplayName("Strings")]
    [Editor(FlaptFile.StringListEditor, typeof(UITypeEditor))]
    public List<string> mpapStrings { get; set; } = [];

    [DisplayName("Special Texture Names"), Description("Textures the game sets at runtime.")]
    [Editor(FlaptFile.StringListEditor, typeof(UITypeEditor))]
    public List<string> mpapSpecialTextureNames { get; set; } = [];

    [DisplayName("Debug Data")]
    public FileDebugData mDEBUGData { get; set; } = new();

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        using MemoryStream ms = entry.MakeStream();
        using BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

        // Decomp has 8-byte pointers, aligned to 8 bytes
        bool x64 = entry.Platform == BundlePlatform.PCx64;

        long readPtr()
        {
            if (!x64)
                return br.ReadUInt32();

            br.Align(8);
            return (long)br.ReadUInt64();
        }

        muVersion = br.ReadByte();

        if (muVersion != 12)
            throw new ReadFailedError($"Unsupported FlaptFile version {muVersion}, expected 12.");

        br.ReadBytes(3); // Padding
        muSizeInBytes = br.ReadUInt32();

        // Empty arrays and missing strings have pointers of 0 or junk past the end
        bool valid(long ptr) => ptr != 0 && ptr < muSizeInBytes;

        List<T> readArray<T>(long ptr, int count, Func<T> read)
        {
            List<T> list = new(count);

            if (count == 0 || !valid(ptr))
                return list;

            long pos = br.BaseStream.Position;
            br.BaseStream.Position = ptr;

            for (int i = 0; i < count; i++)
                list.Add(read());

            br.BaseStream.Position = pos;
            return list;
        }

        string readString()
        {
            long currentPtr = readPtr();

            if (!valid(currentPtr))
                return null;

            long pos = br.BaseStream.Position;
            br.BaseStream.Position = currentPtr;
            string result = br.ReadCStr();
            br.BaseStream.Position = pos;
            return result;
        }

        Color readRGBA8()
        {
            byte r = br.ReadByte(), g = br.ReadByte(), b = br.ReadByte(), a = br.ReadByte();
            return Color.FromArgb(a, r, g, b);
        }

        Vector2 readVector2() => new(br.ReadSingle(), br.ReadSingle());

        Vector4 readVector4() => new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        HashedString readHashedString() => new(br.ReadUInt32(), readString());

        KeyFrameAnims readKeyFrameAnims()
        {
            KeyFrameAnims anims = new()
            {
                maxPlacedChildren = [br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32()],
                muCommandOffset = br.ReadByte(),
                muCommandCount = br.ReadByte()
            };

            byte numTransforms = br.ReadByte();
            byte numColourTransforms = br.ReadByte();
            anims.mpauTransformObjects = readArray(readPtr(), numTransforms, br.ReadByte);
            anims.mpaTransforms = readArray(readPtr(), numTransforms, () => new Transform()
            {
                mRightUp = readVector4(),
                mOriginXYZ = readVector4()
            });
            anims.mpauColourTransformObjects = readArray(readPtr(), numColourTransforms, br.ReadByte);
            anims.mpaColourTransforms = readArray(readPtr(), numColourTransforms, () => new ColourTransform()
            {
                mScale = readVector4(),
                mTranslate = readVector4()
            });
            return anims;
        }

        TextField readTextField()
        {
            TextField textField = new()
            {
                mName = readHashedString(),
                muInitialStringId = br.ReadUInt16(),
                muFontStyleIndex = br.ReadByte(),
                mxFlags = br.ReadByte(),
                muAlignment = br.ReadByte()
            };

            br.ReadBytes(3); // Padding
            textField.mTopLeft = readVector2();
            textField.mBottomRight = readVector2();
            return textField;
        }

        MovieClip readMovieClip()
        {
            MovieClip clip = new()
            {
                mxFlags = br.ReadByte()
            };

            byte numChildren = br.ReadByte();
            byte numMeshes = br.ReadByte();
            byte numTextFields = br.ReadByte();
            byte numRenderLayers = br.ReadByte();
            byte numLabelledFrames = br.ReadByte();
            byte numFScriptCommands = br.ReadByte();
            br.ReadByte(); // Padding
            ushort numFramesInTimeline = br.ReadUInt16();
            ushort numKeyFrames = br.ReadUInt16();
            readPtr(); // mpFile, set at load

            clip.mpauFrameToKeyFrameMap = readArray(readPtr(), numFramesInTimeline, br.ReadUInt16);
            clip.mpaRenderLayers = readArray(readPtr(), numRenderLayers, () => new RenderLayer()
            {
                mxFlags = br.ReadByte(),
                muMovieClipOffset = br.ReadByte(),
                muMovieClipCount = br.ReadByte(),
                muMeshOffset = br.ReadByte(),
                muMeshCount = br.ReadByte(),
                muTextFieldOffset = br.ReadByte(),
                muTextFieldCount = br.ReadByte()
            });
            clip.mpaKeyFrames = readArray(readPtr(), numKeyFrames * numRenderLayers, () => new RenderLayerKeyFrame()
            {
                mxEnabledMovieClips = br.ReadUInt32(),
                mxEnabledMeshes = br.ReadUInt32(),
                mxEnabledTextFields = br.ReadUInt32()
            });
            clip.mpaKeyFrameAnims = readArray(readPtr(), numKeyFrames, readKeyFrameAnims);
            clip.mpaFScriptStream = readArray(readPtr(), numFScriptCommands, () => new FScriptCommand()
            {
                muCommand = br.ReadByte(),
                muParam8 = br.ReadByte(),
                muParam16 = br.ReadUInt16()
            });
            clip.mpauChildMovieClips = readArray(readPtr(), numChildren, br.ReadUInt16);
            clip.mpaChildNames = readArray(readPtr(), numChildren, readHashedString);
            clip.mpaMeshes = readArray(readPtr(), numMeshes, () => new Mesh()
            {
                miTextureId = br.ReadSByte(),
                muNumVerts = br.ReadByte(),
                muVertOffset = br.ReadUInt16()
            });
            clip.mpaTextFields = readArray(readPtr(), numTextFields, readTextField);
            clip.mpaTextFieldNames = readArray(readPtr(), numTextFields, readHashedString);
            clip.mpaFrameLabels = readArray(readPtr(), numLabelledFrames, readHashedString);
            clip.mpauLabelledFrameIds = readArray(readPtr(), numLabelledFrames, br.ReadUInt16);
            clip.mpcComponentName = readString();
            return clip;
        }

        mfTimePerFrame = br.ReadSingle();
        int numMovieClips = br.ReadInt32();
        mpaMovieClips = readArray(readPtr(), numMovieClips, readMovieClip);

        int numTextures = br.ReadInt32();
        long texturesPtr = readPtr();
        int numVerts = br.ReadInt32();
        mpaVerts = readArray(readPtr(), numVerts, () => new GUIVertex()
        {
            mv2Pos = readVector2(),
            mv4Colour = readRGBA8(),
            mv2Tex0UV = readVector2()
        });
        int numFontStyles = br.ReadInt32();
        mpaFontStyles = readArray(readPtr(), numFontStyles, () => new FontStyle()
        {
            mpacFontName = readString(),
            muColour = Color.FromArgb(br.ReadInt32()),
            mfFontHeight = br.ReadSingle()
        });
        int numComponents = br.ReadInt32();
        mpaComponentNames = readArray(readPtr(), numComponents, readHashedString);
        mpaComponentPaths = readArray(readPtr(), numComponents, () => new IndexPath()
        {
            muDepth = br.ReadByte(),
            mauIndices = br.ReadBytes(32)
        });
        int numTriggerParameters = br.ReadInt32();
        mpaTriggerParameters = readArray(readPtr(), numTriggerParameters, () => new TriggerParameters()
        {
            mapcParameters = [readString(), readString(), readString(), readString()]
        });
        int numStrings = br.ReadInt32();
        mpapStrings = readArray(readPtr(), numStrings, readString);
        int numSpecialTextures = br.ReadInt32();
        mpapSpecialTextureNames = readArray(readPtr(), numSpecialTextures, readString);
        int numDebugStrings = br.ReadInt32();
        mDEBUGData = new()
        {
            mpapStrings = readArray(readPtr(), numDebugStrings, readString)
        };

        // The bundle's imports fill the texture slots, the special textures come last and are set at runtime
        int ptrSize = x64
            ? 8
            : 4;
        mpapTextures = [.. Enumerable.Range(0, Math.Max(numTextures - numSpecialTextures, 0)).Select(_ => new GUITexture())];

        foreach (BundleDependency dependency in entry.GetDependencies())
        {
            long slot = (dependency.EntryPointerOffset - texturesPtr) / ptrSize;

            if (dependency.EntryPointerOffset >= texturesPtr && slot < mpapTextures.Count)
                mpapTextures[(int)slot].ResourceId = dependency.EntryID;
        }

        return true;
    }

    // Array layout writer followed by string pool
    private sealed class LayoutWriter(BinaryWriter2 bw, bool x64)
    {
        private readonly Queue<Action> arrays = new();
        private readonly Queue<Action> alignedArrays = new(); // 16-byte aligned transform arrays
        private readonly List<(long Position, string Value)> strings = [];

        public int PtrSize => x64
            ? 8
            : 4;

        public long Position => bw.BaseStream.Position;

        public void Align(int alignment)
        {
            while (Position % alignment != 0)
                bw.Write((byte)0);
        }

        // Returns where the pointer went
        public long Ptr(long value)
        {
            Align(PtrSize);
            long pos = Position;

            if (x64)
                bw.Write((ulong)value);
            else
                bw.Write((uint)value);

            return pos;
        }

        private void Patch(long pos, long value)
        {
            long end = Position;
            bw.BaseStream.Position = pos;
            Ptr(value);
            bw.BaseStream.Position = end;
        }

        public void String(string value)
        {
            long pos = Ptr(0);

            if (value != null)
                strings.Add((pos: pos, value));
        }

        public void Array<T>(IList<T> items, Action<T> write, int alignment = 0)
        {
            long pos = Ptr(0);

            if (items.Count == 0)
                return;

            (alignment > PtrSize
                ? alignedArrays
                : arrays).Enqueue(() =>
            {
                Align(alignment == 0
                    ? PtrSize
                    : alignment); // 0 for structures with pointers
                Patch(pos, Position);

                foreach (T item in items)
                    write(item);
            });
        }

        public void Finish()
        {
            while (arrays.TryDequeue(out Action write) || alignedArrays.TryDequeue(out write))
                write();

            Dictionary<string, long> pool = [];

            foreach ((long pos, string value) in strings)
            {
                if (!pool.TryGetValue(value, out long offset))
                {
                    offset = Position;
                    pool.Add(value, offset);
                    bw.WriteCStr(value);
                }

                Patch(pos, offset);
            }

            Align(16);
        }
    }

    private static byte Count8(int count, string what)
    {
        if (count > byte.MaxValue)
            throw new InvalidDataException($"{what} has {count} entries, the most it can have is {byte.MaxValue}.");

        return (byte)count;
    }

    private static void CheckCount(int count, int expected, string what)
    {
        if (count != expected)
            throw new InvalidDataException($"{what} has {count} entries, it needs {expected}.");
    }

    public bool Write(BundleEntry entry)
    {
        byte[] data = Serialize(entry, out int importCount);

        entry.EntryBlocks[0].Data = data;
        entry.Dependencies.Clear(); // A BND1 import list would hide the table
        entry.DependencyCount = (short)importCount;
        entry.DependenciesListOffset = importCount > 0
            ? data.Length - importCount * Dependency.Size
            : 0;
        entry.Dirty = true;

        return true;
    }

    // Serializes primary data ending with the import table
    public byte[] Serialize(BundleEntry entry, out int importCount)
    {
        using MemoryStream ms = new();
        using BinaryWriter2 bw = new(ms);
        bw.BigEndian = entry.Console;

        LayoutWriter w = new(bw, entry.Platform == BundlePlatform.PCx64);
        List<Dependency> imports = [];

        void writeVector2(Vector2 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
        }

        void writeVector4(Vector4 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
            bw.Write(v.Z);
            bw.Write(v.W);
        }

        void writeHashedString(HashedString hashedString)
        {
            bw.Write(hashedString.muHash);
            w.String(hashedString.mpacDEBUGString);
        }

        void writeKeyFrameAnims(KeyFrameAnims anims, string clip)
        {
            if (anims.maxPlacedChildren.Length > 3)
                throw new InvalidDataException($"A keyframe of movie clip {clip} has {anims.maxPlacedChildren.Length} placed children masks, it needs 3.");

            for (int i = 0; i < 3; i++)
                bw.Write(i < anims.maxPlacedChildren.Length
                    ? anims.maxPlacedChildren[i]
                    : 0u);

            bw.Write(anims.muCommandOffset);
            bw.Write(anims.muCommandCount);
            bw.Write(Count8(anims.mpaTransforms.Count, $"A keyframe's transforms in movie clip {clip}"));
            bw.Write(Count8(anims.mpaColourTransforms.Count, $"A keyframe's colour transforms in movie clip {clip}"));
            CheckCount(anims.mpauTransformObjects.Count, anims.mpaTransforms.Count, $"A keyframe's transform objects in movie clip {clip}");
            CheckCount(anims.mpauColourTransformObjects.Count, anims.mpaColourTransforms.Count, $"A keyframe's colour transform objects in movie clip {clip}");
            w.Array(anims.mpauTransformObjects, bw.Write, 1);
            w.Array(anims.mpaTransforms, t =>
            {
                writeVector4(t.mRightUp);
                writeVector4(t.mOriginXYZ);
            }, 16);
            w.Array(anims.mpauColourTransformObjects, bw.Write, 1);
            w.Array(anims.mpaColourTransforms, t =>
            {
                writeVector4(t.mScale);
                writeVector4(t.mTranslate);
            }, 16);
        }

        void writeMovieClip(MovieClip clip)
        {
            string name = clip.ToString();
            CheckCount(clip.mpaChildNames.Count, clip.mpauChildMovieClips.Count, $"Movie clip {name}'s child names");
            CheckCount(clip.mpaTextFieldNames.Count, clip.mpaTextFields.Count, $"Movie clip {name}'s text field names");
            CheckCount(clip.mpauLabelledFrameIds.Count, clip.mpaFrameLabels.Count, $"Movie clip {name}'s labelled frame IDs");
            CheckCount(clip.mpaKeyFrames.Count, clip.mpaKeyFrameAnims.Count * clip.mpaRenderLayers.Count, $"Movie clip {name}'s render layer keyframes");

            if (clip.mpaKeyFrameAnims.Count > ushort.MaxValue)
                throw new InvalidDataException($"Movie clip {name} has {clip.mpaKeyFrameAnims.Count} keyframes, the most it can have is {ushort.MaxValue}.");

            bw.Write(clip.mxFlags);
            bw.Write(Count8(clip.mpauChildMovieClips.Count, $"Movie clip {name}'s children"));
            bw.Write(Count8(clip.mpaMeshes.Count, $"Movie clip {name}'s meshes"));
            bw.Write(Count8(clip.mpaTextFields.Count, $"Movie clip {name}'s text fields"));
            bw.Write(Count8(clip.mpaRenderLayers.Count, $"Movie clip {name}'s render layers"));
            bw.Write(Count8(clip.mpaFrameLabels.Count, $"Movie clip {name}'s frame labels"));
            bw.Write(Count8(clip.mpaFScriptStream.Count, $"Movie clip {name}'s FScript commands"));
            bw.Write((byte)0); // Padding
            bw.Write(clip.muNumFramesInTimeline);
            bw.Write((ushort)clip.mpaKeyFrameAnims.Count);
            w.Ptr(0); // mpFile, set at load

            w.Array(clip.mpauFrameToKeyFrameMap, bw.Write, 2);
            w.Array(clip.mpaRenderLayers, layer =>
            {
                bw.Write(layer.mxFlags);
                bw.Write(layer.muMovieClipOffset);
                bw.Write(layer.muMovieClipCount);
                bw.Write(layer.muMeshOffset);
                bw.Write(layer.muMeshCount);
                bw.Write(layer.muTextFieldOffset);
                bw.Write(layer.muTextFieldCount);
            }, 1);
            w.Array(clip.mpaKeyFrames, keyFrame =>
            {
                bw.Write(keyFrame.mxEnabledMovieClips);
                bw.Write(keyFrame.mxEnabledMeshes);
                bw.Write(keyFrame.mxEnabledTextFields);
            }, 4);
            w.Array(clip.mpaKeyFrameAnims, anims => writeKeyFrameAnims(anims, name));
            w.Array(clip.mpaFScriptStream, command =>
            {
                bw.Write(command.muCommand);
                bw.Write(command.muParam8);
                bw.Write(command.muParam16);
            }, 2);
            w.Array(clip.mpauChildMovieClips, bw.Write, 2);
            w.Array(clip.mpaChildNames, writeHashedString);
            w.Array(clip.mpaMeshes, mesh =>
            {
                bw.Write(mesh.miTextureId);
                bw.Write(mesh.muNumVerts);
                bw.Write(mesh.muVertOffset);
            }, 2);
            w.Array(clip.mpaTextFields, textField =>
            {
                writeHashedString(textField.mName);
                bw.Write(textField.muInitialStringId);
                bw.Write(textField.muFontStyleIndex);
                bw.Write(textField.mxFlags);
                bw.Write(textField.muAlignment);
                bw.WriteUniquePadding(3);
                writeVector2(textField.mTopLeft);
                writeVector2(textField.mBottomRight);
            });
            w.Array(clip.mpaTextFieldNames, writeHashedString);
            w.Array(clip.mpaFrameLabels, writeHashedString);
            w.Array(clip.mpauLabelledFrameIds, bw.Write, 2);
            w.String(clip.mpcComponentName);
        }

        CheckCount(mpaComponentPaths.Count, mpaComponentNames.Count, "Component paths");

        bw.Write(muVersion);
        bw.WriteUniquePadding(3);
        long sizePosition = w.Position;
        bw.Write(0u); // Size, set at the end
        bw.Write(mfTimePerFrame);
        bw.Write(mpaMovieClips.Count);
        w.Array(mpaMovieClips, writeMovieClip);

        // The bundle's imports point the texture slots at the textures
        List<ulong> textureSlots = [.. mpapTextures.Select(t => t.ResourceId), .. mpapSpecialTextureNames.Select(_ => 0ul)];
        bw.Write(textureSlots.Count);
        w.Array(textureSlots, id =>
        {
            long pos = w.Ptr(0);

            if (id != 0)
                imports.Add(new()
                {
                    ID = id,
                    EntryPointerOffset = (uint)pos
                });
        });

        bw.Write(mpaVerts.Count);
        w.Array(mpaVerts, vertex =>
        {
            writeVector2(vertex.mv2Pos);
            bw.Write(vertex.mv4Colour.R);
            bw.Write(vertex.mv4Colour.G);
            bw.Write(vertex.mv4Colour.B);
            bw.Write(vertex.mv4Colour.A);
            writeVector2(vertex.mv2Tex0UV);
        }, 4);

        bw.Write(mpaFontStyles.Count);
        w.Array(mpaFontStyles, fontStyle =>
        {
            w.String(fontStyle.mpacFontName);
            bw.Write(fontStyle.muColour.ToArgb());
            bw.Write(fontStyle.mfFontHeight);
        });

        bw.Write(mpaComponentNames.Count);
        w.Array(mpaComponentNames, writeHashedString);
        w.Array(mpaComponentPaths, path =>
        {
            if (path.mauIndices.Length > 32)
                throw new InvalidDataException($"Component path {path} has {path.mauIndices.Length} indices, the most it can have is 32.");

            bw.Write(path.muDepth);
            bw.Write(path.mauIndices);
            bw.WriteUniquePadding(32 - path.mauIndices.Length);
        }, 1);

        bw.Write(mpaTriggerParameters.Count);
        w.Array(mpaTriggerParameters, parameters =>
        {
            if (parameters.mapcParameters.Length > 4)
                throw new InvalidDataException($"Trigger parameters {parameters} has {parameters.mapcParameters.Length} parameters, the most it can have is 4.");

            for (int i = 0; i < 4; i++)
                w.String(i < parameters.mapcParameters.Length && !string.IsNullOrEmpty(parameters.mapcParameters[i])
                    ? parameters.mapcParameters[i]
                    : null);
        });

        bw.Write(mpapStrings.Count);
        w.Array(mpapStrings, w.String);
        bw.Write(mpapSpecialTextureNames.Count);
        w.Array(mpapSpecialTextureNames, w.String);
        bw.Write(mDEBUGData.mpapStrings.Count);
        w.Array(mDEBUGData.mpapStrings, w.String);

        w.Finish();

        muSizeInBytes = (uint)w.Position;
        bw.BaseStream.Position = sizePosition;
        bw.Write(muSizeInBytes);
        bw.Flush();

        importCount = imports.Count;
        return Dependency.AppendTable(ms.ToArray(), imports, entry.Console);
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.FlaptFile;
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        Editing = this; // Editors are modal, so only one is open
        return new PropertyGridEditor("Flapt File Editor", () => Serialize(entry, out _), () => Write(entry), ("Flapt File", this));
    }
}

// Picks a text field's font style from the file's font styles
public class FontStyleIndexConverter : ByteConverter
{
    private static List<FontStyle> FontStyles => FlaptFile.Editing?.mpaFontStyles ?? [];

    private static string Name(byte index) => $"{index}: {FontStyles[index]}";

    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => FontStyles.Count > 0;

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        => new(Enumerable.Range(0, Math.Min(FontStyles.Count, byte.MaxValue + 1)).Select(i => (object)(byte)i).ToArray());

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is byte index && index < FontStyles.Count)
            return Name(index);

        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        // "3: B5DotMat - ..." from the list, or a typed number
        if (value is string s && s.IndexOf(':') is int colon and > 0)
            value = s[..colon];

        return base.ConvertFrom(context, culture, value);
    }
}
