using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

// Manages LION particle effect definitions
public static class ParticleEffects
{
    private const uint LionDefinitionMagic = 0x10003;

    public static Dictionary<ulong, string> Names { get; private set; } = [];

    // Reads effect name from cLionEffectDefinition inside BrnParticle::ParticleDescription
    public static string Name(BundleEntry entry)
    {
        byte[] data = entry.GetPrimaryWithoutImports();

        if (data.Length < 0x58 || (entry.Console
            ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16))
            : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(16))) != LionDefinitionMagic)
            return null;

        string name = (entry.Console
            ? Encoding.BigEndianUnicode
            : Encoding.Unicode).GetString(data, 0x18, 64);
        return name.Split('\0')[0];
    }

    public static void Load(BundleArchive archive)
    {
        Names = [];

        foreach (BundleEntry entry in archive?.Entries.Where(e => e.Type == EntryType.ParticleDescription) ?? [])
            Names[entry.ID] = Name(entry) ?? "?";
    }
}

// TypeConverter displaying effect resource ID with resolved name
public class EffectConverter : HexConverter
{
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
        destinationType == typeof(string) && value is uint or ulong && ParticleEffects.Names.TryGetValue(Convert.ToUInt64(value), out string name)
            ? $"0x{value:X8} {name}"
            : base.ConvertTo(context, culture, value, destinationType);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
        base.ConvertFrom(context, culture, value is string text
            ? text.Trim().Split(' ')[0]
            : value);

    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) =>
        new(ParticleEffects.Names.OrderBy(e => e.Value, StringComparer.OrdinalIgnoreCase).Select(e => context?.PropertyDescriptor?.PropertyType == typeof(uint)
            ? (object)(uint)e.Key
            : e.Key).ToArray());
}

// Object tree editor for particle resources (without 3D viewport)
public sealed class ParticleEditor : ResourceEditor
{
    private readonly object data;
    private readonly Func<object, IEnumerable<TrafficRules.Issue>> problems;
    private readonly Action<object> write;

    public ParticleEditor(object source, BundleEntry entry, string title, Func<object, IEnumerable<TrafficRules.Issue>> findProblems, Action<object> writeTo)
        : base(entry, title, title.ToLowerInvariant())
    {
        data = ObjectTreeEditor.Clone(source);
        problems = findProblems;
        write = writeTo;
        TrafficLabels.Install();
        ParticleEffects.Load(entry.Archive);
        VFXPropCollection previous = TrafficReferences.VFXData;
        TrafficReferences.VFXData = data as VFXPropCollection;
        Disposed += (_, _) => TrafficReferences.VFXData = previous;
        Initialize(data, source.GetType().Name, null);
        Tree.CollectionLimit = (_, _) => int.MaxValue;
    }

    protected override List<TrafficRules.Issue> FindIssues() => [.. problems(data)];

    protected override void WriteResource() => write(data);
}

// BrnParticle::VFXPropCollection
public class VFXPropCollection : IEntryData
{
    private const uint CurrentVersion = 3;
    private const int HeaderSize = 0x40, DebugNameLength = 60;
    private static readonly int[] strides = [16, 16, 12, 80, 80, 32];

    public enum eVFXMaterialType : uint
    {
        VFXMaterialTypeDirt = 0,
        VFXMaterialTypeFoliage = 1,
        VFXMaterialTypeMetal = 2,
        VFXMaterialTypePlastic = 3,
        VFXMaterialTypeStuntYellow = 4,
        VFXMaterialTypeWood = 5,
        VFXMaterialTypeWater = 6,
        VFXMaterialTypeStone = 7,
        VFXMaterialTypePaper = 8,
        VFXMaterialTypeBillboard = 9,
        VFXMaterialTypeRedBarrelExplo = 10,
        VFXMaterialTypeBindust = 11,
        VFXMaterialTypeWaterBarrel = 12,
        VFXMaterialTypeDebug = 13,
        VFXMaterialTypeNone = 14
    }

