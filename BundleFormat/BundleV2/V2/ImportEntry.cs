using Serialization.Attributes;

namespace BundleFormat.BundleV2.V2;

[AlignEnd]
internal class ImportEntry
{
	internal ID importId;
	internal uint offset;
}
