using Configuration;
using System.Collections;

namespace BundleFormat.Interfaces;

public interface IResource
{
	FileConfig Config { get; }
	ID Id { get; }
	string Name { get; }
	uint Type { get; }
	string TypeName { get; }
	long Size { get; }
	byte StreamIndex { get; }
	string StreamName { get; }
	MemoryStream Memory { get; set; }
	MemoryStream? BodyMemory { get; set; }
	int BodyMemoryMemTypeIndex { get; set; }
	IList Imports { get; }
}
