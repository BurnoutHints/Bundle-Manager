using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

// AttribSys class layouts from the schema baked into each game exe, made by tools/build_attrib_schema.py
public sealed record AttribSchema(string[] Versions, Dictionary<string, Dictionary<string, string>> Enums, AttribSchema.Class[] Classes)
{
    public sealed record Field(string Name, string Key, string Type, int Offset, int Size, int Max, int Flags, int Align)
    {
        public bool IsArray => (Flags & 1) != 0;

        public bool InLayout => (Flags & 2) != 0;

        public string Label => Name ?? "0x" + Key;
    }

    public sealed record Class(string Name, string Key, int Layout, int[] Versions, Field[] Fields)
    {
        // Where the last field ends; vaults can pack the next payload into the layout's tail padding
        public int Extent => Fields.Where(f => f.InLayout).Select(f => f.Offset + (AttribFields.ArrayData(f) is int data
            ? data + f.Max * f.Size
            : f.Size)).DefaultIfEmpty(0).Max();
    }

    public static AttribSchema Instance { get; } = Load();

    private static AttribSchema Load()
    {
        using Stream stream = typeof(AttribSchema).Assembly.GetManifestResourceStream("AttribSchema.json");
        return JsonSerializer.Deserialize<AttribSchema>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}

// AttribSys vault: collections of attributes, laid out by class.
// Edits patch the bytes in place, so anything this doesn't understand is saved unchanged.
public sealed class AttribVault : IEntryData
{
    private const ulong CollectionLoadData = 0xAD303B8F42B3307E;
    private const uint ExpN = 0x4578704E, PtrN = 0x5074724E; // Read in the file's byte order, LE files store them flipped

    // A PtrN record: kind 2 picks the block (0 vlt, 1 bin) later slots are in, kind 3 points a slot into block Dep
    private sealed record Fixup(int At, int Kind, int Dep, int Base, long Slot, long Target);

    private byte[] data = [];
    private bool bigEndian;
    private int binAt, strESize, headerWord;
    private readonly List<Fixup> fixups = [];
    private readonly Dictionary<(ulong Class, ulong Key), AttribCollection> byKey = [];

    [Category("Vault"), Description("The game versions whose class layouts match all of this vault's.")]
    public string Schema { get; private set; }

    [Category("Vault"), Description("Collections whose class layout isn't known, shown as raw bytes.")]
    public int Unknown { get; private set; }

    [Browsable(false)]
    public ReadOnlyCollection<AttribClass> Classes { get; private set; } = new([]);

    internal bool BigEndian => bigEndian;

    internal IEnumerable<AttribCollection> Collections => Classes.SelectMany(c => c.Collections);

    internal AttribCollection Find(ulong classKey, ulong key) => byKey.GetValueOrDefault((classKey, key));

