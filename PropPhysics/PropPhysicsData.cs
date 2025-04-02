using BundleFormat;
using BundleUtilities;
using PluginAPI;
using System;
using System.Data;
using System.Numerics;

namespace PropPhysics
{
    public class PropPhysicsData : IEntryData
    {
        public class Volume
        {
            public class BoxSpecificData
            {
                public float hx;
                public float hy;
                public float hz;

                public BoxSpecificData()
                {
                    hx = 0;
                    hy = 0;
                    hz = 0;
                }

                public BoxSpecificData(float hx, float hy, float hz)
                {
                    this.hx = hx;
                    this.hy = hy;
                    this.hz = hz;
                }
            }
            
            public Matrix4x4 transform;
            public uint vtablePtr;
            public BoxSpecificData data = new();
            public float radius;
            public uint groupId;
            public uint surfaceId;
            public uint flags;
        }

        public class PropPartTypeData
        {
            public Vector3 offset;
            public Vector3 inertia;
            public float mass;
            public uint collisionVolumesStartIndex;
            public float sphereRadius;
            public byte numVolumes;
        }

        public class PropTypeData
        {
            public Vector3 jointLocator;
            public Vector3 comOffset;
            public Vector3 inertia;
            public ulong resourceId;
            public float mass;
            public uint collisionVolumesStartIndex;
            public uint partsStartIndex;
            public float sphereRadius;
            public float maxJointAngleCos;
            public float leanThreshold;
            public float moveThreshold;
            public float smashThreshold;
            public uint sceneUriId;
            public byte maxState;
            public byte numParts;
            public byte numVolumes;
            public byte jointType;
            public byte extraTypeInfo;
        }

        private uint _numPropTypes;
        public uint NumPropTypes { get => _numPropTypes; }
        private uint numVolumeTypes;
        private uint numPropPartTypes;
        private uint resourceSize;
        private List<PropTypeData> _propTypes = [];
        public List<PropTypeData> PropTypes { get => _propTypes; }
        private List<PropPartTypeData> _propPartTypes = [];
        private List<Volume> _volumeTypes = [];
        private uint timestamp;

        private const int propTypeAlloc = 500;
        private const int propPartTypeAlloc = 300;
        private const int volumeTypeAlloc = 2048;

        private const int propTypeStructLength = 0x70;
        private const int propPartTypeStructLength = 0x30;
        private const int volumeTypeStructLength = 0x60;

        public EntryType GetEntryType(BundleEntry entry)
        {
            return EntryType.PropPhysics;
        }
        
        public IEntryEditor GetEditor(BundleEntry entry)
        {
            PropPhysicsEditor propPhysics = new();
            propPhysics.Data = this;
            propPhysics.Edit += () =>
            {
                Write(entry);
            };

            return propPhysics;
        }

        public void Clear()
        {
            _volumeTypes.Clear();
            _propPartTypes.Clear();
            _propTypes.Clear();
            _numPropTypes = 0;
            numPropPartTypes = 0;
            numVolumeTypes = 0;
        }

        public bool Read(BundleEntry entry, ILoader loader = null)
        {
            Clear();
            MemoryStream ms = new(entry.EntryBlocks[0].Data);
            BinaryReader2 br = new(ms, entry.Console);

            List<uint> propTypePtrs = [];
            List<uint> propPartTypePtrs = [];
            List<uint> volumeTypePtrs = [];
            if (!ReadHeader(br, propTypePtrs, propPartTypePtrs, volumeTypePtrs))
                return false;
            for (int i = 0; i < volumeTypePtrs.Count; ++i)
            {
                br.BaseStream.Position = volumeTypePtrs[i];
                _volumeTypes.Add(ReadVolumeType(br));
            }
            for (int i = 0; i < propPartTypePtrs.Count; ++i)
            {
                br.BaseStream.Position = propPartTypePtrs[i];
                _propPartTypes.Add(ReadPropPartType(br, volumeTypePtrs));
            }
            for (int i = 0; i < propTypePtrs.Count; ++i)
            {
                br.BaseStream.Position = propTypePtrs[i];
                _propTypes.Add(ReadPropType(br, volumeTypePtrs, propPartTypePtrs));
            }

            return true;
        }

