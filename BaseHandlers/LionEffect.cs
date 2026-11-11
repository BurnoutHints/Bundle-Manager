using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

// ParticleDescription handler: reads compiled LION effect data and exports/compiles .lef text definitions
public sealed class LionEffect : IEntryData
{
    // sLionMemberToken type codes: 1=byte, 5/6=int, 7=float, 9=flag bit, 11=color, 14=string, 15=vector
    public sealed record Token(string Name, int Type, int Offset, uint Bit = 0, string[] Names = null);

    private const int DefinitionOffset = 16, EffectLink = 0x48, DescriptorNext = 84, DescriptorBehaviours = 64, DescriptorBehaviourCount = 60,
        DescriptorBlended = 68, DescriptorMaterial = 76, DescriptorChild = 92, DescriptorName = 56, BehaviourNext = 732, MaterialFlags = 36;

    // Compiled layout: header, descriptors with behaviours/waveforms, blended behaviour, material, and strings (16-byte aligned)
    private const int FirstDescriptor = 0x80, DescriptorSize = 0x60, BehaviourSize = 0x4C0, MaterialSize = 0xB0, WaveformSize = 0x38;

    // Enum values from Jan 2008 X360 build
    private static readonly string[] shapes = ["SPRITE", "QUAD", "BOX", "TILT", "AXIS", "RIBBON_QUAD", "RIBBON_TILT", "RIBBON_AXIS", "MESH", "MESH_SPRITES"],
        collisions = ["SPHERE", "BOUNDING_BOX", "MESH"],
        varianceModes = ["NONE", "INDEPENDENT", "LINEAR"],
        alphaTests = ["NEVER", "ALWAYS", "LESS", "LEQUAL", "EQUAL", "GEQUAL", "GREATER", "NOTEQUAL"],
        blends = ["COPYRGB", "SRCALPHA", "SRCALPHA_KEEP", "COPYALPHA", "DESTALPHA", "DESTALPHA_KEEP", "SRCALPHA_ADD", "DESTALPHA_ADD", "SRCALPHA_SUB", "DESTALPHA_SUB",
            "SRCDEST_5050", "SRCALPHA_ADD_KEEP", "DESTALPHA_ADD_KEEP", "COPYRGB_KEEP", "SRCINVALPHA", "SRCINVALPHA_KEEP", "DESTINVALPHA", "DESTINVALPHA_KEEP",
            "SRCALPHA_LIGHTMAP", "SRCALPHA_LIGHTMAP_KEEP", "ADD_SRCALPHA", "ADD_SRCALPHA_KEEP", "ZWRITE_ONLY", "SRCMINUSDEST", "DESTMINUSSRC", "SRCADDDEST"],
        zTests = ["NEVER", "ALWAYS", "GEQUAL", "GREATER", "LEQUAL", "LESS"],
        texAnims = ["NONE", "RANDOM_PLAYBACK", "PLAYBACK_ONCE", "PLAY_OVER_LIFTIME"],
        shaders = ["LION", "LION_NORM", "LION_WARP"],
        normalOptions = ["NONE", "LOCATOR", "LOCATOR_VEL", "PARTICLE_VEL"],
        coords = ["NORMAL", "FLIPPED", "RANDOM"],
        waveTypes = ["null", "NOISE", "NOISE_SMOOTH", "SQUARE", "SAW", "SAW_INV", "SIN", "TRIANGLE"];

    private static Token Flag(string name, int offset, uint bit) => new(name, 9, offset, bit);

    private static Token Float(string name, int offset) => new(name, 7, offset);

    private static Token Vector(string name, int offset) => new(name, 15, offset);

    private static Token Number(string name, int offset, string[] names = null) => new(name, 5, offset, 0, names);

