using BundleFormat.Interfaces;
using Configuration;
using Serialization;
using System.IO.Compression;
using System.Text;

namespace BundleFormat.BundleV2.V5;

public class Bundle : IBundle
{
	public string FilePath { get; set; }
	public FileConfig Config { get; }
	public List<IResource> Resources { get; } = [];

	private const int numMemTypes = 4;
	private const uint headerLength = 0x70;
	private const uint resourceEntryLength = 0x48;
	private const uint importEntryLength = 0x10;
	private BundleV2 header = new();
	private string debugData = string.Empty;
	private readonly List<ResourceEntry> resourceEntries = [];

	public Bundle(string path, FileConfig config)
	{
		FilePath = path;
		Config = config;
	}

	public IResource? GetResource(ID id)
	{
		return Resources.Where(r => r.Id == id).First();
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

			ReadHeader(reader);
			if ((header.flags & (uint)Flags.ContainsDebugData) != 0)
				ReadDebugData(reader);
			ReadResourceEntries(reader);
			for (int resourceIndex = 0; resourceIndex < header.resourceEntriesCount; resourceIndex++)
			{
				for (int memType = 0; memType < numMemTypes; memType++)
				{
					if (ReadResourceData(reader, resourceIndex, memType) == false)
						return false;
				}
			}
			for (int resourceIndex = 0; resourceIndex < header.resourceEntriesCount; resourceIndex++)
				ReadImportEntries(resourceIndex);

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
		byte[] expectedMagic = [0x62, 0x6E, 0x64, 0x32];
		byte[] magic = reader.ReadBytes(4);
		if (!magic.SequenceEqual(expectedMagic))
			return false;

		ushort expectedVersion = 5;
		if (reader.ReadUInt16() != expectedVersion)
			return false;

		return true;
	}

	private void ReadHeader(EndianAwareBinaryReader reader)
	{
		reader.Seek(0, SeekOrigin.Begin);
		header = Deserializer.Deserialize<BundleV2>(reader);
	}

	private void ReadDebugData(EndianAwareBinaryReader reader)
	{
		reader.Seek(header.debugDataOffset, SeekOrigin.Begin);
		StringBuilder debugDataBuilder = new();
		sbyte character;
		while (true)
		{
			character = reader.ReadSByte();
			if (character == 0)
				break;
			debugDataBuilder.Append((char)character);
		}
		debugData = debugDataBuilder.ToString();
	}

	private void ReadResourceEntries(EndianAwareBinaryReader reader)
	{
		reader.Seek(header.resourceEntriesOffset, SeekOrigin.Begin);
		for (int resourceIndex = 0; resourceIndex < header.resourceEntriesCount; resourceIndex++)
			resourceEntries.Add(Deserializer.Deserialize<ResourceEntry>(reader));
	}

	private bool ReadResourceData(EndianAwareBinaryReader reader, int resourceIndex, int memType)
	{
		uint sizeOnDisk = resourceEntries[resourceIndex].sizeAndAlignmentOnDisk[memType] & 0x0FFFFFFF;
		if (sizeOnDisk == 0)
			return true;
		uint uncompressedSize = resourceEntries[resourceIndex].uncompressedSizeAndAlignment[memType] & 0x0FFFFFFF;
		uint offset = header.resourceDataOffset[memType] + resourceEntries[resourceIndex].diskOffset[memType];
		reader.Seek(offset, SeekOrigin.Begin);

		MemoryStream resourceStream;
		if (((Flags)header.flags & Flags.ZlibCompression) != 0)
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

		if (memType == 0)
		{
			StringBuilder streamNameBuilder = new();
			for (int i = 0; i < 15; i++)
			{
				sbyte character = header.streamName[resourceEntries[resourceIndex].streamOffset, i];
				if (character == 0)
					break;
				streamNameBuilder.Append((char)character);
			}
			var resource = new Resource(header, resourceEntries[resourceIndex])
			{
				Config = Config,
				Memory = resourceStream
			};
			Resources.Add(resource);
		}
		else
		{
			Resources[resourceIndex].BodyMemory = resourceStream;
			Resources[resourceIndex].BodyMemoryMemTypeIndex = memType;
		}

		resourceStream.Seek(0, SeekOrigin.Begin);
		return true;
	}

