namespace BundleFormat.Interfaces;

// Bundle-specific counterpart to Serialization.Interfaces.ISerializableObject.
public interface ISerializableResourceType
{
	static abstract object Deserialize(Type type, IResource resource);
	static abstract void Serialize(object obj, IResource resource);
}
