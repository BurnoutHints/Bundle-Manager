﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using BundleUtilities;
using Microsoft.SqlServer.Server;

namespace BundleFormat
{
	public class EntryBlock
	{
		public uint UncompressedAlignment; // default depending on file type
		public byte[] Data;
	}

    public class EntryInfo
    {
        public uint ID;
        public EntryType Type;
        public string Path;
		public DebugInfo DebugInfo;

        public EntryInfo(uint id, EntryType type, string path, DebugInfo debugInfo)
        {
            ID = id;
            Type = type;
            Path = path;
			DebugInfo = debugInfo;
        }
    }

	public struct Dependency
	{
		public ulong ID;
		public uint EntryPointerOffset;
	}

	public struct DebugInfo
	{
		public string Name;
		public string TypeName;
	}

    public class BundleEntry
    {
        public BundleArchive Archive;

        public int Index;

        public ulong ID;
        public ulong References;
		public int DependenciesListOffset;
        public short DependencyCount;
		public List<Dependency> Dependencies;

		public DebugInfo DebugInfo;

		public EntryBlock[] EntryBlocks;

		public bool HasHeader => HasSection(0);
        public bool HasBody => HasSection(1);
		public bool HasThird => HasSection(2);

		public EntryType Type;

        public BundlePlatform Platform;
        public bool Console => Platform == BundlePlatform.X360 || Platform == BundlePlatform.PS3;

        public bool Dirty;

        public BundleEntry(BundleArchive archive)
        {
            Archive = archive;
			Dependencies = new List<Dependency>();
        }

		public bool HasSection(int section)
		{
			return EntryBlocks != null &&
				   section < EntryBlocks.Length &&
				   section >= 0 &&
				   EntryBlocks[section] != null &&
				   EntryBlocks[section].Data != null &&
				   EntryBlocks[section].Data.Length > 0;
		}

        public MemoryStream MakeStream(bool body = false)
        {
			if (EntryBlocks == null)
				return null;

            if (body)
                return new MemoryStream(EntryBlocks[1].Data);
            return new MemoryStream(EntryBlocks[0].Data);
        }

        public List<BundleDependency> GetDependencies()
        {
            List<BundleDependency> result = new List<BundleDependency>();

			if (Dependencies.Count > 0)
			{
				for (int i = 0; i < Dependencies.Count; i++)
				{
					BundleDependency dependency = new BundleDependency();

					dependency.EntryID = Dependencies[i].ID;
					dependency.EntryPointerOffset = (int)Dependencies[i].EntryPointerOffset;

					BundleEntry entry = null;

					for (int j = 0; j < Archive.Entries.Count; j++)
					{
						if (Archive.Entries[j].ID != dependency.EntryID)
							continue;

						dependency.EntryIndex = j;
						entry = Archive.Entries[j];
					}

					dependency.Entry = entry;

					result.Add(dependency);
				}
				return result;
			}

            MemoryStream ms = MakeStream();
            BinaryReader2 br = new BinaryReader2(ms);
            br.BigEndian = Console;

            br.BaseStream.Position = DependenciesListOffset;

            for (int i = 0; i < DependencyCount; i++)
            {
                BundleDependency bundleDependency = new BundleDependency();

                bundleDependency.EntryID = br.ReadUInt64();
                bundleDependency.EntryPointerOffset = br.ReadInt32();
                bundleDependency.Unknown2 = br.ReadInt32();

                BundleEntry entry = null;

                for (int j = 0; j < Archive.Entries.Count; j++)
                {
                    if (Archive.Entries[j].ID != bundleDependency.EntryID)
                        continue;

                    bundleDependency.EntryIndex = j;
                    entry = Archive.Entries[j];
                }

                bundleDependency.Entry = entry;

                result.Add(bundleDependency);
            }

            br.Close();
            ms.Close();

            return result;
        }

