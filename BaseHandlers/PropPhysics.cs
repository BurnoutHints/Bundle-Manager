using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

// BrnPhysics::Props::PropPhysicsDataHeader, https://burnout.wiki/wiki/Prop_Physics
public class PropPhysics : IEntryData
{
    public enum VolumeType : uint
    {
        E_VOLUMETYPE_SPHERE = 1,
        E_VOLUMETYPE_CAPSULE = 2,
        E_VOLUMETYPE_TRIANGLE = 3,
        E_VOLUMETYPE_BOX = 4,
        E_VOLUMETYPE_CYLINDER = 5
    }

    public enum JointType : byte
    {
        E_JOINT_NONE = 0,
        E_JOINT_LEAN = 1,
        E_JOINT_TILT = 2
    }

    public enum ExtraTypeInfo : byte
    {
        E_EXTRA_NONE = 0,
        E_EXTRA_TRAFFIC_LIGHT = 1
    }

    private const int MaxTypes = 500, MaxParts = 300, MaxVolumes = 2048, MaxVolumesPerType = 64,
        DevelopmentMaxParts = 200, DevelopmentMaxVolumes = 10240, DevelopmentPartSize = 0xA0;

    // rw::collision::Volume
    public class Volume
    {
        [Category("Properties"), Description("Sphere, capsule and cylinder volumes use the radius, capsule and cylinder volumes have their height along local Z.")]
        public VolumeType meType { get; set; } = VolumeType.E_VOLUMETYPE_BOX;

        [Category("Geometry"), Description("Matrix44Affine, by row. Relative to the prop, or to its part for a part's volume."), TypeConverter(typeof(ArrayTextConverter))]
        public Vector4[] mTransform { get; set; } = [new(1, 0, 0, 0), new(0, 1, 0, 0), new(0, 0, 1, 0), new(0, 0, 0, 0)];

        [Category("Geometry"), Description("A box's half extents on X, Y and Z. X is the half height of a capsule or cylinder.")]
        public Vector3 mExtent { get; set; } = new(0.5f);

        [Category("Geometry"), Description("radius.")]
        public float mfRadius { get; set; }

        [Category("Advanced"), Description("groupID.")]
        public uint muGroupID { get; set; }

        [Category("Advanced"), Description("surfaceID.")]
        public uint muSurfaceID { get; set; }

        [Category("Advanced"), Description("m_flags.")]
        public uint muFlags { get; set; } = 1;

        public override string ToString() => meType switch
        {
            VolumeType.E_VOLUMETYPE_BOX => $"Box {mExtent.X * 2:0.##} × {mExtent.Y * 2:0.##} × {mExtent.Z * 2:0.##}",
            VolumeType.E_VOLUMETYPE_SPHERE => $"Sphere radius {mfRadius:0.##}",
            VolumeType.E_VOLUMETYPE_CAPSULE => $"Capsule radius {mfRadius:0.##}, height {mExtent.X * 2:0.##}",
            VolumeType.E_VOLUMETYPE_CYLINDER => $"Cylinder radius {mfRadius:0.##}, height {mExtent.X * 2:0.##}",
            _ => meType.ToString()
        };
    }

    public class PropPartTypeData
    {
        [Category("Geometry"), Description("Where the part is on the prop.")]
        public Vector4 mOffset { get; set; }

        [Category("Physics"), Description("Likely kg·m².")]
        public Vector4 mInertia { get; set; }

        [Category("Physics"), Description("kg.")]
        public float mfMass { get; set; } = 1;

        [Category("Geometry")]
        public float mfSphereRadius { get; set; }

        [Browsable(false)] public List<Volume> maCollisionVolumes { get; set; } = [];

        public override string ToString() => $"{mfMass:0.##} kg, {maCollisionVolumes.Count} volumes";
    }

    public class PropTypeData
    {
        [Category("Geometry"), Description("Where the joint is on the prop."), OnlyIn(development: false)]
        public Vector4 mJointLocator { get; set; }