    // Member token tables (0x82F34F30, 0x82F35100, 0x82F357F8, 0x82F35BC8)
    public static readonly Token[] DescriptorTokens =
    [
        Flag("CELL_RENDER_FLAG", 32, 0x8), Flag("DO_IGNORE_ROT", 32, 0x100), Flag("DO_PREFORM", 32, 0x4000), Flag("DO_REPEAT", 32, 0x4),
        Flag("DYNAMIC_PLACEMENT_FLAG", 32, 0x10), Flag("ORIENT_TO_CAMERA_FLAG", 32, 0x40), Flag("DO_USE_MATRICES", 32, 0x20), Flag("DO_WORLD_ACC", 32, 0x80),
        Flag("DO_PHYSICS", 32, 0x200), Flag("DISABLED_FLAG", 32, 0x8000), Float("EMITTER_LIFE_BASE", 20), Number("EMITTER_LIFE_INFINITE", 28),
        Float("EMITTER_LIFE_VARIANCE", 24), Number("LODGROUP", 36), Float("PAUSE_TIME", 4), Float("PAUSE_TIME_VARIANCE", 8), Float("REPEAT_TIME", 12),
        Float("REPEAT_TIME_VARIANCE", 16), Number("RENDERGROUP", 40), new("NAME", 14, 56), Number("SHAPE", 44, shapes), Number("COLLISION_TYPE", 48, collisions)
    ];

    public static readonly Token[] BehaviourTokens =
    [
        Flag("DO_BURST", 708, 0x200), Flag("DO_DRAG", 708, 0x100), Flag("DO_INHERITVEL", 708, 0x800), Flag("DO_ROTATE", 708, 0x1), Flag("DO_REVERSE", 708, 0x8),
        Flag("DO_RADIAL", 708, 0x10), Flag("DO_OFFSETROT", 708, 0x20), Flag("DO_ROTXYZ", 708, 0x40), Flag("DO_SIZEXYZ", 708, 0x80), Flag("DO_CLONE", 708, 0x1000),
        Flag("DO_WAVEALPHA", 708, 0x2000), Flag("DO_WAVERGB", 708, 0x4000), Flag("DO_WAVEX", 708, 0x8000), Flag("DO_WAVEY", 708, 0x10000), Flag("DO_WAVEZ", 708, 0x20000),
        Flag("DO_COLOURSTEP0", 708, 0x40000), Flag("DO_COLOURSTEP1", 708, 0x80000), Flag("DO_COLOURSTEP2", 708, 0x100000), Flag("DO_COLOURSTEP3", 708, 0x200000),
        Flag("DO_ENDON_SPRITE", 708, 0x400000), Flag("DO_ENDON_ACTIVE", 708, 0x800000), Flag("DO_PROPORTIONAL", 708, 0x1000000), Flag("DO_EMITTER_WEIGHTING", 708, 0x2000000),
        Vector("ACC_BASE", 0), Vector("ACC_VARIANCE", 16), Vector("AXIS_BASE", 32), Float("END_ON_ALPHA_FADE", 1128), Float("END_ON_SCALE", 1132),
        Float("END_ON_START_ANGLE", 1136), Float("END_ON_END_ANGLE", 1140), Vector("OFFSETROTXYZ_BASE", 48), Vector("OFFSETROTXYZ_VARIANCE", 64),
        Vector("OFFSETROTXYZ_VEL_BASE", 80), Vector("OFFSETROTXYZ_VEL_VARIANCE", 96), Vector("OFFSETROTXYZ_ACC_BASE", 112), Vector("OFFSETROTXYZ_ACC_VARIANCE", 128),
        Vector("POS_BASE", 256), Vector("POS_VARIANCE", 272), Vector("PIVOT_POINT", 240), Vector("RING_RADIUS", 288), Vector("ROTXYZ_BASE", 144),
        Vector("ROTXYZ_VARIANCE", 160), Vector("ROTXYZ_VEL_BASE", 176), Vector("ROTXYZ_VEL_VARIANCE", 192), Vector("ROTXYZ_ACC_BASE", 208),
        Vector("ROTXYZ_ACC_VARIANCE", 224), Vector("SIZEXYZ_BASE", 304), Vector("SIZEXYZ_VARIANCE", 320), Vector("SIZEXYZ_VEL_BASE", 336),
        Vector("SIZEXYZ_VEL_VARIANCE", 352), Vector("SIZEXYZ_ACC_BASE", 368), Vector("SIZEXYZ_ACC_VARIANCE", 384), Vector("VEL_BASE", 400), Vector("VEL_VARIANCE", 416),
        new("COLOUR0", 11, 544), new("COLOUR1", 11, 548), new("COLOUR2", 11, 552), new("COLOUR3", 11, 556), Float("COLOUR_TIME0", 560), Float("COLOUR_TIME1", 564),
        Float("COLOUR_TIME2", 568), Float("COLOUR_TIME3", 572), Number("RGBA_VARIANCE_MODE", 612, varianceModes), new("RGBA0", 11, 528), new("RGBA1", 11, 532),
        Float("ALPHA_FADEIN", 632), Float("ALPHA_FADEOUT", 636), Float("CELL_SIZE", 640), Number("RIBBON_PARTICLE_COUNT", 1152), Float("CLONE_SCALEIN_TIME", 644),
        Float("DRAG_FACTOR_VEL", 1156), Float("DRAG_FACTOR_ROT", 1160), Float("DRAG_FACTOR_SCALE", 1164), Float("DRAG_FACTOR", 648), Float("MASS", 652),
        Float("EMISSION_RATE_BASE", 680), Float("EMISSION_RATE_VARIANCE", 684), Float("EMITTER_START_WEIGHT", 1168), Float("EMITTER_END_WEIGHT", 1172),
        Float("EMITTER_VEL_WEIGHT", 1176), Number("EMISSION_COUNT_CLAMP", 704), Number("EMISSION_CLAMP_VARIANCE", 1124), Float("LIFE_BASE", 688),
        Float("LIFE_VARIANCE", 692), Float("RADIUS", 696), Float("SCALE", 700), Float("TIME_SCALE", 1144), Float("TIME_SCALE_VARIANCE", 1148)
    ];