	private void ReadImportEntries(int resourceIndex)
	{
		if (resourceEntries[resourceIndex].importCount == 0)
			return;
		EndianAwareBinaryReader reader = new(Resources[resourceIndex].Memory, Config.Platform.Endianness, Config.Platform.Is64Bit);
		reader.Seek(resourceEntries[resourceIndex].importOffset, SeekOrigin.Begin);
		for (ushort importIndex = 0; importIndex < resourceEntries[resourceIndex].importCount; importIndex++)
			resourceEntries[resourceIndex].ImportEntries.Add(Deserializer.Deserialize<ImportEntry>(reader));
		reader.Seek(0, SeekOrigin.Begin);
		reader.BaseStream.SetLength(reader.BaseStream.Length - (importEntryLength * resourceEntries[resourceIndex].ImportEntries.Count));
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
			if ((header.flags & (uint)Flags.ContainsDebugData) != 0)
				WriteDebugData(writer);
			WriteHeader(writer);
			WriteResourceEntries(writer);
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
		header.resourceEntriesCount = (uint)Resources.Count;
		header.resourceEntriesOffset = headerLength;
	}

	private void UpdateResourceEntries()
	{
		for (int resourceIndex = 0; resourceIndex < Resources.Count; resourceIndex++)
		{
			// Uncompressed size: main memory
			resourceEntries[resourceIndex].uncompressedSizeAndAlignment[0] = (uint)Resources[resourceIndex].Memory.Length
				+ (resourceEntries[resourceIndex].uncompressedSizeAndAlignment[0] & 0xF0000000)
				+ (uint)(importEntryLength * resourceEntries[resourceIndex].ImportEntries.Count);

			// Uncompressed size: body (memory type 1 on PC/X360, 2 on PS3, 1 on PS3 shader program buffers)
			if (Resources[resourceIndex].BodyMemory != null)
			{
				for (int memType = 1; memType < numMemTypes; memType++)
				{
					if (Resources[resourceIndex].BodyMemoryMemTypeIndex == memType)
					{
						resourceEntries[resourceIndex].uncompressedSizeAndAlignment[memType] = (uint)Resources[resourceIndex].BodyMemory!.Length
							+ (resourceEntries[resourceIndex].uncompressedSizeAndAlignment[memType] & 0xF0000000);
					}
					else
					{
						resourceEntries[resourceIndex].uncompressedSizeAndAlignment[memType] = 0;
					}
				}
			}
			else
			{
				for (int memType = 1; memType < numMemTypes; memType++)
					resourceEntries[resourceIndex].uncompressedSizeAndAlignment[memType] = 0;
			}

			// Compressed size must be updated after resource compression
			// Disk offset updated during resource writing

			// Import count
			resourceEntries[resourceIndex].importCount = (ushort)resourceEntries[resourceIndex].ImportEntries.Count;

			// Imports offset
			if (resourceEntries[resourceIndex].importCount > 0)
				resourceEntries[resourceIndex].importOffset = (uint)Resources[resourceIndex].Memory.Length;
			else
				resourceEntries[resourceIndex].importOffset = 0;
		}
	}

	private void WriteResourceData(EndianAwareBinaryWriter writer)
	{
		// Set the first resource data offset to right after the resource entries.
		// Unlike other versions, nothing in v5 is aligned.
		header.resourceDataOffset[0] = header.resourceEntriesOffset + (uint)(resourceEntryLength * resourceEntries.Count);

		// Write all the main memory resource portions.
		writer.Seek(header.resourceDataOffset[0], SeekOrigin.Begin);
		for (int resourceIndex = 0; resourceIndex < Resources.Count; resourceIndex++)
			WriteSingleResourceMemType(resourceIndex, 0, writer);

		// Write the body portion of each resource, if present.
		for (int memType = 1; memType < numMemTypes; memType++)
		{
			header.resourceDataOffset[memType] = (uint)writer.BaseStream.Position;
			for (int resourceIndex = 0; resourceIndex < Resources.Count; resourceIndex++)
			{
				if (Resources[resourceIndex].BodyMemory != null
					&& Resources[resourceIndex].BodyMemoryMemTypeIndex == memType)
				{
					WriteSingleResourceMemType(resourceIndex, memType, writer);
				}
				else
				{
					resourceEntries[resourceIndex].diskOffset[memType] = 0;
					resourceEntries[resourceIndex].sizeAndAlignmentOnDisk[memType] = 0;
				}
			}
		}
	}