    private ulong U(int at, int size) => size switch
    {
        2 => bigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(at))
            : BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(at)),
        4 => bigEndian
            ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(at))
            : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(at)),
        _ => bigEndian
            ? BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(at))
            : BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(at))
    };

    private int I32(int at) => (int)U(at, 4);

    internal static void Put(Span<byte> span, ulong value, int size, bool bigEndian)
    {
        switch (size)
        {
            case 1:
                span[0] = (byte)value;
                break;
            case 2:
                if (bigEndian)
                    BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)value);
                else
                    BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)value);
                break;
            case 4:
                if (bigEndian)
                    BinaryPrimitives.WriteUInt32BigEndian(span, (uint)value);
                else
                    BinaryPrimitives.WriteUInt32LittleEndian(span, (uint)value);
                break;
            default:
                if (bigEndian)
                    BinaryPrimitives.WriteUInt64BigEndian(span, value);
                else
                    BinaryPrimitives.WriteUInt64LittleEndian(span, value);
                break;
        }
    }

    private string CString(int at)
    {
        int end = Array.IndexOf(data, (byte)0, at);
        return Encoding.Latin1.GetString(data, at, end - at);
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        Parse(entry.GetPrimaryWithoutImports(), entry.Console);
        return true;
    }

    private void Parse(byte[] bytes, bool console)
    {
        data = bytes;
        bigEndian = console;
        fixups.Clear();
        byKey.Clear();

        // Remastered's 64-bit builds widen the header to four 64-bit words
        headerWord = U(0, 8) == 0x20
            ? 8
            : 4;
        int vltAt = (int)U(0, headerWord), vltSize = (int)U(headerWord, headerWord);
        binAt = (int)U(headerWord * 2, headerWord);

        // Some converted vaults claim more than is there
        int binSize = Math.Min((int)U(headerWord * 3, headerWord), data.Length - binAt);
        strESize = I32(binAt + 4);

        // Some PC vehicle vaults have it big endian; the game never reads it
        if (strESize < 8 || strESize > binSize)
            strESize = (int)BinaryPrimitives.ReverseEndianness((uint)strESize);

        if (strESize < 8 || strESize > binSize)
            throw new ReadFailedError("The vault's string table has a broken size.");

        List<int> exports = [];

        for (int at = vltAt; at < vltAt + vltSize;)
        {
            uint fourCC = (uint)U(at, 4);
            int size = I32(at + 4);

            if (size < 8 || at + size > vltAt + vltSize)
                throw new ReadFailedError($"The vault has a broken chunk at 0x{at:X}.");

            if (fourCC == ExpN)
            {
                // {pad, count} on consoles and the decomp, flipped as one value on PC
                int count = I32(at + 12) != 0
                    ? I32(at + 12)
                    : I32(at + 8);

                for (int i = 0; i < count; i++)
                {
                    if (U(at + 16 + i * 24 + 8, 8) != CollectionLoadData)
                        throw new ReadFailedError("The vault exports something other than collections.");

                    exports.Add(I32(at + 16 + i * 24 + 20));
                }
            }
            else if (fourCC == PtrN)
            {
                int current = -1;

                for (int r = at + 8; r + 16 <= at + size; r += 16)
                {
                    int kind = (int)U(r + 4, 2), dep = (int)U(r + 6, 2);

                    if (kind == 2)
                        current = dep;

                    fixups.Add(new(r, kind, dep, current, (long)U(r, 4), (long)U(r + 8, 8)));
                }
            }

            at += size;
        }

        Dictionary<long, Fixup> vltSlots = [];
        Dictionary<long, long> binSlots = [];

        foreach (Fixup fixup in fixups.Where(f => f.Kind == 3))
        {
            if (fixup.Base == 0)
                vltSlots[fixup.Slot] = fixup;
            else
                binSlots[fixup.Slot] = fixup.Target;
        }

        // Each payload runs up to the next one
        long[] starts = [.. fixups.Where(f => f.Kind == 3 && f.Dep == 1 && f.Target >= strESize).Select(f => f.Target).Append(binSize).Distinct().Order()];

        int region(long target) => (int)(starts[Array.BinarySearch(starts, target) + 1] - target);

        string textAt(int @base, long slot)
        {
            long target = @base == 0
                ? vltSlots.TryGetValue(slot, out Fixup fixup) && fixup.Dep == 1
                    ? fixup.Target
                    : -1
                : binSlots.GetValueOrDefault(slot, -1);
            return target < 0
                ? null
                : CString(binAt + (int)target);
        }

        List<AttribCollection> collections = [];

        foreach (int position in exports)
        {
            int header = vltAt + position;
            AttribCollection collection = new(this, U(header, 8), U(header + 8, 8), U(header + 16, 8));
            List<byte> content = [];

            void segment(int fileAt, int length)
            {
                collection.Segments.Add((fileAt, length));
                content.AddRange(data.AsSpan(fileAt, length));
            }

            if (vltSlots.TryGetValue(position + 0x28, out Fixup layout) && layout.Dep == 1 && layout.Target >= strESize)
            {
                collection.LayoutSlot = layout.Target;
                segment(binAt + (int)layout.Target, region(layout.Target));
            }

            int entries = I32(header + 32), typesLength = (int)U(header + 38, 2);

            for (int i = 0; i < entries; i++)
            {
                int entryAt = header + 48 + typesLength * 8 + i * 16;
                long slot = entryAt + 8 - vltAt;
                AttribCollection.Item item = new(U(entryAt, 8), content.Count);

                // Values that don't fit in the entry live in the bin
                if (vltSlots.TryGetValue(slot, out Fixup value) && value.Dep == 1 && value.Target >= strESize)
                {
                    item.Base = 1;
                    item.Slot = value.Target;
                    segment(binAt + (int)value.Target, region(value.Target));
                }
                else
                {
                    item.Slot = slot;
                    segment(entryAt + 8, 4);
                }

                collection.Items.Add(item);
            }

            collection.Bytes = [.. content];
            collections.Add(collection);
            byKey.TryAdd((collection.Class, collection.Key), collection);
        }

        // The vault doesn't say which game it's from, so take the version that fits the most layouts
        Dictionary<ulong, AttribSchema.Class[]> classes = AttribSchema.Instance.Classes.GroupBy(c => Convert.ToUInt64(c.Key, 16)).ToDictionary(g => g.Key, g => g.ToArray());

        // Layouts of the same size are told apart by their arrays' headers, and by string pointers being as wide as
        // the vault header's words (64-bit only on Xbox One and Switch). Loosely, a converted layout can sit in a bigger slot.
        bool fits(AttribCollection collection, AttribSchema.Class schema, bool tight = true) => schema.Fields.All(f => f.Type != AttribFields.Text || f.Size == headerWord)
            && (collection.LayoutSlot < 0
                ? schema.Layout == 0
                : collection.Segments[0].Length >= schema.Extent && (!tight || collection.Segments[0].Length < schema.Layout + 16)
                    && schema.Fields.Where(f => f.InLayout && f.IsArray).All(f => collection.Get(f.Offset, 2) == (ulong)f.Max
                        && collection.Get(f.Offset + 2, 2) <= (ulong)f.Max && collection.Get(f.Offset + 4, 2) == (ulong)f.Size));

        int[] scores = [.. AttribSchema.Instance.Versions.Select((_, v) => collections.Count(c =>
            classes.TryGetValue(c.Class, out AttribSchema.Class[] options) && options.Any(o => o.Versions.Contains(v) && fits(c, o))))];
        int version = Array.LastIndexOf(scores, scores.Max());

        foreach (AttribCollection collection in collections)
        {
            AttribSchema.Class[] options = classes.GetValueOrDefault(collection.Class, []);
            collection.Schema = options.FirstOrDefault(o => o.Versions.Contains(version) && fits(collection, o)) ?? options.FirstOrDefault(o => fits(collection, o))
                ?? options.FirstOrDefault(o => o.Versions.Contains(version) && fits(collection, o, false)) ?? options.FirstOrDefault(o => fits(collection, o, false));
            collection.ClassName = options.FirstOrDefault()?.Name ?? $"0x{collection.Class:X16}";
            collection.Build(textAt);
        }

        Unknown = collections.Count(c => c.Schema == null);
        string[] versions = [.. AttribSchema.Instance.Versions.Where((_, v) => collections.All(c => c.Schema?.Versions.Contains(v) != false))];
        Schema = versions.Length == AttribSchema.Instance.Versions.Length
            ? "Any version"
            : versions.Length == 0
                ? "Mixed versions"
                : string.Join(" or ", versions);
        Dictionary<ulong, string> names = Names([.. collections.Select(c => c.Key)], StringTable().Keys);

        foreach (AttribCollection collection in collections)
            collection.ID = names.GetValueOrDefault(collection.Key) ?? $"0x{collection.Key:X16}";

        foreach (IGrouping<ulong, AttribCollection> group in collections.GroupBy(c => c.Class))
            if (group.Count() > 1 && group.Select(c => c.FirstText).Distinct().Count() == 1)
                foreach (AttribCollection collection in group)
                    collection.ShowText = false;

        Classes = new([.. collections.GroupBy(c => c.Class).Select(g => new AttribClass(g.First().ClassName, g.Key, [.. g]))
            .OrderBy(c => c.Name.StartsWith("0x")).ThenBy(c => c.Name, StringComparer.Ordinal)]);
    }

    // Collections are named by GameDB node IDs, hashed, so most names come back by hashing numbers
    private static Dictionary<ulong, string> Names(HashSet<ulong> keys, IEnumerable<string> strings)
    {
        Dictionary<ulong, string> names = [];

        foreach (string text in strings)
            if (keys.Contains(Utilities.CalcLookup8(text)))
                names.TryAdd(Utilities.CalcLookup8(text), text);

        byte[] buffer = new byte[16];

        // ponytail: brute force up to 2M IDs (~0.1 s), add a GameDB name list if vaults turn up with other names
        for (int n = 0; n < 1 << 21 && names.Count < keys.Count; n++)
        {
            n.TryFormat(buffer, out int length, default, CultureInfo.InvariantCulture);
            ulong hash = Lookup8.Hash(buffer, (ulong)length, 0xABCDEF0011223344);

            if (keys.Contains(hash))
                names.TryAdd(hash, n.ToString(CultureInfo.InvariantCulture));
        }

        return names;
    }

    // Every string in StrE by its bin offset
    private Dictionary<string, int> StringTable()
    {
        Dictionary<string, int> strings = [];

        for (int at = 8; at < strESize;)
        {
            int end = Array.IndexOf(data, (byte)0, binAt + at, strESize - at);

            if (end < 0)
                break;

            strings.TryAdd(Encoding.Latin1.GetString(data, binAt + at, end - binAt - at), at);
            at = end - binAt + 1;
        }

        return strings;
    }

    public byte[] Serialize()
    {
        byte[] output = [.. data];

        foreach (AttribCollection collection in Collections)
        {
            int at = 0;

            foreach ((int fileAt, int length) in collection.Segments)
            {
                collection.Bytes.AsSpan(at, length).CopyTo(output.AsSpan(fileAt));
                at += length;
            }
        }

        Dictionary<(int Base, long Slot), string> changed = [];

        foreach (AttribCollection collection in Collections)
            for (int i = 0; i < collection.Texts.Length; i++)
                if (collection.Texts[i] != collection.TextSlots[i].Original)
                    changed[(collection.TextSlots[i].Base, collection.TextSlots[i].Slot)] = collection.Texts[i];

        if (changed.Count == 0)
            return output;

        // Existing strings keep their offsets, new ones are added to the end of StrE, moving the payloads after it
        Dictionary<string, int> strings = StringTable();
        List<byte> added = [];

        foreach (string text in changed.Values.Distinct())
            if (!strings.ContainsKey(text))
            {
                strings[text] = strESize + added.Count;
                added.AddRange(Encoding.Latin1.GetBytes(text));
                added.Add(0);
            }

        // Payloads stay 16-byte aligned
        int delta = (added.Count + 15) / 16 * 16;
        byte[] grown = new byte[output.Length + delta];
        output.AsSpan(0, binAt + strESize).CopyTo(grown);
        added.ToArray().CopyTo(grown, binAt + strESize);
        output.AsSpan(binAt + strESize).CopyTo(grown.AsSpan(binAt + strESize + delta));
        Put(grown.AsSpan(headerWord * 3), U(headerWord * 3, headerWord) + (ulong)delta, headerWord, bigEndian);
        Put(grown.AsSpan(binAt + 4), (ulong)(strESize + delta), 4, bigEndian);

        foreach (Fixup fixup in fixups.Where(f => f.Kind == 3))
        {
            long slot = fixup.Base == 1 && fixup.Slot >= strESize
                ? fixup.Slot + delta
                : fixup.Slot;
            long target = changed.TryGetValue((fixup.Base, fixup.Slot), out string text)
                ? strings[text]
                : fixup.Dep == 1 && fixup.Target >= strESize
                    ? fixup.Target + delta
                    : fixup.Target;
            Put(grown.AsSpan(fixup.At), (ulong)slot, 4, bigEndian);
            Put(grown.AsSpan(fixup.At + 8), (ulong)target, 8, bigEndian);
        }

        return grown;
    }

    public AttribVault Copy()
    {
        AttribVault copy = new();
        copy.Parse(Serialize(), bigEndian);
        return copy;
    }

    public bool Write(BundleEntry entry)
    {
        entry.SetPrimary(Serialize(), entry.GetImports());
        return true;
    }

    public EntryType GetEntryType(BundleEntry entry) => EntryType.AttribSysVault;

    public IEntryEditor GetEditor(BundleEntry entry) => new AttribVaultEditor(this, entry);
}

