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

// BrnFlapt::FlaptFile, see https://burnout.wiki/wiki/Flapt_File
// Counts and pointers aren't stored, Write takes them from the list sizes and the layout it builds.

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

    public HashedString() { }

    public HashedString(uint hash, string debugString)
    {
        muHash = hash;
        this.debugString = debugString;
    }

    public static uint Hash(string value) => ~(uint)Crc32.HashCrc32B(value);

    public override string ToString() => mpacDEBUGString ?? $"0x{muHash:X8}";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class GuiTexture
{
    [Description("ID of the imported Texture resource."), TypeConverter(typeof(HexConverter))]
    public ulong ResourceId { get; set; }

    public override string ToString() => $"0x{ResourceId:X8}";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public class GuiVertex
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
    public ushort muNumFramesInTimeline => (ushort)(mpauFrameToKeyFrameMap.Count > 0 ? mpauFrameToKeyFrameMap.Count : mpaKeyFrameAnims.Count);

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

    [Category("Info"), ReadOnly(true)]
    public byte muVersion { get; set; } = 12;

    [Category("Info"), ReadOnly(true)]
    public uint muSizeInBytes { get; set; }

    [Category("Global Properties"), DisplayName("Seconds Per Frame"), Description("Seconds per frame, for instance 0.03333333 equates to ~30 frames per second.")]
    public float mfTimePerFrame { get; set; } = 1 / 30f;

    [DisplayName("Movie Clips")]
    public List<MovieClip> mpaMovieClips { get; set; } = [];

    [DisplayName("Textures"), Description("Imported textures. The special textures take the slots after these.")]
    public List<GuiTexture> mpapTextures { get; set; } = [];

    [DisplayName("Vertices")]
    public List<GuiVertex> mpaVerts { get; set; } = [];

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
        long ReadPtr()
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
        bool Valid(long ptr) => ptr != 0 && ptr < muSizeInBytes;

        List<T> ReadArray<T>(long ptr, int count, Func<T> read)
        {
            List<T> list = new(count);
            if (count == 0 || !Valid(ptr))
                return list;

            long pos = br.BaseStream.Position;
            br.BaseStream.Position = ptr;
            for (int i = 0; i < count; i++)
                list.Add(read());
            br.BaseStream.Position = pos;
            return list;
        }

        string ReadString()
        {
            long ptr = ReadPtr();
            if (!Valid(ptr))
                return null;

            long pos = br.BaseStream.Position;
            br.BaseStream.Position = ptr;
            string result = br.ReadCStr();
            br.BaseStream.Position = pos;
            return result;
        }

        Color ReadRGBA8()
        {
            byte r = br.ReadByte(), g = br.ReadByte(), b = br.ReadByte(), a = br.ReadByte();
            return Color.FromArgb(a, r, g, b);
        }

        Vector2 ReadVector2() => new(br.ReadSingle(), br.ReadSingle());
        Vector4 ReadVector4() => new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        HashedString ReadHashedString() => new(br.ReadUInt32(), ReadString());

        KeyFrameAnims ReadKeyFrameAnims()
        {
            KeyFrameAnims anims = new() { maxPlacedChildren = [br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32()] };
            anims.muCommandOffset = br.ReadByte();
            anims.muCommandCount = br.ReadByte();
            byte numTransforms = br.ReadByte();
            byte numColourTransforms = br.ReadByte();
            anims.mpauTransformObjects = ReadArray(ReadPtr(), numTransforms, br.ReadByte);
            anims.mpaTransforms = ReadArray(ReadPtr(), numTransforms, () => new Transform { mRightUp = ReadVector4(), mOriginXYZ = ReadVector4() });
            anims.mpauColourTransformObjects = ReadArray(ReadPtr(), numColourTransforms, br.ReadByte);
            anims.mpaColourTransforms = ReadArray(ReadPtr(), numColourTransforms, () => new ColourTransform { mScale = ReadVector4(), mTranslate = ReadVector4() });
            return anims;
        }

        TextField ReadTextField()
        {
            TextField textField = new() { mName = ReadHashedString() };
            textField.muInitialStringId = br.ReadUInt16();
            textField.muFontStyleIndex = br.ReadByte();
            textField.mxFlags = br.ReadByte();
            textField.muAlignment = br.ReadByte();
            br.ReadBytes(3); // Padding
            textField.mTopLeft = ReadVector2();
            textField.mBottomRight = ReadVector2();
            return textField;
        }

        MovieClip ReadMovieClip()
        {
            MovieClip clip = new() { mxFlags = br.ReadByte() };
            byte numChildren = br.ReadByte();
            byte numMeshes = br.ReadByte();
            byte numTextFields = br.ReadByte();
            byte numRenderLayers = br.ReadByte();
            byte numLabelledFrames = br.ReadByte();
            byte numFScriptCommands = br.ReadByte();
            br.ReadByte(); // Padding
            ushort numFramesInTimeline = br.ReadUInt16();
            ushort numKeyFrames = br.ReadUInt16();
            ReadPtr(); // mpFile, set at load

            clip.mpauFrameToKeyFrameMap = ReadArray(ReadPtr(), numFramesInTimeline, br.ReadUInt16);
            clip.mpaRenderLayers = ReadArray(ReadPtr(), numRenderLayers, () => new RenderLayer
            {
                mxFlags = br.ReadByte(),
                muMovieClipOffset = br.ReadByte(),
                muMovieClipCount = br.ReadByte(),
                muMeshOffset = br.ReadByte(),
                muMeshCount = br.ReadByte(),
                muTextFieldOffset = br.ReadByte(),
                muTextFieldCount = br.ReadByte()
            });
            clip.mpaKeyFrames = ReadArray(ReadPtr(), numKeyFrames * numRenderLayers, () => new RenderLayerKeyFrame
            {
                mxEnabledMovieClips = br.ReadUInt32(),
                mxEnabledMeshes = br.ReadUInt32(),
                mxEnabledTextFields = br.ReadUInt32()
            });
            clip.mpaKeyFrameAnims = ReadArray(ReadPtr(), numKeyFrames, ReadKeyFrameAnims);
            clip.mpaFScriptStream = ReadArray(ReadPtr(), numFScriptCommands, () => new FScriptCommand
            {
                muCommand = br.ReadByte(),
                muParam8 = br.ReadByte(),
                muParam16 = br.ReadUInt16()
            });
            clip.mpauChildMovieClips = ReadArray(ReadPtr(), numChildren, br.ReadUInt16);
            clip.mpaChildNames = ReadArray(ReadPtr(), numChildren, ReadHashedString);
            clip.mpaMeshes = ReadArray(ReadPtr(), numMeshes, () => new Mesh
            {
                miTextureId = br.ReadSByte(),
                muNumVerts = br.ReadByte(),
                muVertOffset = br.ReadUInt16()
            });
            clip.mpaTextFields = ReadArray(ReadPtr(), numTextFields, ReadTextField);
            clip.mpaTextFieldNames = ReadArray(ReadPtr(), numTextFields, ReadHashedString);
            clip.mpaFrameLabels = ReadArray(ReadPtr(), numLabelledFrames, ReadHashedString);
            clip.mpauLabelledFrameIds = ReadArray(ReadPtr(), numLabelledFrames, br.ReadUInt16);
            clip.mpcComponentName = ReadString();
            return clip;
        }

        mfTimePerFrame = br.ReadSingle();
        int numMovieClips = br.ReadInt32();
        mpaMovieClips = ReadArray(ReadPtr(), numMovieClips, ReadMovieClip);

        int numTextures = br.ReadInt32();
        long texturesPtr = ReadPtr();
        int numVerts = br.ReadInt32();
        mpaVerts = ReadArray(ReadPtr(), numVerts, () => new GuiVertex
        {
            mv2Pos = ReadVector2(),
            mv4Colour = ReadRGBA8(),
            mv2Tex0UV = ReadVector2()
        });
        int numFontStyles = br.ReadInt32();
        mpaFontStyles = ReadArray(ReadPtr(), numFontStyles, () => new FontStyle
        {
            mpacFontName = ReadString(),
            muColour = Color.FromArgb(br.ReadInt32()),
            mfFontHeight = br.ReadSingle()
        });
        int numComponents = br.ReadInt32();
        mpaComponentNames = ReadArray(ReadPtr(), numComponents, ReadHashedString);
        mpaComponentPaths = ReadArray(ReadPtr(), numComponents, () => new IndexPath { muDepth = br.ReadByte(), mauIndices = br.ReadBytes(32) });
        int numTriggerParameters = br.ReadInt32();
        mpaTriggerParameters = ReadArray(ReadPtr(), numTriggerParameters, () => new TriggerParameters { mapcParameters = [ReadString(), ReadString(), ReadString(), ReadString()] });
        int numStrings = br.ReadInt32();
        mpapStrings = ReadArray(ReadPtr(), numStrings, ReadString);
        int numSpecialTextures = br.ReadInt32();
        mpapSpecialTextureNames = ReadArray(ReadPtr(), numSpecialTextures, ReadString);
        int numDebugStrings = br.ReadInt32();
        mDEBUGData = new() { mpapStrings = ReadArray(ReadPtr(), numDebugStrings, ReadString) };

        // The bundle's imports fill the texture slots, the special textures come last and are set at runtime
        int ptrSize = x64 ? 8 : 4;
        mpapTextures = [.. Enumerable.Range(0, Math.Max(numTextures - numSpecialTextures, 0)).Select(_ => new GuiTexture())];
        foreach (BundleDependency dependency in entry.GetDependencies())
        {
            long slot = (dependency.EntryPointerOffset - texturesPtr) / ptrSize;
            if (dependency.EntryPointerOffset >= texturesPtr && slot < mpapTextures.Count)
                mpapTextures[(int)slot].ResourceId = dependency.EntryID;
        }

        return true;
    }

    // Lays out pointed-to arrays after the structures that point to them, then a string pool
    private sealed class LayoutWriter(BinaryWriter2 bw, bool x64)
    {
        private readonly Queue<Action> arrays = new();
        private readonly Queue<Action> alignedArrays = new(); // Transforms, kept together so they need no padding between them
        private readonly List<(long Position, string Value)> strings = [];

        public int PtrSize => x64 ? 8 : 4;
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
                strings.Add((pos, value));
        }

        public void Array<T>(IList<T> items, Action<T> write, int alignment = 0)
        {
            long pos = Ptr(0);
            if (items.Count == 0)
                return;

            (alignment > PtrSize ? alignedArrays : arrays).Enqueue(() =>
            {
                Align(alignment == 0 ? PtrSize : alignment); // 0 for structures with pointers
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
        entry.DependenciesListOffset = importCount > 0 ? data.Length - importCount * Dependency.Size : 0;
        entry.Dirty = true;

        return true;
    }

    // The primary data Write saves, ending with the import table. The same file always gives the same bytes.
    public byte[] Serialize(BundleEntry entry, out int importCount)
    {
        using MemoryStream ms = new();
        using BinaryWriter2 bw = new(ms);
        bw.BigEndian = entry.Console;

        LayoutWriter w = new(bw, entry.Platform == BundlePlatform.PCx64);
        List<Dependency> imports = [];

        void WriteVector2(Vector2 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
        }

        void WriteVector4(Vector4 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
            bw.Write(v.Z);
            bw.Write(v.W);
        }

        void WriteHashedString(HashedString hashedString)
        {
            bw.Write(hashedString.muHash);
            w.String(hashedString.mpacDEBUGString);
        }

        void WriteKeyFrameAnims(KeyFrameAnims anims, string clip)
        {
            if (anims.maxPlacedChildren.Length > 3)
                throw new InvalidDataException($"A keyframe of movie clip {clip} has {anims.maxPlacedChildren.Length} placed children masks, it needs 3.");
            for (int i = 0; i < 3; i++)
                bw.Write(i < anims.maxPlacedChildren.Length ? anims.maxPlacedChildren[i] : 0u);
            bw.Write(anims.muCommandOffset);
            bw.Write(anims.muCommandCount);
            bw.Write(Count8(anims.mpaTransforms.Count, $"A keyframe's transforms in movie clip {clip}"));
            bw.Write(Count8(anims.mpaColourTransforms.Count, $"A keyframe's colour transforms in movie clip {clip}"));
            CheckCount(anims.mpauTransformObjects.Count, anims.mpaTransforms.Count, $"A keyframe's transform objects in movie clip {clip}");
            CheckCount(anims.mpauColourTransformObjects.Count, anims.mpaColourTransforms.Count, $"A keyframe's colour transform objects in movie clip {clip}");
            w.Array(anims.mpauTransformObjects, bw.Write, 1);
            w.Array(anims.mpaTransforms, t =>
            {
                WriteVector4(t.mRightUp);
                WriteVector4(t.mOriginXYZ);
            }, 16);
            w.Array(anims.mpauColourTransformObjects, bw.Write, 1);
            w.Array(anims.mpaColourTransforms, t =>
            {
                WriteVector4(t.mScale);
                WriteVector4(t.mTranslate);
            }, 16);
        }

        void WriteMovieClip(MovieClip clip)
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
            w.Array(clip.mpaKeyFrameAnims, anims => WriteKeyFrameAnims(anims, name));
            w.Array(clip.mpaFScriptStream, command =>
            {
                bw.Write(command.muCommand);
                bw.Write(command.muParam8);
                bw.Write(command.muParam16);
            }, 2);
            w.Array(clip.mpauChildMovieClips, bw.Write, 2);
            w.Array(clip.mpaChildNames, WriteHashedString);
            w.Array(clip.mpaMeshes, mesh =>
            {
                bw.Write(mesh.miTextureId);
                bw.Write(mesh.muNumVerts);
                bw.Write(mesh.muVertOffset);
            }, 2);
            w.Array(clip.mpaTextFields, textField =>
            {
                WriteHashedString(textField.mName);
                bw.Write(textField.muInitialStringId);
                bw.Write(textField.muFontStyleIndex);
                bw.Write(textField.mxFlags);
                bw.Write(textField.muAlignment);
                bw.WriteUniquePadding(3);
                WriteVector2(textField.mTopLeft);
                WriteVector2(textField.mBottomRight);
            });
            w.Array(clip.mpaTextFieldNames, WriteHashedString);
            w.Array(clip.mpaFrameLabels, WriteHashedString);
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
        w.Array(mpaMovieClips, WriteMovieClip);

        // The bundle's imports point the texture slots at the textures
        List<ulong> textureSlots = [.. mpapTextures.Select(t => t.ResourceId), .. mpapSpecialTextureNames.Select(_ => 0ul)];
        bw.Write(textureSlots.Count);
        w.Array(textureSlots, id =>
        {
            long pos = w.Ptr(0);
            if (id != 0)
                imports.Add(new Dependency { ID = id, EntryPointerOffset = (uint)pos });
        });

        bw.Write(mpaVerts.Count);
        w.Array(mpaVerts, vertex =>
        {
            WriteVector2(vertex.mv2Pos);
            bw.Write(vertex.mv4Colour.R);
            bw.Write(vertex.mv4Colour.G);
            bw.Write(vertex.mv4Colour.B);
            bw.Write(vertex.mv4Colour.A);
            WriteVector2(vertex.mv2Tex0UV);
        }, 4);

        bw.Write(mpaFontStyles.Count);
        w.Array(mpaFontStyles, fontStyle =>
        {
            w.String(fontStyle.mpacFontName);
            bw.Write(fontStyle.muColour.ToArgb());
            bw.Write(fontStyle.mfFontHeight);
        });

        bw.Write(mpaComponentNames.Count);
        w.Array(mpaComponentNames, WriteHashedString);
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
                w.String(i < parameters.mapcParameters.Length && !string.IsNullOrEmpty(parameters.mapcParameters[i]) ? parameters.mapcParameters[i] : null);
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
        return new FlaptFileEditor(this, entry);
    }
}

// Shows a hex string for uint and ulong values, accepts hex with or without 0x, or decimal
public class HexConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is uint or ulong)
            return $"0x{value:X8}";
        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string s)
            return base.ConvertFrom(context, culture, value);

        s = s.Trim();
        bool hex = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        ulong result = hex ? ulong.Parse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture) : ulong.Parse(s, CultureInfo.InvariantCulture);
        return context?.PropertyDescriptor?.PropertyType == typeof(uint) ? (object)checked((uint)result) : result;
    }
}

// Edits a list or array of numbers as "1, 2, 3", the collection editor can't change number items
public class NumberListConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is System.Collections.IEnumerable items)
            return string.Join(", ", items.Cast<object>());
        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string s || context?.PropertyDescriptor == null)
            return base.ConvertFrom(context, culture, value);

        Type type = context.PropertyDescriptor.PropertyType;
        Type elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
        string[] parts = s.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
        Array result = Array.CreateInstance(elementType, parts.Length);
        for (int i = 0; i < parts.Length; i++)
            result.SetValue(Convert.ChangeType(parts[i], elementType, CultureInfo.InvariantCulture), i);
        return type.IsArray ? result : Activator.CreateInstance(type, result);
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
