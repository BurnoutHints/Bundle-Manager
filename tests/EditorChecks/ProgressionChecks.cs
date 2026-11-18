using System.Buffers.Binary;
using System.ComponentModel;
using System.Text;
using BaseHandlers;
using BundleFormat;
using BundleUtilities;

internal static partial class Program
{
    private static string CgsID(ulong id) => TrafficCgsIDConverter.Label(id);

    private static void CheckProgression()
    {
        (BundlePlatform Platform, uint Version)[] layouts =
        [
            (BundlePlatform.PC, 43),
            (BundlePlatform.X360, 43),
            (BundlePlatform.PS3, 43),
            (BundlePlatform.PS3, 42),
            (BundlePlatform.PC, 44),
            (BundlePlatform.PCx64, 44),
            (BundlePlatform.PCx64, 43)
        ];

        foreach ((BundlePlatform platform, uint version) in layouts)
        {
            string label = $"Progression {platform} v{version}: ";
            bool wide = platform == BundlePlatform.PCx64 && version == 44;
            byte[] bytes = ProgressionFixture(platform, version);
            byte[] original = [.. bytes];
            BundleEntry entry = Entry(bytes);
            entry.Platform = platform;
            ProgressionData data = new();
            Check(data.Read(entry), label + "reads documented layout");
            Check(data.muVersionNumber == version && data.muSize == bytes.Length - 8 && data.Wide == wide &&
                data.mpaPlayerCarIds.Count == 1 && data.mpaProgressionRanks.Count == 1 &&
                data.mpaEventJunctions.Count == 1 && data.mpaEvents.Count == 1 && data.mpaRivals.Count == 1 &&
                data.mpaAIBalances.Count == 1 && data.mpaPersonalities.Count == 1 &&
                data.mpaTrophyUnlocks.Count == 1 && data.mpaCarOpponentSet.Count == 1 &&
                CgsID(data.mpaPlayerCarIds.Single().mCarId) == "STARTERCAR", label + "header counts and starter CgsID");
            Check(data.BaseEventJunctionCount == (version == 44
                ? 120u
                : null), label + "optional remaster field");
            Check(data.PointerPadding.SequenceEqual(wide
                ? Enumerable.Range(0, 8).Select(index => 0xAC000000u + (uint)index)
                : []), label + "header alignment words retained");

            ProgressionRankData rank = data.mpaProgressionRanks.Single();
            float[] rankSettings = [rank.mfTrafficDensityRace, rank.mfBurningRouteTimeScale,
                rank.mfTrafficDensityBurningRoute, rank.mfTrafficDensityRoadRage, rank.mfTrafficDensitySurvival,
                rank.mfTrafficDensityPursuit, rank.mfShuntStrengthRace, rank.mfShuntStrengthRoadRage,
                rank.mfShuntStrengthMarkedMan, rank.mfLargeVehicleProbability];
            Check(rankSettings.SequenceEqual(Enumerable.Range(0, 10).Select(index => index + 0.25f)) &&
                rank.muId == 0xFEDC1234 && rank.maOvertakingDifficulty.SequenceEqual(
                    Enumerable.Range(0, 8).Select(index => index + 10.25f)), label + "rank traffic, shunt and overtaking fields");
            ushort[] rankThresholds = [rank.mu16MedalThresholdToNextRank, rank.mu16EventThresholdToNextRank,
                rank.muRoadRageTakedownTarget, rank.muRoadRageTime, rank.muRoadRageTimeExtensions,
                rank.muRoadRageExtensionTime, rank.muRoadRageDamageLimit, rank.muRoadRageTriggerExtension];
            byte[] rankCounts = [rank.muRaceRivalsNumber, rank.muGauntletRivalsNumber, rank.muRoadRageRivalsNumber,
                rank.muNumGiftCars, rank.muNumWinsToRankUpRace, rank.muNumWinsToRankUpStunt,
                rank.muNumWinsToRankUpRoadRage, rank.muNumWinsToRankUpMarkedMan];
            Check(rankThresholds.SequenceEqual(Enumerable.Range(0, 8).Select(index => (ushort)(0x8000 + index))) &&
                rankCounts.SequenceEqual(Enumerable.Range(1, 8).Select(index => (byte)index)) &&
                (version == 42
                    ? rank.Padding64 == 0 && rank.mFreeCarForRankUpID == 0
                    : rank.Padding64 == 0xA1B2C3D4 && CgsID(rank.mFreeCarForRankUpID) == "RANKCAR"),
                label + (version == 42
                    ? "0x64-byte revision 42 rank stops before the free car"
                    : "unsigned rank thresholds, packed counts and gift car"));

            EventJunction junction = data.mpaEventJunctions.Single();
            RaceEventData race = data.mpaEvents.Single();
            Check(junction.muID == 0xF1234567 && junction.miShotGroup == -1 &&
                junction.mpOfflineEvent == race && junction.mpOnlineEvent == race &&
                junction.BikeEvent == (version == 44
                    ? race
                    : null) &&
                junction.Padding04 == (wide
                    ? 0xCAFE0001u
                    : 0) &&
                junction.Padding24 == (wide
                    ? 0xCAFE0002u
                    : 0), label + "junctions refer to the event objects, signed shot group and padding");
            Check(race.muId == 0x89ABCDEF && race.muFlags == 4 && race.mfTrafficDensity == 1.25f &&
                race.mfBoostEarning == 2.5f && CgsID(race.mSpecialEventCarId) == "EVENTCAR" &&
                race.mpaCheckpoints.Count == 1 &&
                race.mfTimeLimitFast == 30.5f && race.mfTimeLimitSlow == 45.5f && race.mfExtensionTime == 3.25f &&
                race.miRankScore.SequenceEqual(Enumerable.Range(1, 6).Select(index => index * 100)) &&
                race.mfRankTime.SequenceEqual(Enumerable.Range(60, 6).Select(index => (float)index)),
                label + "event IDs, checkpoints, scores and times");
            Check(race.muStartGridCount == 1 && race.maStartGridSlots.Count == 7 &&
                race.maStartGridSlots.Select((slot, index) => slot.muOpponentIndex == index &&
                    slot.muPersonalityIndex == index + 10 && slot.miFastAIBalanceGraphIndex == index - 1 &&
                    slot.miSlowAIBalanceGraphIndex == index - 2 && slot.muColourIndex == index + 3 &&
                    slot.muFlags == 2 && slot.Padding12 == 0xBEEF).All(matches => matches),
                label + "all seven start slots retained when only one is active");
            byte[] eventModes = [race.mu8Mode, race.mu8OnlineMode, race.mu8StartRivalCount,
                race.mu8AddRivalCount, race.mu8TakeDownBronze, race.mu8TakeDownSilver, race.mu8TakeDownGold,
                race.mu8DamageLimit, race.mu8ExtensionTimeCount, race.mu8AStarType];
            Check(eventModes.SequenceEqual(new byte[]
            {
                4,
                2,
                7,
                3,
                11,
                22,
                33,
                44,
                5,
                2
            }) &&
                race.mi8UnlockRank == -1 && race.Padding.SequenceEqual(Enumerable.Range(0, wide
                    ? 5
                    : 1)
                    .Select(index => (byte)(0xAC + index))), label + "individual event mode bytes and signed unlock rank");
            CheckpointData checkpoint = race.mpaCheckpoints.Single();
            Check(checkpoint.muLandmarkId == 0xF9988776 && checkpoint.miBlockSectionCount == 1 &&
                checkpoint.mauBlockSectionIds.SequenceEqual(Enumerable.Range(0, 8).Select(index => 0xA0000000u + (uint)index)),
                label + "checkpoint retains inactive block section IDs");

            Rival rival = data.mpaRivals.Single();
            Check(CgsID(rival.mId) == "RIVAL_ID" && CgsID(rival.mCarId) == "RIVALCAR" &&
                rival.miPersonalityIndex == -3 && rival.miPursuitTarget == -5 && rival.miDistrictIndex == -2 &&
                rival.miUnlockRank == -1 && rival.mu8NumMedalsToUnlock == 250 &&
                rival.mbIsUsedForRankUpGiftCar && rival.macName == "Rival fixture", label + "rival signed fields, flag, name and CgsIDs");
            OpponentBalanceData balance = data.mpaAIBalances.Single();
            Check(balance.mafAheadGraphPoints.SequenceEqual(Enumerable.Range(0, 8).Select(index => index + 0.125f)) &&
                balance.mafBehindGraphPoints.SequenceEqual(Enumerable.Range(8, 8).Select(index => index + 0.125f)) &&
                balance.mfCatchUpCutOffRatio == 0.8f, label + "AI graphs and console builder's little-endian cutoff");
            EventRacerPersonality personality = data.mpaPersonalities.Single();
            Check(personality.mfMinAggression == 0.1f && personality.mfMaxAggression == 0.9f &&
                personality.mfSkill == 0.7f && personality.mfSpeed == 0.8f, label + "personality fields");
            TrophyUnlockData trophy = data.mpaTrophyUnlocks.Single();
            Check(trophy.muNumberTrophyUnlock == 12 && trophy.mu16UnlockType == 6 &&
                trophy.Padding06 == 0xCAFE && CgsID(trophy.mCarUnlockId) == "TROPHYCAR", label + "trophy condition and vehicle");
            CarOpponentSet opponents = data.mpaCarOpponentSet.Single();
            Check(CgsID(opponents.mPlayerCarId) == "PLAYERCAR" && opponents.miRank == -4 && opponents.miOpponentCount == 2 &&
                opponents.maCarOpponents.Count == 8 && opponents.maCarOpponents.Select((opponent, index) =>
                    CgsID(opponent.mCarId) == $"OPP{index}" && opponent.miPersonalityIndex == 100 + index &&
                    opponent.Padding0C == 0xAABB0000u + (uint)index).All(matches => matches),
                label + "eight opponents precede the player car, including unused slots");

            Check(data.Read(entry) && data.mpaEvents.Count == 1 && data.mpaProgressionRanks.Count == 1 &&
                data.mpaRivals.Count == 1 && data.mpaCarOpponentSet.Count == 1,
                label + "reloading replaces decoded collections");

            if (wide)
            {
                Check(data.Problems().Any() && !data.Write(entry) && !entry.Dirty && entry.EntryBlocks[0].Data.SequenceEqual(original),
                    label + "native 64-bit progression is view-only and keeps its bytes");
                continue;
            }

            // Saving lays the tables out the way the game's builder does; a second save is identical
            Check(!data.Problems().Any() && data.Write(entry), label + "saves");
            byte[] saved = entry.EntryBlocks[0].Data;
            ProgressionData reread = new();
            reread.Read(entry);
            Check(ProgressionFingerprint(reread) == ProgressionFingerprint(data) && reread.Serialize(entry.Console).SequenceEqual(saved) &&
                saved.Length % 16 == 0 && reread.muSize <= saved.Length && saved.Length - reread.muSize < 16,
                label + "saved resource reads back the same and saves unchanged");

            uint word(int at) => entry.Console
                ? BinaryPrimitives.ReadUInt32BigEndian(saved.AsSpan(at))
                : BinaryPrimitives.ReadUInt32LittleEndian(saved.AsSpan(at));

            uint rankSize = version == 42
                ? 0x64u
                : 0x70u;
            uint first = data.BaseEventJunctionCount.HasValue
                ? 0x60u
                : 0x50u;
            Check(word(8) == first && word(24) - word(16) == ((rankSize + 15) & ~15u) &&
                Enumerable.Range(0, 9).All(table => word(8 + table * 8) % 16 == 0),
                label + "tables start 16-byte aligned straight after the header, ranks take their revision's size");
            entry.EntryBlocks[0].Data = original;
        }

        CheckProgressionConversion();
        CheckProgressionValidation();
        CheckProgressionEditor();
    }

