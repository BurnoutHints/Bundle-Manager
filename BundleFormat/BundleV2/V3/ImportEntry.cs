using Serialization.Attributes;

namespace BundleFormat.BundleV2.V3;

[AlignEnd]
internal class ImportEntry
{
	internal ID importId;
	internal uint importTypeAndOffset;
}
