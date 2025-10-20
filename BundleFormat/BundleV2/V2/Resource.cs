using BundleFormat.Interfaces;
using Configuration;
using Information;
using Information.Enums;
using System.Collections;
using System.IO.Hashing;
using System.Text;

namespace BundleFormat.BundleV2.V2;

internal class Resource : IResource
{
	private readonly ResourceEntry resourceEntry;
	public required FileConfig Config { get; init; }
	public ID Id { get => resourceEntry.resourceId; }
	private string name;
	public string Name
	{
		get
		{
			if (name == string.Empty)
				return Id.ToString("X8");
			return name;
		}
		set
		{
			name = value;
			Crc32 crc = new();
			crc.Append(Encoding.ASCII.GetBytes(value));
			resourceEntry.resourceId = crc.GetCurrentHashAsUInt32();
		}
	}
	public uint Type { get => resourceEntry.resourceTypeId; }
	public string TypeName
	{
		get
		{
			if (Type == 0x32 && (Config.Platform.Id == Platform.PC
				|| Config.Platform.Id == Platform.PlayStation4
				|| Config.Platform.Id == Platform.Switch
				|| Config.Platform.Id == Platform.XboxOne))
				return "Shader";
			return ResourceTypeInfo.Info[(ResourceType)Type];
		}
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
	public byte StreamIndex { get => resourceEntry.streamIndex; }
	public string StreamName { get; } = string.Empty;
	public required MemoryStream Memory { get; set; }
	public MemoryStream? BodyMemory { get; set; }
	public int BodyMemoryMemTypeIndex { get; set; }
	public IList Imports { get => resourceEntry.ImportEntries; }

	internal Resource(ResourceEntry resourceEntry)
	{
		this.resourceEntry = resourceEntry;
		name = ResourceInfo.Ids.GetValueOrDefault(resourceEntry.resourceId) ?? string.Empty;
	}
}
