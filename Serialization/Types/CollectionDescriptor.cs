using Serialization.Interfaces;
using System.Collections.ObjectModel;

namespace Serialization.Types;


public class CollectionDescriptor<T> : ICollectionDescriptor
{
	// The underlying list.
	public ObservableCollection<T> Collection { get; } = [];

	// Maximum length for fixed-length arrays.
	// Zero if this is an ArrayPointer<T>.
	public int Capacity { get; set; }

	// Notes whether this array uses all the elements in the array, all the
	// time. If true, it gets its element count from a sibling field.
	// This is not checked in ArrayPointer<T>.
	public bool VariableElementUsage { get; set; }
}
