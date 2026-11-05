using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Numerics;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

public class Traffic : IEntryData
{
    [ReadOnly(true), Description("44 for v1.0 - v1.3, 45 for v1.4+\n")]
    public byte muDataVersion { get; set; }

    [Browsable(false)] public PVS mPvs { get; set; } = new();

    [Browsable(false)] public List<Hull> maHulls { get; set; } = [];

    [Browsable(false)] public List<FlowType> maFlowTypes { get; set; } = [];

    [Browsable(false)] public List<VehicleType> maVehicleTypes { get; set; } = [];

    [Browsable(false)] public List<VehicleAsset> maVehicleAssets { get; set; } = [];

    [Browsable(false)] public List<VehicleTraits> maVehicleTraits { get; set; } = [];

    [Browsable(false)] public List<KillZone> maKillZones { get; set; } = [];

    [Browsable(false)] public List<TrafficLightType> maTrafficLightTypes { get; set; } = [];

    [Browsable(false)] public List<PaintColour> maPaintColours { get; set; } = [];

    public class PVS
    {
        public Vector4 mGridMin { get; set; }

        public Vector4 mCellSize { get; set; }

        public Vector4 mRecipCellSize { get; set; }

        public uint muNumCells_X { get; set; }

        [Description("The grid needs X times Z cells.")]
        public uint muNumCells_Z { get; set; }

        [Browsable(false)] public List<PVSCell> maHullPvs { get; set; } = [];
    }

    public class PVSCell
    {
        [Description("Hulls visible from this grid cell, up to 8."), TypeConverter(typeof(TrafficReferenceListConverter)), Editor(typeof(UITypeEditor), typeof(UITypeEditor))]
        public List<Hull> maHulls { get; set; } = [];
    }

    public class Hull
    {
        [Description("Up to 16."), TypeConverter(typeof(TrafficReferenceListConverter)), Editor(typeof(UITypeEditor), typeof(UITypeEditor))]
        public List<VehicleAsset> maVehicleAssets { get; set; } = [];

        [Browsable(false)] public List<Section> maSections { get; set; } = [];

        [Browsable(false)] public List<SectionSpan> maSectionSpans { get; set; } = [];

        [Browsable(false)] public List<JunctionLogicBox> maJunctions { get; set; } = [];

        [Browsable(false)] public List<LightTrigger> maLightTriggers { get; set; } = [];

        [Browsable(false)] public List<LightTriggerStartData> maLightTriggerStartData { get; set; } = [];

        [Browsable(false), Description("From muFirstTrafficLight to muLastTrafficLight.")]
        public List<TrafficLight> maTrafficLights { get; set; } = [];

        [Browsable(false)] public List<StaticTrafficVehicle> maStaticTrafficVehicles { get; set; } = [];

        public override string ToString() => $"{maSections.Count} sections, {maJunctions.Count} junctions";
    }

    public class Section
    {
        [Description("From muSpanIndex."), TypeConverter(typeof(TrafficReferenceConverter))]
        public SectionSpan mSpan { get; set; }

        [Description("From mauForwardHulls and mauForwardSections, E_DIR_STRAIGHT_ON."), TypeConverter(typeof(TrafficReferenceConverter))]
        public Section mForwardStraightOn { get; set; }

        [Description("From mauForwardHulls and mauForwardSections, E_DIR_LEFT."), TypeConverter(typeof(TrafficReferenceConverter))]
        public Section mForwardLeft { get; set; }

        [Description("From mauForwardHulls and mauForwardSections, E_DIR_RIGHT."), TypeConverter(typeof(TrafficReferenceConverter))]
        public Section mForwardRight { get; set; }

        [Description("From mauBackwardHulls and mauBackwardSections, E_DIR_STRAIGHT_ON."), TypeConverter(typeof(TrafficReferenceConverter))]
        public Section mBackwardStraightOn { get; set; }

        [Description("From mauBackwardHulls and mauBackwardSections, E_DIR_LEFT."), TypeConverter(typeof(TrafficReferenceConverter))]
        public Section mBackwardLeft { get; set; }

        [Description("From mauBackwardHulls and mauBackwardSections, E_DIR_RIGHT."), TypeConverter(typeof(TrafficReferenceConverter))]
        public Section mBackwardRight { get; set; }

        public byte muTurnLeftProb { get; set; }

        public byte muTurnRightProb { get; set; }

        public byte muChangeLeftProb { get; set; }

        public byte muChangeRightProb { get; set; }

        public float mfSpeed { get; set; }

        public float mfLength { get; set; }

        [Description("From this section's SectionFlow."), TypeConverter(typeof(TrafficReferenceConverter))]
        public FlowType mFlowType { get; set; }

        [Description("From this section's SectionFlow.")]
        public ushort muVehiclesPerMinute { get; set; }

        [Browsable(false)] public List<LaneRung> maRungs { get; set; } = [];

        [Browsable(false)] public List<Neighbour> maLeftNeighbours { get; set; } = [];

        [Browsable(false)] public List<Neighbour> maRightNeighbours { get; set; } = [];

        [Browsable(false)] public List<StopLine> maStopLines { get; set; } = [];

        // By Directions: E_DIR_STRAIGHT_ON, E_DIR_LEFT, E_DIR_RIGHT
        internal Section[] Forward
        {
            get => [mForwardStraightOn, mForwardLeft, mForwardRight];
            set => (mForwardStraightOn, mForwardLeft, mForwardRight) = (value[0], value[1], value[2]);
        }

        internal Section[] Backward
        {
            get => [mBackwardStraightOn, mBackwardLeft, mBackwardRight];
            set => (mBackwardStraightOn, mBackwardLeft, mBackwardRight) = (value[0], value[1], value[2]);
        }

        public override string ToString() => $"{maRungs.Count} lane points, speed {mfSpeed}";
    }

    public class LaneRung
    {
        [TypeConverter(typeof(ArrayTextConverter))] public Vector4[] maPoints { get; set; } = new Vector4[2];

        [Description("From the hull's mpafCumulativeRungLengths.")]
        public float mfCumulativeLength { get; set; }

        public override string ToString()
        {
            if (maPoints?.Length != 2)
                return "Set two lane endpoints";

            Vector4 middle = (maPoints[0] + maPoints[1]) / 2;
            return $"{middle.X:0.#}, {middle.Y:0.#}, {middle.Z:0.#}";
        }
    }

    public class Neighbour
    {
        [Description("A section in the same hull."), TypeConverter(typeof(TrafficReferenceConverter))]
        public Section mSection { get; set; }

        public byte muSharedLength { get; set; }

        [Description("A rung of the section this neighbour belongs to."), TypeConverter(typeof(TrafficReferenceConverter))]
        public LaneRung mOurStartRung { get; set; }

        [Description("A rung of mSection."), TypeConverter(typeof(TrafficReferenceConverter))]
        public LaneRung mTheirStartRung { get; set; }
    }

    public class StopLine
    {
        public ushort muParamFixed { get; set; }

        public override string ToString() => muParamFixed.ToString();
    }

    public class SectionSpan
    {
        public ushort muMaxVehicles { get; set; }

        public float mfMaxVehicleRecip { get; set; }

        public override string ToString() => $"{muMaxVehicles} vehicles";
    }

    public class StaticTrafficVehicle
    {
        [Description("Matrix44Affine, by row."), TypeConverter(typeof(ArrayTextConverter))]
        public Vector4[] mTransform { get; set; } = [Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW];

        [Description("From mFlowTypeID."), TypeConverter(typeof(TrafficReferenceConverter))]
        public FlowType mFlowType { get; set; }

        public byte mExistsAtAllChance { get; set; }

        public byte muFlags { get; set; }
    }

    public class JunctionLogicBox
    {
        public uint muID { get; set; }

        public uint muEventJunctionID { get; set; }

        [Description("From miOfflineStartDataIndex."), TypeConverter(typeof(TrafficReferenceConverter))]
        public LightTriggerStartData mOfflineStartData { get; set; }

        [Description("From miOnlineStartDataIndex. Selecting none uses the offline one."), TypeConverter(typeof(TrafficReferenceConverter))]
        public LightTriggerStartData mOnlineStartData { get; set; }