    public static readonly Token[] MaterialTokens =
    [
        new("ALPHA_TEST_MODE", 1, 59, 0, alphaTests), new("BLEND_MODE", 1, 58, 0, blends), new("Z_TEST_MODE", 1, 61, 0, zTests),
        Flag("FLAG_MULTIFRAME", 36, 0x1), Flag("FLAG_INTERFRAMEBLEND", 36, 0x2), Flag("ALPHA_TEST_ENABLE", 36, 0x4), Flag("Z_TEST_ENABLE", 36, 0x10),
        Flag("Z_WRITE_ENABLE", 36, 0x20), Flag("FLAG_LAYERGROUP", 36, 0x80), Flag("FLAG_WRAP_U", 36, 0x100), Flag("FLAG_WRAP_V", 36, 0x200),
        Flag("DO_MESH0", 36, 0x2000), Flag("DO_MESH1", 36, 0x4000), Flag("DO_MESH2", 36, 0x8000), Flag("DO_MESH3", 36, 0x10000), Flag("DO_MESH4", 36, 0x20000),
        Number("FRAME_BASE", 44), Number("FRAME_VARIANCE", 48), Float("FPS", 156), Float("FPS_VARIANCE", 160), new("XFRAMES", 1, 56), new("YFRAMES", 1, 57),
        Float("RIBBON_TEX_STRETCH", 72), new("ALPHA_TEST_VALUE", 1, 60), Float("NORMAL_BLEND_VALUE", 140), Float("KEY_LIGHT_VALUE", 144), Float("IBL_VALUE", 148),
        new("MESH0", 14, 96, 0x2000), new("MESH1", 14, 100, 0x4000), new("MESH2", 14, 104, 0x8000), new("MESH3", 14, 108, 0x10000), new("MESH4", 14, 112, 0x20000),
        Number("MESH_PERCENT0", 116), Number("MESH_PERCENT1", 120), Number("MESH_PERCENT2", 124), Number("MESH_PERCENT3", 128), Number("MESH_PERCENT4", 132),
        new("MESH", 14, 28), new("TEXTURE", 14, 16), new("LAYERGROUPNAME", 14, 32), new("NORMAL_MAP", 14, 24), new("TEX_ANIM_OPTIONS", 1, 65, 0, texAnims),
        new("SHADER", 1, 66, 0, shaders), new("NORMAL_OPTION", 1, 67, 0, normalOptions), new("U_COORD_OPTION", 1, 63, 0, coords), new("V_COORD_OPTION", 1, 64, 0, coords)
    ];

    public static readonly Token[] WaveformTokens =
    [
        Float("AMP", 20), Float("BASE", 8), Float("CLAMP_MIN", 24), Float("CLAMP_MAX", 28), Float("FREQ", 16), Float("PHASE", 12), Float("AMP_VARIANCE", 44),
        Float("BASE_VARIANCE", 32), Float("CLAMPMIN_VARIANCE", 48), Float("CLAMPMAX_VARIANCE", 52), Float("FREQ_VARIANCE", 40), Float("PHASE_VARIANCE", 36),
        Number("TYPE", 4, waveTypes)
    ];

