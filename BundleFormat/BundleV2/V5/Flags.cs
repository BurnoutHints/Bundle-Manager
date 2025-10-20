namespace BundleFormat.BundleV2.V5;

[Flags]
internal enum Flags
{
	Default = 0,
	ZlibCompression = 1,
	ContainsDebugData = 1 << 1,
	NonAsyncFixupRequired = 1 << 2,
	MultistreamBundle = 1 << 3,
	DeltaBundle = 1 << 4,
	ContainsDefaultResource = 1 << 5
}