    public enum eVFXCoronaType : uint
    {
        VFXCorona_Texture_0 = 0,
        VFXCorona_Texture_1 = 1,
        VFXCorona_Texture_2 = 2,
        VFXCorona_Texture_3 = 3,
        VFXCorona_Texture_4 = 4,
        VFXCorona_Texture_5 = 5,
        VFXCorona_Texture_6 = 6,
        VFXCorona_Texture_7 = 7,
        VFXCorona_Texture_8 = 8,
        VFXCorona_Texture_9 = 9,
        VFXCorona_Texture_10 = 10,
        VFXCorona_Texture_11 = 11,
        VFXCorona_Texture_12 = 12,
        VFXCorona_Texture_13 = 13,
        VFXCorona_Texture_14 = 14,
        VFXCorona_Texture_15 = 15,
        VFXCorona_Texture_16 = 16
    }

    public class VFXLocator
    {
        [Category("Geometry"), Description("Where the effect starts, relative to the prop.")]
        public Vector3 mPosition { get; set; }

        // Fourth position vector component (padding)
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public uint PositionPadding { get; set; }

        [Category("Properties"), Description("The LION effect, by the resource ID of its ParticleDescription in this bundle."), TypeConverter(typeof(EffectConverter))]
        public uint mHashedName { get; set; }

        [Category("Advanced"), Description("The effect's GameDB path, only for debugging. A longer one keeps its last 59 characters, as the game's tools did.")]
        public string macDebugLefName { get; set; } = "";

        public override string ToString() => ParticleEffects.Names.TryGetValue(mHashedName, out string name)
            ? name
            : $"0x{mHashedName:X8}";
    }

    public class VFXMaterial
    {
        [Category("Properties")]
        public eVFXMaterialType mType { get; set; } = eVFXMaterialType.VFXMaterialTypeNone;

        [Browsable(false)] public List<VFXLocator> mpLocators { get; set; } = [];

        public override string ToString() => $"{mType.ToString().Replace("VFXMaterialType", "")}, {mpLocators.Count} effects";
    }

    public class VFXCoronaTypeData
    {
        [Category("Properties"), Description("Looked up by the game with GetCoronaTypeDataByID.")]
        public int mnID { get; set; }

        [Category("Properties"), Description("The glow's texture.")]
        public eVFXCoronaType mType { get; set; }

        [Category("Timing"), Description("Seconds.")]
        public float mrTimeOn { get; set; }

        [Category("Timing"), Description("Seconds.")]
        public float mrTimeOff { get; set; }

        [Category("Size")]
        public float mrSizeMin { get; set; }

        [Category("Size")]
        public float mrSizeMax { get; set; }

        [Category("Timing")]
        public float mrMasterTime { get; set; }

        [Category("Timing"), Description("Whether every glow of this type blinks together.")]
        public bool mbSynchronised { get; set; }

        public override string ToString() => $"ID {mnID}, texture {(uint)mType}";
    }

    public class VFXCoronaType
    {
        [Category("Geometry"), Description("Matrix44Affine, by row, relative to the prop."), TypeConverter(typeof(ArrayTextConverter))]
        public Vector4[] mTransform { get; set; } = [new(1, 0, 0, 0), new(0, 1, 0, 0), new(0, 0, 1, 0), new(0, 0, 0, 1)];

        [Category("Properties"), Description("How the glow looks and blinks, shared between glows."), TypeConverter(typeof(TrafficReferenceConverter))]
        public VFXCoronaTypeData mpTypeData { get; set; }

        [Category("Timing"), Description("Seconds.")]
        public float mrTimeOffset { get; set; }

        public override string ToString() => mpTypeData == null
            ? "Light glow"
            : $"Light glow, ID {mpTypeData.mnID}";
    }

    public class VFXPropState
    {
        [Browsable(false)] public List<VFXMaterial> mpVFXMaterial { get; set; } = [];

        [Browsable(false)] public List<VFXCoronaType> mpCoronaType { get; set; } = [];

        public override string ToString() => $"{mpVFXMaterial.Sum(m => m.mpLocators.Count)} effects, {mpCoronaType.Count} light glows";
    }

    public class VFXProp
    {
        [Category("Properties"), Description("The prop's resource ID."), TypeConverter(typeof(HexConverter))]
        public ulong mPropID { get; set; }

        [Browsable(false)] public List<VFXPropState> mpPropStates { get; set; } = [];

