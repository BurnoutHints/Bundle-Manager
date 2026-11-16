using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

// CgsGui::GuiHudMessageResource (https://burnout.wiki/wiki/HUD_Message)
public class HudMessages : IEntryData
{
    private const int TableOffset = 0x80;

    public enum HudMessageGroup
    {
        E_HUDMESSAGEGROUP_ALL,
        E_HUDMESSAGEGROUP_ONLINE_LIVEREVENGE,
        E_HUDMESSAGEGROUP_ONLINE_DIRTY_TRICKS,
        E_HUDMESSAGEGROUP_INGAMEMESSAGES
    }

    public enum HudMessageParamTypes
    {
        E_HUDMESSAGEPARAMTYPES_UNUSED,
        E_HUDMESSAGEPARAMTYPES_STRING,
        E_HUDMESSAGEPARAMTYPES_INT,
        E_HUDMESSAGEPARAMTYPES_FLOAT,
        E_HUDMESSAGEPARAMTYPES_MONEY,
        E_HUDMESSAGEPARAMTYPES_TIME,
        E_HUDMESSAGEPARAMTYPES_STRINGID
    }

    public class HudMessageString
    {
        [Category("Properties"), Description("Language string ID of the text, up to 63 characters.")]
        public string macStringId { get; set; } = "";

        [Category("Parameters"), Description("Values filled into the text, in order. Parameters after an unused one aren't allowed.")]
        public HudMessageParamTypes Param1 { get; set; }

        [Category("Parameters")]
        public HudMessageParamTypes Param2 { get; set; }

        [Category("Parameters")]
        public HudMessageParamTypes Param3 { get; set; }

        [Category("Parameters")]
        public HudMessageParamTypes Param4 { get; set; }

        public HudMessageParamTypes[] Params() => [Param1, Param2, Param3, Param4];

        public bool IsEmpty() => macStringId.Length == 0 && Params().All(p => p == HudMessageParamTypes.E_HUDMESSAGEPARAMTYPES_UNUSED);

        public override string ToString() => macStringId.Length > 0
            ? macStringId
            : "(empty)";
    }

    // CgsGui::GuiHudMessageData
    public class HudMessage
    {
        [Browsable(false)]
        public List<HudMessageString> Strings { get; set; } = [];

        [Category("Properties"), Description("Name the game asks for the message by, up to 12 letters, digits, spaces, underscores, hyphens or slashes.")]
        public string macMessageId { get; set; } = "";

        [Category("Properties"), Description("CgsID the game looks the message up by, worked out from the message ID."), TypeConverter(typeof(HexConverter))]
        public ulong mMessageIdHash => Hash(macMessageId);

        [Category("Properties"), Description("Flash frame label of the message's look, such as PosMessage01 or NeutralMessage. Up to 31 characters.")]
        public string macMessageStyle { get; set; } = "";

        [Category("Properties"), Description("Flash frame label of the icon, such as invisible. Up to 31 characters.")]
        public string macDefaultIcon { get; set; } = "";

        [Category("Properties"), Description("Which group of messages this belongs to.")]
        public HudMessageGroup meMessageGroup { get; set; }

        [Category("Timing"), Description("How long the message shows, in seconds.")]
        public float mfDuration { get; set; } = 2.5f;

        [Category("Timing"), Description("How long to wait before showing the message, in seconds.")]
        public float mfTimeToWait { get; set; } = 0.5f;

        [Category("Timing"), Description("Priority as a percentage, 0 to 100.")]
        public int miPriority { get; set; }

        [Category("Timing"), Description("Priority threshold for removing this message early, 0 to 100.")]
        public int miForceRemoveThreshold { get; set; }

        [Browsable(false)]
        public uint muAvailabilityBitSet { get; set; }

        [Category("Availability"), Description("Shown in races.")]
        public bool Race { get => Bit(0x1); set => Bit(0x1, value); }

        [Category("Availability"), Description("Shown in Road Rage.")]
        public bool RoadRage { get => Bit(0x2); set => Bit(0x2, value); }

        [Category("Availability"), Description("Shown in Showtime.")]
        public bool Showtime { get => Bit(0x4); set => Bit(0x4, value); }

        [Category("Availability"), Description("Shown offline.")]
        public bool Offline { get => Bit(0x8); set => Bit(0x8, value); }

        [Category("Availability"), Description("Shown online.")]
        public bool Online { get => Bit(0x10); set => Bit(0x10, value); }

        [Category("Availability"), Description("Shown while crashed.")]
        public bool InCrash { get => Bit(0x20); set => Bit(0x20, value); }

        private bool Bit(uint mask) => (muAvailabilityBitSet & mask) != 0;

        private void Bit(uint mask, bool on) => muAvailabilityBitSet = on
            ? muAvailabilityBitSet | mask
            : muAvailabilityBitSet & ~mask;

        public override string ToString() => Strings.FirstOrDefault(s => s.macStringId.Length > 0) is HudMessageString text
            ? $"{macMessageId} — {text.macStringId}"
            : macMessageId;
    }

