using Serialization.Interfaces;

namespace Serialization.Types;

// Represents a pointer to an array of objects.
// The C++ equivalent is T* with a length field. It must be annotated with the
// ArrayPointerLengthField attribute.
// Like Pointer, this handles 32/64 bit differences.
// During deserialization, the address is read, then seeked to. The data at the
// address is interpreted as an array of the specified type and stored as such.
// During serialization, the address is written, then seeked to. The stored
// elements are written at the address sequentially.
public class ArrayPointer<T> : IAddressHolder
{
	public ulong Address { get; set; }
	public int Length { get; set; }
	public CollectionDescriptor<T>? Data { get; set; } = new();
}
