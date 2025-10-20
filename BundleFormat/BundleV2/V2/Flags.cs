namespace BundleFormat.BundleV2.V2;

[Flags]
internal enum Flags
{
	Compressed = 1,
	MainMemOptimized = 1 << 1,
	GraphicsMemOptimized = 1 << 2,
	HasDebugData = 1 << 3
}