    // Everything the model holds, in order, with junction links as event indices
    private static string ProgressionFingerprint(ProgressionData data)
    {
        StringBuilder text = new();

        void add(object value)
        {
            if (value == null)
            {
                text.Append("null;");
                return;
            }

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value))
            {
                object item = property.GetValue(value);
                text.Append(property.Name).Append('=');

                if (item is RaceEventData race && property.ComponentType != typeof(ProgressionData))
                    text.Append("event#").Append(data.mpaEvents.IndexOf(race));
                else if (item is System.Collections.IEnumerable items and not string)
                    foreach (object element in items)
                    {
                        if (element is null || element.GetType().IsPrimitive)
                            text.Append(element).Append(',');
                        else
                            add(element);
                    }
                else
                    text.Append(item);

                text.Append(';');
            }
        }

        foreach (System.Collections.IList list in (System.Collections.IList[])[data.mpaPlayerCarIds, data.mpaProgressionRanks, data.mpaEventJunctions,
            data.mpaEvents, data.mpaRivals, data.mpaAIBalances, data.mpaPersonalities, data.mpaTrophyUnlocks, data.mpaCarOpponentSet])
            foreach (object item in list)
                add(item);

        foreach (RaceEventData race in data.mpaEvents)
        {
            foreach (CheckpointData checkpoint in race.mpaCheckpoints)
                add(checkpoint);

            foreach (EventStartGridSlot slot in race.maStartGridSlots)
                add(slot);
        }

        return text.ToString();
    }

    // Revision 43 -> 42 drops each rank's padding and free car and keeps everything else
    private static void CheckProgressionConversion()
    {
        BundleEntry entry = Entry(ProgressionFixture(BundlePlatform.PS3, 43));
        entry.Platform = BundlePlatform.PS3;
        ProgressionData data = new();
        data.Read(entry);
        data.muVersionNumber = 42;
        Check(data.Write(entry), "Progression 43 -> 42: saves");
        ProgressionData converted = new();
        converted.Read(entry);
        ProgressionRankData rank = converted.mpaProgressionRanks.Single();
        data.mpaProgressionRanks.Single().Padding64 = 0;
        data.mpaProgressionRanks.Single().mFreeCarForRankUpID = 0;
        Check(converted.muVersionNumber == 42 && rank.muNumWinsToRankUpMarkedMan == 8 && rank.mFreeCarForRankUpID == 0 &&
            ProgressionFingerprint(converted) == ProgressionFingerprint(data) &&
            BinaryPrimitives.ReadUInt32BigEndian(entry.EntryBlocks[0].Data.AsSpan(24)) == 0x60 + 0x64 + 12,
            "Progression 43 -> 42: ranks shrink to 0x64 bytes, nothing else changes");

        data.mpaEventJunctions.Single().BikeEvent = data.mpaEvents.Single();
        Check(data.Problems().Any(p => p.Property == nameof(EventJunction.BikeEvent)), "Progression: bike events need revision 44");
    }

    private static void CheckProgressionEditor()
    {
        BundleEntry entry = Entry(ProgressionFixture(BundlePlatform.PS3, 42));
        entry.Platform = BundlePlatform.PS3;
        ProgressionData source = new();
        source.Read(entry);
        byte[] before = [.. entry.EntryBlocks[0].Data];
        using ProgressionEditor editor = (ProgressionEditor)source.GetEditor(entry);
        Hidden(editor);
        ProgressionData draft = Field<ProgressionData>(editor, "data");
        ObjectTreeEditor tree = Field<ObjectTreeEditor>(editor, "tree");
        RaceEventData race = draft.mpaEvents.Single();
        EventJunction junction = draft.mpaEventJunctions.Single();
        PropertyDescriptor offline = TypeDescriptor.GetProperties(junction)[nameof(EventJunction.mpOfflineEvent)];
        Check(junction.mpOfflineEvent == race && race != source.mpaEvents.Single() &&
            offline.Converter.ConvertToString(new PropertyContext(junction, offline), race).StartsWith("0: ") &&
            tree.CollectionLimit(race, nameof(RaceEventData.maStartGridSlots)) == 7,
            "Progression editor: junctions point at the draft's events and pick them by index; grids hold 7 slots");

        tree.SelectObject(race);
        editor.ClientSize = new(1000, 700);
        Application.DoEvents();
        Capture(editor, "progression-ps3-v42.png");

        race.mfTimeLimitFast = 99.5f;
        tree.RecordChange();
        Check(entry.EntryBlocks[0].Data.SequenceEqual(before) && (bool)Call(editor, "ApplyChanges"), "Progression editor: edits stay in the draft until Apply writes them");
        ProgressionData applied = new();
        applied.Read(entry);
        Check(applied.muVersionNumber == 42 && applied.mpaEvents.Single().mfTimeLimitFast == 99.5f && source.mpaEvents.Single().mfTimeLimitFast != 99.5f,
            "Progression editor: Apply writes the edited event as revision 42");

        race.maStartGridSlots.RemoveAt(6);
        tree.RecordChange();
        Check(Field<ListBox>(editor, "issues").Items.Count > 0 && !(bool)Call(editor, "ApplyChanges"),
            "Progression editor: an event without 7 grid slots is listed and blocks Apply");
    }

    private sealed class PropertyContext(object instance, PropertyDescriptor property) : ITypeDescriptorContext
    {
        public IContainer Container => null;

        public object Instance => instance;

        public PropertyDescriptor PropertyDescriptor => property;

        public object GetService(Type serviceType) => null;

        public void OnComponentChanged() { }

        public bool OnComponentChanging() => true;
    }

    private static void CheckProgressionValidation()
    {
        (int Offset, uint Value, string Description)[] malformed =
        [
            (0x2C, uint.MaxValue, "negative rival count"),
            (0x24, uint.MaxValue, "event table beyond resource"),
            (0x124, 0x151, "junction pointer inside an event"),
            (0x238, 8, "too many start grid slots"),
            (0x3C4, 9, "too many block sections"),
            (0, 8, "the February 2007 revision")
        ];

        foreach ((int offset, uint value, string description) in malformed)
        {
            byte[] bytes = ProgressionFixture(BundlePlatform.PC, 43);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
            byte[] original = [.. bytes];
            BundleEntry entry = Entry(bytes);
            ProgressionData data = new();
            data.Read(Entry(ProgressionFixture(BundlePlatform.PC, 43)));
            bool rejected = false;

            try
            {
                data.Read(entry);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Check(rejected && !entry.Dirty && entry.EntryBlocks[0].Data.SequenceEqual(original) && data.mpaEvents.Count == 1,
                $"Progression rejects {description} without changing source bytes or the loaded model");
        }

        byte[] empty = new byte[0x50];
        BinaryPrimitives.WriteUInt32LittleEndian(empty, 43);
        BinaryPrimitives.WriteUInt32LittleEndian(empty.AsSpan(8), uint.MaxValue);
        ProgressionData blank = new();
        Check(blank.Read(Entry(empty)) && blank.mpaPlayerCarIds.Count == 0,
            "Progression ignores unused pointers in empty tables");
    }

    private static byte[] ProgressionFixture(BundlePlatform platform, uint version)
    {
        // Absolute offsets matching wiki specification
        byte[] bytes = new byte[0x400];
        bool bigEndian = platform is BundlePlatform.X360 or BundlePlatform.PS3;
        bool wide = platform == BundlePlatform.PCx64 && version == 44;

        void write32(int offset, uint value)
        {
            if (bigEndian)
                BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset), value);
            else
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
        }

        void write16(int offset, ushort value)
        {
            if (bigEndian)
                BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset), value);
            else
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), value);
        }

        void write64(int offset, ulong value)
        {
            if (bigEndian)
                BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(offset), value);
            else
                BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(offset), value);
        }

        void writePointer(int offset, uint value)
        {
            if (wide)
                write64(offset, value);
            else
                write32(offset, value);
        }

        void writeFloat(int offset, float value) => write32(offset, BitConverter.SingleToUInt32Bits(value));

        void writeCgsID(int offset, string value) => write64(offset, new EncryptedString(value).Encrypted);

        write32(0, version);
        write32(4, (uint)bytes.Length - 8);
        uint[] tableOffsets = [0xA0, 0xB0, 0x120, 0x150, 0x250, 0x290, 0x2E0, 0x300, 0x320];

        for (int index = 0; index < tableOffsets.Length; index++)
        {
            int offset = 8 + index * (wide
                ? 16
                : 8);
            writePointer(offset, tableOffsets[index]);
            write32(offset + (wide
                ? 8
                : 4), 1);

            if (wide && index < 8)
                write32(offset + 12, 0xAC000000u + (uint)index);
        }

        if (version == 44)
            write32(wide
                ? 0x94
                : 0x50, 120);

        writeCgsID(0xA0, "STARTERCAR");

        for (int index = 0; index < 10; index++)
            writeFloat(0xB0 + index * 4, index + 0.25f);

        write32(0xD8, 0xFEDC1234);

        for (int index = 0; index < 8; index++)
        {
            writeFloat(0xDC + index * 4, index + 10.25f);
            write16(0xFC + index * 2, (ushort)(0x8000 + index));
            bytes[0x10C + index] = (byte)(index + 1);
        }

        // Revision 42 ranks end at 0x114, so these are only rank fields from 43 on
        write32(0x114, 0xA1B2C3D4);
        writeCgsID(0x118, "RANKCAR");
        write32(0x120, 0xF1234567);
        writePointer(wide
            ? 0x128
            : 0x124, 0x150);
        writePointer(wide
            ? 0x130
            : 0x128, 0x150);

        if (version == 44)
            writePointer(wide
                ? 0x138
                : 0x12C, 0x150);

        write32(wide
            ? 0x140
            : version == 44
                ? 0x130
                : 0x12C, uint.MaxValue);

        if (wide)
        {
            write32(0x124, 0xCAFE0001);
            write32(0x144, 0xCAFE0002);
        }

        write32(0x150, 0x89ABCDEF);
        write32(0x154, 4);
        writeFloat(0x158, 1.25f);
        writeFloat(0x15C, 2.5f);
        writeCgsID(0x160, "EVENTCAR");
        writePointer(0x168, 0x3C0);
        int shift = wide
            ? 4
            : 0;
        write32(0x16C + shift, 1);
        writeFloat(0x170 + shift, 30.5f);
        writeFloat(0x174 + shift, 45.5f);

        for (int index = 0; index < 6; index++)
        {
            write32(0x178 + shift + index * 4, (uint)(index + 1) * 100);
            writeFloat(0x190 + shift + index * 4, 60 + index);
        }

        writeFloat(0x1A8 + shift, 3.25f);

        for (int index = 0; index < 7; index++)
        {
            int offset = 0x1AC + shift + index * 20;
            write32(offset, (uint)index);
            write32(offset + 4, (uint)index + 10);
            write32(offset + 8, unchecked((uint)(index - 1)));
            write32(offset + 12, unchecked((uint)(index - 2)));
            bytes[offset + 16] = (byte)(index + 3);
            bytes[offset + 17] = 2;
            write16(offset + 18, 0xBEEF);
        }

        write32(0x238 + shift, 1);
        byte[] modes = [4, 2, 7, 3, 11, 22, 33, 44, 5, 2, 255];
        modes.CopyTo(bytes, 0x23C + shift);

        for (int index = 0; index < (wide
            ? 5
            : 1); index++)
            bytes[0x247 + shift + index] = (byte)(0xAC + index);

        writeCgsID(0x250, "RIVAL_ID");
        writeCgsID(0x258, "RIVALCAR");
        write16(0x260, unchecked((ushort)-3));
        write16(0x262, unchecked((ushort)-5));
        bytes[0x264] = 254;
        bytes[0x265] = 255;
        bytes[0x266] = 250;
        bytes[0x267] = 1;
        Encoding.Latin1.GetBytes("Rival fixture").CopyTo(bytes, 0x268);

        for (int index = 0; index < 16; index++)
            writeFloat(0x290 + index * 4, index + 0.125f);

        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(0x2D0), 0.8f);
        writeFloat(0x2E0, 0.1f);
        writeFloat(0x2E4, 0.9f);
        writeFloat(0x2E8, 0.7f);
        writeFloat(0x2EC, 0.8f);
        write32(0x300, 12);
        write16(0x304, 6);
        write16(0x306, 0xCAFE);
        writeCgsID(0x308, "TROPHYCAR");

        for (int index = 0; index < 8; index++)
        {
            int offset = 0x320 + index * 16;
            writeCgsID(offset, $"OPP{index}");
            write32(offset + 8, (uint)(100 + index));
            write32(offset + 12, 0xAABB0000u + (uint)index);
        }

        writeCgsID(0x3A0, "PLAYERCAR");
        write32(0x3A8, unchecked((uint)-4));
        write32(0x3AC, 2);
        write32(0x3C0, 0xF9988776);
        write32(0x3C4, 1);

        for (int index = 0; index < 8; index++)
            write32(0x3C8 + index * 4, 0xA0000000u + (uint)index);

        return bytes;
    }

    private static void CheckProgressionFiles(string[] args)
    {
        // Optional local fixtures: raw ProgressionData blocks, saved unchanged byte for byte
        foreach (string argument in args.Where(argument => argument.StartsWith("--progression-file=")))
        {
            string path = argument["--progression-file=".Length..];
            byte[] bytes = File.ReadAllBytes(path);
            byte[] original = [.. bytes];
            BundleEntry entry = Entry(bytes);
            entry.Platform = path.Contains("PS3", StringComparison.OrdinalIgnoreCase)
                ? BundlePlatform.PS3
                : BundlePlatform.X360;
            ProgressionData data = new();
            Check(data.Read(entry) && data.muVersionNumber is >= 42 and <= 44 && data.muSize <= bytes.Length &&
                data.mpaProgressionRanks.Count == 6 && data.mpaEventJunctions.Count > 0 && data.mpaEvents.Count > 0,
                $"Fixture {Path.GetFileName(path)} reads all progression tables (v{data.muVersionNumber})");
            Check(data.mpaAIBalances.All(balance => float.IsFinite(balance.mfCatchUpCutOffRatio)) &&
                data.mpaEvents.All(race => race.maStartGridSlots.Count == 7) &&
                data.mpaCarOpponentSet.All(set => set.maCarOpponents.Count == 8) &&
                !entry.Dirty && entry.EntryBlocks[0].Data.SequenceEqual(original),
                $"Fixture {Path.GetFileName(path)} keeps fixed slots and original bytes");
            Check(!data.Problems().Any() && data.Serialize(entry.Console).SequenceEqual(original),
                $"Fixture {Path.GetFileName(path)} saves byte for byte");
        }
    }
}
