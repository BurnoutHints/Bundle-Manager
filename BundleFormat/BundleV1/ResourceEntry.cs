using Serialization.Attributes;
using Information.Enums;
using Serialization.Types;

namespace BundleFormat.BundleV1;

internal class ResourceEntry
{
	internal VoidPointer resource = new();
	internal VoidPointer importTable = new();
	internal int type; // CgsResource::Type in struct, but file holds an integral type ID
	internal ResourceDescriptor serializedResourceDescriptor = new();
	internal ResourceDescriptor serializedOffsetResourceDescriptor = new();
	[PlatformSpecificArrayLength(Platform.PC, 4, Platform.Xbox360, 5, Platform.PlayStation3, 6)]
	internal VoidPointer[] serializedResource = [];

	internal ulong Id { get; set; }
	internal List<ImportEntry> ImportEntries { get; } = [];
}
