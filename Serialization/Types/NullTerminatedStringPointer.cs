using Serialization.Interfaces;

namespace Serialization.Types;

// Represents a pointer to a null-terminated string (C string).
public class NullTerminatedStringPointer : IAddressHolder
{
	public ulong Address { get; set; }
	public string? Data { get; set; }
}