        [Description("For bike events."), TypeConverter(typeof(TrafficReferenceConverter))]
        public LightTriggerStartData mBikeStartData { get; set; }

        public Vector4 mPosition { get; set; }

        [Browsable(false)] public List<JunctionState> maStates { get; set; } = [];

        [Browsable(false)] public List<TrafficLightController> maTrafficLightControllers { get; set; } = [];

        public override string ToString() => $"ID {muID}";
    }

    public class JunctionState
    {
        [Description("From mauStateTimings.")]
        public ushort muStateTiming { get; set; }

        [Description("From mauStoppedLightStates.")]
        public byte muStoppedLightState { get; set; }
    }

    public class TrafficLightController
    {
        [Description("From mauTrafficLightIds, up to 2 of the hull's traffic lights."), TypeConverter(typeof(TrafficReferenceListConverter)), Editor(typeof(UITypeEditor), typeof(UITypeEditor))]
        public List<TrafficLight> maTrafficLights { get; set; } = [];

        [Description("From mauStopLineIds and mauStopLineHulls, up to 6, as hull/section/stop line."), TypeConverter(typeof(TrafficReferenceListConverter)), Editor(typeof(UITypeEditor), typeof(UITypeEditor))]
        public List<StopLine> maStopLines { get; set; } = [];
    }

    public class LightTrigger
    {
        public Vector4 mDimensions { get; set; }

        public Vector4 mPosPlusYRot { get; set; }

        [Description("From mpaLightTriggerJunctionLookup."), TypeConverter(typeof(TrafficReferenceConverter))]
        public JunctionLogicBox mJunction { get; set; }

        public override string ToString() => $"Junction {mJunction}";
    }

    public class LightTriggerStartData
    {
        public byte muNumLanes { get; set; }

        [Browsable(false)] public List<StartingPosition> maStartingPositions { get; set; } = [];

        [Browsable(false)] public List<Destination> maDestinations { get; set; } = [];

        public override string ToString() => $"{maStartingPositions.Count} starting positions";
    }

    public class StartingPosition
    {
        [Description("From maStartingPositions.")]
        public Vector4 mPosition { get; set; }

        [Description("From maStartingDirections.")]
        public Vector4 mDirection { get; set; }
    }

    public class Destination
    {
        [Description("From maDestinationIDs."), TypeConverter(typeof(HexConverter))]
        public ulong mID { get; set; }

        [Description("From maeDestinationDifficulties.")]
        public byte meDifficulty { get; set; }
    }

    public class FlowType
    {
        [Browsable(false)] public List<FlowVehicleType> maVehicleTypes { get; set; } = [];

        public override string ToString() => string.Join(", ", maVehicleTypes.Select(v => v.mVehicleType));
    }

    public class FlowVehicleType
    {
        [Description("From mpauVehicleTypeIds."), TypeConverter(typeof(TrafficReferenceConverter))]
        public VehicleType mVehicleType { get; set; }

        [Description("From mpauCumulativeProb, the running total of each type's chance, out of 255.")]
        public byte muCumulativeProb { get; set; }

        public override string ToString() => mVehicleType?.ToString() ?? "Choose a vehicle type";
    }

    public enum VehicleClass : byte
    {
        E_VEHICLECLASS_CAR,
        E_VEHICLECLASS_VAN,
        E_VEHICLECLASS_BUS,
        E_VEHICLECLASS_BIGRIG
    }

    public enum ETrafficLightState : byte
    {
        E_TRAFFICLIGHTSTATE_RED,
        E_TRAFFICLIGHTSTATE_AMBER,
        E_TRAFFICLIGHTSTATE_GREEN
    }

    // VehicleTypeData and VehicleTypeUpdateData, the two arrays have an entry for each type
    public class VehicleType
    {
        [Description("From muTrailerFlowTypeId."), TypeConverter(typeof(TrafficReferenceConverter))]
        public FlowType mTrailerFlowType { get; set; }

        public byte mxVehicleFlags { get; set; }

        public VehicleClass muVehicleClass { get; set; }

        public byte muInitialDirt { get; set; }

        [Description("From muAssetId."), TypeConverter(typeof(TrafficReferenceConverter))]
        public VehicleAsset mAsset { get; set; }

        [Description("From muTraitsId."), TypeConverter(typeof(TrafficReferenceConverter))]
        public VehicleTraits mTraits { get; set; }

        public float mfWheelRadius { get; set; }

        public float mfSuspensionRoll { get; set; }

        public float mfSuspensionPitch { get; set; }

        public float mfSuspensionTravel { get; set; }

        public float mfMass { get; set; }

        public override string ToString() => $"{mAsset} ({muVehicleClass.ToString().Replace("E_VEHICLECLASS_", "")})";
    }

    public class VehicleAsset
    {
        [TypeConverter(typeof(TrafficCgsIDConverter))]
        public ulong mVehicleId { get; set; }

        public override string ToString() => TrafficCgsIDConverter.Label(mVehicleId);
    }

    public class VehicleTraits
    {
        public float mfSwervingAmountModifier { get; set; }

        public float mfAcceleration { get; set; }

        public byte muCuttingUpChance { get; set; }

        public byte muTailgatingChance { get; set; }

        public byte muPatience { get; set; }

        public byte muTantrumAttackCumProb { get; set; }

        public byte muTantrumStopCumProb { get; set; }
    }

    public class KillZone
    {
        [Description("From mpaKillZoneIds. Kill zones are saved in ID order, as the game looks them up by ID."), TypeConverter(typeof(HexConverter))]
        public ulong mId { get; set; }

        [Browsable(false)] public List<KillZoneRegion> maRegions { get; set; } = [];

        public override string ToString() => $"0x{mId:X}";
    }

    public class KillZoneRegion
    {
        [Description("From muHull and muSection."), TypeConverter(typeof(TrafficReferenceConverter))]
        public Section mSection { get; set; }

        [Description("A rung of mSection."), TypeConverter(typeof(TrafficReferenceConverter))]
        public LaneRung mStartRung { get; set; }

        [Description("A rung of mSection."), TypeConverter(typeof(TrafficReferenceConverter))]
        public LaneRung mEndRung { get; set; }
    }

    public class TrafficLight
    {
        [Description("From mpaPosAndYRotations, W is the Y rotation.")]
        public Vector4 mPosAndYRotation { get; set; }

        [Description("From mpaInstanceIDs.")]
        public uint muInstanceID { get; set; }

        [Description("From mpauInstanceTypes."), TypeConverter(typeof(TrafficReferenceConverter))]
        public TrafficLightType mType { get; set; }

        public override string ToString() => $"ID {muInstanceID}";
    }

    public class TrafficLightType
    {
        [Browsable(false)] public List<Corona> maCoronas { get; set; } = [];

        public override string ToString() => $"{maCoronas.Count} coronas";
    }

    public class Corona
    {
        [Description("From mpaCoronaTypes.")]
        public ETrafficLightState meState { get; set; }

        [Description("From mpaCoronaPositions.")]
        public Vector4 mPosition { get; set; }
    }

    public class PaintColour
    {
        [Editor(typeof(VectorColorEditor), typeof(UITypeEditor)), TypeConverter(typeof(VectorColorConverter))]
        [Description("Vehicle paint colour. Use the colour picker to choose a colour and adjust its alpha.")]
        public Vector4 mColour { get; set; }

        public override string ToString() => VectorColorConverter.Describe(mColour);
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        return new TrafficEditor(this, entry);
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.TrafficData;
    }

    private static int PointerSize(BundleEntry entry) => entry.Platform == BundlePlatform.PCx64
        ? 8
        : 4;

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        using BinaryReader2 br = new(new MemoryStream(entry.GetPrimaryWithoutImports()));
        br.BigEndian = entry.Console;
        int pointerSize = PointerSize(entry);

        long Ptr()
        {
            br.BaseStream.Position = (br.BaseStream.Position + pointerSize - 1) & -pointerSize;
            return pointerSize == 8
                ? (long)br.ReadUInt64()
                : br.ReadUInt32();
        }

        List<T> At<T>(long offset, int count, Func<T> read)
        {
            long back = br.BaseStream.Position;
            br.BaseStream.Position = offset;
            List<T> items = new(count);

            for (int currentI = 0; currentI < count; currentI++)
                items.Add(read());

            br.BaseStream.Position = back;
            return items;
        }

