using BundleFormat.Interfaces;
using Configuration;
using Information.Enums;
using Serialization;
using System.IO.Compression;

namespace BundleFormat.BundleV1;

public class Bundle : IBundle
{
	public string FilePath { get; set; }
	public FileConfig Config { get; }
	public List<IResource> Resources { get; } = [];

	private int numBaseResourceTypes;
	private uint headerLength;
	private uint resourceEntryLength;
	private const uint baseResourceDescriptorLength = 0x8;
	private const uint importTableLength = 0x8;
	private const uint importEntryLength = 0x10;
	private BundleV5ExtendedData header = new();
	private readonly List<ResourceEntry> resourceEntries = [];

	public Bundle(string path, FileConfig config)
	{
		FilePath = path;
		Config = config;
		if (Config.Platform.Id == Platform.PlayStation3)
			numBaseResourceTypes = 6;
		else if (Config.Platform.Id == Platform.Xbox360)
			numBaseResourceTypes = 5;
		else if (Config.Platform.Id == Platform.PC)
			numBaseResourceTypes = 4;
		else
			throw new Exception("Invalid BundleV1 platform specified.");
	}

	public IResource? GetResource(ID id)
	{
		return Resources.Where(r => r.Id == id).FirstOrDefault();
	}

	public bool Load()
	{
		try
		{
			using FileStream fileStream = new(FilePath, FileMode.Open);
			EndianAwareBinaryReader reader = new(fileStream, Config.Platform.Endianness, Config.Platform.Is64Bit);
			if (!ValidateVersion(reader))
			{
				reader.Close();
				return false;
			}
			reader.Seek(4, SeekOrigin.Begin);
			uint version = reader.ReadUInt32();

			ReadHeader(reader, version);
			SetStructLengths();
			List<ID> ids = ReadHashTable(reader);
			ReadResourceEntries(ids, reader);
			for (int resourceIndex = 0; resourceIndex < header.numResources; resourceIndex++)
				ReadImportEntries(reader, resourceIndex);
			for (int resourceIndex = 0; resourceIndex < header.numResources; resourceIndex++)
			{
				for (int baseResourceType = 0; baseResourceType < numBaseResourceTypes; baseResourceType++)
				{
					if (ReadResourceData(reader, resourceIndex, baseResourceType) == false)
						return false;
				}
			}

			reader.Close();
		}
		catch
		{
			return false;
		}
		return true;
	}

	public bool ValidateVersion(EndianAwareBinaryReader reader)
	{
		reader.Seek(0, SeekOrigin.Begin);
		uint magic = reader.ReadUInt32();
		if (magic != 0x626E646C)
			return false;

		uint version = reader.ReadUInt32();
		if (version < 3 || version > 5)
			return false;

		return true;
	}

	private void ReadHeader(EndianAwareBinaryReader reader, uint version)
	{
		reader.Seek(0, SeekOrigin.Begin);
		if (version == 3)
		{
			var headerBase = Deserializer.Deserialize<BundleBase>(reader);
			header = new(headerBase);
		}
		else if (version == 4)
		{
			var headerV4 = Deserializer.Deserialize<BundleV4ExtendedData>(reader);
			header = new(headerV4);
		}
		else if (version == 5)
			header = Deserializer.Deserialize<BundleV5ExtendedData>(reader);
	}

	private void SetStructLengths()
	{
		headerLength = 0x50; // PC v3 - smallest
		resourceEntryLength = 0x5C; // PC
		if (header.platform == (uint)BundlePlatform.Xbox360)
		{
			headerLength += 0xC;
			resourceEntryLength = 0x70;
		}
		else if (header.platform == (uint)BundlePlatform.PlayStation3)
		{
			headerLength += 0xC * 2;
			resourceEntryLength = 0x84;
		}
		if (header.version >= 4)
			headerLength += 0xC;
		if (header.version >= 5)
			headerLength += 0x8;
	}

	private List<ID> ReadHashTable(EndianAwareBinaryReader reader)
	{
		reader.Seek(header.hashTableOffset, SeekOrigin.Begin);
		List<ID> ids = [];
		for (int resourceIndex = 0; resourceIndex < header.numResources; resourceIndex++)
			ids.Add(Deserializer.Deserialize<ID>(reader));
		return ids;
	}

	private void ReadResourceEntries(List<ID> ids, EndianAwareBinaryReader reader)
	{
		reader.Seek(header.resourceEntriesOffset, SeekOrigin.Begin);
		for (int resourceIndex = 0; resourceIndex < header.numResources; resourceIndex++)
		{
			ResourceEntry resourceEntry = Deserializer.Deserialize<ResourceEntry>(reader);
			resourceEntry.Id = ids[resourceIndex];
			resourceEntries.Add(resourceEntry);
		}
	}