        public override string ToString() => $"{ResourceDB.Lookup(mPropID) ?? $"0x{mPropID:X8}"}, {mpPropStates.Count} states";
    }

    [Browsable(false)] public List<VFXProp> mpPropTable { get; set; } = [];

    [Browsable(false)] public List<VFXCoronaTypeData> mpCoronaTypeDataTable { get; set; } = [];

    public IEntryEditor GetEditor(BundleEntry entry) => new ParticleEditor(this, entry, "VFX props", data => ((VFXPropCollection)data).Problems(), data => ((VFXPropCollection)data).Write(entry));

    public EntryType GetEntryType(BundleEntry entry) => EntryType.VFXPropCollection;

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        byte[] bytes = entry.GetPrimaryWithoutImports();
        using BinaryReader2 br = new(new MemoryStream(bytes), entry.Console);
        uint[] header = [.. Enumerable.Range(0, 13).Select(_ => br.ReadUInt32())];

        if (header[12] != CurrentVersion)
            throw new ReadFailedError($"Only version {CurrentVersion} VFX prop collections can be read, this one is version {header[12]}.");

        T[] table<T>(int index, Func<T> read)
        {
            T[] items = new T[header[index * 2 + 1]];

            for (int i = 0; i < items.Length; i++)
            {
                br.BaseStream.Position = header[index * 2] + i * strides[index];
                items[i] = read();
            }

            return items;
        }

        // Resolves table slice by offset and count; -1 indicates empty
        List<T> run<T>(T[] items, int first, uint count) => count == 0
            ? []
            : first >= 0 && first + count <= items.Length
                ? [.. items.AsSpan(first, (int)count)]
                : throw new ReadFailedError($"A VFX prop collection list runs past the {items.Length} {typeof(T).Name} records.");

        Vector4 v4() => new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        VFXCoronaTypeData[] typeData = table(5, () => new VFXCoronaTypeData
        {
            mnID = br.ReadInt32(),
            mType = (eVFXCoronaType)br.ReadUInt32(),
            mrTimeOn = br.ReadSingle(),
            mrTimeOff = br.ReadSingle(),
            mrSizeMin = br.ReadSingle(),
            mrSizeMax = br.ReadSingle(),
            mrMasterTime = br.ReadSingle(),
            mbSynchronised = br.ReadUInt32() != 0
        });

        VFXLocator[] locators = table(3, () => new VFXLocator
        {
            mPosition = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
            PositionPadding = br.ReadUInt32(),
            mHashedName = br.ReadUInt32(),
            macDebugLefName = Encoding.ASCII.GetString(br.ReadBytes(DebugNameLength)).Split('\0')[0]
        });

        VFXCoronaType[] coronas = table(4, () =>
        {
            VFXCoronaType corona = new()
            {
                mTransform = [v4(), v4(), v4(), v4()]
            };

            int data = br.ReadInt32();
            corona.mpTypeData = data >= 0 && data < typeData.Length
                ? typeData[data]
                : null;
            corona.mrTimeOffset = br.ReadSingle();
            return corona;
        });

        VFXMaterial[] materials = table(2, () =>
        {
            VFXMaterial material = new()
            {
                mType = (eVFXMaterialType)br.ReadUInt32()
            };

            uint count = br.ReadUInt32();
            material.mpLocators = run(locators, br.ReadInt32(), count);
            return material;
        });

        VFXPropState[] states = table(1, () =>
        {
            int firstMaterial = br.ReadInt32();
            uint materialCount = br.ReadUInt32();
            int firstCorona = br.ReadInt32();
            return new VFXPropState
            {
                mpVFXMaterial = run(materials, firstMaterial, materialCount),
                mpCoronaType = run(coronas, firstCorona, br.ReadUInt32())
            };
        });

        mpPropTable = [.. table(0, () =>
        {
            VFXProp prop = new()
            {
                mPropID = br.ReadUInt64()
            };

            int first = br.ReadInt32();
            prop.mpPropStates = run(states, first, br.ReadUInt32());
            return prop;
        })];

