# Serialization guide

This guide covers usage of Bundle Manager Neo's included binary serialization library.

## Usage

Deserialization is usually very simple:

```cs
public static object Deserialize(Type type, IResource resource)
{
	resource.Memory.Seek(0, SeekOrigin.Begin);
	return Deserializer.Deserialize(type, resource.Memory, resource.Config);
}
```

Most resource types will require something more for serialization, but it should still be quite short. ProgressionData's Serialize(), for example, updates the pointers and the size field, then serializes.

```cs
public static void Serialize(object obj, IResource resource)
{
	var progressionData = (ProgressionData)obj;
	progressionData.UpdatePointers(resource.Config.Platform.Is64Bit, out ulong position);
	progressionData.size = (uint)Serializer.Align(position, 0x10);
	resource.Memory.Seek(0, SeekOrigin.Begin);
	resource.Memory.SetLength(0);
	Serializer.Serialize(resource.Memory, resource.Config, obj);
}
```

Of course, if this isn't appealing to you, the library makes available its EndianAwareBinaryReader/Writer classes and you can use those directly. However, the built-in editor is designed around the model using serialization types and attributes, so a custom view will be needed if they are not used. If there is demand for it, the universal editor may be updated to support non-serializable models in the future.

## Creating serializable models

The deserializer and serializer work exclusively with non-public, non-static fields. Public fields and both non-public and public properties (including compiler-generated backing fields) are ignored. The fields it works with should generally be a one-to-one match with the documented structure.

Public properties are what get displayed in the UI/editor. These should access the non-public fields as backing fields. Any desired changes to the fields (for example, turning a char array into a string) needs to be done in the getter/setter. If a property has no setter, its UI control will be read-only/disabled.

Non-public properties may be used if you have intermediate data that must be hidden both from the deserializer/serializer and from the UI. The most common use case for this is for storing the resource body object, if there is one.

Below is an excerpt of the model for the ProgressionData resource type. Notice the non-standard types and attributes. These will be covered in the next section.

```cs
internal class ProgressionData : ISerializableResourceType
{
	internal uint versionNumber;
	internal uint size;
	[ArrayLengthField(nameof(playerCarIdCount))]
	internal ArrayPointer<CgsId> playerCarIds = new();
	internal uint playerCarIdCount;
	[ArrayLengthField(nameof(progressionRankCount))]
	internal ArrayPointer<ProgressionRankData> progressionRanks = new();
	internal uint progressionRankCount;
	// ...
	[Tooltip("Vehicles to unlock at the beginning of the game.\nThis is not used in the final game.")]
	public CollectionDescriptor<CgsId> PlayerCarIds { get => playerCarIds.Data!; }
	[Tooltip("Licenses.\nSettings include traffic density and requirements to reach the next license.")]
	public CollectionDescriptor<ProgressionRankData> ProgressionRanks { get => progressionRanks.Data!; }
	//...
}
```

## Types and attributes

The library contains specific types and attributes that are required for the deserializer/serializer to correctly interpret data. Most of these have to do with automatic pointer handling, alignment, and updating of linked fields, though some are used for displaying data.

### Pointers

Neo uses no unsafe code; there are no actual pointers. Instead, there are four dedicated types for handling the various uses of pointers, each of which holds an address and (if applicable) the data that is pointed to. When used, the deserializer/serializer handles 32/64 bit differences automatically.