public sealed class AttribClass(string name, ulong key, List<AttribCollection> collections)
{
    [Category("Class"), Description("The class name, or its hash if it isn't known.")]
    public string Name { get; } = name;

    [Category("Class"), TypeConverter(typeof(HexConverter))]
    public ulong Key { get; } = key;

    [Category("Class"), Description("The class layout's size in bytes.")]
    public int Layout => Collections[0].Schema?.Layout ?? 0;

    [Browsable(false)]
    public ReadOnlyCollection<AttribCollection> Collections { get; private set; } = new(collections);

    public override string ToString() => $"{Name} ({Collections.Count})";
}

// One collection's attributes. Its bytes and strings are the only state, so history and undo track them as arrays.
public sealed class AttribCollection(AttribVault vault, ulong key, ulong classKey, ulong parent) : ICustomTypeDescriptor
{
    // An attribute outside the layout, stored in the collection entry (Base 0) or out in the bin (Base 1)
    internal sealed class Item(ulong key, int at)
    {
        public ulong Key = key;
        public int At = at, Base;
        public long Slot;
    }

    internal List<(int FileAt, int Length)> Segments { get; } = [];

    internal List<Item> Items { get; } = [];

    internal List<(int Base, long Slot, string Original)> TextSlots { get; } = [];