	private void ReadImportEntries(EndianAwareBinaryReader reader, int resourceIndex)
	{
		if (resourceEntries[resourceIndex].importTable.Address == 0)
			return;
		reader.Seek((long)resourceEntries[resourceIndex].importTable.Address, SeekOrigin.Begin);
		var table = Deserializer.Deserialize<ImportTable>(reader);
		for (int importIndex = 0; importIndex < table.numImports; importIndex++)
			resourceEntries[resourceIndex].ImportEntries.Add(Deserializer.Deserialize<ImportEntry>(reader));
	}

	private bool ReadResourceData(EndianAwareBinaryReader reader, int resourceIndex, int baseResourceType)
	{
		uint sizeOnDisk = resourceEntries[resourceIndex].serializedResourceDescriptor.baseResourceDescriptors[baseResourceType].size;
		if (sizeOnDisk == 0)
			return true;
		uint uncompressedSize = sizeOnDisk;
		if ((header.flags & (uint)Flags.Compressed) != 0)
			uncompressedSize = header.uncompressedResourceDescriptors.Data!.Collection[resourceIndex].baseResourceDescriptors[baseResourceType].size;
		uint offset = baseResourceType == 0
			? resourceEntries[resourceIndex].serializedOffsetResourceDescriptor.baseResourceDescriptors[0].size
			: header.bundleResourceDescriptor.baseResourceDescriptors[0].size
				+ resourceEntries[resourceIndex].serializedOffsetResourceDescriptor.baseResourceDescriptors[baseResourceType].size;
		reader.Seek(offset, SeekOrigin.Begin);
		
		MemoryStream resourceStream;
		if ((header.flags & (uint)Flags.Compressed) != 0)
		{
			resourceStream = new();
			byte[] buffer = reader.ReadBytes((int)sizeOnDisk);
			using ZLibStream zlibStream = new(new MemoryStream(buffer), CompressionMode.Decompress);
			zlibStream.CopyTo(resourceStream);
		}
		else
		{
			byte[] buffer = reader.ReadBytes((int)uncompressedSize);
			resourceStream = new(buffer);
		}

		if (baseResourceType == 0)
		{
			var resource = new Resource(resourceEntries[resourceIndex])
			{
				Config = Config,
				Memory = resourceStream
			};
			Resources.Add(resource);
		}
		else
		{
			Resources[resourceIndex].BodyMemory = resourceStream;
			Resources[resourceIndex].BodyMemoryMemTypeIndex = baseResourceType;
		}

		return true;
	}

	public bool Save()
	{
		try
		{
			using FileStream fileStream = new(FilePath, FileMode.Create, FileAccess.Write);
			EndianAwareBinaryWriter writer = new(fileStream, Config.Platform.Endianness, Config.Platform.Is64Bit);
			UpdateHeader();
			UpdateResourceEntries();
			WriteResourceData(writer);
			WriteHeader(writer);
			WriteHashTable(writer);
			WriteResourceEntries(writer);
			if ((header.flags & (uint)Flags.Compressed) != 0)
				WriteUncompressedDescriptors(writer);
			WriteImportTables(writer);
			writer.Close();
		}
		catch
		{
			return false;
		}
		return true;
	}

	private void UpdateHeader()
	{
		header.numResources = (uint)Resources.Count;
		// Bundle resource descriptor (chunk sizes) must be updated after any potential compression

		// Hash table start is always aligned.
		header.hashTableOffset = (uint)Serializer.Align(headerLength, 0x10);
		// Resource entries offset is unaligned.
		header.resourceEntriesOffset = header.hashTableOffset + (sizeof(ID) * header.numResources);

		// Import table offset is impacted if compression is used.
		if (header.version >= 4 && (header.flags & (uint)Flags.Compressed) != 0)
		{
			// Compression info is aligned 8.
			header.uncompressedResourceDescriptors.Address = (uint)Serializer.Align(header.resourceEntriesOffset + (resourceEntryLength * header.numResources), 0x8);
			// Import table is unaligned, but will effectively be aligned 8 due to compression info.
			header.importTablesOffset = (uint)header.uncompressedResourceDescriptors.Address
				+ (baseResourceDescriptorLength * (uint)numBaseResourceTypes * header.uncompressedDescriptorCount);
		}
		else
		{
			// Import table is unaligned, but will effectively be aligned 4 on PS3/PC and 0x10 on X360 due to resource entries.
			header.importTablesOffset = header.resourceEntriesOffset + (resourceEntryLength * header.numResources);
		}

		// Update resource data offset.
		// Import-related information is not updated here; it is updated when resource entries are.
		// This is aligned 0x10.
		uint importTablesLength = 0;
		foreach (var resource in Resources)
		{
			if (resource.Imports.Count == 0)
				continue;
			importTablesLength += importTableLength + (importEntryLength * (uint)resource.Imports.Count);
		}
		header.resourceDataOffset = (uint)Serializer.Align(header.importTablesOffset + importTablesLength, 0x10);
	}

