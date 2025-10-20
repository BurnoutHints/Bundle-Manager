using Serialization.Attributes;
using Information.Enums;
using Serialization.Types;

namespace BundleFormat.BundleV1;

internal class BundleBase
{
	internal uint magicNumber;
	internal uint version;
	internal uint numResources;
	internal ResourceDescriptor bundleResourceDescriptor = new();
	[PlatformSpecificArrayLength(Platform.PC, 4, Platform.Xbox360, 5, Platform.PlayStation3, 6)]
	internal VoidPointer[] allocatedResource = [];
	internal uint hashTableOffset;
	internal uint resourceEntriesOffset;
	internal uint importTablesOffset;
	internal uint resourceDataOffset;
	internal uint platform;
}
