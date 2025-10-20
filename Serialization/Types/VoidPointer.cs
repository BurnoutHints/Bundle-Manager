using Serialization.Interfaces;

namespace Serialization.Types;

// Represents a pointer.
// The C++ equivalent is void*.
// Serialization uses this to handle 32/64 bit differences.
// Does not store data, only an address.
public class VoidPointer : IAddressHolder
{
	public ulong Address { get; set; }
}
