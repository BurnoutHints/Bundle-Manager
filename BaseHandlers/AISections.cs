using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

// BrnAI::AISectionsData, https://burnout.wiki/wiki/AI_Data/Burnout_Paradise
public class AISections : IEntryData
{
    public enum SectionSpeed : byte
    {
        E_SECTION_SPEED_VERY_SLOW = 0,
        E_SECTION_SPEED_SLOW = 1,
        E_SECTION_SPEED_NORMAL = 2,
        E_SECTION_SPEED_FAST = 3,
        E_SECTION_SPEED_VERY_FAST = 4
    }

    public enum EResetSpeedType : uint
    {
        E_RESET_SPEED_TYPE_CUSTOM = 0,
        E_RESET_SPEED_TYPE_NONE = 1,
        E_RESET_SPEED_TYPE_SLOW = 2,
        E_RESET_SPEED_TYPE_FAST = 3,
        E_RESET_SPEED_TYPE_SLOW_NORTH_FACE = 4,
        E_RESET_SPEED_TYPE_SLOW_SOUTH_FACE = 5,
        E_RESET_SPEED_TYPE_SLOW_EAST_FACE = 6,
        E_RESET_SPEED_TYPE_SLOW_WEST_FACE = 7,
        E_RESET_SPEED_TYPE_SLOW_REVERSE = 8,
        E_RESET_SPEED_TYPE_STOP_REVERSE = 9,
        E_RESET_SPEED_TYPE_STOP_NORTH_FACE = 10,
        E_RESET_SPEED_TYPE_STOP_SOUTH_FACE = 11,
        E_RESET_SPEED_TYPE_STOP_EAST_FACE = 12,
        E_RESET_SPEED_TYPE_STOP_WEST_FACE = 13,
        E_RESET_SPEED_TYPE_STOP_NORTH_EAST_FACE = 14,
        E_RESET_SPEED_TYPE_STOP_SOUTH_WEST_FACE = 15,
        E_RESET_SPEED_TYPE_NONE_AND_IGNORE = 16,
        E_RESET_SPEED_TYPE_WEST_AND_IGNORE = 17,
        E_RESET_SPEED_TYPE_REVERSE_AND_IGNORE = 18,
        E_RESET_SPEED_TYPE_REVERSE_AND_IGNORE_SLOW = 19
    }

    public enum DangerRating : byte
    {
        E_DANGER_RATING_FREEWAY = 0,
        E_DANGER_RATING_NORMAL = 1,
        E_DANGER_RATING_DANGEROUS = 2
    }

    public enum EDistrict : byte
    {
        E_DISTRICT_SUBURBS = 0,
        E_DISTRICT_INDUSTRIAL = 1,
        E_DISTRICT_COUNTRY = 2,
        E_DISTRICT_CITY = 3,
        E_DISTRICT_AIRPORT = 4
    }

    private sealed class SectionBlocks
    {
        public readonly Traffic.FlatWriter.Block Portals = new(), NoGo = new(), Corners = new();
    }

    private const int MaxSections = 20000, MaxPortals = 32, DevelopmentSectionSize = 0x34, DevelopmentPortalSize = 0x20;

    // KX_SECTION_FLAG_* definitions
    public static readonly (string Name, byte Mask, string Help)[] Flags =
    [
        ("Shortcut", 0x01, "KX_SECTION_FLAG_SHORTCUT. Resets on track don't use this section."),
        ("No reset", 0x02, "KX_SECTION_FLAG_NO_RESET. No reset on track here."),
        ("In air", 0x04, "KX_SECTION_FLAG_IN_AIR. Usually just before a jump."),
        ("Split", 0x08, "KX_SECTION_FLAG_SPLIT. The road splits here."),
        ("Junction", 0x10, "KX_SECTION_FLAG_JUNCTION."),
        ("Terminator", 0x20, "KX_SECTION_FLAG_TERMINATOR. Walls and the like, not always the end of the road."),
        ("AI shortcut", 0x40, "KX_SECTION_FLAG_AI_SHORTCUT. Resets on track don't use this section."),
        ("Interstate exit", 0x80, "KX_SECTION_FLAG_AI_INTERSTATE_EXIT.")
    ];

    public static readonly (string Name, byte Mask, string Help)[] DevelopmentFlags =
    [
        ("In air", 0x01, "KX_SECTION_FLAGS_IS_IN_AIR."),
        ("Shortcut", 0x02, "KX_SECTION_FLAGS_IS_SHORTCUT."),
        ("Junction", 0x04, "KX_SECTION_FLAGS_IS_JUNCTION.")
    ];

