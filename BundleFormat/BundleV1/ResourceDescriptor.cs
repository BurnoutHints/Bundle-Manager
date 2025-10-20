using Information.Enums;
using Serialization.Attributes;

namespace BundleFormat.BundleV1;

internal class ResourceDescriptor
{
	[PlatformSpecificArrayLength(Platform.PC, 4, Platform.Xbox360, 5, Platform.PlayStation3, 6)]
	internal BaseResourceDescriptor[] baseResourceDescriptors = [];
}