    private readonly Dictionary<(int, long), int> textIndex = [];
    private PropertyDescriptorCollection properties;

    internal AttribSchema.Class Schema { get; set; }

    internal long LayoutSlot { get; set; } = -1;

    [Browsable(false)]
    public byte[] Bytes { get; set; } = [];

    [Browsable(false)]
    public string[] Texts { get; set; } = [];

    internal AttribVault Vault => vault;

    internal ulong Key => key;

    internal ulong Class => classKey;

    internal ulong ParentKey => parent;

    internal string ClassName { get; set; }

    internal string ID { get; set; }

    internal string Label => $"{ClassName} {ID}";

    // Hidden when every collection of the class has the same one
    internal bool ShowText { get; set; } = true;

    internal string FirstText => Texts.FirstOrDefault(t => !string.IsNullOrEmpty(t));

    public override string ToString() => ShowText && FirstText is string text
        ? $"{ID} ({text})"
        : ID;

    internal ulong Get(int at, int size) => size switch
    {
        1 => Bytes[at],
        2 => vault.BigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(Bytes.AsSpan(at))
            : BinaryPrimitives.ReadUInt16LittleEndian(Bytes.AsSpan(at)),
        4 => vault.BigEndian
            ? BinaryPrimitives.ReadUInt32BigEndian(Bytes.AsSpan(at))
            : BinaryPrimitives.ReadUInt32LittleEndian(Bytes.AsSpan(at)),
        _ => vault.BigEndian
            ? BinaryPrimitives.ReadUInt64BigEndian(Bytes.AsSpan(at))
            : BinaryPrimitives.ReadUInt64LittleEndian(Bytes.AsSpan(at))
    };