    public class Portal
    {
        [Category("Geometry"), Description("mPositionX, mPositionY and mPositionZ.")]
        public Vector3 mPosition { get; set; }

        [Category("Geometry"), Description("BoundaryLine: start X, start Z, end X, end Z. The gap through this portal, the game needs at least one.")]
        public Vector4[] maBoundaryLines { get; set; } = [];

        [Category("Connections"), Description("The section this portal leads to, from mu16LinkSection."), TypeConverter(typeof(TrafficReferenceConverter))]
        public AISection mLinkSection { get; set; }

        public override string ToString() => mLinkSection == null
            ? "Leads nowhere"
            : $"To {mLinkSection.Name}";
    }

    public class AISection
    {
        [Browsable(false)] public List<Portal> maPortals { get; set; } = [];

        [Category("Geometry"), Description("BoundaryLine: start X, start Z, end X, end Z. Walls the AI won't drive through.")]
        public Vector4[] maNoGoLines { get; set; } = [];

        [Category("Geometry"), Description("The four corners of the section's footprint, X and Z.")]
        public Vector2[] maCorners { get; set; } = new Vector2[4];

        [Category("Properties"), Description("AISectionId, this section's GameDB ID."), TypeConverter(typeof(HexConverter)), OnlyIn(development: false)]
        public uint mId { get; set; }

        [Category("Properties"), Description("Street Data span this section is on, -1 for none.")]
        public int miSpanIndex { get; set; } = -1;

        [Category("Properties"), Description("Which of the speed bands applies here."), OnlyIn(development: false)]
        public SectionSpeed muSpeed { get; set; } = SectionSpeed.E_SECTION_SPEED_NORMAL;

        [Category("Properties"), Description("Picks this section's estimated speed."), OnlyIn(development: true)]
        public DangerRating muDangerRating { get; set; }

        [Category("Advanced"), Description("District, always 0 in the game's data.")]
        public EDistrict mu8eDistrict { get; set; }

        [Category("Advanced"), Description("The flags below, as stored.")]
        public byte mx8Flags { get; set; }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Name => TrafficLabels.Development
            ? $"{TrafficReferences.AIData?.maSections.IndexOf(this)}"
            : $"0x{mId:X}";

        private static string Words(Enum value, string prefix) => value.ToString().Replace(prefix, "").Replace('_', ' ').ToLowerInvariant();

        public override string ToString() => TrafficLabels.Development
            ? $"{Words(muDangerRating, "E_DANGER_RATING_")}, {maPortals.Count} portals"
            : $"{Name}, {Words(muSpeed, "E_SECTION_SPEED_")}, {maPortals.Count} portals";
    }

    public class SectionResetPair
    {
        [Category("Properties"), Description("meResetSpeed.")]
        public EResetSpeedType meResetSpeed { get; set; }

        [Category("Connections"), Description("Where the car is, from muStartSectionIndex."), TypeConverter(typeof(TrafficReferenceConverter))]
        public AISection mStartSection { get; set; }

        [Category("Connections"), Description("Where the reset puts it, from muResetSectionIndex."), TypeConverter(typeof(TrafficReferenceConverter))]
        public AISection mResetSection { get; set; }

        public override string ToString() => $"{mStartSection?.Name ?? "?"} resets to {mResetSection?.Name ?? "?"}";
    }

    [ReadOnly(true), Category("Properties"), Description("12 on every released version of the game.")]
    public uint muVersion { get; set; } = 12;

    // BND1 development layout: https://burnout.wiki/wiki/AI_Data/Burnout_Paradise/Development
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Development { get; set; }

    [Category("Properties"), Description("Minimum speed for each band: very slow, slow, normal, fast, very fast."), TypeConverter(typeof(ArrayTextConverter)), OnlyIn(development: false)]
    public float[] mafSectionMinSpeeds { get; set; } = new float[5];

    [Category("Properties"), Description("Maximum speed for each band: very slow, slow, normal, fast, very fast."), TypeConverter(typeof(ArrayTextConverter)), OnlyIn(development: false)]
    public float[] mafSectionMaxSpeeds { get; set; } = new float[5];

    [Browsable(false)] public List<AISection> maSections { get; set; } = [];

    [Browsable(false)] public List<SectionResetPair> maSectionResetPairs { get; set; } = [];

    public IEntryEditor GetEditor(BundleEntry entry) => new AISectionsEditor(this, entry);