        public string DetectName()
        {
			if (!string.IsNullOrWhiteSpace(DebugInfo.Name))
				return DebugInfo.Name;

            string theName = "worldvault";
            ulong theID = Crc32.HashCrc32B(theName);
            if (theID == ID)
                return theName;
            theName = "postfxvault";
            theID = Crc32.HashCrc32B(theName);
            if (theID == ID)
                return theName;
            theName = "cameravault";
            theID = Crc32.HashCrc32B(theName);
            if (theID == ID)
                return theName;

            string path = Path.GetFileNameWithoutExtension(Archive.Path);
            string file = null;
            if (path != null)
                file = path.ToUpper();

            if (file != null && file.StartsWith("TRK_UNIT") && file.EndsWith("_GR"))
            {
                string trackID = file.Substring(8).Replace("_GR", "").ToLower();
                string name = "trk_unit" + trackID + "_list";
                ulong newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
                name = "prp_inst_" + trackID;
                newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
                name = "prp_gl__" + trackID;
                newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
                name = "trk_unit" + trackID + "_passby";
                newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
                name = "trk_unit" + trackID + "_emitter";
                newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
            }

            if (file != null)
            {
                string aptName = file.ToLower() + ".swf";
                ulong aptID = Crc32.HashCrc32B(aptName);
                if (aptID == ID)
                    return aptName;
            }

            if (file != null && file.StartsWith("WHE_") && file.EndsWith("_GR"))
            {
                string wheelID = file.Substring(4).Replace("_GR", "").ToLower();
                string name = wheelID + "_graphics";
                ulong newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
            }

            if (file != null && file.StartsWith("VEH_"))
            {
                if (file.EndsWith("_AT"))
                {
                    string vehicleID = file.Substring(4).Replace("_AT", "").ToLower();
                    string name = vehicleID + "_attribsys";
                    ulong newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                    name = vehicleID + "deformationmodel";
                    newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                    name = vehicleID + "_bpr";
                    newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                    name = vehicleID + "_anim";
                    newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                    name = vehicleID + "_trafficstub";
                    newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                    name = vehicleID + "_vanm";
                    newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                } else if (file.EndsWith("_CD"))
                {
                    string vehicleID = file.Substring(4).Replace("_CD", "").ToLower();
                    string name = vehicleID;
                    ulong newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                } else if (file.EndsWith("_GR"))
                {
                    string vehicleID = file.Substring(4).Replace("_GR", "").ToLower();
                    string name = vehicleID + "_graphics";
                    ulong newID = Crc32.HashCrc32B(name);
                    if (newID == ID)
                        return name;
                }
            }

            // WorldCol Names
            for (int i = 0; i < Archive.Entries.Count; i++)
            {
                string name = "trk_col_" + i;
                ulong newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
                name = "trk_clil" + i;
                newID = Crc32.HashCrc32B(name);
                if (newID == ID)
                    return name;
            }

            return "";
        }