        public bool Write(BundleEntry entry)
        {
            // This works as a hack as long as nothing is added or deleted.
            MemoryStream ms = new MemoryStream();
            BinaryWriter2 bw = new BinaryWriter2(ms);
            bw.BigEndian = entry.Console;

            bw.Write(PropTypes.Count);
            bw.Write(_volumeTypes.Count);
            bw.Write(_propPartTypes.Count);
            bw.Write(0); // Reserve resource size
            for (int i = 0; i < propTypeAlloc; ++i)
                bw.Write(0); // Reserve prop type ptrs
            for (int i = 0; i < volumeTypeAlloc; ++i)
                bw.Write(0); // Reserve volume type ptrs
            for (int i = 0; i < propPartTypeAlloc; ++i)
                bw.Write(0); // Reserve prop part type ptrs
            bw.Write(timestamp);

            bw.Align(0x10);
            List<uint> propTypePtrs = [];
            List<uint> propPartTypePtrs = [];
            List<uint> volumeTypePtrs = [];
            foreach (PropTypeData propType in PropTypes)
            {
                propTypePtrs.Add((uint)bw.BaseStream.Position);
                bw.Write(propType.jointLocator.X);
                bw.Write(propType.jointLocator.Y);
                bw.Write(propType.jointLocator.Z);
                bw.Write(0);
                bw.Write(propType.comOffset.X);
                bw.Write(propType.comOffset.Y);
                bw.Write(propType.comOffset.Z);
                bw.Write(0);
                bw.Write(propType.inertia.X);
                bw.Write(propType.inertia.Y);
                bw.Write(propType.inertia.Z);
                bw.Write(0);
                bw.Write(propType.resourceId);
                bw.Write(propType.mass);
                if (propType.numVolumes > 0 && propTypePtrs.Count > 0)
                    bw.Write((uint)(propTypePtrs.Last() + propTypeStructLength
                        + propPartTypeStructLength * propType.numParts));
                else
                    bw.Write(0);
                if (propType.numParts > 0 && propTypePtrs.Count > 0)
                    bw.Write((uint)(propTypePtrs.Last() + propTypeStructLength));
                else
                    bw.Write(0);
                bw.Write(propType.sphereRadius);
                bw.Write(propType.maxJointAngleCos);
                bw.Write(propType.leanThreshold);
                bw.Write(propType.moveThreshold);
                bw.Write(propType.smashThreshold);
                bw.Write(propType.sceneUriId);
                bw.Write(propType.maxState);
                bw.Write(propType.numParts);
                bw.Write(propType.numVolumes);
                bw.Write(propType.jointType);
                bw.Write(propType.extraTypeInfo);
                bw.Align(0x10);
                int prevPartVolumeCount = 0;
                for (int i = 0; i < propType.numParts; ++i)
                {
                    propPartTypePtrs.Add((uint)bw.BaseStream.Position);
                    bw.Write(_propPartTypes[(int)propType.partsStartIndex + i].offset.X);
                    bw.Write(_propPartTypes[(int)propType.partsStartIndex + i].offset.Y);
                    bw.Write(_propPartTypes[(int)propType.partsStartIndex + i].offset.Z);
                    bw.Write(0);
                    bw.Write(_propPartTypes[(int)propType.partsStartIndex + i].inertia.X);
                    bw.Write(_propPartTypes[(int)propType.partsStartIndex + i].inertia.Y);
                    bw.Write(_propPartTypes[(int)propType.partsStartIndex + i].inertia.Z);
                    bw.Write(0);
                    bw.Write(_propPartTypes[(int)propType.partsStartIndex + i].mass);
                    if (_propPartTypes[(int)propType.partsStartIndex + i].numVolumes > 0
                        && propTypePtrs.Count > 0)
                    {
                        bw.Write((uint)(propTypePtrs.Last() + propTypeStructLength
                            + propPartTypeStructLength * propType.numParts
                            + volumeTypeStructLength * propType.numVolumes
                            + volumeTypeStructLength * prevPartVolumeCount));
                        prevPartVolumeCount += _propPartTypes[(int)propType.partsStartIndex + i].numVolumes;
                    }
                    else
                        bw.Write(0);
                    bw.Write(_propPartTypes[(int)propType.partsStartIndex + i].sphereRadius);
                    bw.Write(_propPartTypes[(int)propType.partsStartIndex + i].numVolumes);
                    bw.Align(0x10);
                }
                for (int i = 0; i < propType.numVolumes; ++i)
                {
                    volumeTypePtrs.Add((uint)bw.BaseStream.Position);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M11);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M12);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M13);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M14);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M21);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M22);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M23);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M24);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M31);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M32);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M33);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M34);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M41);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M42);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M43);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].transform.M44);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].vtablePtr);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].data.hx);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].data.hy);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].data.hz);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].radius);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].groupId);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].surfaceId);
                    bw.Write(_volumeTypes[(int)propType.collisionVolumesStartIndex + i].flags);
                }
                if ((int)propType.partsStartIndex == -1)
                    continue;
                for (int i = (int)propType.partsStartIndex; i < propType.partsStartIndex + propType.numParts; ++i)
                {
                    for (int j = 0; j < _propPartTypes[i].numVolumes; ++j)
                    {
                        volumeTypePtrs.Add((uint)bw.BaseStream.Position);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M11);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M12);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M13);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M14);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M21);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M22);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M23);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M24);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M31);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M32);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M33);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M34);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M41);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M42);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M43);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].transform.M44);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].vtablePtr);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].data.hx);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].data.hy);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].data.hz);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].radius);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].groupId);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].surfaceId);
                        bw.Write(_volumeTypes[(int)_propPartTypes[i].collisionVolumesStartIndex + j].flags);
                    }
                }
            }

            // Write resource size
            resourceSize = (uint)bw.BaseStream.Position;
            bw.Align(0x10);
            bw.BaseStream.Position = 0xC;
            bw.Write(resourceSize);

            // Write header pointer blocks
            bw.BaseStream.Position = 0x10;
            foreach (uint propTypePtr in propTypePtrs)
                bw.Write(propTypePtr);
            bw.BaseStream.Position = 0x7E0;
            foreach (uint propPartTypePtr in propPartTypePtrs)
                bw.Write(propPartTypePtr);
            bw.BaseStream.Position = 0xC90;
            foreach (uint volumeTypePtr in volumeTypePtrs)
                bw.Write(volumeTypePtr);

            bw.Flush();
            byte[] data = ms.ToArray();
            bw.Close();
            ms.Close();

            entry.EntryBlocks[0].Data = data;
            entry.Dirty = true;

            return true;
        }

        private bool ReadHeader(BinaryReader2 br, List<uint> propTypePtrs, List<uint> propPartTypesPtrs, List<uint> volumeTypePtrs)
        {
            // Read num entries and validate
            _numPropTypes = br.ReadUInt32();
            if (_numPropTypes > propTypeAlloc)
            {
                MessageBox.Show("Number of prop types (" + _numPropTypes + ") >= array size (" + propTypeAlloc + ")",
                    "Error", MessageBoxButtons.OK);
                return false;
            }
            numVolumeTypes = br.ReadUInt32();
            if (numPropPartTypes > propPartTypeAlloc)
            {
                MessageBox.Show("Number of prop part types (" + numPropPartTypes + ") >= array size (" + propPartTypeAlloc + ")",
                    "Error", MessageBoxButtons.OK);
                return false;
            }
            numPropPartTypes = br.ReadUInt32();
            if (numVolumeTypes > volumeTypeAlloc)
            {
                MessageBox.Show("Number of prop part types (" + numVolumeTypes + ") >= array size (" + volumeTypeAlloc + ")",
                    "Error", MessageBoxButtons.OK);
                return false;
            }
            resourceSize = br.ReadUInt32();

            // Read pointers
            for (uint i = 0; i < _numPropTypes; ++i)
                propTypePtrs.Add(br.ReadUInt32());
            br.BaseStream.Position = 0x7E0;
            for (uint i = 0; i < numPropPartTypes; ++i)
                propPartTypesPtrs.Add(br.ReadUInt32());
            br.BaseStream.Position = 0xC90;
            for (uint i = 0; i < numVolumeTypes; ++i)
                volumeTypePtrs.Add(br.ReadUInt32());

            // Timestamp is shoved in at the end
            timestamp = br.ReadUInt32();

            return true;
        }

        private PropTypeData ReadPropType(BinaryReader2 br, List<uint> volumeTypePtrs, List<uint> propPartTypePtrs)
        {
            PropTypeData propType = new();
            propType.jointLocator = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            br.SkipUniquePadding(4);
            propType.comOffset = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            br.SkipUniquePadding(4);
            propType.inertia = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            br.SkipUniquePadding(4);
            propType.resourceId = br.ReadUInt64();
            propType.mass = br.ReadSingle();
            propType.collisionVolumesStartIndex = br.ReadUInt32();
            propType.partsStartIndex = br.ReadUInt32();
            propType.sphereRadius = br.ReadSingle();
            propType.maxJointAngleCos = br.ReadSingle();
            propType.leanThreshold = br.ReadSingle();
            propType.moveThreshold = br.ReadSingle();
            propType.smashThreshold = br.ReadSingle();
            propType.sceneUriId = br.ReadUInt32();
            propType.maxState = br.ReadByte();
            propType.numParts = br.ReadByte();
            propType.numVolumes = br.ReadByte();
            propType.jointType = br.ReadByte();
            propType.extraTypeInfo = br.ReadByte();

            // Convert pointers to indices
            propType.collisionVolumesStartIndex = (uint)volumeTypePtrs.FindIndex(v => v == propType.collisionVolumesStartIndex);
            propType.partsStartIndex = (uint)propPartTypePtrs.FindIndex(p => p == propType.partsStartIndex);

            return propType;
        }

        private PropPartTypeData ReadPropPartType(BinaryReader2 br, List<uint> volumeTypePtrs)
        {
            PropPartTypeData propPartType = new();
            propPartType.offset = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            br.SkipUniquePadding(4);
            propPartType.inertia = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            br.SkipUniquePadding(4);
            propPartType.mass = br.ReadSingle();
            propPartType.collisionVolumesStartIndex = br.ReadUInt32();
            propPartType.sphereRadius = br.ReadSingle();
            propPartType.numVolumes = br.ReadByte();

            // Convert pointer to index
            propPartType.collisionVolumesStartIndex = (uint)volumeTypePtrs.FindIndex(v => v == propPartType.collisionVolumesStartIndex);

            return propPartType;
        }

        private Volume ReadVolumeType(BinaryReader2 br)
        {
            Volume volumeType = new();
            volumeType.transform = new Matrix4x4(
                br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(),
                br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(),
                br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(),
                br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()
            );
            volumeType.vtablePtr = br.ReadUInt32();
            volumeType.data = new Volume.BoxSpecificData(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            volumeType.radius = br.ReadSingle();
            volumeType.groupId = br.ReadUInt32();
            volumeType.surfaceId = br.ReadUInt32();
            volumeType.flags = br.ReadUInt32();
            return volumeType;
        }
    }
}