    public EntryType GetEntryType(BundleEntry entry) => EntryType.AISections;

    private static int PointerSize(BundleEntry entry) => entry.Platform == BundlePlatform.PCx64
        ? 8
        : 4;

    private static Vector4[] Lines(BinaryReader2 br, long offset, int count)
    {
        long back = br.BaseStream.Position;
        br.BaseStream.Position = offset;
        Vector4[] items = new Vector4[count];

        for (int i = 0; i < count; i++)
            items[i] = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        br.BaseStream.Position = back;
        return items;
    }

    private AISection SectionAt(int index) => index < maSections.Count
        ? maSections[index]
        : throw new ReadFailedError($"AI sections refer to section {index}, there are only {maSections.Count}.");

    private void LinkPortals(List<int[]> links)
    {
        for (int s = 0; s < links.Count; s++)
            for (int p = 0; p < links[s].Length; p++)
                maSections[s].maPortals[p].mLinkSection = SectionAt(links[s][p]);
    }

    private void ReadDevelopment(BinaryReader2 br)
    {
        long sectionsAt = br.ReadUInt32();
        int sectionCount = br.ReadInt32();
        muVersion = br.ReadUInt32();
        maSections = [];
        maSectionResetPairs = [];
        List<int[]> links = new(sectionCount);

        for (int s = 0; s < sectionCount; s++)
        {
            br.BaseStream.Position = sectionsAt + s * DevelopmentSectionSize;
            long portalsAt = br.ReadUInt32(), noGoAt = br.ReadUInt32();

            // Feb 2007 retains version 4; distinguished from Nov 2006 by section stride
            if (s == 0 && portalsAt != ((sectionsAt + sectionCount * DevelopmentSectionSize + 15) & -16))
                throw new ReadFailedError("Only AI sections from the Feb 2007 builds can be read from a BND1 bundle.");

            AISection section = new();
            float[] cornersX = [br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()];

            for (int c = 0; c < 4; c++)
                section.maCorners[c] = new(cornersX[c], br.ReadSingle());

            section.miSpanIndex = br.ReadInt32();
            int noGoCount = br.ReadUInt16(), portalCount = br.ReadByte();
            section.muDangerRating = (DangerRating)br.ReadByte();
            section.mu8eDistrict = (EDistrict)br.ReadByte();
            section.mx8Flags = br.ReadByte();
            section.maNoGoLines = Lines(br, noGoAt, noGoCount);
            int[] portalLinks = new int[portalCount];

            for (int p = 0; p < portalCount; p++)
            {
                br.BaseStream.Position = portalsAt + p * DevelopmentPortalSize;
                Portal portal = new()
                {
                    mPosition = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle())
                };

                br.ReadSingle();
                long linesAt = br.ReadUInt32();
                portalLinks[p] = br.ReadUInt16();
                portal.maBoundaryLines = Lines(br, linesAt, br.ReadByte());
                section.maPortals.Add(portal);
            }

            maSections.Add(section);
            links.Add(portalLinks);
        }

        LinkPortals(links);
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        using BinaryReader2 br = new(new MemoryStream(entry.GetPrimaryWithoutImports()), entry.Console);
        Development = entry.Archive?.BNDVersion == 1;

        if (Development)
        {
            ReadDevelopment(br);
            return true;
        }

        int pointerSize = PointerSize(entry);

        long ptr()
        {
            br.BaseStream.Position = (br.BaseStream.Position + pointerSize - 1) & -pointerSize;
            return pointerSize == 8
                ? (long)br.ReadUInt64()
                : br.ReadUInt32();
        }

        long sectionsAt = ptr(), pairsAt = ptr();

        for (int i = 0; i < 5; i++)
            mafSectionMinSpeeds[i] = br.ReadSingle();

        for (int i = 0; i < 5; i++)
            mafSectionMaxSpeeds[i] = br.ReadSingle();

        int sectionCount = br.ReadInt32(), pairCount = br.ReadInt32();
        muVersion = br.ReadUInt32();

        if (muVersion != 12)
            throw new ReadFailedError($"AI sections version {muVersion} isn't supported, only 12 is.");

        maSections = [];
        maSectionResetPairs = [];
        br.BaseStream.Position = sectionsAt;
        List<int[]> links = new(sectionCount);

