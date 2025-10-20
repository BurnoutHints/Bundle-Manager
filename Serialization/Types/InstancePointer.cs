using Serialization.Interfaces;

namespace Serialization.Types;

// A pointer to an instance of an object.
// The C++ equivalent is T*.
// Like Pointer, this handles 32/64 bit differences.
// During deserialization, the address is read, then seeked to. The data at the
// address is interpreted as the specified type and stored as such.
// During serialization, the address is written, then seeked to. The stored
// object is written at the address.
public class InstancePointer<T> : IAddressHolder
{
	public ulong Address { get; set; }
	public T? Data { get; set; }
}