        mpCoronaTypeDataTable = [.. typeData];
        return true;
    }

    // Validates constraints expected by the game engine
    public IEnumerable<TrafficRules.Issue> Problems()
    {
        foreach (VFXProp prop in mpPropTable)
        {
            string name = $"Prop {mpPropTable.IndexOf(prop)}";

            foreach (VFXMaterial material in prop.mpPropStates.SelectMany(s => s.mpVFXMaterial))
                if (!Enum.IsDefined(material.mType))
                    yield return new(material, nameof(VFXMaterial.mType), $"{name} has a material of a type the game doesn't have.");

            foreach (VFXCoronaType corona in prop.mpPropStates.SelectMany(s => s.mpCoronaType))
            {
                if (corona.mpTypeData == null || !mpCoronaTypeDataTable.Contains(corona.mpTypeData))
                    yield return new(corona, nameof(VFXCoronaType.mpTypeData), $"A light glow of {name.ToLowerInvariant()} needs a light glow type.");

                if (corona.mTransform?.Length != 4)
                    yield return new(corona, nameof(VFXCoronaType.mTransform), $"A light glow of {name.ToLowerInvariant()} needs a transform of 4 rows.");
            }
        }

        foreach (VFXCoronaTypeData data in mpCoronaTypeDataTable)
            if (!Enum.IsDefined(data.mType))
                yield return new(data, nameof(VFXCoronaTypeData.mType), $"Light glow type {mpCoronaTypeDataTable.IndexOf(data)} has a texture the game doesn't have.");
    }

    public byte[] Serialize(bool bigEndian)
    {
        if (Problems().FirstOrDefault() is TrafficRules.Issue problem)
            throw new InvalidDataException(problem.Message);

        List<VFXPropState> states = [.. mpPropTable.SelectMany(p => p.mpPropStates)];
        List<VFXMaterial> materials = [.. states.SelectMany(s => s.mpVFXMaterial)];
        List<VFXLocator> locators = [.. materials.SelectMany(m => m.mpLocators)];
        List<VFXCoronaType> coronas = [.. states.SelectMany(s => s.mpCoronaType)];
        int[] counts = [mpPropTable.Count, states.Count, materials.Count, locators.Count, coronas.Count, mpCoronaTypeDataTable.Count];
        uint[] bases = new uint[6];
        uint at = HeaderSize;

        for (int i = 0; i < 6; i++)
        {
            bases[i] = at;
            at = (uint)(at + counts[i] * strides[i] + 15) & ~15u;
        }

        using MemoryStream ms = new(new byte[at]);
        BinaryWriter2 bw = new(ms, bigEndian);

        for (int i = 0; i < 6; i++)
        {
            bw.Write(bases[i]);
            bw.Write(counts[i]);
        }

        bw.Write(CurrentVersion);

        int[] next = new int[6];

        void run(int table, int count)
        {
            bw.Write(count == 0
                ? -1
                : next[table]);
            next[table] += count;
        }

        void start(int table, int index) => ms.Position = bases[table] + index * strides[table];

        for (int i = 0; i < mpPropTable.Count; i++)
        {
            start(0, i);
            bw.Write(mpPropTable[i].mPropID);
            run(1, mpPropTable[i].mpPropStates.Count);
            bw.Write(mpPropTable[i].mpPropStates.Count);
        }

        for (int i = 0; i < states.Count; i++)
        {
            start(1, i);
            run(2, states[i].mpVFXMaterial.Count);
            bw.Write(states[i].mpVFXMaterial.Count);
            run(4, states[i].mpCoronaType.Count);
            bw.Write(states[i].mpCoronaType.Count);
        }

        for (int i = 0; i < materials.Count; i++)
        {
            start(2, i);
            bw.Write((uint)materials[i].mType);
            bw.Write(materials[i].mpLocators.Count);
            run(3, materials[i].mpLocators.Count);
        }

        for (int i = 0; i < locators.Count; i++)
        {
            start(3, i);
            bw.Write(locators[i].mPosition.X);
            bw.Write(locators[i].mPosition.Y);
            bw.Write(locators[i].mPosition.Z);
            bw.Write(locators[i].PositionPadding);
            bw.Write(locators[i].mHashedName);
            string name = locators[i].macDebugLefName ?? "";
            bw.Write(Encoding.ASCII.GetBytes(name.Length < DebugNameLength
                ? name
                : name[^(DebugNameLength - 1)..]));
        }

        for (int i = 0; i < coronas.Count; i++)
        {
            start(4, i);

            foreach (Vector4 row in coronas[i].mTransform)
            {
                bw.Write(row.X);
                bw.Write(row.Y);
                bw.Write(row.Z);
                bw.Write(row.W);
            }

            bw.Write(mpCoronaTypeDataTable.IndexOf(coronas[i].mpTypeData));
            bw.Write(coronas[i].mrTimeOffset);
        }

        for (int i = 0; i < mpCoronaTypeDataTable.Count; i++)
        {
            VFXCoronaTypeData data = mpCoronaTypeDataTable[i];
            start(5, i);
            bw.Write(data.mnID);
            bw.Write((uint)data.mType);
            bw.Write(data.mrTimeOn);
            bw.Write(data.mrTimeOff);
            bw.Write(data.mrSizeMin);
            bw.Write(data.mrSizeMax);
            bw.Write(data.mrMasterTime);
            bw.Write(data.mbSynchronised
                ? 1u
                : 0u);
        }

        return ms.ToArray();
    }

    public bool Write(BundleEntry entry)
    {
        entry.SetPrimary(Serialize(entry.Console), entry.GetImports());
        return true;
    }
}