        Vector4 V4() => new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        ushort U16() => br.ReadUInt16();

        int Limit(int count, int max, string what) => count <= max
            ? count
            : throw new ReadFailedError($"Traffic data has {count} {what}, more than the {max} there's room for.");

        // Ranges into a shared array become lists in their owners, which only keeps the data if they follow on from each other
        List<T> Take<T>(List<T> all, ref int next, int offset, int count, string what)
        {
            if (offset != next || offset + count > all.Count)
                throw new ReadFailedError($"Traffic data {what} ranges overlap or have gaps, so they can't be edited as lists.");

            next += count;
            return all.GetRange(offset, count);
        }

        void TookAll<T>(List<T> all, int next, string what)
        {
            if (next != all.Count)
                throw new ReadFailedError($"Traffic data has {what} that nothing uses, so it can't be edited as lists.");
        }

        // Indexes become references once everything they can refer to is read
        List<Action> links = [];

        T Get<T>(List<T> list, int index, string what) =>
                    index >= 0 && index < list.Count
                        ? list[index]
                        : throw new ReadFailedError($"Traffic data refers to {what} {index}, which doesn't exist.");

        muDataVersion = br.ReadByte();

        if (muDataVersion is not (44 or 45))
            throw new ReadFailedError($"Traffic data version {muDataVersion} can't be read, only versions 44 (v1.0 to v1.3) and 45.");

        br.ReadByte();
        int numHulls = U16();
        br.ReadUInt32(); // muSizeInBytes
        long pvs = Ptr(), hulls = Ptr(), flowTypes = Ptr();
        int numFlowTypes = U16(), numVehicleTypes = U16(), numVehicleAssets = br.ReadByte(), numVehicleTraits = br.ReadByte();
        int numKillZones = U16(), numKillZoneRegions = U16();
        long killZoneIDs = Ptr(), killZones = Ptr(), killZoneRegions = Ptr(), vehicleTypes = Ptr(), vehicleTypesUpdate = Ptr(), vehicleAssets = Ptr(), vehicleTraits = Ptr();
        int numLights = U16(), numLightTypes = U16();
        U16(); // muNumCoronas, the light types give it (and the v1.0 X360 file has it byte swapped)
        long lightPositions = Ptr(), instanceIDs = Ptr(), instanceTypes = Ptr(), lightTypes = Ptr(), coronaTypes = Ptr(), coronaPositions = Ptr();
        br.ReadBytes(129 * 2); // mauInstanceHashOffsets, and the two hash tables after it, are rebuilt when saving
        Ptr();
        Ptr();
        int numPaintColours = br.ReadByte();
        long paintColours = Ptr();

        br.BaseStream.Position = pvs;
        mPvs = new()
        {
            mGridMin = V4(),
            mCellSize = V4(),
            mRecipCellSize = V4(),
            muNumCells_X = br.ReadUInt32(),
            muNumCells_Z = br.ReadUInt32()
        };
        int numCells = (int)br.ReadUInt32();
        mPvs.maHullPvs = At(Ptr(), numCells, () =>
        {
            ushort[] currentHullIDs = [.. Enumerable.Range(0, 8).Select(_ => U16())];
            int currentCount = Limit((int)br.ReadUInt32(), 8, "hulls in a PVS cell");
            PVSCell cell = new();
            links.Add(() => cell.maHulls = [.. currentHullIDs[..currentCount].Select(id => Get(maHulls, id, "hull"))]);
            return cell;
        });

        List<(int First, int Last)> lightRanges = [];
        List<List<StopLine>> hullStopLines = [];

        Hull ReadHull(long offset)
        {
            br.BaseStream.Position = offset;
            int numSections = br.ReadByte(), numSpans = br.ReadByte(), numJunctions = br.ReadByte(), numStopLines = br.ReadByte();
            int numNeighbours = br.ReadByte(), numStatic = br.ReadByte(), numAssets = Limit(br.ReadByte(), 16, "vehicle assets in a hull");
            br.ReadByte();
            int numRungs = U16();
            lightRanges.Add((U16(), U16()));
            int numTriggers = br.ReadByte(), numStartData = br.ReadByte();
            long sections = Ptr(), rungs = Ptr(), rungLengths = Ptr(), neighbours = Ptr(), spans = Ptr(), statics = Ptr(), flows = Ptr();
            long junctions = Ptr(), stopLines = Ptr(), triggers = Ptr(), startData = Ptr(), junctionLookup = Ptr();
            Hull currentHull = new();
            byte[] assetIDs = br.ReadBytes(16)[..numAssets];
            links.Add(() => currentHull.maVehicleAssets = [.. assetIDs.Select(id => Get(maVehicleAssets, id, "vehicle asset"))]);

            List<LaneRung> allRungs = At(rungs, numRungs, () => new LaneRung()
            {
                maPoints = [V4(), V4()]
            });

            List<float> lengths = At(rungLengths, numRungs, br.ReadSingle);

            for (int currentI2 = 0; currentI2 < numRungs; currentI2++)
                allRungs[currentI2].mfCumulativeLength = lengths[currentI2];

            List<(byte Section, byte SharedLength, byte OurRung, byte TheirRung)> allNeighbours =
                            At(neighbours, numNeighbours, () => (br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte()));
            List<StopLine> allStopLines = At(stopLines, numStopLines, () => new StopLine()
            {
                muParamFixed = U16()
            });

            hullStopLines.Add(allStopLines);
            List<(ushort Type, ushort PerMinute)> sectionFlows = At(flows, numSections, () => (U16(), U16()));

            int nextRung = 0, nextNeighbour = 0, nextStopLine = 0;
            currentHull.maSections = At(sections, numSections, () =>
            {
                int rungOffset = (int)br.ReadUInt32(), sectionRungs = br.ReadByte(), stopLineOffset = br.ReadByte(), sectionStopLines = br.ReadByte();
                int spanIndex = br.ReadByte();
                ushort[] forwardHulls = [U16(), U16(), U16()], backwardHulls = [U16(), U16(), U16()];
                byte[] forwardSections = br.ReadBytes(3), backwardSections = br.ReadBytes(3);
                Section section = new()
                {
                    muTurnLeftProb = br.ReadByte(),
                    muTurnRightProb = br.ReadByte()
                };

                int neighbourOffset = U16(), left = br.ReadByte(), right = br.ReadByte();
                section.muChangeLeftProb = br.ReadByte();
                section.muChangeRightProb = br.ReadByte();
                U16();
                section.mfSpeed = br.ReadSingle();
                section.mfLength = br.ReadSingle();
                br.ReadUInt32();
                section.maRungs = Take(allRungs, ref nextRung, rungOffset, sectionRungs, "section rung");

                if (sectionStopLines > 0) // A section without any has 255 as its offset
                    section.maStopLines = Take(allStopLines, ref nextStopLine, stopLineOffset, sectionStopLines, "section stop line");

                List<Neighbour> Neighbours(int start, int count) => [..Take(allNeighbours, ref nextNeighbour, start, count, "section neighbour").Select(raw =>
                {
                    Neighbour neighbour = new()
                    {
                        muSharedLength = raw.SharedLength
                    };

                    links.Add(() =>
                    {
                        neighbour.mSection = Get(currentHull.maSections, raw.Section, "section");
                        neighbour.mOurStartRung = Get(section.maRungs, raw.OurRung, "rung");
                        neighbour.mTheirStartRung = Get(neighbour.mSection.maRungs, raw.TheirRung, "rung");
                    });
                    return neighbour;
                })];

                section.maLeftNeighbours = Neighbours(neighbourOffset, left);
                section.maRightNeighbours = Neighbours(neighbourOffset + left, right);

                Section[] Links(ushort[] hullIDs, byte[] sectionIDs) => [..hullIDs.Zip(sectionIDs, (h, s) => h == 0xFFFF
                    ? null
                    : Get(Get(maHulls, h, "hull").maSections, s, "section"))];

                links.Add(() =>
                {
                    section.mSpan = Get(currentHull.maSectionSpans, spanIndex, "section span");
                    section.Forward = Links(forwardHulls, forwardSections);
                    section.Backward = Links(backwardHulls, backwardSections);
                });
                return section;
            });
            TookAll(allRungs, nextRung, "rungs");
            TookAll(allNeighbours, nextNeighbour, "neighbours");
            TookAll(allStopLines, nextStopLine, "stop lines");

            for (int currentI3 = 0; currentI3 < numSections; currentI3++)
            {
                (ushort flowType, ushort perMinute) = sectionFlows[currentI3];
                Section section = currentHull.maSections[currentI3];
                section.muVehiclesPerMinute = perMinute;
                links.Add(() => section.mFlowType = flowType == 0xFFFF
                    ? null
                    : Get(maFlowTypes, flowType, "flow type"));
            }

            currentHull.maSectionSpans = At(spans, numSpans, () =>
            {
                SectionSpan span = new()
                {
                    muMaxVehicles = U16()
                };

                U16();
                span.mfMaxVehicleRecip = br.ReadSingle();
                return span;
            });
            currentHull.maStaticTrafficVehicles = At(statics, numStatic, () =>
            {
                StaticTrafficVehicle vehicle = new()
                {
                    mTransform = [V4(), V4(), V4(), V4()]
                };

                ushort flowType = U16();
                vehicle.mExistsAtAllChance = br.ReadByte();
                vehicle.muFlags = br.ReadByte();
                br.ReadBytes(12);
                links.Add(() => vehicle.mFlowType = flowType == 0xFFFF
                    ? null
                    : Get(maFlowTypes, flowType, "flow type"));
                return vehicle;
            });
            currentHull.maJunctions = At(junctions, numJunctions, () => ReadJunction(currentHull));
            currentHull.maLightTriggers = At(triggers, numTriggers, () => new LightTrigger()
            {
                mDimensions = V4(),
                mPosPlusYRot = V4()
            });
            List<byte> triggerJunctions = At(junctionLookup, numTriggers, br.ReadByte);

            for (int currentI4 = 0; currentI4 < numTriggers; currentI4++)
                currentHull.maLightTriggers[currentI4].mJunction = Get(currentHull.maJunctions, triggerJunctions[currentI4], "junction");

            currentHull.maLightTriggerStartData = At(startData, numStartData, () =>
            {
                Vector4[] positions = [.. Enumerable.Range(0, 8).Select(_ => V4())];
                Vector4[] directions = [.. Enumerable.Range(0, 8).Select(_ => V4())];
                ulong[] ids = [.. Enumerable.Range(0, 16).Select(_ => br.ReadUInt64())];
                byte[] difficulties = br.ReadBytes(16);
                int numPositions = Limit(br.ReadByte(), 8, "starting positions"), numDestinations = Limit(br.ReadByte(), 16, "destinations");
                LightTriggerStartData data = new()
                {
                    muNumLanes = br.ReadByte(),
                    maStartingPositions = [..Enumerable.Range(0, numPositions).Select(i => new StartingPosition()
                    {
                        mPosition = positions[i],
                        mDirection = directions[i]
                    })],
                    maDestinations = [..Enumerable.Range(0, numDestinations).Select(i => new Destination()
                    {
                        mID = ids[i],
                        meDifficulty = difficulties[i]
                    })]
                };

                br.ReadBytes(13);
                return data;
            });
            return currentHull;
        }