        [Category("Physics")]
        public Vector4 mCOMOffset { get; set; }

        [Category("Physics"), Description("Likely kg·m².")]
        public Vector4 mInertia { get; set; }

        [Category("Properties"), Description("The prop's model resource."), TypeConverter(typeof(HexConverter))]
        public ulong mResourceId { get; set; }

        [Category("Physics"), Description("kg.")]
        public float mfMass { get; set; } = 1;

        [Category("Geometry")]
        public float mfSphereRadius { get; set; }

        [Category("Physics"), Description("Cosine of how far the joint can bend."), OnlyIn(development: false)]
        public float mfMaxJointAngleCos { get; set; }

        [Category("Physics"), Description("Speed that makes the prop lean, MPH."), OnlyIn(development: false)]
        public float mfLeanThreshold { get; set; }

        [Category("Physics"), Description("Speed that moves the prop, MPH."), OnlyIn(development: false)]
        public float mfMoveThreshold { get; set; }

        [Category("Physics"), Description("Speed that smashes the prop, MPH. Only props with parts smash."), OnlyIn(development: false)]
        public float mfSmashThreshold { get; set; }

        [Category("Properties"), Description("The model's GameDB ID. The game treats some IDs specially, such as lampposts and overhead signs."), OnlyIn(development: false)]
        public uint muSceneUriId { get; set; }

        [Category("Advanced"), Description("Always 0 in the game's data.")]
        public byte muMaxState { get; set; }

        [Category("Physics"), Description("mu8JointType."), OnlyIn(development: false)]
        public JointType mu8JointType { get; set; }

        [Category("Properties"), Description("mu8ExtraTypeInfo."), OnlyIn(development: false)]
        public ExtraTypeInfo mu8ExtraTypeInfo { get; set; }

        [Category("Properties"), OnlyIn(development: true)]
        public uint muCrashModeScore { get; set; }

        [Category("Physics"), Description("One for each change of state."), TypeConverter(typeof(ArrayTextConverter)), OnlyIn(development: true)]
        public float[] mfaCollisionThresholds { get; set; } = new float[3];

        [Category("Physics"), Description("One for each collision event."), TypeConverter(typeof(ArrayTextConverter)), OnlyIn(development: true)]
        public uint[] muaResponses { get; set; } = new uint[7];

        [Category("Physics"), OnlyIn(development: true)]
        public byte muNumberOfJoints { get; set; }

        [Browsable(false)] public List<Volume> maCollisionVolumes { get; set; } = [];

        [Browsable(false)] public List<PropPartTypeData> maParts { get; set; } = [];

