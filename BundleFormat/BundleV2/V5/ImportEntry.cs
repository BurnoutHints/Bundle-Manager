using Serialization.Attributes;

namespace BundleFormat.BundleV2.V5;

[AlignEnd]
internal class ImportEntry
{
	internal ID importId;
	internal uint importTypeAndOffset;
}
