using Serialization.Attributes;

namespace BundleFormat.BundleV1;

[AlignEnd]
internal class ImportEntry
{
	internal ID importId;
	internal uint offset;
}
