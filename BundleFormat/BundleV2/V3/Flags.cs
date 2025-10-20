namespace BundleFormat.BundleV2.V3;

[Flags]
internal enum Flags
{
	Default = 0,
	ZlibCompression = 1,
	MainMemOptimization = 1 << 1,
	GraphicsMemOptimization = 1 << 2,
	ContainsDebugData = 1 << 3,
	NonAsyncFixupRequired = 1 << 4,
	MultistreamBundle = 1 << 5,
	DeltaBundle = 1 << 6
}