    internal void Set(int at, int size, ulong value) => AttribVault.Put(Bytes.AsSpan(at), value, size, vault.BigEndian);

    internal int TextIndex(int @base, long slot) => textIndex.GetValueOrDefault((@base, slot), -1);

    // Finds every string slot up front, so Texts lines up with them for history
    internal void Build(Func<int, long, string> text)
    {
        void add(AttribSchema.Field field, int at, int @base, long slot)
        {
            if (field == null || AttribFields.ArrayData(field) is not int data)
            {
                if (field?.Type == AttribFields.Text)
                    register(@base, slot);

                return;
            }

            if (field.Type == AttribFields.Text && at + 8 <= Bytes.Length)
                for (int i = 0; i < (int)Get(at, 2) && at + data + (i + 1) * field.Size <= Bytes.Length; i++)
                    register(@base, slot + data + i * field.Size);
        }

        void register(int @base, long slot)
        {
            textIndex[(@base, slot)] = TextSlots.Count;
            TextSlots.Add((@base, slot, text(@base, slot)));
        }

        if (Schema != null)
            foreach (AttribSchema.Field field in Schema.Fields.Where(f => f.InLayout))
                add(field, field.Offset, 1, LayoutSlot + field.Offset);

        foreach (Item item in Items)
            add(FieldOf(item), item.At, item.Base, item.Slot);

        Texts = [.. TextSlots.Select(s => s.Original)];
    }

    internal AttribSchema.Field FieldOf(Item item) =>
        Schema?.Fields.FirstOrDefault(f => !f.InLayout && Convert.ToUInt64(f.Key, 16) == item.Key);

    private PropertyDescriptorCollection Properties() => properties ??= new([.. AttribFields.For(this)]);

    AttributeCollection ICustomTypeDescriptor.GetAttributes() => TypeDescriptor.GetAttributes(this, true);

    string ICustomTypeDescriptor.GetClassName() => TypeDescriptor.GetClassName(this, true);

    string ICustomTypeDescriptor.GetComponentName() => null;

    TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(this, true);

    EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => null;

    PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => null;

    object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => EventDescriptorCollection.Empty;

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => EventDescriptorCollection.Empty;

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => Properties();

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => Properties();

    object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => this;
}

public sealed class AttribVaultEditor : ResourceEditor
{
    private readonly AttribVault data;

    public AttribVaultEditor(AttribVault source, BundleEntry initialEntry) : base(initialEntry, "AttribSys vault", "vault")
    {
        data = source.Copy();
        Initialize(data, "Vault", null);
        Tree.SearchPrompt = "Search classes and collections";

        // References are RefSpecs in the bytes, which the object graph can't see
        Tree.FindUses = target =>
        {
            Dictionary<object, ObjectGraph.Node> nodes = ObjectGraph.Nodes(data).ToDictionary(n => n.Value, ReferenceEqualityComparer.Instance);
            return [.. data.Collections.SelectMany(c => AttribFields.Uses(c, target).Select(property => new ObjectGraph.Usage(nodes[c], property)))];
        };
    }

    protected override List<TrafficRules.Issue> FindIssues() => [];

    protected override void WriteResource() => data.Write(ResourceEntry);
}