    [Browsable(false)]
    public List<HudMessage> Messages { get; set; } = [];

    // Builds up to Sep 4 2007 have two strings per message instead of three 
    [Browsable(false)]
    public int StringSlots { get; set; } = 3;

    // Bytes per message: 0x170 with three strings, 0x118 with two (padded to 0x120 in Aug 31/Sep 4 2007)
    [Browsable(false)]
    public int RecordSize { get; set; } = 0x170;

    [Browsable(false)]
    public int PointerSize { get; set; } = 4;

    // Nov 2006 keeps one message per resource without the header
    [Browsable(false)]
    public bool OnePerResource { get; set; }

    private static bool ValidID(string id) => id.Length is > 0 and <= 12
        && id.ToUpperInvariant().All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-' or '/' or ' ');

    public static ulong Hash(string id) => ValidID(id)
        ? new EncryptedString(id.ToUpperInvariant()).Encrypted
        : 0;

    // Room for the terminator
    private static bool Fits(string text, int size) => text.Length < size && text.All(c => c <= 0x7F);

    private static int Align(int value, int alignment) => (value + alignment - 1) / alignment * alignment;

    // Up to the end of maaeParams
    private static int FieldsSize(int slots) => Align(slots * 64 + 32 + 32 + 13, 8) + 8 + 6 * 4 + slots * 4 + slots * 4 * 4;

    public IEnumerable<TrafficRules.Issue> Problems()
    {
        if (OnePerResource && Messages.Count != 1)
            yield return new(Messages, null, "This build keeps one message per resource, so there must be exactly one.");

        Dictionary<ulong, int> seen = [];

        for (int i = 0; i < Messages.Count; i++)
        {
            HudMessage message = Messages[i];

            if (!ValidID(message.macMessageId))
                yield return new(message, nameof(HudMessage.macMessageId), $"Message {i}: the message ID needs 1 to 12 letters, digits, spaces, underscores, hyphens or slashes.");
            else if (!seen.TryAdd(message.mMessageIdHash, i))
                yield return new(message, nameof(HudMessage.macMessageId), $"Message {i}: {message.macMessageId} is also message {seen[message.mMessageIdHash]}'s ID, the game would only find the first.");

            if (!Fits(message.macMessageStyle, 32))
                yield return new(message, nameof(HudMessage.macMessageStyle), $"Message {i}: the message style needs up to 31 ASCII characters.");

            if (!Fits(message.macDefaultIcon, 32))
                yield return new(message, nameof(HudMessage.macDefaultIcon), $"Message {i}: the default icon needs up to 31 ASCII characters.");

            if (message.Strings.Count > StringSlots)
                yield return new(message.Strings, null, $"Message {i} has {message.Strings.Count} strings, this build has room for {StringSlots}.");

            foreach (HudMessageString text in message.Strings)
            {
                if (!Fits(text.macStringId, 64))
                    yield return new(text, nameof(HudMessageString.macStringId), $"Message {i}: string IDs need up to 63 ASCII characters.");

                if (text.Params().SkipWhile(p => p != HudMessageParamTypes.E_HUDMESSAGEPARAMTYPES_UNUSED).Any(p => p != HudMessageParamTypes.E_HUDMESSAGEPARAMTYPES_UNUSED))
                    yield return new(text, nameof(HudMessageString.Param1), $"Message {i}: {text} has a parameter after an unused one, the game stops at the first unused one.");
            }
        }
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        byte[] data = entry.GetPrimaryWithoutImports();
        using BinaryReader2 br = new(new MemoryStream(data), entry.Console);
        // 64 bit
        br.BaseStream.Position = 4;
        int size32 = br.ReadInt32(), size64 = br.ReadInt32();
        PointerSize = size32 == data.Length
            ? 4
            : 8;
        OnePerResource = size32 != data.Length && size64 != data.Length;
        List<long> records = [0];

        long pointer() => PointerSize == 8
            ? br.ReadInt64()
            : br.ReadUInt32();

        if (!OnePerResource)
        {
            br.BaseStream.Position = 0;
            long table = pointer();
            br.BaseStream.Position = PointerSize + 4;
            int count = br.ReadInt32();
            br.BaseStream.Position = table;
            records = [.. Enumerable.Range(0, count).Select(_ => pointer())];
        }

        RecordSize = records.Count switch
        {
            0 => 0x170,
            1 => data.Length - (int)records[0],
            _ => (int)(records[1] - records[0])
        };
        StringSlots = RecordSize >= FieldsSize(3)
            ? 3
            : 2;

        if (RecordSize < FieldsSize(StringSlots))
            throw new ReadFailedError($"HUD messages are {RecordSize} bytes each, too small for either known layout.");

        Messages = [];

        foreach (long record in records)
        {
            br.BaseStream.Position = record;
            string[] strings = [.. Enumerable.Range(0, StringSlots).Select(_ => br.ReadLenString(64))];
            HudMessage message = new()
            {
                macMessageStyle = br.ReadLenString(32),
                macDefaultIcon = br.ReadLenString(32),
                macMessageId = br.ReadLenString(13)
            };

            br.Align(8);
            br.ReadUInt64(); // always the message ID's CgsID
            message.muAvailabilityBitSet = br.ReadUInt32();
            message.mfDuration = br.ReadSingle();
            message.mfTimeToWait = br.ReadSingle();
            message.miPriority = br.ReadInt32();
            message.miForceRemoveThreshold = br.ReadInt32();
            message.meMessageGroup = (HudMessageGroup)br.ReadInt32();
            br.BaseStream.Position += 4 * StringSlots; // always the number of params in use
            HudMessageParamTypes[] parameters = [.. Enumerable.Range(0, StringSlots * 4).Select(_ => (HudMessageParamTypes)br.ReadInt32())];

            message.Strings = [.. strings.Select((text, s) => new HudMessageString
            {
                macStringId = text,
                Param1 = parameters[s * 4],
                Param2 = parameters[s * 4 + 1],
                Param3 = parameters[s * 4 + 2],
                Param4 = parameters[s * 4 + 3]
            })];

            // Unused slots at the end are added back when saving
            while (message.Strings.Count > 0 && message.Strings[^1].IsEmpty())
                message.Strings.RemoveAt(message.Strings.Count - 1);

            Messages.Add(message);
        }

        return true;
    }