        public Color GetColor()
        {
            switch (Type)
            {
                case EntryType.RasterResourceType:
                    return Color.Orange;
                case EntryType.MaterialResourceType:
                    return Color.DeepPink;
                case EntryType.TextFileResourceType:
                    break;
                case EntryType.RwVertexDescResourceType:
                    break;
                case EntryType.RwRenderableResourceType:
                    return Color.Aquamarine;
                case EntryType.unknown_file_type_00D:
                    break;
                case EntryType.RwTextureStateResourceType:
                    break;
                case EntryType.MaterialStateResourceType:
                    break;
                case EntryType.RwShaderProgramBufferResourceType:
                    break;
                case EntryType.RwShaderParameterResourceType:
                    break;
                case EntryType.RwDebugResourceType:
                    break;
                case EntryType.KdTreeResourceType:
                    break;
                case EntryType.SnrResourceType:
                    break;
                case EntryType.AttribSysSchemaResourceType:
                    break;
                case EntryType.AttribSysVaultResourceType:
                    break;
                case EntryType.AptDataHeaderType:
                    break;
                case EntryType.GuiPopupResourceType:
                    break;
                case EntryType.FontResourceType:
                    break;
                case EntryType.LuaCodeResourceType:
                    break;
                case EntryType.InstanceListResourceType:
                    return Color.BlueViolet;
                case EntryType.IDList:
                    return Color.Tomato;
                case EntryType.LanguageResourceType:
                    break;
                case EntryType.SatNavTileResourceType:
                    break;
                case EntryType.SatNavTileDirectoryResourceType:
                    break;
                case EntryType.ModelResourceType:
                    return Color.Blue;
                case EntryType.RwColourCubeResourceType:
                    break;
                case EntryType.HudMessageResourceType:
                    break;
                case EntryType.HudMessageListResourceType:
                    break;
                case EntryType.unknown_file_type_02E:
                    break;
                case EntryType.unknown_file_type_02F:
                    break;
                case EntryType.WorldPainter2DResourceType:
                    break;
                case EntryType.PFXHookBundleResourceType:
                    break;
                case EntryType.ShaderResourceType:
                    break;
                case EntryType.ICETakeDictionaryResourceType:
                    break;
                case EntryType.VideoDataResourceType:
                    break;
                case EntryType.PolygonSoupListResourceType:
                    return Color.Goldenrod;
                case EntryType.CommsToolListDefinitionResourceType:
                    break;
                case EntryType.CommsToolListResourceType:
                    break;
                case EntryType.AnimationCollectionResourceType:
                    break;
                case EntryType.RegistryResourceType:
                    break;
                case EntryType.GenericRwacWaveContentResourceType:
                    break;
                case EntryType.GinsuWaveContentResourceType:
                    break;
                case EntryType.AemsBankResourceType:
                    break;
                case EntryType.CsisResourceType:
                    break;
                case EntryType.NicotineResourceType:
                    break;
                case EntryType.SplicerResourceType:
                    break;
                case EntryType.GenericRwacReverbIRContentResourceType:
                    break;
                case EntryType.SnapshotDataResourceType:
                    break;
                case EntryType.ZoneListResourceType:
                    break;
                case EntryType.LoopModelResourceType:
                    break;
                case EntryType.AISectionsResourceType:
                    break;
                case EntryType.TrafficDataResourceType:
                    break;
                case EntryType.TriggerResourceType:
                    break;
                case EntryType.VehicleListResourceType:
                    break;
                case EntryType.GraphicsSpecResourceType:
                    return Color.SeaGreen;
                case EntryType.ParticleDescriptionCollectionResourceType:
                    break;
                case EntryType.WheelListResourceType:
                    break;
                case EntryType.WheelGraphicsSpecResourceType:
                    break;
                case EntryType.TextureNameMapResourceType:
                    break;
                case EntryType.ProgressionResourceType:
                    break;
                case EntryType.PropPhysicsResourceType:
                    break;
                case EntryType.PropGraphicsListResourceType:
                    break;
                case EntryType.PropInstanceDataResourceType:
                    break;
                case EntryType.BrnEnvironmentKeyframeResourceType:
                    break;
                case EntryType.BrnEnvironmentTimeLineResourceType:
                    break;
                case EntryType.BrnEnvironmentDictionaryResourceType:
                    break;
                case EntryType.GraphicsStubResourceType:
                    break;
                case EntryType.StaticSoundMapResourceType:
                    break;
                case EntryType.StreetDataResourceType:
                    break;
                case EntryType.BrnVFXMeshCollectionResourceType:
                    break;
                case EntryType.MassiveLookupTableResourceType:
                    break;
                case EntryType.VFXPropCollectionResourceType:
                    break;
                case EntryType.StreamedDeformationSpecResourceType:
                    break;
                case EntryType.ParticleDescriptionResourceType:
                    break;
                case EntryType.PlayerCarColoursResourceType:
                    break;
                case EntryType.ChallengeListResourceType:
                    break;
                case EntryType.FlaptFileResourceType:
                    break;
                case EntryType.ProfileUpgradeResourceType:
                    break;
                case EntryType.VehicleAnimationResourceType:
                    break;
                case EntryType.BodypartRemappingResourceType:
                    break;
                case EntryType.LUAListResourceType:
                    break;
                case EntryType.LUAScriptResourceType:
                    break;
            }
            return Color.Transparent;
        }
    }
}