        public override string ToString()
        {
            string name = ResourceDB.Lookup(mResourceId);
            return $"{name ?? (TrafficLabels.Development
                ? $"0x{mResourceId:X}"
                : muSceneUriId.ToString())}, {mfMass:0.##} kg{(maParts.Count > 0
                ? $", {maParts.Count} parts"
                : "")}";
        }
    }

    [Category("Properties"), Description("When the data was built, 0 in the PC versions."), OnlyIn(development: false)]
    public uint muTimeStamp { get; set; }

    // BND1 layout (Feb 2007)
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Development { get; set; }

    private int PartLimit => Development
        ? DevelopmentMaxParts
        : MaxParts;

    private int VolumeLimit => Development
        ? DevelopmentMaxVolumes
        : MaxVolumes;

    // Unused dummy parts pointer emitted by tools for parts-free props
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ulong NoPartsPointer { get; set; }

    // Note: muSizeInBytes is little-endian on X360
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SizeIsLittleEndian { get; set; }

    [Browsable(false)] public List<PropTypeData> maPropTypes { get; set; } = [];

    public IEntryEditor GetEditor(BundleEntry entry) => new PropPhysicsEditor(this, entry);

    public EntryType GetEntryType(BundleEntry entry) => EntryType.PropPhysics;

    private static int PointerSize(BundleEntry entry) => entry.Platform == BundlePlatform.PCx64
        ? 8
        : 4;

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        byte[] bytes = entry.GetPrimaryWithoutImports();
        using BinaryReader2 br = new(new MemoryStream(bytes), entry.Console);
        int pointerSize = PointerSize(entry);

        long ptr()
        {
            br.BaseStream.Position = (br.BaseStream.Position + pointerSize - 1) & -pointerSize;
            return pointerSize == 8
                ? (long)br.ReadUInt64()
                : br.ReadUInt32();
        }

        Vector4 v4() => new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        Volume volume()
        {
            Volume item = new()
            {
                mTransform = [v4(), v4(), v4(), v4()],
                meType = (VolumeType)br.ReadUInt32(),
                mExtent = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                mfRadius = br.ReadSingle(),
                muGroupID = br.ReadUInt32(),
                muSurfaceID = br.ReadUInt32(),
                muFlags = br.ReadUInt32()
            };

            return item;
        }

        List<Volume> volumes(long offset, int count)
        {
            long back = br.BaseStream.Position;
            br.BaseStream.Position = offset;
            List<Volume> items = new(count);

            for (int i = 0; i < count; i++)
                items.Add(volume());

            br.BaseStream.Position = back;
            return items;
        }

        // Unused BodySpec fields (muIndex, transform, and pointers are zeroed)
        (float Mass, Vector4 Inertia) body(long at)
        {
            br.BaseStream.Position = at + 4;
            float mass = br.ReadSingle();
            br.BaseStream.Position = at + 0x10;
            return (mass, v4());
        }

        PropTypeData developmentType(long at)
        {
            (float mass, Vector4 inertia) = body(at);
            br.BaseStream.Position = at + 0x70;
            long volumesAt = ptr(), partsAt = ptr();
            br.ReadUInt32();
            PropTypeData type = new()
            {
                mfMass = mass,
                mInertia = inertia,
                muCrashModeScore = br.ReadUInt32(),
                mfSphereRadius = br.ReadSingle(),
                mfaCollisionThresholds = [br.ReadSingle(), br.ReadSingle(), br.ReadSingle()],
                muaResponses = [br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32()],
                muMaxState = br.ReadByte()
            };

            int partCount = br.ReadByte(), volumeCount = br.ReadByte();
            type.muNumberOfJoints = br.ReadByte();
            type.mCOMOffset = v4();
            type.mResourceId = br.ReadUInt64();
            type.maCollisionVolumes = volumes(volumesAt, volumeCount);

            if (partCount == 0)
                NoPartsPointer = (ulong)partsAt;

            for (int p = 0; p < partCount; p++)
            {
                long partAt = partsAt + p * DevelopmentPartSize;
                (float partMass, Vector4 partInertia) = body(partAt + 0x10);
                br.BaseStream.Position = partAt + 0x80;
                long partVolumesAt = ptr();
                int partVolumeCount = br.ReadByte();
                br.BaseStream.Position = partAt + 0x88;
                float radius = br.ReadSingle();
                br.BaseStream.Position = partAt + 0x90;
                type.maParts.Add(new()
                {
                    mfMass = partMass,
                    mInertia = partInertia,
                    mfSphereRadius = radius,
                    mOffset = v4(),
                    maCollisionVolumes = volumes(partVolumesAt, partVolumeCount)
                });
            }

            return type;
        }

        int typeCount = br.ReadInt32();
        br.ReadInt32();
        br.ReadInt32();
        uint size = br.ReadUInt32();
        SizeIsLittleEndian = entry.Console && size != bytes.Length && BinaryPrimitives.ReverseEndianness(size) == bytes.Length;

        if (typeCount > MaxTypes)
            throw new ReadFailedError($"Prop physics has {typeCount} prop types, more than the {MaxTypes} there's room for.");

        long[] typesAt = new long[typeCount];

        for (int i = 0; i < typeCount; i++)
            typesAt[i] = ptr();

        Development = entry.Archive?.BNDVersion == 1;

        if (Development && typeCount > 0 && typesAt[0] != 0x10 + pointerSize * (MaxTypes + DevelopmentMaxParts + DevelopmentMaxVolumes))
            throw new ReadFailedError("Only prop physics from the Feb 2007 builds can be read from a BND1 bundle.");

        if (!Development)
        {
            br.BaseStream.Position = 0x10 + pointerSize * (MaxTypes + MaxParts + MaxVolumes);
            muTimeStamp = br.ReadUInt32();
        }

        maPropTypes = [];

        foreach (long at in typesAt)
        {
            if (Development)
            {
                maPropTypes.Add(developmentType(at));
                continue;
            }

            br.BaseStream.Position = at;
            PropTypeData type = new()
            {
                mJointLocator = v4(),
                mCOMOffset = v4(),
                mInertia = v4(),
                mResourceId = br.ReadUInt64(),
                mfMass = br.ReadSingle()
            };

            long volumesAt = ptr(), partsAt = ptr();
            type.mfSphereRadius = br.ReadSingle();
            type.mfMaxJointAngleCos = br.ReadSingle();
            type.mfLeanThreshold = br.ReadSingle();
            type.mfMoveThreshold = br.ReadSingle();
            type.mfSmashThreshold = br.ReadSingle();
            type.muSceneUriId = br.ReadUInt32();
            type.muMaxState = br.ReadByte();
            int partCount = br.ReadByte(), volumeCount = br.ReadByte();
            type.mu8JointType = (JointType)br.ReadByte();
            type.mu8ExtraTypeInfo = (ExtraTypeInfo)br.ReadByte();
            type.maCollisionVolumes = volumes(volumesAt, volumeCount);

            if (partCount == 0)
                NoPartsPointer = (ulong)partsAt;
            else
                br.BaseStream.Position = partsAt;

            for (int p = 0; p < partCount; p++)
            {
                PropPartTypeData part = new()
                {
                    mOffset = v4(),
                    mInertia = v4(),
                    mfMass = br.ReadSingle()
                };

                long partVolumesAt = ptr();
                part.mfSphereRadius = br.ReadSingle();
                int partVolumeCount = br.ReadByte();
                br.BaseStream.Position = (br.BaseStream.Position + 15) & -16;
                part.maCollisionVolumes = volumes(partVolumesAt, partVolumeCount);
                type.maParts.Add(part);
            }

            maPropTypes.Add(type);
        }

        return true;
    }

    // Validates structural constraints required by the game engine
    public IEnumerable<TrafficRules.Issue> Problems()
    {
        if (maPropTypes.Count > MaxTypes)
            yield return new(maPropTypes, null, $"There are {maPropTypes.Count} prop types, the game has room for {MaxTypes}.");

        int parts = maPropTypes.Sum(t => t.maParts.Count);

        if (parts > PartLimit)
            yield return new(maPropTypes, null, $"There are {parts} parts over all the prop types, the game has room for {PartLimit}.");

        int volumes = maPropTypes.Sum(t => t.maCollisionVolumes.Count + t.maParts.Sum(p => p.maCollisionVolumes.Count));

        if (volumes > VolumeLimit)
            yield return new(maPropTypes, null, $"There are {volumes} volumes over all the prop types, the game has room for {VolumeLimit}.");

        foreach (PropTypeData type in maPropTypes)
        {
            int index = maPropTypes.IndexOf(type);
            int typeVolumes = type.maCollisionVolumes.Count + type.maParts.Sum(p => p.maCollisionVolumes.Count);

            if (type.maCollisionVolumes.Count > byte.MaxValue || type.maParts.Count > byte.MaxValue)
                yield return new(type, null, $"Prop type {index} has more parts or volumes than the format can hold.");

            if (typeVolumes >= MaxVolumesPerType)
                yield return new(type, nameof(PropTypeData.maCollisionVolumes), $"Prop type {index} has {typeVolumes} volumes counting its parts', the game numbers them in 6 bits so it needs fewer than {MaxVolumesPerType}.");

            if (!(type.mfMass > 0))
                yield return new(type, nameof(PropTypeData.mfMass), $"Prop type {index} needs a mass above 0.");

            if (Development && (type.mfaCollisionThresholds?.Length != 3 || type.muaResponses?.Length != 7))
                yield return new(type, nameof(PropTypeData.mfaCollisionThresholds), $"Prop type {index} needs 3 collision thresholds and 7 responses.");

            foreach (Volume volume in type.maCollisionVolumes.Concat(type.maParts.SelectMany(p => p.maCollisionVolumes)))
            {
                if (!Enum.IsDefined(volume.meType))
                    yield return new(volume, nameof(Volume.meType), $"A volume of prop type {index} doesn't have a type the game can load.");

                if (volume.mTransform?.Length != 4)
                    yield return new(volume, nameof(Volume.mTransform), $"A volume of prop type {index} needs a transform of 4 rows.");
            }

            foreach (PropPartTypeData part in type.maParts)
            {
                if (part.maCollisionVolumes.Count > byte.MaxValue)
                    yield return new(part, null, $"A part of prop type {index} has more volumes than the format can hold.");

                if (!(part.mfMass > 0))
                    yield return new(part, nameof(PropPartTypeData.mfMass), $"A part of prop type {index} needs a mass above 0.");
            }
        }
    }

    public byte[] Serialize(BundleEntry entry) => Serialize(entry.Console, PointerSize(entry));

    public byte[] Serialize(bool bigEndian, int pointerSize)
    {
        if (Problems().FirstOrDefault() is TrafficRules.Issue problem)
            throw new InvalidDataException(problem.Message);

        Traffic.FlatWriter w = new(bigEndian, pointerSize);
        BinaryWriter2 bw = w.Bw;
        Dictionary<object, Traffic.FlatWriter.Block> blocks = new(ReferenceEqualityComparer.Instance);

        Traffic.FlatWriter.Block blockOf(object item)
        {
            if (!blocks.TryGetValue(item, out Traffic.FlatWriter.Block block))
                blocks[item] = block = new();

            return block;
        }

        Traffic.FlatWriter.Block startOf(List<Volume> list) => blockOf(list.Count > 0
            ? list[0]
            : list);

        void pointers(IEnumerable<object> items, int count, int capacity)
        {
            foreach (object item in items)
                w.Ptr(blockOf(item));

            bw.Write(new byte[(capacity - count) * pointerSize]);
        }

        List<PropPartTypeData> parts = [.. maPropTypes.SelectMany(t => t.maParts)];
        List<Volume> volumes = [.. maPropTypes.SelectMany(t => t.maCollisionVolumes.Concat(t.maParts.SelectMany(p => p.maCollisionVolumes)))];
        bw.Write(maPropTypes.Count);
        bw.Write(volumes.Count);
        bw.Write(parts.Count);
        bw.Write(0u);
        pointers(maPropTypes, maPropTypes.Count, MaxTypes);
        pointers(parts, parts.Count, PartLimit);
        pointers(volumes, volumes.Count, VolumeLimit);

        if (!Development)
            bw.Write(muTimeStamp);

        void writeVolumes(List<Volume> list)
        {
            if (list.Count == 0)
                w.Start(blockOf(list));

            foreach (Volume volume in list)
            {
                w.Start(blockOf(volume));

                foreach (Vector4 row in volume.mTransform)
                    w.V4(row);

                bw.Write((uint)volume.meType);
                bw.Write(volume.mExtent.X);
                bw.Write(volume.mExtent.Y);
                bw.Write(volume.mExtent.Z);
                bw.Write(volume.mfRadius);
                bw.Write(volume.muGroupID);
                bw.Write(volume.muSurfaceID);
                bw.Write(volume.muFlags);
            }
        }

        void partsPointer(PropTypeData type)
        {
            if (type.maParts.Count > 0)
                w.Ptr(blockOf(type.maParts[0]));
            else
            {
                w.Pad(pointerSize);

                if (pointerSize == 8)
                    bw.Write(NoPartsPointer);
                else
                    bw.Write((uint)NoPartsPointer);
            }
        }

        void body(float mass, Vector4 inertia)
        {
            bw.Write(0u);
            bw.Write(mass);
            w.Pad(16);
            w.V4(inertia);
            bw.Write(new byte[0x50]);
        }

        void writeDevelopmentType(PropTypeData type)
        {
            body(type.mfMass, type.mInertia);
            w.Ptr(startOf(type.maCollisionVolumes));
            partsPointer(type);
            bw.Write(0u);
            bw.Write(type.muCrashModeScore);
            bw.Write(type.mfSphereRadius);

            foreach (float threshold in type.mfaCollisionThresholds)
                bw.Write(threshold);

            foreach (uint response in type.muaResponses)
                bw.Write(response);

            bw.Write(type.muMaxState);
            bw.Write((byte)type.maParts.Count);
            bw.Write((byte)type.maCollisionVolumes.Count);
            bw.Write(type.muNumberOfJoints);
            w.V4(type.mCOMOffset);
            bw.Write(type.mResourceId);
            w.Pad(16);
        }

        void writeDevelopmentPart(PropPartTypeData part)
        {
            bw.Write(0u);
            w.Pad(16);
            body(part.mfMass, part.mInertia);
            w.Ptr(startOf(part.maCollisionVolumes));
            bw.Write((byte)part.maCollisionVolumes.Count);
            w.Pad(8);
            bw.Write(part.mfSphereRadius);
            w.Pad(16);
            w.V4(part.mOffset);
        }

        void writeType(PropTypeData type)
        {
            w.V4(type.mJointLocator);
            w.V4(type.mCOMOffset);
            w.V4(type.mInertia);
            bw.Write(type.mResourceId);
            bw.Write(type.mfMass);
            w.Ptr(startOf(type.maCollisionVolumes));
            partsPointer(type);
            bw.Write(type.mfSphereRadius);
            bw.Write(type.mfMaxJointAngleCos);
            bw.Write(type.mfLeanThreshold);
            bw.Write(type.mfMoveThreshold);
            bw.Write(type.mfSmashThreshold);
            bw.Write(type.muSceneUriId);
            bw.Write(type.muMaxState);
            bw.Write((byte)type.maParts.Count);
            bw.Write((byte)type.maCollisionVolumes.Count);
            bw.Write((byte)type.mu8JointType);
            bw.Write((byte)type.mu8ExtraTypeInfo);
        }

        void writePart(PropPartTypeData part)
        {
            w.V4(part.mOffset);
            w.V4(part.mInertia);
            bw.Write(part.mfMass);
            w.Ptr(startOf(part.maCollisionVolumes));
            bw.Write(part.mfSphereRadius);
            bw.Write((byte)part.maCollisionVolumes.Count);
        }

        foreach (PropTypeData type in maPropTypes)
        {
            w.Start(blockOf(type));

            if (Development)
                writeDevelopmentType(type);
            else
                writeType(type);

            foreach (PropPartTypeData part in type.maParts)
            {
                w.Start(blockOf(part));

                if (Development)
                    writeDevelopmentPart(part);
                else
                    writePart(part);
            }

            writeVolumes(type.maCollisionVolumes);

            foreach (PropPartTypeData part in type.maParts)
                writeVolumes(part.maCollisionVolumes);
        }

        uint size = (uint)bw.BaseStream.Position;
        w.Finish();
        bw.BaseStream.Position = 0xC;
        bw.Write(bigEndian && SizeIsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(size)
            : size);
        return ((MemoryStream)bw.BaseStream).ToArray();
    }

    public bool Write(BundleEntry entry)
    {
        entry.SetPrimary(Serialize(entry), entry.GetImports());
        return true;
    }
}