	private void UpdateResourceEntries()
	{
		uint currentImportTableAddress = header.importTablesOffset;
		for (int resourceIndex = 0; resourceIndex < Resources.Count; resourceIndex++)
		{
			// Resource pointer is set at runtime and not updated here.

			// Import table address
			if (Resources[resourceIndex].Imports.Count == 0)
			{
				resourceEntries[resourceIndex].importTable.Address = 0;
			}
			else
			{
				resourceEntries[resourceIndex].importTable.Address = currentImportTableAddress;
				currentImportTableAddress += importTableLength + (importEntryLength * (uint)Resources[resourceIndex].Imports.Count);
			}

			// Resource type ID does not need updating.

			// If compressed, update uncompressed sizes
			if ((header.flags & (uint)Flags.Compressed) != 0)
			{
				header.uncompressedResourceDescriptors.Data!.Collection[resourceIndex].baseResourceDescriptors[0].size =
					(uint)Resources[resourceIndex].Memory.Length;
				if (Resources[resourceIndex].BodyMemory != null)
				{
					header.uncompressedResourceDescriptors.Data!.Collection[resourceIndex]
						.baseResourceDescriptors[Resources[resourceIndex].BodyMemoryMemTypeIndex].size =
						(uint)Resources[resourceIndex].BodyMemory!.Length;
				}
			}

			// Serialized sizes and offsets must wait till compression to be updated, in case they are compressed.
			// Serialized resource pointers are set at runtime and not updated here.
		}

		// Now that import table offset has served its purpose, set to 0 if there are no imports.
		if (!Resources.Any(r => r.Imports.Count != 0))
			header.importTablesOffset = 0;
	}

	private void WriteResourceData(EndianAwareBinaryWriter writer)
	{
		// Write all the main memory resource portions.
		writer.Seek(header.resourceDataOffset, SeekOrigin.Begin);
		for (int resourceIndex = 0; resourceIndex < Resources.Count; resourceIndex++)
			WriteSingleResourceBaseResourceType(0, resourceIndex, 0, writer);

		// Main memory base resource type size is set as the entire size up to this point.
		header.bundleResourceDescriptor.baseResourceDescriptors[0].size = (uint)writer.BaseStream.Position;

		// Write the body portion of each resource, if present.
		uint currentBundleBaseResourceTypeOffset = (uint)writer.BaseStream.Position;
		for (int baseResourceType = 1; baseResourceType < numBaseResourceTypes; baseResourceType++)
		{
			for (int resourceIndex = 0; resourceIndex < Resources.Count; resourceIndex++)
			{
				if (Resources[resourceIndex].BodyMemory != null
					&& Resources[resourceIndex].BodyMemoryMemTypeIndex == baseResourceType)
				{
					WriteSingleResourceBaseResourceType(currentBundleBaseResourceTypeOffset, resourceIndex, baseResourceType, writer);
				}
				else
				{
					resourceEntries[resourceIndex].serializedOffsetResourceDescriptor.baseResourceDescriptors[baseResourceType].size = 0;
					resourceEntries[resourceIndex].serializedOffsetResourceDescriptor.baseResourceDescriptors[baseResourceType].alignment = 1;
					resourceEntries[resourceIndex].serializedResourceDescriptor.baseResourceDescriptors[baseResourceType].size = 0;
					resourceEntries[resourceIndex].serializedResourceDescriptor.baseResourceDescriptors[baseResourceType].alignment = 1;
				}
			}

			// Body base resource type sizes are the unaligned end of the previous type to the unaligned end of their type.
			header.bundleResourceDescriptor.baseResourceDescriptors[baseResourceType].size =
				(uint)writer.BaseStream.Position - currentBundleBaseResourceTypeOffset;
			currentBundleBaseResourceTypeOffset = (uint)writer.BaseStream.Position;
		}
	}

