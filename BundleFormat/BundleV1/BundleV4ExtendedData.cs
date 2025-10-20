using Serialization.Attributes;
using Serialization.Types;

namespace BundleFormat.BundleV1;

internal class BundleV4ExtendedData : BundleBase
{
	internal uint flags;
	internal uint uncompressedDescriptorCount;
	[ArrayLengthField(nameof(uncompressedDescriptorCount))]
	internal ArrayPointer<ResourceDescriptor> uncompressedResourceDescriptors = new();
}