        TrafficLightController ReadController(Hull hull)
        {
            ushort[] lightIDs = [U16(), U16()];
            byte[] stopLineIDs = br.ReadBytes(6);
            ushort[] stopLineHulls = [U16(), U16(), U16(), U16(), U16(), U16()];
            int numStopLines = Limit(br.ReadByte(), 6, "stop lines in a traffic light controller");
            int numLightIDs = Limit(br.ReadByte(), 2, "traffic lights in a traffic light controller");
            TrafficLightController controller = new();
            links.Add(() =>
            {
                int first = lightRanges[maHulls.IndexOf(hull)].First;
                controller.maTrafficLights = [.. lightIDs[..numLightIDs].Select(id => Get(hull.maTrafficLights, id - first, "hull traffic light"))];
                controller.maStopLines = [.. Enumerable.Range(0, numStopLines).Select(i => Get(Get(hullStopLines, stopLineHulls[i], "hull"), stopLineIDs[i], "stop line"))];
            });
            return controller;
        }

        JunctionLogicBox ReadJunction(Hull hull)
        {
            JunctionLogicBox junction = new()
            {
                muID = br.ReadUInt32()
            };

            ushort[] timings = [.. Enumerable.Range(0, 16).Select(_ => U16())];
            byte[] stoppedStates = br.ReadBytes(16);
            int numStates = Limit(br.ReadByte(), 16, "junction states"), numControllers = Limit(br.ReadByte(), 8, "traffic light controllers");
            U16();
            junction.muEventJunctionID = br.ReadUInt32();
            int[] startData = [br.ReadInt32(), br.ReadInt32(), muDataVersion >= 45
                ? br.ReadInt32()
                : -1];
            List<TrafficLightController> controllers = [.. Enumerable.Range(0, 8).Select(_ => ReadController(hull))];
            br.ReadBytes(muDataVersion >= 45
                ? 8
                : 12);
            junction.mPosition = V4();
            junction.maStates = [.. Enumerable.Range(0, numStates).Select(i => new JunctionState()
            {
                muStateTiming = timings[i],
                muStoppedLightState = stoppedStates[i]
            })];
            junction.maTrafficLightControllers = controllers[..numControllers];
            links.Add(() =>
            {
                LightTriggerStartData[] data = [..startData.Select(i => i < 0
                    ? null
                    : Get(hull.maLightTriggerStartData, i, "light trigger start data"))];
                (junction.mOfflineStartData, junction.mOnlineStartData, junction.mBikeStartData) = (data[0], data[1], data[2]);
            });
            return junction;
        }

        maHulls = [.. At(hulls, numHulls, Ptr).Select(ReadHull)];

        maFlowTypes = [.. At(flowTypes, numFlowTypes, Ptr).Select(offset =>
        {
            br.BaseStream.Position = offset;
            long ids = Ptr(), probs = Ptr();
            int currentCount2 = br.ReadByte();
            List<ushort> typeIDs = At(ids, currentCount2, U16);
            List<byte> cumulative = At(probs, currentCount2, br.ReadByte);
            return new FlowType()
            {
                maVehicleTypes = [..typeIDs.Select((id, i) =>
                {
                    FlowVehicleType currentType = new()
                    {
                        muCumulativeProb = cumulative[i]
                    };

                    links.Add(() => currentType.mVehicleType = Get(maVehicleTypes, id, "vehicle type"));
                    return currentType;
                })]
            };
        })];

        maVehicleTypes = At(vehicleTypes, numVehicleTypes, () =>
        {
            int trailer = U16();
            VehicleType currentType2 = new()
            {
                mxVehicleFlags = br.ReadByte(),
                muVehicleClass = (VehicleClass)br.ReadByte(),
                muInitialDirt = br.ReadByte()
            };

            int asset = br.ReadByte(), traits = br.ReadByte();
            br.ReadByte();
            links.Add(() =>
            {
                currentType2.mTrailerFlowType = trailer == 0xFFFF
                    ? null
                    : Get(maFlowTypes, trailer, "flow type");
                currentType2.mAsset = Get(maVehicleAssets, asset, "vehicle asset");
                currentType2.mTraits = Get(maVehicleTraits, traits, "vehicle traits");
            });
            return currentType2;
        });
        br.BaseStream.Position = vehicleTypesUpdate;

        foreach (VehicleType currentType3 in maVehicleTypes)
        {
            currentType3.mfWheelRadius = br.ReadSingle();
            currentType3.mfSuspensionRoll = br.ReadSingle();
            currentType3.mfSuspensionPitch = br.ReadSingle();
            currentType3.mfSuspensionTravel = br.ReadSingle();
            currentType3.mfMass = br.ReadSingle();
        }

        maVehicleAssets = At(vehicleAssets, numVehicleAssets, () => new VehicleAsset()
        {
            mVehicleId = br.ReadUInt64()
        });
        maVehicleTraits = At(vehicleTraits, numVehicleTraits, () =>
        {
            VehicleTraits traits = new()
            {
                mfSwervingAmountModifier = br.ReadSingle(),
                mfAcceleration = br.ReadSingle(),
                muCuttingUpChance = br.ReadByte(),
                muTailgatingChance = br.ReadByte(),
                muPatience = br.ReadByte(),
                muTantrumAttackCumProb = br.ReadByte(),
                muTantrumStopCumProb = br.ReadByte()
            };

            br.ReadBytes(3);
            return traits;
        });