    // Waveform offsets: alpha/RGB precede X/Y/Z (offsets 712..728)
    private static readonly (string Name, int Offset)[] waveforms = [("WAVEFORM_ALPHA", 724), ("WAVEFORM_RGB", 728), ("WAVEFORM_X", 712), ("WAVEFORM_Y", 716), ("WAVEFORM_Z", 720)];

    // Tokenized record (cParticleDescriptor, cParticleBehaviour, cParticleMaterial, or cParticleWaveForm)
    public sealed class Record(byte[] data, int at, bool bigEndian, Token[] tokens)
    {
        public Token[] Tokens => tokens;

        // Original raw bytes used as template during compilation
        internal byte[] Bytes(int size) => data.AsSpan(at, Math.Min(size, data.Length - at)).ToArray();

        public uint Word(int offset) => at + offset >= 0 && at + offset + 4 <= data.Length
            ? bigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(at + offset))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(at + offset))
            : throw new InvalidDataException("The effect reads past its end.");

        private float Single(int offset) => BitConverter.UInt32BitsToSingle(Word(offset));

        private Token Find(string name) => Array.Find(tokens, t => t.Name == name) ?? throw new ArgumentException($"There's no {name} here.");

        public float Float(string name) => Single(Find(name).Offset);

        public Vector3 Vector(string name)
        {
            int offset = Find(name).Offset;
            return new(Single(offset), Single(offset + 4), Single(offset + 8));
        }

        public bool Flag(string name)
        {
            Token token = Find(name);
            return (Word(token.Offset) & token.Bit) != 0;
        }

        public int Number(string name)
        {
            Token token = Find(name);
            return token.Type == 1
                ? data[at + token.Offset]
                : (int)Word(token.Offset);
        }

        // Low byte to high byte: R, G, B, A (0-255)
        public Vector4 Colour(string name)
        {
            uint colour = Word(Find(name).Offset);
            return new Vector4(colour & 0xFF, (colour >> 8) & 0xFF, (colour >> 16) & 0xFF, colour >> 24) / 255;
        }

        public string Text(string name) => Text(Find(name));

        private string Text(Token token)
        {
            uint offset = Word(token.Offset);

            // Ignore MESH0..4 when DO_MESH is disabled
            if (offset == 0 || offset >= data.Length - at || token.Bit != 0 && (Word(MaterialFlags) & token.Bit) == 0)
                return null;

            int start = at + (int)offset, end = Array.IndexOf(data, (byte)0, start);
            return end < 0
                ? null
                : Encoding.ASCII.GetString(data, start, end - start);
        }

        // sLionMemberToken::ValueToString
        public string Value(Token token)
        {
            string number(float value) => value.ToString("F6", CultureInfo.InvariantCulture);

            string named(uint value) => token.Names != null && value < token.Names.Length
                ? token.Names[value]
                : ((int)value).ToString(CultureInfo.InvariantCulture);

            switch (token.Type)
            {
                case 1:
                    return named(data[at + token.Offset]);
                case 7:
                    return number(Single(token.Offset));
                case 9:
                    return (Word(token.Offset) & token.Bit) != 0
                        ? "1"
                        : "0";
                case 11:
                    uint colour = Word(token.Offset);
                    return $"{colour & 0xFF}, {(colour >> 8) & 0xFF}, {(colour >> 16) & 0xFF}, {colour >> 24}";
                case 14:
                    return Text(token) ?? "(null)";
                case 15:
                    return $"{number(Single(token.Offset))}, {number(Single(token.Offset + 4))}, {number(Single(token.Offset + 8))}";
                default:
                    return named(Word(token.Offset));
            }
        }
    }

    public sealed class Behaviour
    {
        public Record Fields { get; init; }

        public List<(string Name, Record Wave)> Waveforms { get; } = [];
    }

    public sealed class Descriptor
    {
        public Record Fields { get; init; }

        public List<Behaviour> Behaviours { get; } = [];

        // Pre-blended behaviour record
        public Record Blended { get; set; }

        public Record Material { get; set; }

        public Descriptor Child { get; set; }
    }

    // Header: resource hash, pointer, cLionEffectDefinition, and cLionParticleEffect
    private byte[] header = [];

    public string Name { get; private set; }

    public List<Descriptor> Descriptors { get; private set; } = [];

    public static LionEffect FromEntry(BundleEntry entry)
    {
        LionEffect effect = new();
        effect.Read(entry);
        return effect;
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        byte[] data = entry.GetPrimaryWithoutImports();
        bool big = entry.Console;
        string name = ParticleEffects.Name(entry) ?? throw new ReadFailedError($"0x{entry.ID:X8} isn't a LION effect.");
        HashSet<int> seen = [];
        Record words = new(data, 0, big, []);

        // Resolves record offset pointer (-1 for none)
        int link(int record, int field)
        {
            uint offset = words.Word(record + field);

            if (offset == 0)
                return -1;

            int at = record + (int)Math.Min(offset, int.MaxValue / 2);
            return offset < data.Length - record && seen.Add(at)
                ? at
                : throw new ReadFailedError($"{name} has a link that loops or points outside it.");
        }

        // Descriptor traversal: behaviours/waveforms, material, and child descriptor
        Descriptor descriptor(int at)
        {
            Descriptor result = new()
            {
                Fields = new(data, at, big, DescriptorTokens)
            };

            for (int b = link(at, DescriptorBehaviours); b >= 0; b = link(b, BehaviourNext))
            {
                Behaviour behaviour = new()
                {
                    Fields = new(data, b, big, BehaviourTokens)
                };

                foreach ((string wave, int field) in waveforms)
                    if (link(b, field) is int w and >= 0)
                        behaviour.Waveforms.Add((wave, new(data, w, big, WaveformTokens)));

                result.Behaviours.Add(behaviour);
            }

            if (link(at, DescriptorBlended) is int blended and >= 0)
                result.Blended = new(data, blended, big, BehaviourTokens);

            if (link(at, DescriptorMaterial) is int m and >= 0)
                result.Material = new(data, m, big, MaterialTokens);

            if (link(at, DescriptorChild) is int child and >= 0)
                result.Child = descriptor(child);

            return result;
        }

        Name = name;
        Descriptors = [];
        int effect = link(DefinitionOffset, EffectLink);
        header = data[..Math.Min(FirstDescriptor, data.Length)];

        if (effect >= 0)
            for (int at = link(effect, 4); at >= 0; at = link(at, DescriptorNext))
                Descriptors.Add(descriptor(at));

        return true;
    }

    public string ToText()
    {
        StringBuilder text = new();

        void block(string header, Record record, Action inside = null)
        {
            text.Append(header).Append("\n{\n");

            foreach (Token token in record.Tokens)
                text.Append(token.Name).Append(" = ").Append(record.Value(token)).Append('\n');

            inside?.Invoke();
            text.Append("}\n");
        }

        void descriptor(Descriptor d) => block("<DESCRIPTOR name =\"des\">", d.Fields, () =>
        {
            foreach (Behaviour b in d.Behaviours)
                block("<BEHAVIOUR name =\"beh\">", b.Fields, () =>
                {
                    foreach ((string wave, Record w) in b.Waveforms)
                        block($"<WAVEFORM name =\"{wave}\">", w);
                });

            if (d.Material != null)
                block("<MATERIAL name =\"beh\">", d.Material);

            if (d.Child != null)
                descriptor(d.Child);
        });

        text.Append($"<LION_EFFECT name=\"{Name}\">\n{{\n<LION_PARTICLES name=\"tool.lef\">\n{{\n");

        foreach (Descriptor d in Descriptors)
            descriptor(d);

        return text.Append("}\n}\n").ToString();
    }

    // Parses .lef block: <TAG name="..."> { KEY = value }
    private sealed class TextBlock(string tag, string name)
    {
        public string Tag => tag;

        public string Name => name;

        public List<(string Key, string Value)> Fields { get; } = [];

        public List<TextBlock> Blocks { get; } = [];

        public IEnumerable<TextBlock> Within(string blockTag) => Blocks.Where(b => b.Tag == blockTag);

        public IEnumerable<TextBlock> Descendants() => Blocks.SelectMany(b => b.Descendants().Prepend(b));
    }

    private static TextBlock Parse(string text)
    {
        TextBlock root = new("", ""), pending = null;
        Stack<TextBlock> open = new();
        open.Push(root);

        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();

            if (line.StartsWith('<'))
            {
                int end = line.IndexOfAny([' ', '>']), quote = line.IndexOf('"');
                pending = new(line[1..(end < 0
                    ? line.Length
                    : end)].ToUpperInvariant(), quote < 0
                    ? ""
                    : line[(quote + 1)..].Split('"')[0]);
                open.Peek().Blocks.Add(pending);
            }
            else if (line == "{")
            {
                open.Push(pending ?? throw new InvalidDataException("A { doesn't follow a <BLOCK> line."));
                pending = null;
            }
            else if (line == "}")
            {
                if (open.Count == 1)
                    throw new InvalidDataException("There's a } with no block to close.");

                open.Pop();
            }
            else if (line.IndexOf('=') is int equals and > 0)
                open.Peek().Fields.Add((line[..equals].Trim().ToUpperInvariant(), line[(equals + 1)..].Trim().Trim('"')));
        }

        return open.Count == 1
            ? root
            : throw new InvalidDataException("A block isn't closed.");
    }

    // Material string offsets preceding descriptor name
    private static readonly int[] materialStrings = [16, 28, 32, 96, 100, 104, 108, 112, 24];

    // Textures in .lef are paths; extract uppercase file name without extension for game format
    private static string TextureName(string value)
    {
        string file = value.Replace('\\', '/').Split('/')[^1];
        return Path.GetExtension(file).ToLowerInvariant() is ".tga" or ".dds" or ".png" or ".bmp"
            ? Path.GetFileNameWithoutExtension(file).ToUpperInvariant()
            : file;
    }

    // Compiles .lef text over existing effect template, preserving unexposed fields and binary padding
    public byte[] Compile(string text, bool bigEndian, List<string> notes)
    {
        TextBlock root = Parse(text);
        HashSet<string> unknown = [];

        foreach (TextBlock skipped in root.Descendants().Where(b => b.Tag.StartsWith("LION_") && b.Tag is not ("LION_EFFECT" or "LION_PARTICLES")))
            notes.Add($"Burnout's LION only has particles, so {skipped.Tag} was left out.");

        List<TextBlock> top = [.. root.Descendants().Where(b => b.Tag == "LION_PARTICLES").SelectMany(b => b.Within("DESCRIPTOR"))];

        if (top.Count == 0)
            throw new InvalidDataException("The file has no particle emitters (DESCRIPTOR blocks).");

        if (header.Length < FirstDescriptor)
            throw new InvalidDataException("This effect's header isn't the one Burnout's tools wrote.");

        List<Descriptor> templates = [];

        void collect(Descriptor d)
        {
            templates.Add(d);

            if (d.Child != null)
                collect(d.Child);
        }

        foreach (Descriptor d in Descriptors)
            collect(d);

        using MemoryStream ms = new();
        ms.Write(header.AsSpan(0, FirstDescriptor));

        // Relative record offsets resolved after layout
        Dictionary<int, uint> links = [];
        List<(int Record, int Field, string Text)> strings = [];
        int index = 0;

        uint read(byte[] record, int offset) => bigEndian
            ? BinaryPrimitives.ReadUInt32BigEndian(record.AsSpan(offset))
            : BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(offset));

        void write(byte[] record, int offset, uint value)
        {
            if (bigEndian)
                BinaryPrimitives.WriteUInt32BigEndian(record.AsSpan(offset), value);
            else
                BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(offset), value);
        }

        void link(int record, int field, int target) => links[record + field] = target < 0
            ? 0
            : (uint)(target - record);

        int named(Token token, string value) => token.Names != null && Array.FindIndex(token.Names, n => n.Equals(value, StringComparison.OrdinalIgnoreCase)) is int i and >= 0
            ? i
            : int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
                ? number
                : throw new InvalidDataException($"{token.Name} = {value} isn't a number or one of its names.");

        uint single(Token token, string value) => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number)
            ? BitConverter.SingleToUInt32Bits(number)
            : throw new InvalidDataException($"{token.Name} = {value} isn't a number.");

        // The text's values over a record from its template; its strings are returned for after the records
        Dictionary<int, string> record(byte[] bytes, Token[] tokens, TextBlock block, Record template)
        {
            Dictionary<int, string> texts = [];

            foreach (Token token in tokens.Where(t => t.Type == 14))
                texts[token.Offset] = template?.Text(token.Name);

            foreach ((string key, string value) in block.Fields)
            {
                Token token = Array.Find(tokens, t => t.Name == key);
                string[] parts = value.Split(',', StringSplitOptions.TrimEntries);

                switch (token?.Type)
                {
                    case null:
                        unknown.Add(key);
                        break;
                    case 1:
                        bytes[token.Offset] = (byte)named(token, value);
                        break;
                    case 7:
                        write(bytes, token.Offset, single(token, value));
                        break;
                    case 9:
                        uint flags = read(bytes, token.Offset);
                        write(bytes, token.Offset, named(token, value) != 0
                            ? flags | token.Bit
                            : flags & ~token.Bit);
                        break;
                    case 11 when parts.Length == 4:
                        uint colour = 0;

                        for (int i = 0; i < 4; i++)
                            colour |= (uint)Math.Clamp(named(token, parts[i]), 0, 255) << (8 * i);

                        write(bytes, token.Offset, colour);
                        break;
                    case 14:
                        texts[token.Offset] = value is "" or "(null)" or "(NULL)"
                            ? null
                            : value;
                        break;
                    case 15 when parts.Length >= 3:
                        for (int i = 0; i < 3; i++)
                            write(bytes, token.Offset + 4 * i, single(token, parts[i]));
                        break;
                    case 5 or 6:
                        write(bytes, token.Offset, (uint)named(token, value));
                        break;
                    default:
                        throw new InvalidDataException($"{token.Name} = {value} doesn't have the parts it needs.");
                }
            }

            return texts;
        }

        byte[] from(Record template, int size)
        {
            byte[] bytes = new byte[size];
            template?.Bytes(size).CopyTo(bytes, 0);
            return bytes;
        }

        int place(byte[] bytes)
        {
            int at = (int)ms.Length;
            ms.Position = at;
            ms.Write(bytes);
            return at;
        }

        // Serializes descriptor tree: behaviours, waveforms, blended behaviour, material, and children
        int descriptor(TextBlock block)
        {
            Descriptor template = templates.Count > 0
                ? templates[Math.Min(index, templates.Count - 1)]
                : null;
            index++;
            List<TextBlock> behaviours = [.. block.Within("BEHAVIOUR")];
            TextBlock materialBlock = block.Within("MATERIAL").FirstOrDefault();

            if (behaviours.Count is 0 or > 2)
                throw new InvalidDataException($"An emitter has {behaviours.Count} behaviours, Burnout's LION needs 1 or 2.");

            byte[] fields = from(template?.Fields, DescriptorSize);
            Dictionary<int, string> names = record(fields, DescriptorTokens, block, template?.Fields);
            write(fields, DescriptorBehaviourCount, (uint)behaviours.Count);
            int at = place(fields);
            byte[] first = null;
            int previous = -1;

            foreach ((TextBlock behaviourBlock, int j) in behaviours.Select((b, j) => (b, j)))
            {
                Behaviour behaviourTemplate = template?.Behaviours[Math.Min(j, template.Behaviours.Count - 1)];
                byte[] bytes = from(behaviourTemplate?.Fields, BehaviourSize);
                record(bytes, BehaviourTokens, behaviourBlock, behaviourTemplate?.Fields);
                first ??= bytes;
                int b = place(bytes);
                link(previous < 0
                    ? at
                    : previous, previous < 0
                    ? DescriptorBehaviours
                    : BehaviourNext, b);
                link(b, BehaviourNext, -1);
                previous = b;

                foreach ((string wave, int field) in waveforms)
                {
                    TextBlock waveBlock = behaviourBlock.Within("WAVEFORM").FirstOrDefault(w => w.Name.Equals(wave, StringComparison.OrdinalIgnoreCase));
                    Record waveTemplate = behaviourTemplate?.Waveforms.FirstOrDefault(w => w.Name == wave).Wave;
                    int w = -1;

                    if (waveBlock != null)
                    {
                        byte[] waveBytes = from(waveTemplate, WaveformSize);
                        record(waveBytes, WaveformTokens, waveBlock, waveTemplate);
                        w = place(waveBytes);
                    }

                    link(b, field, w);
                }
            }

            // Populates blended behaviour record
            int blended = -1;

            if (behaviours.Count > 1)
            {
                byte[] bytes = template?.Blended != null
                    ? from(template.Blended, BehaviourSize)
                    : [.. first];

                if (template?.Blended == null)
                    Array.Clear(bytes, 712, BehaviourNext + 4 - 712);

                blended = place(bytes);
            }

            link(at, DescriptorBlended, blended);
            int material = -1;

            if (materialBlock != null)
            {
                Record materialTemplate = template?.Material ?? templates.Select(t => t.Material).LastOrDefault(m => m != null);
                byte[] bytes = from(materialTemplate, MaterialSize);
                Dictionary<int, string> texts = record(bytes, MaterialTokens, materialBlock, materialTemplate);
                material = place(bytes);

                // Preserve existing string slot or "(NULL)" placeholder
                foreach (int field in materialStrings)
                {
                    bool unused = field >= 96 && texts[field] == null && materialTemplate != null && (read(bytes, MaterialFlags) & (0x2000u << ((field - 96) / 4))) == 0;

                    if (texts[field] != null || (materialTemplate?.Word(field) ?? (field == 24
                        ? 0u
                        : 1u)) != 0)
                        strings.Add((material, unused
                            ? -1
                            : field, texts[field] == null
                            ? "(NULL)"
                            : field == 32
                                ? texts[field]
                                : TextureName(texts[field])));
                    else
                        link(material, field, -1);
                }
            }

            link(at, DescriptorMaterial, material);
            strings.Add((at, DescriptorName, names[DescriptorName] ?? "(NULL)"));

            // Sibling descriptors chain via next-sibling pointers
            int child = -1;
            link(at, DescriptorChild, -1);

            foreach (TextBlock childBlock in block.Within("DESCRIPTOR"))
            {
                int c = descriptor(childBlock);
                link(child < 0
                    ? at
                    : child, child < 0
                    ? DescriptorChild
                    : DescriptorNext, c);
                child = c;
            }

            link(at, DescriptorNext, -1);
            return at;
        }

        int last = -1;

        foreach (TextBlock block in top)
        {
            int d = descriptor(block);

            if (last >= 0)
                link(last, DescriptorNext, d);

            last = d;
        }

        // Unreferenced string written by tools
        foreach ((int at, int field, string value) in strings)
        {
            if (field >= 0)
                link(at, field, (int)ms.Length);

            place(Encoding.ASCII.GetBytes(value + "\0"));
        }

        ms.SetLength((ms.Length + 15) & ~15);
        byte[] result = ms.ToArray();

        foreach ((int at, uint value) in links)
            write(result, at, value);

        if (unknown.Count > 0)
            notes.Add($"Burnout's LION doesn't have {string.Join(", ", unknown.Order())}, so they were left out.");

        return result;
    }

    public IEntryEditor GetEditor(BundleEntry entry) => new LionEffectViewer(this, entry);

    public EntryType GetEntryType(BundleEntry entry) => EntryType.ParticleDescription;

    public bool Write(BundleEntry entry) => true;

    // Exports all ParticleDescription entries in the archive to individual .lef files
    public static void ExportAll(IWin32Window owner, BundleArchive archive)
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Export the bundle's LION effects as .lef files to:",
            UseDescriptionForTitle = true,
            InitialDirectory = Path.GetDirectoryName(archive.Path) ?? ""
        };

        if (dialog.ShowDialog(owner) != DialogResult.OK)
            return;

        List<string> failed = [];
        HashSet<string> used = new(StringComparer.OrdinalIgnoreCase);
        int written = 0;

        foreach (BundleEntry entry in archive.Entries.Where(e => e.Type == EntryType.ParticleDescription))
        {
            try
            {
                string file = string.Concat((ParticleEffects.Name(entry) ?? $"{entry.ID:X8}.lef").Select(c => Path.GetInvalidFileNameChars().Contains(c)
                    ? '_'
                    : c));

                if (!used.Add(file))
                    file = $"{entry.ID:X8}_{file}";

                File.WriteAllText(Path.Combine(dialog.SelectedPath, file), FromEntry(entry).ToText());
                written++;
            }
            catch (Exception ex) when (ex is ReadFailedError or InvalidDataException or IOException or UnauthorizedAccessException)
            {
                failed.Add($"0x{entry.ID:X8}: {ex.Message}");
            }
        }

        MessageBox.Show(owner, $"Exported {written} effects to {dialog.SelectedPath}.{(failed.Count > 0
            ? "\n\nCouldn't export:\n" + string.Join("\n", failed)
            : "")}", "Export LION effects", MessageBoxButtons.OK, failed.Count > 0
            ? MessageBoxIcon.Warning
            : MessageBoxIcon.Information);
    }
}