    public byte[] Serialize(BundleEntry entry)
    {
        if (Problems().FirstOrDefault() is TrafficRules.Issue problem)
            throw new InvalidDataException(problem.Message);

        int recordSize = RecordSize;
        int first = OnePerResource
            ? 0
            : Align(TableOffset + Messages.Count * PointerSize, entry.Platform == BundlePlatform.PCx64
                ? 0x10
                : 0x80);

        using MemoryStream ms = new();
        using BinaryWriter2 bw = new(ms, entry.Console);

        void pointer(long value)
        {
            if (PointerSize == 8)
                bw.Write(value);
            else
                bw.Write((uint)value);
        }

        if (!OnePerResource)
        {
            pointer(TableOffset);
            bw.Write(first + Messages.Count * recordSize);
            bw.Write(Messages.Count);
            ms.SetLength(TableOffset);
            ms.Position = TableOffset;

            for (int i = 0; i < Messages.Count; i++)
                pointer(first + i * recordSize);

            ms.SetLength(first);
            ms.Position = first;
        }

        HudMessageString none = new();

        foreach (HudMessage message in Messages)
        {
            long start = ms.Position;
            HudMessageString[] strings = [.. message.Strings.Concat(Enumerable.Repeat(none, StringSlots - message.Strings.Count))];

            foreach (HudMessageString text in strings)
                bw.WriteLenString(text.macStringId, 64);

            bw.WriteLenString(message.macMessageStyle, 32);
            bw.WriteLenString(message.macDefaultIcon, 32);
            bw.WriteLenString(message.macMessageId, 13);
            bw.Align(8);
            bw.Write(message.mMessageIdHash);
            bw.Write(message.muAvailabilityBitSet);
            bw.Write(message.mfDuration);
            bw.Write(message.mfTimeToWait);
            bw.Write(message.miPriority);
            bw.Write(message.miForceRemoveThreshold);
            bw.Write((int)message.meMessageGroup);

            foreach (HudMessageString text in strings)
                bw.Write(text.Params().Count(p => p != HudMessageParamTypes.E_HUDMESSAGEPARAMTYPES_UNUSED));

            foreach (HudMessageParamTypes parameter in strings.SelectMany(s => s.Params()))
                bw.Write((int)parameter);

            bw.Flush();
            ms.SetLength(start + recordSize);
            ms.Position = start + recordSize;
        }

        bw.Flush();
        return ms.ToArray();
    }

    public bool Write(BundleEntry entry)
    {
        entry.SetPrimary(Serialize(entry), entry.GetImports());
        return true;
    }

    public EntryType GetEntryType(BundleEntry entry) => EntryType.HudMessage;

    public IEntryEditor GetEditor(BundleEntry entry) => new HudMessagesEditor(this, entry);
}

public sealed class HudMessagesEditor : ResourceEditor
{
    private readonly HudMessages data;

    public HudMessagesEditor(HudMessages source, BundleEntry initialEntry) : base(initialEntry, "HUD messages", "HUD message")
    {
        data = (HudMessages)ObjectTreeEditor.Clone(source);
        TrafficLabels.Install();
        Initialize(data, "HudMessages", null);
        Tree.SearchPrompt = "Search message IDs or string IDs";
        Tree.CollectionLimit = (_, list) => list switch
        {
            nameof(HudMessages.HudMessage.Strings) => data.StringSlots,
            _ => data.OnePerResource
                ? 1
                : int.MaxValue
        };
    }

    protected override List<TrafficRules.Issue> FindIssues() => [.. data.Problems()];

    protected override void WriteResource() => data.Write(ResourceEntry);
}
