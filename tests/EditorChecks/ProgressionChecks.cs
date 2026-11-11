using System.Buffers.Binary;
using System.Text;
using BaseHandlers;
using BundleFormat;
using BundleUtilities;

internal static partial class Program
{
    private static void CheckProgression()
    {
        (BundlePlatform Platform, uint Version)[] layouts =
        [
            (BundlePlatform.PC, 43),
            (BundlePlatform.X360, 43),
            (BundlePlatform.PS3, 43),
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
            Check(data.muVersionNumber == version && data.muSize == bytes.Length - 8 &&
                data.muPlayerCarIdCount == 1 && data.muProgressionRankCount == 1 &&
                data.muEventJunctionCount == 1 && data.muEventCount == 1 && data.miRivalCount == 1 &&
                data.muAIBalanceCount == 1 && data.muPersonalityCount == 1 &&
                data.muTrophyUnlockCount == 1 && data.muCarOpponentsCount == 1 &&
                data.mpaPlayerCarIds.Single().Value == "STARTERCAR", label + "header counts and starter CgsID");
            Check(data.PlayerCarIDsOffset == 0xA0 && data.ProgressionRanksOffset == 0xB0 &&
                data.EventJunctionsOffset == 0x120 && data.EventsOffset == 0x150 && data.RivalsOffset == 0x250 &&
                data.AIBalancesOffset == 0x290 && data.PersonalitiesOffset == 0x2E0 &&
                data.TrophyUnlocksOffset == 0x300 && data.CarOpponentSetOffset == 0x320 &&
                data.BaseEventJunctionCount == (version == 44
                    ? 120u
                    : null), label + "pointer width and optional remaster field");
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
                rank.Padding64 == 0xA1B2C3D4 && rank.mFreeCarForRankUpID.Value == "RANKCAR",
                label + "unsigned rank thresholds, packed counts and gift car");

            EventJunction junction = data.mpaEventJunctions.Single();
            RaceEventData race = data.mpaEvents[0x150];
            Check(junction.muID == 0xF1234567 && junction.miShotGroup == -1 &&
                junction.mpOfflineEvent.muId == race.muId && junction.mpOnlineEvent.muId == race.muId &&
                junction.BikeEventOffset == (version == 44
                    ? 0x150UL
                    : 0) &&
                (version != 44 || junction.BikeEvent.muId == race.muId) &&
                junction.Padding04 == (wide
                    ? 0xCAFE0001u
                    : 0) &&
                junction.Padding24 == (wide
                    ? 0xCAFE0002u
                    : 0), label + "junction links, signed shot group and padding");
            Check(race.muId == 0x89ABCDEF && race.muFlags == 4 && race.mfTrafficDensity == 1.25f &&
                race.mfBoostEarning == 2.5f && race.mSpecialEventCarId.Value == "EVENTCAR" &&
                race.CheckpointsOffset == 0x3C0 && race.miCheckpointCount == 1 &&
                race.mfTimeLimitFast == 30.5f && race.mfTimeLimitSlow == 45.5f && race.mfExtensionTime == 3.25f &&
                race.miRankScore.SequenceEqual(Enumerable.Range(1, 6).Select(index => index * 100)) &&
                race.mfRankTime.SequenceEqual(Enumerable.Range(60, 6).Select(index => (float)index)),
                label + "event IDs, checkpoint pointer, scores and times");
            Check(race.muStartGridCount == 1 && race.maStartGridSlots.Length == 7 &&
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
            Check(rival.mId.Value == "RIVAL_ID" && rival.mCarId.Value == "RIVALCAR" &&
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
                trophy.Padding06 == 0xCAFE && trophy.mCarUnlockId.Value == "TROPHYCAR", label + "trophy condition and vehicle");
            CarOpponentSet opponents = data.mpaCarOpponentSet.Single();
            Check(opponents.mPlayerCarId.Value == "PLAYERCAR" && opponents.miRank == -4 && opponents.miOpponentCount == 2 &&
                opponents.maCarOpponents.Length == 8 && opponents.maCarOpponents.Select((opponent, index) =>
                    opponent.mCarId.Value == $"OPP{index}" && opponent.miPersonalityIndex == 100 + index &&
                    opponent.Padding0C == 0xAABB0000u + (uint)index).All(matches => matches),
                label + "eight opponents precede the player car, including unused slots");

            Check(data.Read(entry) && data.mpaEvents.Count == 1 && data.mpaProgressionRanks.Count == 1 &&
                data.mpaRivals.Count == 1 && data.mpaCarOpponentSet.Count == 1,
                label + "reloading replaces decoded collections");
            Check(!data.Write(entry) && !entry.Dirty && entry.EntryBlocks[0].Data.SequenceEqual(original),
                label + "read-only handler preserves resource bytes");
        }

        CheckProgressionValidation();
    }

    private static void CheckProgressionValidation()
    {
        (int Offset, uint Value, string Description)[] malformed =
        [
            (0x2C, uint.MaxValue, "negative rival count"),
            (0x24, uint.MaxValue, "event table beyond resource"),
            (0x124, 0x151, "junction pointer inside an event"),
            (0x238, 8, "too many start grid slots"),
            (0x3C4, 9, "too many block sections")
        ];

        foreach ((int offset, uint value, string description) in malformed)
        {
            byte[] bytes = ProgressionFixture(BundlePlatform.PC, 43);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
            byte[] original = [.. bytes];
            BundleEntry entry = Entry(bytes);
            bool rejected = false;

            try
            {
                new ProgressionData().Read(entry);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Check(rejected && !entry.Dirty && entry.EntryBlocks[0].Data.SequenceEqual(original),
                $"Progression rejects {description} without changing source bytes");
        }

        byte[] empty = new byte[0x50];
        BinaryPrimitives.WriteUInt32LittleEndian(empty, 43);
        BinaryPrimitives.WriteUInt32LittleEndian(empty.AsSpan(8), uint.MaxValue);
        ProgressionData data = new();
        Check(data.Read(Entry(empty)) && data.mpaPlayerCarIds.Count == 0,
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
        // Optional local retail fixtures
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
            Check(data.Read(entry) && data.muVersionNumber == 43 && data.muSize == bytes.Length - 8 &&
                data.mpaProgressionRanks.Count == 6 && data.mpaEventJunctions.Count == 120 && data.mpaEvents.Count > 0,
                $"Retail fixture {Path.GetFileName(path)} reads all progression tables");
            Check(data.mpaAIBalances.All(balance => float.IsFinite(balance.mfCatchUpCutOffRatio)) &&
                data.mpaEvents.Values.All(race => race.maStartGridSlots.Length == 7) &&
                data.mpaCarOpponentSet.All(set => set.maCarOpponents.Length == 8) &&
                !entry.Dirty && entry.EntryBlocks[0].Data.SequenceEqual(original),
                $"Retail fixture {Path.GetFileName(path)} preserves fixed slots and original bytes");
        }
    }
}