// BrnParticle::ParticleDescriptionCollection
public class ParticleDescriptionCollection : IEntryData
{
    private const int TableOffset = 8;

    public class Effect
    {
        [Category("Properties"), Description("The ParticleDescription, whose ID is the hash the game finds the effect by."), TypeConverter(typeof(EffectConverter))]
        public ulong mResourceId { get; set; }

        public override string ToString() => ParticleEffects.Names.TryGetValue(mResourceId, out string name)
            ? name
            : $"0x{mResourceId:X8}";
    }

    [Browsable(false)] public List<Effect> mpTable { get; set; } = [];

    public IEntryEditor GetEditor(BundleEntry entry) => new ParticleEditor(this, entry, "Effects", data => ((ParticleDescriptionCollection)data).Problems(), data => ((ParticleDescriptionCollection)data).Write(entry));

    public EntryType GetEntryType(BundleEntry entry) => EntryType.ParticleDescriptionCollection;

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        // BND1 builds store inline effects in older format (unsupported)
        if (entry.Archive?.BNDVersion == 1)
            throw new ReadFailedError("The effect collections of BND1 bundles hold the effects themselves, in an older LION format that can't be read yet.");

        using BinaryReader2 br = new(new MemoryStream(entry.GetPrimaryWithoutImports()), entry.Console);

        if (br.ReadUInt32() != TableOffset)
            throw new ReadFailedError("This effect collection doesn't have its table where the game expects it.");

        uint count = br.ReadUInt32();
        Dictionary<uint, ulong> imports = [];

        foreach (Dependency import in entry.GetImports())
            imports[import.EntryPointerOffset] = import.ID;

        mpTable = [.. Enumerable.Range(0, (int)count).Select(i => new Effect
        {
            mResourceId = imports.GetValueOrDefault((uint)(TableOffset + i * 4))
        })];

        return true;
    }

    // Validates that referenced particle effects exist in the bundle
    public IEnumerable<TrafficRules.Issue> Problems()
    {
        for (int i = 0; i < mpTable.Count; i++)
            if (!ParticleEffects.Names.ContainsKey(mpTable[i].mResourceId))
                yield return new(mpTable[i], nameof(Effect.mResourceId), $"Effect {i}, 0x{mpTable[i].mResourceId:X8}, isn't a ParticleDescription in this bundle.");
    }

    // Serializes import table slots (numbered 1..N in little-endian)
    public (byte[] Data, List<Dependency> Imports) Serialize(bool bigEndian)
    {
        using MemoryStream ms = new(new byte[16 + ((mpTable.Count * 4 + 15) & ~15)]);
        BinaryWriter2 bw = new(ms, bigEndian);
        bw.Write(TableOffset);
        bw.Write(mpTable.Count);
        List<Dependency> imports = [];

        for (int i = 0; i < mpTable.Count; i++)
        {
            bw.Write(bigEndian
                ? BinaryPrimitives.ReverseEndianness((uint)i + 1)
                : (uint)i + 1);
            imports.Add(new()
            {
                ID = mpTable[i].mResourceId,
                EntryPointerOffset = (uint)(TableOffset + i * 4)
            });
        }

        return (ms.ToArray(), imports);
    }

    public bool Write(BundleEntry entry)
    {
        (byte[] data, List<Dependency> imports) = Serialize(entry.Console);
        entry.SetPrimary(data, imports);
        return true;
    }
}