	private void WriteSingleResourceBaseResourceType(uint baseResourceTypeOffset, int resourceIndex, int baseResourceType, EndianAwareBinaryWriter writer)
	{
		// Main memory resources are aligned 0x10, otherwise aligned 0x80.
		if (baseResourceType == 0)
			writer.Align(0x10);
		else
			writer.Align(0x80);

		// Set the serialized resource offset to the current position relative to the memory type's start offset.
		if (baseResourceType == 0 || Resources[resourceIndex].BodyMemoryMemTypeIndex == baseResourceType)
		{
			resourceEntries[resourceIndex].serializedOffsetResourceDescriptor.baseResourceDescriptors[baseResourceType].size = (uint)writer.BaseStream.Position - baseResourceTypeOffset;
		}
		else
		{
			resourceEntries[resourceIndex].serializedOffsetResourceDescriptor.baseResourceDescriptors[baseResourceType].size = 0;
			resourceEntries[resourceIndex].serializedOffsetResourceDescriptor.baseResourceDescriptors[baseResourceType].alignment = 1;
		}

		// Ensure the resource stream is at the beginning before writing.
		// Particularly important for main memory portions with imports, since they will be at the end now.
		Resources[resourceIndex].Memory.Seek(0, SeekOrigin.Begin);
		Resources[resourceIndex].BodyMemory?.Seek(0, SeekOrigin.Begin);

		// If the bundle is set as compressed, compress the resource portion using zlib and write it
		if ((header.flags & (uint)Flags.Compressed) != 0)
		{
			using ZLibStream zlibStream = new(writer.BaseStream, CompressionLevel.SmallestSize, true);
			if (baseResourceType == 0)
				Resources[resourceIndex].Memory.CopyTo(zlibStream);
			else
				Resources[resourceIndex].BodyMemory?.CopyTo(zlibStream);
			zlibStream.Close();
		}
		// Otherwise, write the uncompressed resource to the stream
		else
		{
			if (baseResourceType == 0)
				Resources[resourceIndex].Memory.CopyTo(writer.BaseStream);
			else
				Resources[resourceIndex].BodyMemory?.CopyTo(writer.BaseStream);
		}

		// Set the on-disk resource size by subtracting the disk offset from the current position.
		// The alignment nibble is always set to 0, i.e., 1-byte alignment.
		if (baseResourceType == 0 || Resources[resourceIndex].BodyMemoryMemTypeIndex == baseResourceType)
		{
			resourceEntries[resourceIndex].serializedResourceDescriptor.baseResourceDescriptors[baseResourceType].size = (uint)writer.BaseStream.Position
				- resourceEntries[resourceIndex].serializedOffsetResourceDescriptor.baseResourceDescriptors[baseResourceType].size - baseResourceTypeOffset;
		}
		else
		{
			resourceEntries[resourceIndex].serializedResourceDescriptor.baseResourceDescriptors[baseResourceType].size = 0;
			resourceEntries[resourceIndex].serializedResourceDescriptor.baseResourceDescriptors[baseResourceType].alignment = 1;
		}
	}

	private void WriteHeader(EndianAwareBinaryWriter writer)
	{
		writer.Seek(0, SeekOrigin.Begin);
		Serializer.Serialize(writer, header);
	}

	private void WriteHashTable(EndianAwareBinaryWriter writer)
	{
		writer.Seek(header.hashTableOffset, SeekOrigin.Begin);
		for (int i = 0; i < resourceEntries.Count; i++)
			Serializer.Serialize(writer, resourceEntries[i].Id);
	}

	private void WriteResourceEntries(EndianAwareBinaryWriter writer)
	{
		writer.Seek((int)header.resourceEntriesOffset, SeekOrigin.Begin);
		for (int i = 0; i < resourceEntries.Count; i++)
			Serializer.Serialize(writer, resourceEntries[i]);
	}

	private void WriteUncompressedDescriptors(EndianAwareBinaryWriter writer)
	{
		writer.Seek((long)header.uncompressedResourceDescriptors.Address, SeekOrigin.Begin);
		for (int i = 0; i < header.uncompressedDescriptorCount; i++)
			Serializer.Serialize(writer, header.uncompressedResourceDescriptors.Data!.Collection[i]);
	}

	private void WriteImportTables(EndianAwareBinaryWriter writer)
	{
		writer.Seek(header.importTablesOffset, SeekOrigin.Begin);
		foreach (var resourceEntry in resourceEntries)
		{
			if (resourceEntry.ImportEntries.Count == 0)
				continue;
			writer.Write((uint)resourceEntry.ImportEntries.Count);
			writer.Seek(writer.BaseStream.Position + 4, SeekOrigin.Begin);
			foreach (var importEntry in resourceEntry.ImportEntries)
				Serializer.Serialize(writer, importEntry);
		}
	}
}
