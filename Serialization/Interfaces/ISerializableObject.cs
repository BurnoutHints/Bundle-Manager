using Configuration;

namespace Serialization.Interfaces;

public interface ISerializableObject
{
	static abstract object Deserialize(Type type, Stream stream, FileConfig config);
	static abstract void Serialize(object obj, Stream stream, FileConfig config);
}
