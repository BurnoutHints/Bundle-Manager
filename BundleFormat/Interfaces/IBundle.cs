using Serialization;

namespace BundleFormat.Interfaces;

public interface IBundle
{
	// Path of the file that is read from and saved to.
	string FilePath { get; set; }

	// All the resources in the bundle.
	List<IResource> Resources { get; }

	// Retrieves the resource associated with the given ID.
	IResource? GetResource(ID id);

	// Loads the bundle from the given file path.
	bool Load();

	// Saves the bundle (with any modifications) to a file.
	bool Save();

	// Validates the magic and version match those expected.
	bool ValidateVersion(EndianAwareBinaryReader reader);
}
