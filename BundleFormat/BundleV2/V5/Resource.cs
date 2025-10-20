using BundleFormat.Interfaces;
using Configuration;
using Information;
using Information.Enums;
using System.Collections;
using System.IO.Hashing;
using System.Text;

namespace BundleFormat.BundleV2.V5;

internal class Resource : IResource
{
	private readonly BundleV2 header;
	private readonly ResourceEntry resourceEntry;
	public required FileConfig Config { get; init; }
	public ID Id { get => resourceEntry.resourceId; }
	private string name;
	public string Name
	{
		get
		{
			if (name == string.Empty)
				return Id.ToString("X16");
			return name;
		}
		set
		{
			// TODO: Handle IDs that aren't normal type
			if ((resourceEntry.resourceId & 0xF000000000000000) == 0)
			{
				name = value;
				Crc32 crc = new();
				crc.Append(Encoding.ASCII.GetBytes(value));
				resourceEntry.resourceId = crc.GetCurrentHashAsUInt32();
			}
		}
	}
	public uint Type { get => resourceEntry.resourceTypeId; }
	public string TypeName
	{
		get => ResourceTypeV2Info.Info[(ResourceTypeV2)Type];
	}
	public long Size
	{
		get
		{
			if (BodyMemory == null)
				return Memory.Length;
			return Memory.Length + BodyMemory.Length;
		}
	}
	public byte StreamIndex { get => resourceEntry.streamOffset; }
	public string StreamName
	{
		get
		{
			StringBuilder streamNameBuilder = new();
			for (int i = 0; i < 15; i++)
			{
				sbyte character = header.streamName[StreamIndex, i];
				if (character == 0)
					break;
				streamNameBuilder.Append((char)character);
			}
			return streamNameBuilder.ToString();
		}
	}
	public required MemoryStream Memory { get; set; }
	public MemoryStream? BodyMemory { get; set; }
	public int BodyMemoryMemTypeIndex { get; set; }
	public IList Imports { get => resourceEntry.ImportEntries; }

	internal Resource(BundleV2 header, ResourceEntry resourceEntry)
	{
		this.header = header;
		this.resourceEntry = resourceEntry;
		name = ResourceInfo.Ids.GetValueOrDefault(resourceEntry.resourceId) ?? string.Empty;
	}
}