- **VoidPointer**: Represents `void*`. This is the most basic pointer type and only holds an address.
- **NullTerminatedStringPointer**: Represents `char*`. `Data` is a `string`.
- **InstancePointer<T>**: Represents `T*`. `Data` is an instance of `T`.
- **ArrayPointer<T>**: Represents `T*` where the pointer goes to an array instead of an instance. `Data` is a `CollectionDescriptor<T>`. This is covered further in the [arrays section](#Arrays).

Due to inconsistencies in how pointers are calculated between files, the addresses will need to be manually recalculated just before serialization. This is the only manual serialization-related work required by the library.

### Arrays

There are two types used for array handling:

- **CollectionDescriptor<T>**: Used for fixed-length arrays. Tracks the array's capacity and whether it has a used element count.
- **ArrayPointer<T>**: Used for pointers to arrays. 

These types cover three kinds of arrays: fixed length, fixed length with a used element count, and variable length with an element count. They can be used as follows:

```cs
// Fixed-length array that always uses all of its elements.
internal class OpponentBalanceData
{
	[ArrayLength(8)]
	internal CollectionDescriptor<float> aheadGraphPoints = new();
	// ...
	public CollectionDescriptor<float> AheadGraphPoints { get => aheadGraphPoints; }
	// ...
}

// Fixed-length array with a used element count.
internal class CarOpponentSet
{
	[ArrayLength(8)]
	[ArrayLengthField(nameof(opponentCount))]
	internal CollectionDescriptor<CarOpponent> carOpponents = new();
	// ...
	public CollectionDescriptor<CarOpponent> CarOpponents { get => carOpponents; }
	// ...
}

// Pointer to an array of elements, plus an element count.
internal class ProgressionData : ISerializableResourceType
{
	[ArrayLengthField(nameof(eventCount))]
	internal ArrayPointer<BP_19.RaceEventData> events = new();
	internal uint eventCount;
	// ...
	public CollectionDescriptor<BP_19.RaceEventData> Events { get => events.Data!; }
}
```

The attributes are fairly straightforward:

- **ArrayLength**: Defines the length of the underlying array. This mostly exists to support two-dimensional arrays and may be removed in the future.
- **ArrayLengthField**: Defines the field which stores the element count. The specified field is automatically updated when elements are added to or removed from the collection.
- **PlatformSpecificArrayLength**: Not shown here. If an array's fixed length differs between platforms, this can be used to define the per-platform array lengths.

### Referencing array elements

You may also encounter cases where an instance pointer references an object in an array instead of a standalone object. Here, several attributes are used:

```cs
[AlignEnd]
[CloneBehavior(CloneMode.Shallow)]
internal class EventJunction
{
	internal uint id;
	internal InstancePointer<RaceEventData> offlineEvent = new();
	// ...
	[RequiresValue("Offline event requires a value to be set.")]
	[FromArray(nameof(ProgressionData.Events))]
	public RaceEventData OfflineEvent { get => offlineEvent.Data!; set => offlineEvent.Data = value; }
	// ...
}
```

`FromArray` causes the UI/editor to display an array element picker instead of a button that goes to an object editor. It is often used with `DisplayMember`, which is set on the instance pointer's type `T`'s class/struct:

```cs
[AlignEnd]
[DisplayMember(nameof(Id))]
internal class RaceEventData
{
	// ...
	public uint Id { get => id; set => id = value; }
	// ...
}
```

If `DisplayMember` is not set, element indices will be displayed instead.

The `CloneBehavior` attribute indicates the mode to use when the parent object is duplicated in the editor. By default, a `Deep` clone is performed, but this is undesirable when the instance pointer needs to point to an existing object, so a `Shallow` clone should be used here instead. If you have an object that has some members that need to be deep cloned and some that need to be shallow cloned, you can use the `Custom` clone mode and have the model implement `ICloneable`.

Finally, the `RequiresValue` attribute enforces the value being non-null and disallows saving without setting a value. The chosen message string will be displayed to the user.

### Alignment

The deserializer/serializer automatically performs most alignment. However, there are a few cases where specific behavior is desired or alignment needs to be changed or prevented. The following attributes enable this.

- **AlignEnd**: Aligns the end of a structure to the size of its largest member, up to 16 bytes, or to an arbitrary power of 2.
- **Unaligned**: Disables the automatic field alignment on this specific field.

### Editor-related attributes

- **Bounds**: When applied to a numeric property, defines the upper and lower bounds the user can choose.
- **MaxStringLength**: When applied to a string property, defines the maximum length of the string the user can input.
- **Tooltip**: Sets the text to display on a property's header on hover.

## Handling strings

Better ways of handling strings are being considered. For now, they are manually built on a per-property basis:

```cs
internal class Rival
{
	// ...
	[ArrayLength(32)]
	internal CollectionDescriptor<sbyte> name = new();
	// ...
	[MaxStringLength(32)]
	public string Name
	{
		get
		{
			StringBuilder nameBuilder = new();
			for (int i = 0; i < 32; i++)
			{
				sbyte character = name.Collection[i];
				if (character == 0)
					break;
				nameBuilder.Append((char)character);
			}
			return nameBuilder.ToString();
		}
		set
		{
			name.Collection.Clear();
			var array = (Array.ConvertAll(Encoding.ASCII.GetBytes(value), c => Convert.ToSByte(c)));
			for (int i = 0; i < 32; i++)
			{
				if (i < array.Length)
					name.Collection.Add(array[i]);
				else
					name.Collection.Add(0);
			}
		}
	}
}
```