        List<ulong> zoneIDs = At(killZoneIDs, numKillZones, br.ReadUInt64);
        List<KillZoneRegion> regions = At(killZoneRegions, numKillZoneRegions, () =>
        {
            int currentHull2 = U16(), section = br.ReadByte(), currentStart = br.ReadByte(), end = br.ReadByte();
            br.ReadByte();
            KillZoneRegion region = new();
            links.Add(() =>
            {
                region.mSection = Get(Get(maHulls, currentHull2, "hull").maSections, section, "section");
                region.mStartRung = Get(region.mSection.maRungs, currentStart, "rung");
                region.mEndRung = Get(region.mSection.maRungs, end, "rung");
            });
            return region;
        });
        List<(int Offset, int Count)> zoneRanges = At(killZones, numKillZones, () =>
        {
            int regionOffset = U16(), regionCount = br.ReadByte();
            br.ReadByte();
            return (regionOffset: regionOffset, regionCount: regionCount);
        });
        int nextRegion = 0;
        maKillZones = new(numKillZones);

        for (int currentI5 = 0; currentI5 < numKillZones; currentI5++)
            maKillZones.Add(new()
            {
                mId = zoneIDs[currentI5],
                maRegions = Take(regions, ref nextRegion, zoneRanges[currentI5].Offset, zoneRanges[currentI5].Count, "kill zone region")
            });

        TookAll(regions, nextRegion, "kill zone regions");

        List<Vector4> positions = At(lightPositions, numLights, V4);
        List<uint> ids = At(instanceIDs, numLights, br.ReadUInt32);
        List<byte> types = At(instanceTypes, numLights, br.ReadByte);
        List<TrafficLight> lights = [.. positions.Select((position, i) => new TrafficLight()
        {
            mPosAndYRotation = position,
            muInstanceID = ids[i]
        })];

        for (int currentI6 = 0; currentI6 < numLights; currentI6++)
        {
            TrafficLight light = lights[currentI6];
            int currentType4 = types[currentI6];
            links.Add(() => light.mType = Get(maTrafficLightTypes, currentType4, "traffic light type"));
        }

        // Each hull has a range of the lights, and a hull without any has where the next range would start
        int nextLight = 0;

        for (int currentI7 = 0; currentI7 < numHulls; currentI7++)
        {
            (int first, int last) = lightRanges[currentI7];

            if (first != nextLight || last < first)
                throw new ReadFailedError("Traffic data hull traffic light ranges overlap or have gaps, so they can't be edited as lists.");

            maHulls[currentI7].maTrafficLights = Take(lights, ref nextLight, first, last - first, "traffic light");
        }

        TookAll(lights, nextLight, "traffic lights");

        List<(int Offset, int Count)> typeCoronas = At(lightTypes, numLightTypes, () => ((int)br.ReadByte(), (int)br.ReadByte()));
        int numCoronas = typeCoronas.Sum(t => t.Count);
        List<byte> coronaStates = At(coronaTypes, numCoronas, br.ReadByte);
        List<Vector4> coronaPositionList = At(coronaPositions, numCoronas, V4);
        List<Corona> coronas = [.. coronaStates.Select((state, i) => new Corona()
        {
            meState = (ETrafficLightState)state,
            mPosition = coronaPositionList[i]
        })];

        int nextCorona = 0;
        maTrafficLightTypes = [.. typeCoronas.Select(t => new TrafficLightType()
        {
            maCoronas = Take(coronas, ref nextCorona, t.Offset, t.Count, "traffic light corona")
        })];

        maPaintColours = At(paintColours, numPaintColours, () => new PaintColour()
        {
            mColour = V4()
        });

        foreach (Action link in links)
            link();