// BrnParticle::TextureNameMap: maps texture FNV-1a hash to GameDB path CRC32
public class TextureNameMap : IEntryData
{
    private const int TableOffset = 16;

    public class Entry
    {
        [Category("Properties"), Description("The texture's GameDB path, such as gamedb://burnout5/Burnout/Effects/Textures/fxskid.TextureConfig2d?ID=226049.")]
        public string mpGDBTextureName { get; set; } = "";

        [Category("Properties"), Description("The part of the path effects refer to the texture by.")]
        public string LionName => mpGDBTextureName.Split('/')[^1].Split('.')[0];

        [Category("Advanced"), Description("Worked out from the name."), TypeConverter(typeof(HexConverter))]
        public uint muHash => Fnv1a(LionName.ToLowerInvariant());

        public override string ToString() => LionName;
    }

    [Browsable(false)] public List<Entry> mpEntries { get; set; } = [];

    public static uint Fnv1a(string text)
    {
        uint hash = 0x811C9DC5;

        foreach (byte b in Encoding.ASCII.GetBytes(text))
            hash = (hash ^ b) * 0x01000193;

        return hash;
    }

    public IEntryEditor GetEditor(BundleEntry entry) => new ParticleEditor(this, entry, "Effect textures", data => ((TextureNameMap)data).Problems(), data => ((TextureNameMap)data).Write(entry));

    public EntryType GetEntryType(BundleEntry entry) => EntryType.TextureNameMap;

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        byte[] bytes = entry.GetPrimaryWithoutImports();
        using BinaryReader2 br = new(new MemoryStream(bytes), entry.Console);
        br.ReadUInt32();
        uint count = br.ReadUInt32();
        br.BaseStream.Position = TableOffset;
        uint[] names = [.. Enumerable.Range(0, (int)count).Select(_ =>
        {
            br.ReadUInt32();
            return br.ReadUInt32();
        })];

        mpEntries = [.. names.Select(at =>
        {
            br.BaseStream.Position = at;
            return new Entry
            {
                mpGDBTextureName = br.ReadCStr()
            };
        })];

        return true;
    }

    public IEnumerable<TrafficRules.Issue> Problems()
    {
        foreach (IGrouping<uint, Entry> same in mpEntries.GroupBy(e => e.muHash).Where(g => g.Count() > 1))
            yield return new(same.Last(), nameof(Entry.mpGDBTextureName), $"{same.Count()} textures are called {same.Key:X8} ({same.First().LionName}), effects can only find one.");
    }

    public byte[] Serialize(bool bigEndian)
    {
        using MemoryStream ms = new();
        BinaryWriter2 bw = new(ms, bigEndian);
        bw.Write(TableOffset);
        bw.Write(mpEntries.Count);
        bw.Write(new byte[8]);
        long at = (TableOffset + mpEntries.Count * 8 + 15) & ~15;

        foreach (Entry entry in mpEntries)
        {
            bw.Write(entry.muHash);
            bw.Write((uint)at);
            at = (at + Encoding.ASCII.GetByteCount(entry.mpGDBTextureName) + 1 + 15) & ~15;
        }

        foreach (Entry entry in mpEntries)
        {
            ms.Position = (ms.Position + 15) & ~15;
            bw.Write(Encoding.ASCII.GetBytes(entry.mpGDBTextureName));
            bw.Write((byte)0);
        }

        ms.SetLength((ms.Length + 15) & ~15);
        return ms.ToArray();
    }

    public bool Write(BundleEntry entry)
    {
        entry.SetPrimary(Serialize(entry.Console), entry.GetImports());
        return true;
    }
}