	private void WriteSingleResourceMemType(int resourceIndex, int memType, EndianAwareBinaryWriter writer)
	{
		// Set the disk offset to the current position relative to the memory type's start offset.
		if (resourceEntries[resourceIndex].uncompressedSizeAndAlignment[memType] != 0)
			resourceEntries[resourceIndex].diskOffset[memType] = (uint)writer.BaseStream.Position - header.resourceDataOffset[memType];

		// Append import entries to the end of the resource if they exist.
		if (memType == 0 && resourceEntries[resourceIndex].ImportEntries.Count > 0)
		{
			Resources[resourceIndex].Memory.Seek(0, SeekOrigin.End);
			foreach (var importEntry in resourceEntries[resourceIndex].ImportEntries)
				Serializer.Serialize(Resources[resourceIndex].Memory, Config, importEntry);
		}

		// Ensure the resource stream is at the beginning before writing.
		// Particularly important for main memory portions with imports, since they will be at the end now.
		Resources[resourceIndex].Memory.Seek(0, SeekOrigin.Begin);
		Resources[resourceIndex].BodyMemory?.Seek(0, SeekOrigin.Begin);

		// If the bundle is set as compressed, compress the resource portion using zlib and write it
		if ((header.flags & (uint)Flags.ZlibCompression) != 0)
		{
			using ZLibStream zlibStream = new(writer.BaseStream, CompressionLevel.SmallestSize, true);
			if (memType == 0)
				Resources[resourceIndex].Memory.CopyTo(zlibStream);
			else
				Resources[resourceIndex].BodyMemory?.CopyTo(zlibStream);
			zlibStream.Close();
		}
		// Otherwise, write the uncompressed resource to the stream
		else
		{
			if (memType == 0)
				Resources[resourceIndex].Memory.CopyTo(writer.BaseStream);
			else
				Resources[resourceIndex].BodyMemory?.CopyTo(writer.BaseStream);
		}

		// Set the compressed resource size by subtracting the disk offset from the current position.
		// The alignment nibble is always set to 0, i.e., 1-byte alignment.
		if (resourceEntries[resourceIndex].uncompressedSizeAndAlignment[memType] == 0)
			resourceEntries[resourceIndex].sizeAndAlignmentOnDisk[memType] = 0;
		else
			resourceEntries[resourceIndex].sizeAndAlignmentOnDisk[memType] = (uint)writer.BaseStream.Position
				- resourceEntries[resourceIndex].diskOffset[memType] - header.resourceDataOffset[memType];

		// Remove imports from the end of the resource if they were previously appended.
		if (memType == 0 && Resources[resourceIndex].Imports.Count > 0)
		{
			Resources[resourceIndex].Memory.SetLength(
				Resources[resourceIndex].Memory.Length - (importEntryLength * Resources[resourceIndex].Imports.Count));
		}
	}

	private void WriteHeader(EndianAwareBinaryWriter writer)
	{
		writer.Seek(0, SeekOrigin.Begin);
		Serializer.Serialize(writer, header);
	}

	private void WriteDebugData(EndianAwareBinaryWriter writer)
	{
		// Debug data is always at the end of the bundle.
		header.debugDataOffset = (uint)writer.BaseStream.Position;
		writer.Write(debugData.ToArray());
	}

	private void WriteResourceEntries(EndianAwareBinaryWriter writer)
	{
		writer.Seek((int)header.resourceEntriesOffset, SeekOrigin.Begin);
		for (int i = 0; i < resourceEntries.Count; i++)
			Serializer.Serialize(writer, resourceEntries[i]);
	}
}