        return true;
    }

    private sealed class FlatWriter(bool bigEndian, int pointerSize)
    {
        public sealed class Block
        {
            public long Offset = -1;
        }

        private readonly List<(long Slot, Block Target)> pointers = [];
        public readonly BinaryWriter2 Bw = new(new MemoryStream(), bigEndian);

        public void Pad(int alignment)
        {
            while (Bw.BaseStream.Position % alignment != 0)
                Bw.Write((byte)0);
        }

        public void Ptr(Block target)
        {
            Pad(pointerSize);
            pointers.Add((Bw.BaseStream.Position, target));
            Bw.Write(new byte[pointerSize]);
        }

        // An empty block is placed too, its pointer is where it would start
        public void Start(Block block)
        {
            Pad(16);
            block.Offset = Bw.BaseStream.Position;
        }

        public void V4(Vector4 v)
        {
            Bw.Write(v.X);
            Bw.Write(v.Y);
            Bw.Write(v.Z);
            Bw.Write(v.W);
        }

        public byte[] Finish()
        {
            Pad(16);

            foreach ((long slot, Block target) in pointers)
            {
                if (target.Offset < 0)
                    throw new InvalidOperationException("A block was pointed to but never placed.");

                Bw.BaseStream.Position = slot;

                if (pointerSize == 8)
                    Bw.Write((ulong)target.Offset);
                else
                    Bw.Write((uint)target.Offset);
            }

            return ((MemoryStream)Bw.BaseStream).ToArray();
        }
    }

    private static byte U8(int value, string what) =>
        value is >= 0 and <= byte.MaxValue
            ? (byte)value
            : throw new InvalidDataException($"{what}: {value} is more than the format can hold ({byte.MaxValue}).");

    private static ushort U16(int value, string what) =>
        value is >= 0 and <= ushort.MaxValue
            ? (ushort)value
            : throw new InvalidDataException($"{what}: {value} is more than the format can hold ({ushort.MaxValue}).");

    private static List<T> UpTo<T>(List<T> list, int max, string what) =>
        list.Count <= max
            ? list
            : throw new InvalidDataException($"{what}: there can be at most {max}, there are {list.Count}.");

    private static Dictionary<T, int> Indexes<T>(IEnumerable<T> items)
    {
        Dictionary<T, int> indexes = [];

        foreach (T item in items)
            indexes.TryAdd(item, indexes.Count);

        return indexes;
    }

    // A reference's index, or none's value for one that isn't set
    private static int Index<T>(Dictionary<T, int> indexes, T target, string what, int? none = null) where T : class
    {
        if (target == null)
            return none ?? throw new InvalidDataException($"{what} isn't set.");

        return indexes.TryGetValue(target, out int index)
            ? index
            : throw new InvalidDataException($"{what} refers to one that's been deleted, or that it can't refer to from where it is.");
    }

    private sealed class References
    {
        public Dictionary<Hull, int> Hulls;
        public Dictionary<Section, (int Hull, int Index)> Sections = [];
        public Dictionary<StopLine, (int Hull, int Index)> StopLines = [];
        public Dictionary<TrafficLight, int> Lights;
        public Dictionary<FlowType, int> FlowTypes;
        public Dictionary<VehicleType, int> VehicleTypes;
        public Dictionary<VehicleAsset, int> Assets;
        public Dictionary<VehicleTraits, int> Traits;
        public Dictionary<TrafficLightType, int> LightTypes;

        public (int Hull, int Index) Section(Section section, string what) =>
            section != null && Sections.TryGetValue(section, out (int Hull, int Index) index)
                ? index
                : throw new InvalidDataException(section == null
                    ? $"{what} isn't set."
                    : $"{what} refers to a section that's been deleted.");
    }

    public byte[] Serialize(BundleEntry entry) => Serialize(entry.Console, PointerSize(entry));

    public byte[] Serialize(bool bigEndian, int pointerSize)
    {
        FlatWriter w = new(bigEndian, pointerSize);
        BinaryWriter2 bw = w.Bw;

        List<TrafficLight> lights = [.. maHulls.SelectMany(h => h.maTrafficLights)];
        References refs = new()
        {
            Hulls = Indexes(maHulls),
            Lights = Indexes(lights),
            FlowTypes = Indexes(maFlowTypes),
            VehicleTypes = Indexes(maVehicleTypes),
            Assets = Indexes(maVehicleAssets),
            Traits = Indexes(maVehicleTraits),
            LightTypes = Indexes(maTrafficLightTypes)
        };

        for (int currentH = 0; currentH < maHulls.Count; currentH++)
        {
            for (int currentS = 0; currentS < maHulls[currentH].maSections.Count; currentS++)
                refs.Sections.TryAdd(maHulls[currentH].maSections[currentS], (h: currentH, s: currentS));

            int stopLine = 0;

            foreach (StopLine line in maHulls[currentH].maSections.SelectMany(s => s.maStopLines))
                refs.StopLines.TryAdd(line, (h: currentH, stopLine++));
        }

        // The game finds kill zones by binary search on their IDs, and traffic lights by ID in 128 buckets of its low bits
        List<KillZone> zones = [.. maKillZones.OrderBy(z => z.mId)];
        int[] hashed = [.. Enumerable.Range(0, lights.Count).OrderBy(i => lights[i].muInstanceID & 127).ThenBy(i => lights[i].muInstanceID)];
        List<Corona> coronas = [.. maTrafficLightTypes.SelectMany(t => t.maCoronas)];

        FlatWriter.Block pvs = new(), cells = new(), hullPointers = new(), flowPointers = new();
        FlatWriter.Block vehicleTypes = new(), vehicleTypesUpdate = new(), vehicleTraits = new(), vehicleAssets = new();
        FlatWriter.Block killZoneIDs = new(), killZones = new(), killZoneRegions = new();
        FlatWriter.Block lightPositions = new(), instanceIDs = new(), instanceTypes = new(), hashTable = new(), hashLookup = new();
        FlatWriter.Block lightTypeBlock = new(), coronaTypes = new(), coronaPositions = new(), paintColours = new();

        bw.Write(muDataVersion);
        bw.Write((byte)0);
        bw.Write(U16(maHulls.Count, "Hulls"));
        bw.Write(0u); // muSizeInBytes, filled in at the end
        w.Ptr(pvs);
        w.Ptr(hullPointers);
        w.Ptr(flowPointers);
        bw.Write(U16(maFlowTypes.Count, "Flow types"));
        bw.Write(U16(maVehicleTypes.Count, "Vehicle types"));
        bw.Write(U8(maVehicleAssets.Count, "Vehicle assets"));
        bw.Write(U8(maVehicleTraits.Count, "Vehicle traits"));
        bw.Write(U16(zones.Count, "Kill zones"));
        bw.Write(U16(zones.Sum(z => z.maRegions.Count), "Kill zone regions"));
        w.Ptr(killZoneIDs);
        w.Ptr(killZones);
        w.Ptr(killZoneRegions);
        w.Ptr(vehicleTypes);
        w.Ptr(vehicleTypesUpdate);
        w.Ptr(vehicleAssets);
        w.Ptr(vehicleTraits);
        bw.Write(U16(lights.Count, "Traffic lights"));
        bw.Write(U16(maTrafficLightTypes.Count, "Traffic light types"));
        bw.Write(U16(coronas.Count, "Coronas"));
        w.Ptr(lightPositions);
        w.Ptr(instanceIDs);
        w.Ptr(instanceTypes);
        w.Ptr(lightTypeBlock);
        w.Ptr(coronaTypes);
        w.Ptr(coronaPositions);

        for (int bucket = 0; bucket <= 128; bucket++)
            bw.Write((ushort)lights.Count(l => (l.muInstanceID & 127) < bucket));

        w.Ptr(hashTable);
        w.Ptr(hashLookup);
        bw.Write(U8(maPaintColours.Count, "Paint colours"));
        w.Ptr(paintColours);

        w.Start(pvs);
        w.V4(mPvs.mGridMin);
        w.V4(mPvs.mCellSize);
        w.V4(mPvs.mRecipCellSize);
        bw.Write(mPvs.muNumCells_X);
        bw.Write(mPvs.muNumCells_Z);

        if ((long)mPvs.muNumCells_X * mPvs.muNumCells_Z != mPvs.maHullPvs.Count)
            throw new InvalidDataException($"PVS: the grid is {mPvs.muNumCells_X} by {mPvs.muNumCells_Z} cells, so it needs {(long)mPvs.muNumCells_X * mPvs.muNumCells_Z} cells, it has {mPvs.maHullPvs.Count}.");

        bw.Write((uint)mPvs.maHullPvs.Count);
        w.Ptr(cells);
        w.Start(cells);

        for (int currentI = 0; currentI < mPvs.maHullPvs.Count; currentI++)
        {
            List<Hull> cellHulls = UpTo(mPvs.maHullPvs[currentI].maHulls, 8, $"PVS cell {currentI} hulls");

            for (int j = 0; j < 8; j++)
                bw.Write(j < cellHulls.Count
                    ? (ushort)Index(refs.Hulls, cellHulls[j], $"PVS cell {currentI} hull {j}")
                    : (ushort)0);

            bw.Write((uint)cellHulls.Count);
        }

        FlatWriter.Block[] hullBlocks = [.. maHulls.Select(_ => new FlatWriter.Block())];
        w.Start(hullPointers);

        foreach (FlatWriter.Block block in hullBlocks)
            w.Ptr(block);

        int firstLight = 0;

        for (int currentI2 = 0; currentI2 < maHulls.Count; currentI2++)
        {
            WriteHull(w, maHulls[currentI2], currentI2, firstLight, hullBlocks[currentI2], refs);
            firstLight += maHulls[currentI2].maTrafficLights.Count;
        }

        FlatWriter.Block[] flowBlocks = [.. maFlowTypes.Select(_ => new FlatWriter.Block())];
        w.Start(flowPointers);

        foreach (FlatWriter.Block block in flowBlocks)
            w.Ptr(block);

        for (int currentI3 = 0; currentI3 < maFlowTypes.Count; currentI3++)
        {
            List<FlowVehicleType> types = maFlowTypes[currentI3].maVehicleTypes;
            FlatWriter.Block ids = new(), probs = new();
            w.Start(flowBlocks[currentI3]);
            w.Ptr(ids);
            w.Ptr(probs);
            bw.Write(U8(types.Count, $"Flow type {currentI3} vehicle types"));
            w.Start(probs);

            foreach (FlowVehicleType currentType in types)
                bw.Write(currentType.muCumulativeProb);

            w.Start(ids);

            for (int j = 0; j < types.Count; j++)
                bw.Write((ushort)Index(refs.VehicleTypes, types[j].mVehicleType, $"Flow type {currentI3} vehicle type {j}"));
        }

        w.Start(vehicleTypes);

        for (int currentI4 = 0; currentI4 < maVehicleTypes.Count; currentI4++)
        {
            VehicleType currentType2 = maVehicleTypes[currentI4];
            bw.Write((ushort)Index(refs.FlowTypes, currentType2.mTrailerFlowType, $"Vehicle type {currentI4} trailer flow type", 0xFFFF));
            bw.Write(currentType2.mxVehicleFlags);
            bw.Write((byte)currentType2.muVehicleClass);
            bw.Write(currentType2.muInitialDirt);
            bw.Write((byte)Index(refs.Assets, currentType2.mAsset, $"Vehicle type {currentI4} asset"));
            bw.Write((byte)Index(refs.Traits, currentType2.mTraits, $"Vehicle type {currentI4} traits"));
            bw.Write((byte)0);
        }

        w.Start(vehicleTypesUpdate);

        foreach (VehicleType currentType3 in maVehicleTypes)
        {
            bw.Write(currentType3.mfWheelRadius);
            bw.Write(currentType3.mfSuspensionRoll);
            bw.Write(currentType3.mfSuspensionPitch);
            bw.Write(currentType3.mfSuspensionTravel);
            bw.Write(currentType3.mfMass);
        }

        w.Start(vehicleTraits);

        foreach (VehicleTraits traits in maVehicleTraits)
        {
            bw.Write(traits.mfSwervingAmountModifier);
            bw.Write(traits.mfAcceleration);
            bw.Write(traits.muCuttingUpChance);
            bw.Write(traits.muTailgatingChance);
            bw.Write(traits.muPatience);
            bw.Write(traits.muTantrumAttackCumProb);
            bw.Write(traits.muTantrumStopCumProb);
            bw.Write(new byte[3]);
        }

        w.Start(vehicleAssets);

        foreach (VehicleAsset asset in maVehicleAssets)
            bw.Write(asset.mVehicleId);

        w.Start(killZoneIDs);

        foreach (KillZone zone in zones)
            bw.Write(zone.mId);

        w.Start(killZones);
        int regionOffset = 0;

        foreach (KillZone zone in zones)
        {
            bw.Write(U16(regionOffset, "Kill zone regions"));
            bw.Write(U8(zone.maRegions.Count, $"Kill zone 0x{zone.mId:X} regions"));
            bw.Write((byte)0);
            regionOffset += zone.maRegions.Count;
        }

        w.Start(killZoneRegions);

        foreach (KillZone zone in zones)
        {
            for (int currentI5 = 0; currentI5 < zone.maRegions.Count; currentI5++)
            {
                KillZoneRegion region = zone.maRegions[currentI5];
                string name = $"Kill zone 0x{zone.mId:X} region {currentI5}";
                (int hull, int section) = refs.Section(region.mSection, $"{name} section");
                bw.Write((ushort)hull);
                bw.Write((byte)section);
                bw.Write((byte)RungIndex(region.mSection, region.mStartRung, $"{name} start rung"));
                bw.Write((byte)RungIndex(region.mSection, region.mEndRung, $"{name} end rung"));
                bw.Write((byte)0);
            }
        }

        w.Start(lightPositions);

        foreach (TrafficLight light in lights)
            w.V4(light.mPosAndYRotation);

        w.Start(instanceIDs);

        foreach (TrafficLight light in lights)
            bw.Write(light.muInstanceID);

        w.Start(instanceTypes);

        for (int currentI6 = 0; currentI6 < lights.Count; currentI6++)
            bw.Write((byte)Index(refs.LightTypes, lights[currentI6].mType, $"Traffic light ID {lights[currentI6].muInstanceID} type"));

        w.Start(hashTable);

        foreach (int currentI7 in hashed)
            bw.Write(lights[currentI7].muInstanceID);

        w.Start(hashLookup);

        foreach (int currentI8 in hashed)
            bw.Write((ushort)currentI8);

        w.Start(lightTypeBlock);
        int coronaOffset = 0;

        for (int currentI9 = 0; currentI9 < maTrafficLightTypes.Count; currentI9++)
        {
            bw.Write(U8(coronaOffset, "Coronas before a traffic light type"));
            bw.Write(U8(maTrafficLightTypes[currentI9].maCoronas.Count, $"Traffic light type {currentI9} coronas"));
            coronaOffset += maTrafficLightTypes[currentI9].maCoronas.Count;
        }

        w.Start(coronaTypes);

        foreach (Corona corona in coronas)
            bw.Write((byte)corona.meState);

        w.Start(coronaPositions);

        foreach (Corona corona in coronas)
            w.V4(corona.mPosition);

        w.Start(paintColours);

        foreach (PaintColour colour in maPaintColours)
            w.V4(colour.mColour);

        byte[] data = w.Finish();

        if (bigEndian)
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), (uint)data.Length);
        else
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), (uint)data.Length);

        return data;
    }

    private static int RungIndex(Section section, LaneRung rung, string what)
    {
        if (rung == null)
            throw new InvalidDataException($"{what} isn't set.");

        int index = section?.maRungs.IndexOf(rung) ?? -1;
        return index >= 0
            ? index
            : throw new InvalidDataException($"{what} isn't one of its section's rungs.");
    }

    private void WriteHull(FlatWriter w, Hull hull, int hullIndex, int firstLight, FlatWriter.Block block, References refs)
    {
        BinaryWriter2 bw = w.Bw;
        string name = $"Hull {hullIndex}";
        List<Section> sections = hull.maSections;
        List<LightTrigger> triggers = hull.maLightTriggers;
        Dictionary<SectionSpan, int> spanIndexes = Indexes(hull.maSectionSpans);
        Dictionary<JunctionLogicBox, int> junctionIndexes = Indexes(hull.maJunctions);
        Dictionary<LightTriggerStartData, int> startDataIndexes = Indexes(hull.maLightTriggerStartData);
        Dictionary<TrafficLight, int> lightIndexes = Indexes(hull.maTrafficLights);
        FlatWriter.Block sectionBlock = new(), spans = new(), rungs = new(), rungLengths = new(), neighbours = new(), flows = new();
        FlatWriter.Block junctions = new(), stopLines = new(), triggerBlock = new(), startData = new(), junctionLookup = new(), statics = new();

        w.Start(block);
        bw.Write(U8(sections.Count, $"{name} sections"));
        bw.Write(U8(hull.maSectionSpans.Count, $"{name} section spans"));
        bw.Write(U8(hull.maJunctions.Count, $"{name} junctions"));
        bw.Write(U8(sections.Sum(s => s.maStopLines.Count), $"{name} stop lines"));
        bw.Write(U8(sections.Sum(s => s.maLeftNeighbours.Count + s.maRightNeighbours.Count), $"{name} neighbours"));
        bw.Write(U8(hull.maStaticTrafficVehicles.Count, $"{name} static traffic vehicles"));
        bw.Write((byte)UpTo(hull.maVehicleAssets, 16, $"{name} vehicle assets").Count);
        bw.Write((byte)0);
        bw.Write(U16(sections.Sum(s => s.maRungs.Count), $"{name} rungs"));
        bw.Write(U16(firstLight, $"{name} first traffic light"));
        bw.Write(U16(firstLight + hull.maTrafficLights.Count, $"{name} last traffic light"));
        bw.Write(U8(triggers.Count, $"{name} light triggers"));
        bw.Write(U8(hull.maLightTriggerStartData.Count, $"{name} light trigger start data"));
        FlatWriter.Block[] pointers = [sectionBlock, rungs, rungLengths, neighbours, spans, statics, flows, junctions, stopLines, triggerBlock, startData, junctionLookup];

        foreach (FlatWriter.Block pointer in pointers)
            w.Ptr(pointer);

        for (int i = 0; i < 16; i++)
            bw.Write(i < hull.maVehicleAssets.Count
                ? (byte)Index(refs.Assets, hull.maVehicleAssets[i], $"{name} vehicle asset {i}")
                : (byte)0);

        w.Start(sectionBlock);
        int rungOffset = 0, neighbourOffset = 0, stopLineOffset = 0;

        for (int i = 0; i < sections.Count; i++)
        {
            Section section = sections[i];
            string sectionName = $"{name} section {i}";
            byte stopLineStart = byte.MaxValue; // What a section without stop lines has

            if (section.maStopLines.Count > 0)
                stopLineStart = stopLineOffset < byte.MaxValue
                    ? (byte)stopLineOffset
                    : throw new InvalidDataException($"{sectionName}: its stop lines can't start at 255 or later.");

            bw.Write((uint)rungOffset);
            bw.Write(U8(section.maRungs.Count, $"{sectionName} rungs"));
            bw.Write(stopLineStart);
            bw.Write(U8(section.maStopLines.Count, $"{sectionName} stop lines"));
            bw.Write((byte)Index(spanIndexes, section.mSpan, $"{sectionName} span"));
            string[] directions = ["straight on", "left", "right"];
            (int Hull, int Index)[] forward = [.. section.Forward.Select((s, d) => s == null
                ? (0xFFFF, 0xFF)
                : refs.Section(s, $"{sectionName} forward {directions[d]}"))];
            (int Hull, int Index)[] backward = [.. section.Backward.Select((s, d) => s == null
                ? (0xFFFF, 0xFF)
                : refs.Section(s, $"{sectionName} backward {directions[d]}"))];

            foreach ((int h, _) in forward)
                bw.Write((ushort)h);

            foreach ((int h, _) in backward)
                bw.Write((ushort)h);

            foreach ((_, int currentS) in forward)
                bw.Write((byte)currentS);

            foreach ((_, int currentS2) in backward)
                bw.Write((byte)currentS2);

            bw.Write(section.muTurnLeftProb);
            bw.Write(section.muTurnRightProb);
            bw.Write(U16(neighbourOffset, $"{name} neighbours before section {i}"));
            bw.Write(U8(section.maLeftNeighbours.Count, $"{sectionName} left neighbours"));
            bw.Write(U8(section.maRightNeighbours.Count, $"{sectionName} right neighbours"));
            bw.Write(section.muChangeLeftProb);
            bw.Write(section.muChangeRightProb);
            bw.Write((ushort)0);
            bw.Write(section.mfSpeed);
            bw.Write(section.mfLength);
            bw.Write(0u);
            rungOffset += section.maRungs.Count;
            neighbourOffset += section.maLeftNeighbours.Count + section.maRightNeighbours.Count;
            stopLineOffset += section.maStopLines.Count;
        }

        w.Start(spans);

        foreach (SectionSpan span in hull.maSectionSpans)
        {
            bw.Write(span.muMaxVehicles);
            bw.Write((ushort)0);
            bw.Write(span.mfMaxVehicleRecip);
        }

        w.Start(rungs);

        foreach (LaneRung rung in sections.SelectMany(s => s.maRungs))
        {
            if (rung.maPoints?.Length != 2)
                throw new InvalidDataException($"{name}: a rung's maPoints needs exactly 2 values.");

            w.V4(rung.maPoints[0]);
            w.V4(rung.maPoints[1]);
        }

        w.Start(rungLengths);

        foreach (LaneRung rung in sections.SelectMany(s => s.maRungs))
            bw.Write(rung.mfCumulativeLength);

        w.Start(neighbours);

        for (int i = 0; i < sections.Count; i++)
        {
            foreach (Neighbour neighbour in sections[i].maLeftNeighbours.Concat(sections[i].maRightNeighbours))
            {
                string neighbourName = $"{name} section {i} neighbour";
                (int neighbourHull, int neighbourSection) = refs.Section(neighbour.mSection, $"{neighbourName} section");

                if (neighbourHull != hullIndex)
                    throw new InvalidDataException($"{neighbourName} section has to be in the same hull.");

                bw.Write((byte)neighbourSection);
                bw.Write(neighbour.muSharedLength);
                bw.Write((byte)RungIndex(sections[i], neighbour.mOurStartRung, $"{neighbourName} mOurStartRung"));
                bw.Write((byte)RungIndex(neighbour.mSection, neighbour.mTheirStartRung, $"{neighbourName} mTheirStartRung"));
            }
        }

        w.Start(flows);

        for (int i = 0; i < sections.Count; i++)
        {
            bw.Write((ushort)Index(refs.FlowTypes, sections[i].mFlowType, $"{name} section {i} flow type", 0xFFFF));
            bw.Write(sections[i].muVehiclesPerMinute);
        }

        w.Start(junctions);

        for (int i = 0; i < hull.maJunctions.Count; i++)
        {
            JunctionLogicBox junction = hull.maJunctions[i];
            string junctionName = $"{name} junction {i}";
            List<JunctionState> states = UpTo(junction.maStates, 16, $"{junctionName} states");
            List<TrafficLightController> controllers = UpTo(junction.maTrafficLightControllers, 8, $"{junctionName} traffic light controllers");
            bw.Write(junction.muID);

            for (int j = 0; j < 16; j++)
                bw.Write(j < states.Count
                    ? states[j].muStateTiming
                    : (ushort)0);

            for (int j = 0; j < 16; j++)
                bw.Write(j < states.Count
                    ? states[j].muStoppedLightState
                    : (byte)0);

            bw.Write((byte)states.Count);
            bw.Write((byte)controllers.Count);
            bw.Write((ushort)0);
            bw.Write(junction.muEventJunctionID);
            bw.Write(Index(startDataIndexes, junction.mOfflineStartData, $"{junctionName} offline start data", -1));
            bw.Write(Index(startDataIndexes, junction.mOnlineStartData, $"{junctionName} online start data", -1));

            if (muDataVersion >= 45)
                bw.Write(Index(startDataIndexes, junction.mBikeStartData, $"{junctionName} bike start data", -1));

            for (int j = 0; j < 8; j++)
            {
                TrafficLightController controller = j < controllers.Count
                    ? controllers[j]
                    : new();
                string controllerName = $"{junctionName} controller {j}";
                List<TrafficLight> controllerLights = UpTo(controller.maTrafficLights, 2, $"{controllerName} traffic lights");
                List<StopLine> controllerStopLines = UpTo(controller.maStopLines, 6, $"{controllerName} stop lines");
                (int Hull, int Index)[] stops = [.. controllerStopLines.Select((s, k) => s != null && refs.StopLines.TryGetValue(s, out (int Hull, int Index) at)
                    ? at
                    : throw new InvalidDataException($"{controllerName} stop line {k} refers to one that's been deleted."))];

                for (int currentK = 0; currentK < 2; currentK++)
                    bw.Write(currentK < controllerLights.Count
                        ? (ushort)(firstLight + Index(lightIndexes, controllerLights[currentK], $"{controllerName} traffic light {currentK}"))
                        : (ushort)0);

                for (int currentK2 = 0; currentK2 < 6; currentK2++)
                    bw.Write(currentK2 < stops.Length
                        ? U8(stops[currentK2].Index, $"{controllerName} stop line {currentK2}")
                        : (byte)0);

                for (int currentK3 = 0; currentK3 < 6; currentK3++)
                    bw.Write(currentK3 < stops.Length
                        ? (ushort)stops[currentK3].Hull
                        : (ushort)0);

                bw.Write((byte)stops.Length);
                bw.Write((byte)controllerLights.Count);
            }

            bw.Write(new byte[muDataVersion >= 45
                ? 8
                : 12]);
            w.V4(junction.mPosition);
        }

        w.Start(stopLines);

        foreach (StopLine stopLine in sections.SelectMany(s => s.maStopLines))
            bw.Write(stopLine.muParamFixed);

        w.Start(triggerBlock);

        foreach (LightTrigger trigger in triggers)
        {
            w.V4(trigger.mDimensions);
            w.V4(trigger.mPosPlusYRot);
        }

        w.Start(startData);

        for (int i = 0; i < hull.maLightTriggerStartData.Count; i++)
        {
            LightTriggerStartData data = hull.maLightTriggerStartData[i];
            List<StartingPosition> positions = UpTo(data.maStartingPositions, 8, $"{name} start data {i} starting positions");
            List<Destination> destinations = UpTo(data.maDestinations, 16, $"{name} start data {i} destinations");

            for (int j = 0; j < 8; j++)
                w.V4(j < positions.Count
                    ? positions[j].mPosition
                    : Vector4.Zero);

            for (int j = 0; j < 8; j++)
                w.V4(j < positions.Count
                    ? positions[j].mDirection
                    : Vector4.Zero);

            for (int j = 0; j < 16; j++)
                bw.Write(j < destinations.Count
                    ? destinations[j].mID
                    : 0UL);

            for (int j = 0; j < 16; j++)
                bw.Write(j < destinations.Count
                    ? destinations[j].meDifficulty
                    : (byte)0);

            bw.Write((byte)positions.Count);
            bw.Write((byte)destinations.Count);
            bw.Write(data.muNumLanes);
            bw.Write(new byte[13]);
        }

        w.Start(junctionLookup);

        for (int i = 0; i < triggers.Count; i++)
            bw.Write((byte)Index(junctionIndexes, triggers[i].mJunction, $"{name} light trigger {i} junction"));

        w.Start(statics);

        for (int i = 0; i < hull.maStaticTrafficVehicles.Count; i++)
        {
            StaticTrafficVehicle vehicle = hull.maStaticTrafficVehicles[i];

            if (vehicle.mTransform?.Length != 4)
                throw new InvalidDataException($"{name} static traffic vehicle {i} mTransform needs exactly 4 rows.");

            foreach (Vector4 row in vehicle.mTransform)
                w.V4(row);

            bw.Write((ushort)Index(refs.FlowTypes, vehicle.mFlowType, $"{name} static traffic vehicle {i} flow type", 0xFFFF));
            bw.Write(vehicle.mExistsAtAllChance);
            bw.Write(vehicle.muFlags);
            bw.Write(new byte[12]);
        }
    }

    public bool Write(BundleEntry entry)
    {
        entry.SetPrimary(Serialize(entry), entry.GetImports());
        return true;
    }
}