        for (int s = 0; s < sectionCount; s++)
        {
            long portalsAt = ptr(), noGoAt = ptr(), cornersAt = ptr();
            AISection section = new()
            {
                mId = br.ReadUInt32(),
                miSpanIndex = br.ReadInt16()
            };

            int noGoCount = br.ReadUInt16(), portalCount = br.ReadByte();
            section.muSpeed = (SectionSpeed)br.ReadByte();
            section.mu8eDistrict = (EDistrict)br.ReadByte();
            section.mx8Flags = br.ReadByte();
            section.maNoGoLines = Lines(br, noGoAt, noGoCount);
            long next = br.BaseStream.Position;
            br.BaseStream.Position = cornersAt;

            for (int c = 0; c < 4; c++)
                section.maCorners[c] = new(br.ReadSingle(), br.ReadSingle());

            br.BaseStream.Position = portalsAt;
            int[] portalLinks = new int[portalCount];

            for (int p = 0; p < portalCount; p++)
            {
                Portal portal = new()
                {
                    mPosition = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle())
                };

                long linesAt = ptr();
                portalLinks[p] = br.ReadUInt16();
                int lineCount = br.ReadByte();
                br.ReadByte();
                br.BaseStream.Position = (br.BaseStream.Position + pointerSize - 1) & -pointerSize;
                portal.maBoundaryLines = Lines(br, linesAt, lineCount);
                section.maPortals.Add(portal);
            }

            br.BaseStream.Position = next;
            maSections.Add(section);
            links.Add(portalLinks);
        }

        LinkPortals(links);
        br.BaseStream.Position = pairsAt;

        for (int i = 0; i < pairCount; i++)
        {
            SectionResetPair pair = new()
            {
                meResetSpeed = (EResetSpeedType)br.ReadUInt32(),
                mStartSection = SectionAt(br.ReadUInt16()),
                mResetSection = SectionAt(br.ReadUInt16())
            };

            maSectionResetPairs.Add(pair);
        }

        return true;
    }

    // Validates constraints expected by the game engine
    public IEnumerable<TrafficRules.Issue> Problems()
    {
        if (maSections.Count > MaxSections)
            yield return new(maSections, null, $"There are {maSections.Count} sections, the game has room for {MaxSections}.");

        HashSet<AISection> sections = [.. maSections];

        foreach (AISection section in maSections)
        {
            if (section.maPortals.Count is 0 or > MaxPortals)
                yield return new(section, nameof(AISection.maPortals), $"Section {section.Name} has {section.maPortals.Count} portals, it needs from 1 to {MaxPortals}. The game takes a section's height from its first portal.");

            if (section.maCorners?.Length != 4)
                yield return new(section, nameof(AISection.maCorners), $"Section {section.Name} needs exactly 4 corners.");

            if (!Enum.IsDefined(section.muSpeed))
                yield return new(section, nameof(AISection.muSpeed), $"Section {section.Name} has a speed that isn't one of the 5 bands, the game looks it up in them.");

            if (!Enum.IsDefined(section.muDangerRating))
                yield return new(section, nameof(AISection.muDangerRating), $"Section {section.Name} has a danger rating that isn't one of the 3, the game looks its speed up by it.");

            if (!Development && section.miSpanIndex is < short.MinValue or > short.MaxValue)
                yield return new(section, nameof(AISection.miSpanIndex), $"Section {section.Name} has a span index the format can't hold.");

            if (section.maNoGoLines.Length > ushort.MaxValue)
                yield return new(section, nameof(AISection.maNoGoLines), $"Section {section.Name} has more no-go lines than the format can hold.");

            foreach (Portal portal in section.maPortals)
            {
                if (portal.maBoundaryLines.Length is 0 or > byte.MaxValue)
                    yield return new(portal, nameof(Portal.maBoundaryLines), $"A portal of section {section.Name} has {portal.maBoundaryLines.Length} boundary lines, it needs from 1 to 255. The game reads the first one without checking.");

                if (portal.mLinkSection == null || !sections.Contains(portal.mLinkSection))
                    yield return new(portal, nameof(Portal.mLinkSection), $"A portal of section {section.Name} doesn't lead to a section.");
            }
        }

        if (mafSectionMinSpeeds.Length != 5 || mafSectionMaxSpeeds.Length != 5)
            yield return new(this, nameof(mafSectionMinSpeeds), "There have to be exactly 5 speed bands.");

        foreach (SectionResetPair pair in maSectionResetPairs)
        {
            if (pair.mStartSection == null || !sections.Contains(pair.mStartSection))
                yield return new(pair, nameof(SectionResetPair.mStartSection), "A reset pair needs a start section.");

            if (pair.mResetSection == null || !sections.Contains(pair.mResetSection))
                yield return new(pair, nameof(SectionResetPair.mResetSection), "A reset pair needs a section to reset to.");
        }
    }

    public byte[] Serialize(BundleEntry entry) => Serialize(entry.Console, PointerSize(entry));

    public byte[] Serialize(bool bigEndian, int pointerSize)
    {
        if (Problems().FirstOrDefault() is TrafficRules.Issue problem)
            throw new InvalidDataException(problem.Message);

        Traffic.FlatWriter w = new(bigEndian, pointerSize);
        BinaryWriter2 bw = w.Bw;
        Dictionary<AISection, int> indexes = maSections.Select((section, i) => (section, i)).ToDictionary(p => p.section, p => p.i);
        Traffic.FlatWriter.Block sectionsBlock = new(), pairsBlock = new();
        w.Ptr(sectionsBlock);

        if (!Development)
        {
            w.Ptr(pairsBlock);

            foreach (float speed in mafSectionMinSpeeds.Concat(mafSectionMaxSpeeds))
                bw.Write(speed);
        }

        bw.Write(maSections.Count);

        if (!Development)
            bw.Write(maSectionResetPairs.Count);

        bw.Write(muVersion);
        long sizeAt = bw.BaseStream.Position;
        bw.Write(0u);
        w.Start(sectionsBlock);
        List<SectionBlocks> blocks = [.. maSections.Select(_ => new SectionBlocks())];

        for (int s = 0; s < maSections.Count; s++)
        {
            AISection section = maSections[s];
            w.Ptr(blocks[s].Portals);
            w.Ptr(blocks[s].NoGo);

            if (Development)
            {
                foreach (Vector2 corner in section.maCorners)
                    bw.Write(corner.X);

                foreach (Vector2 corner in section.maCorners)
                    bw.Write(corner.Y);

                bw.Write(section.miSpanIndex);
            }
            else
            {
                w.Ptr(blocks[s].Corners);
                bw.Write(section.mId);
                bw.Write((short)section.miSpanIndex);
            }

            bw.Write((ushort)section.maNoGoLines.Length);
            bw.Write((byte)section.maPortals.Count);
            bw.Write(Development
                ? (byte)section.muDangerRating
                : (byte)section.muSpeed);
            bw.Write((byte)section.mu8eDistrict);
            bw.Write(section.mx8Flags);

            if (Development)
                bw.Write((ushort)0);
        }

        for (int s = 0; s < maSections.Count; s++)
        {
            AISection section = maSections[s];
            w.Start(blocks[s].Portals);
            List<Traffic.FlatWriter.Block> lines = [.. section.maPortals.Select(_ => new Traffic.FlatWriter.Block())];

            for (int p = 0; p < section.maPortals.Count; p++)
            {
                Portal portal = section.maPortals[p];
                bw.Write(portal.mPosition.X);
                bw.Write(portal.mPosition.Y);
                bw.Write(portal.mPosition.Z);

                if (Development)
                    bw.Write(0f);

                w.Ptr(lines[p]);
                bw.Write((ushort)indexes[portal.mLinkSection]);
                bw.Write((byte)portal.maBoundaryLines.Length);
                bw.Write((byte)0);
                w.Pad(Development
                    ? 16
                    : pointerSize);
            }

            for (int p = 0; p < section.maPortals.Count; p++)
            {
                w.Start(lines[p]);

                foreach (Vector4 line in section.maPortals[p].maBoundaryLines)
                    w.V4(line);
            }

            w.Start(blocks[s].NoGo);

            foreach (Vector4 line in section.maNoGoLines)
                w.V4(line);

            if (Development)
                continue;

            w.Start(blocks[s].Corners);

            foreach (Vector2 corner in section.maCorners)
            {
                bw.Write(corner.X);
                bw.Write(corner.Y);
            }
        }

        w.Start(pairsBlock);

        foreach (SectionResetPair pair in maSectionResetPairs)
        {
            bw.Write((uint)pair.meResetSpeed);
            bw.Write((ushort)indexes[pair.mStartSection]);
            bw.Write((ushort)indexes[pair.mResetSection]);
        }

        // muSizeInBytes excludes terminal padding
        uint size = (uint)bw.BaseStream.Position;
        w.Finish();
        bw.BaseStream.Position = sizeAt;
        bw.Write(size);
        return ((MemoryStream)bw.BaseStream).ToArray();
    }

    public bool Write(BundleEntry entry)
    {
        entry.SetPrimary(Serialize(entry), entry.GetImports());
        return true;
    }
}
