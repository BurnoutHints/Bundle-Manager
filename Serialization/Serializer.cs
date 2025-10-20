using Configuration;
using Serialization.Attributes;
using Serialization.Interfaces;
using Serialization.Types;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Serialization;

public static class Serializer
{
	public static Dictionary<Tuple<Type, bool>, ulong> StructureSizes { get; } = [];

	public static void Serialize<T>(Stream stream, FileConfig config, T obj)
	{
		if (obj == null)
			throw new Exception("Attempted to serialize null object.");
		EndianAwareBinaryWriter writer = new(stream, config.Platform.Endianness, config.Platform.Is64Bit);
		SerializeObject(obj.GetType(), obj, writer);
	}

	public static void Serialize<T>(EndianAwareBinaryWriter writer, T obj)
	{
		if (obj == null)
			throw new Exception("Attempted to serialize null object.");
		SerializeObject(obj.GetType(), obj, writer);
	}

	private static void SerializeObject(Type type, object obj, EndianAwareBinaryWriter writer)
	{
		Stack<Type> classHierarchy = new();
		for (Type? t = type; t != null && t != typeof(object); t = t.BaseType)
			classHierarchy.Push(t);
		while (classHierarchy.Count > 0)
		{
			Type currentClass = classHierarchy.Pop();
			foreach (FieldInfo fieldInfo in currentClass.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
			{
				if (fieldInfo.IsDefined(typeof(CompilerGeneratedAttribute)))
					continue;
				object value = fieldInfo.GetValue(obj)
					?? throw new Exception($"Field {fieldInfo.Name}: Value was null.");
				WriteField(fieldInfo.FieldType, value, writer, fieldInfo, obj);
			}
		}

		var alignEndAttribute = type.GetCustomAttribute<AlignEndAttribute>();
		if (alignEndAttribute != null)
		{
			if (alignEndAttribute.Alignment == 0)
				writer.Align(GetAlignment(type, writer));
			else
				writer.Align(alignEndAttribute.Alignment);
		}
	}

	private static void WriteField(Type type, object value, EndianAwareBinaryWriter writer, FieldInfo fieldInfo, object parent)
	{
		var unalignedAttribute = fieldInfo.GetCustomAttribute<UnalignedAttribute>();
		if (unalignedAttribute == null)
			writer.Align(GetAlignment(type, writer));

		if (type.IsPrimitive)
		{
			WritePrimitive(type, value, writer);
			return;
		}

		if (type.IsEnum)
		{
			WritePrimitive(type.GetEnumUnderlyingType(), value, writer);
			return;
		}

		if (type == typeof(VoidPointer))
		{
			WriteVoidPointer((VoidPointer)value, writer);
			return;
		}

		if (type.IsGenericType)
		{
			if (type.GetGenericTypeDefinition() == typeof(InstancePointer<>))
			{
				WriteInstancePointer(type, value, writer);
				return;
			}
			if (type.GetGenericTypeDefinition() == typeof(ArrayPointer<>))
			{
				WriteArrayPointer(type, value, writer, fieldInfo, parent);
				return;
			}
			if (type.GetGenericTypeDefinition() == typeof(CollectionDescriptor<>))
			{
				WriteCollectionDescriptor(type, value, writer, fieldInfo, parent);
				return;
			}
			throw new Exception($"Unsupported generic field type {type.FullName}");
		}

		if (type == typeof(string))
		{
			WriteString((NullTerminatedStringPointer)value, writer);
			return;
		}

		if (type.IsArray)
		{
			WriteArray(type, (Array)value, writer, fieldInfo, parent);
			return;
		}

		if (type.IsClass)
		{
			SerializeObject(type, value, writer);
			return;
		}

		throw new Exception($"Unsupported field type {type.FullName}");
	}

	private static uint GetAlignment(Type type, EndianAwareBinaryWriter writer)
	{
		if (type.IsPrimitive)
		{
			if (type == typeof(bool) || type == typeof(sbyte) || type == typeof(byte))
				return 1;
			else if (type == typeof(short) || type == typeof(ushort))
				return 2;
			else if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
				return 4;
			else if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
				return 8;
		}

		if (type.IsEnum)
			return GetAlignment(type.GetEnumUnderlyingType(), writer);

		if (type == typeof(VoidPointer) || type == typeof(NullTerminatedStringPointer))
			return (uint)(writer.Is64Bit ? 8 : 4);

		if (type.IsGenericType)
		{
			if (type.GetGenericTypeDefinition() == typeof(InstancePointer<>))
				return (uint)(writer.Is64Bit ? 8 : 4);
			if (type.GetGenericTypeDefinition() == typeof(ArrayPointer<>))
				return (uint)(writer.Is64Bit ? 8 : 4);
			if (type.GetGenericTypeDefinition() == typeof(CollectionDescriptor<>))
				return GetAlignment(type.GenericTypeArguments[0], writer);
		}

		if (type.IsArray)
			return GetAlignment(type.GetElementType()!, writer);

		if (type.IsClass)
		{
			uint maxSize = 1;
			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
			{
				uint fieldAlignment = GetAlignment(fieldInfo.FieldType, writer);
				if (fieldAlignment > maxSize)
					maxSize = fieldAlignment;
			}
			if (maxSize > 8)
				return 0x10;
			else if (maxSize > 4)
				return 8;
			else if (maxSize > 2)
				return 4;
			else if (maxSize > 1)
				return 2;
		}

		return 1;
	}

	private static void WritePrimitive(Type type, object value, EndianAwareBinaryWriter writer)
	{
		if (type == typeof(bool)) { writer.Write((bool)value); return; }
		if (type == typeof(sbyte)) { writer.Write((sbyte)value); return; }
		if (type == typeof(byte)) { writer.Write((byte)value); return; }
		if (type == typeof(short)) { writer.Write((short)value); return; }
		if (type == typeof(ushort)) { writer.Write((ushort)value); return; }
		if (type == typeof(int)) { writer.Write((int)value); return; }
		if (type == typeof(uint)) { writer.Write((uint)value); return; }
		if (type == typeof(long)) { writer.Write((long)value); return; }
		if (type == typeof(ulong)) { writer.Write((ulong)value); return; }
		if (type == typeof(float)) { writer.Write((float)value); return; }
		if (type == typeof(double)) { writer.Write((double)value); return; }
		if (type == typeof(nint) || type == typeof(nuint) || type == typeof(char))
			throw new Exception($"Encountered unsupported type {type.FullName}.");
		throw new Exception($"Encountered unhandled primitive type {type.FullName}.");
	}

	private static void WriteVoidPointer(VoidPointer pointer, EndianAwareBinaryWriter writer)
	{
		if (writer.Is64Bit)
			writer.Write(pointer.Address);
		else
			writer.Write((uint)pointer.Address);
	}

	private static void WriteInstancePointer(Type type, object value, EndianAwareBinaryWriter writer)
	{
		Type pointedToType = type.GetGenericArguments()[0];

		ulong address = (ulong)type.GetProperty("Address")!.GetValue(value)!;
		object data = type.GetProperty("Data")!.GetValue(value)!;

		if (writer.Is64Bit)
			writer.Write(address);
		else
			writer.Write((uint)address);
		if (address == 0)
			return;

		long pos = writer.BaseStream.Position;
		writer.Seek((long)address, SeekOrigin.Begin);
		SerializeObject(pointedToType, data, writer);
		writer.Seek(pos, SeekOrigin.Begin);
	}

	private static void WriteArrayPointer(Type type, object value, EndianAwareBinaryWriter writer, FieldInfo fieldInfo, object parent)
	{
		Type elementType = type.GetGenericArguments()[0];

		ulong address = (ulong)type.GetProperty("Address")!.GetValue(value)!;
		int length = (int)type.GetProperty("Length")!.GetValue(value)!;
		var dataRef = type.GetProperty("Data")!.GetValue(value);
		var list = dataRef != null ? (IList?)dataRef.GetType().GetProperty("Collection")!.GetValue(dataRef) : null;

		if (writer.Is64Bit)
			writer.Write(address);
		else
			writer.Write((uint)address);
		if (address == 0 || list == null)
			return;

		long pos = writer.BaseStream.Position;
		writer.Seek((long)address, SeekOrigin.Begin);
		for (int i = 0; i < length; i++)
		{
			var element = list[i]
				?? throw new Exception($"Array {fieldInfo.Name}: Element {i} was null.");
			WriteField(elementType, element, writer, fieldInfo, parent);
		}
		writer.Seek(pos, SeekOrigin.Begin);
	}

	private static void WriteString(NullTerminatedStringPointer cString, EndianAwareBinaryWriter writer)
	{
		if (writer.Is64Bit)
			writer.Write(cString.Address);
		else
			writer.Write((uint)cString.Address);

		if (cString.Address == 0 || cString.Data == null)
			return;

		long pos = writer.BaseStream.Position;
		writer.Seek((long)cString.Address, SeekOrigin.Begin);
		byte[] bytes = Encoding.UTF8.GetBytes(cString.Data);
		writer.Write(bytes);
		writer.Write((byte)0);
		writer.Seek(pos, SeekOrigin.Begin);
	}

	private static void WriteArray(Type type, Array array, EndianAwareBinaryWriter writer, FieldInfo fieldInfo, object parent)
	{
		var elementType = type.GetElementType()
			?? throw new Exception($"Array {fieldInfo.Name}: Element type was null.");

		// Not technically needed when serializing, but fine as a sanity check
		var sizeInfo = fieldInfo.GetCustomAttribute<ArrayLengthAttribute>();
		var platformSpecificSizeInfo = fieldInfo.GetCustomAttribute<PlatformSpecificArrayLengthAttribute>();
		int[] length = [0];
		if (sizeInfo != null)
			length = sizeInfo.Length;
		else if (platformSpecificSizeInfo != null)
			length[0] = platformSpecificSizeInfo.GetLength(UserConfig.Config.Platform);
		else
			throw new Exception($"Array {fieldInfo.Name} was not annotated with size information.");
		for (int dimension = 0; dimension < length.Length; dimension++)
		{
			if (array.GetLength(dimension) != length[dimension])
				throw new Exception($"Array {fieldInfo.Name}: Dimension {dimension} has length {array.GetLength(dimension)}, expected {length[dimension]}.");
		}

		if (length.Length == 1)
		{
			for (int i = 0; i < length[0]; i++)
			{
				var elementValue = array.GetValue(i)
					?? throw new Exception($"Array {fieldInfo.Name}: Value of element {i} was null.");
				WriteField(elementType, elementValue, writer, fieldInfo, parent);
			}
		}
		else if (length.Length == 2)
		{
			// GetValue()'s current overload can't handle more than two dimensions.
			// >2 dimensional arrays will remain unsupported unless they occur in a resource somewhere.
			for (int dim0 = 0; dim0 < length[0]; dim0++)
			{
				for (int dim1 = 0; dim1 < length[1]; dim1++)
				{
					var elementValue = array.GetValue(dim0, dim1)
						?? throw new Exception($"Array {fieldInfo.Name}: Value of element {dim0},{dim1} was null.");
					WriteField(elementType, elementValue, writer, fieldInfo, parent);
				}
			}
		}
		else
			throw new Exception("Only one- and two-dimensional arrays are supported.");
	}

	private static void WriteCollectionDescriptor(Type type, object value, EndianAwareBinaryWriter writer, FieldInfo fieldInfo, object parent)
	{
		Type elementType = type.GetGenericArguments()[0];
		var list = (IList)type.GetProperty("Collection")!.GetValue(value)!;

		var sizeInfo = fieldInfo.GetCustomAttribute<ArrayLengthAttribute>();
		var platformSpecificSizeInfo = fieldInfo.GetCustomAttribute<PlatformSpecificArrayLengthAttribute>();
		int length;
		if (sizeInfo != null)
			length = sizeInfo.Length[0];
		else if (platformSpecificSizeInfo != null)
			length = platformSpecificSizeInfo.GetLength(UserConfig.Config.Platform);
		else
			throw new Exception($"Array {fieldInfo.Name} was not annotated with size information.");
		
		for (int i = 0; i < list.Count; i++)
		{
			var elementValue = list[i]
				?? throw new Exception($"Array {fieldInfo.Name}: Value of element {i} was null.");
			WriteField(elementType, elementValue, writer, fieldInfo, parent);
		}

		for (int i = list.Count; i < length; i++)
		{
			var defaultValue = Activator.CreateInstance(elementType);
			WriteField(elementType, defaultValue!, writer, fieldInfo, parent);
		}
	}

	public static long Align(long value, long alignment)
	{
		if (alignment == 0 || (alignment & (alignment - 1)) != 0)
			throw new Exception("Specified alignment was not a power of 2.");
		return value + ((alignment - (value % alignment)) % alignment);
	}

	public static ulong Align(ulong value, ulong alignment)
	{
		if (alignment == 0 || (alignment & (alignment - 1)) != 0)
			throw new Exception("Specified alignment was not a power of 2.");
		return value + ((alignment - (value % alignment)) % alignment);
	}

	public static void UpdatePointer(ref ulong currentPosition, IAddressHolder addressHolder, bool is64Bit)
	{
		addressHolder.Address = currentPosition;
		currentPosition += (ulong)(is64Bit ? 8 : 4);
	}

	public static void UpdateInstanceAddress(ref ulong currentPosition, IAddressHolder addressHolder, ulong instanceLength)
	{
		addressHolder.Address = currentPosition;
		currentPosition += instanceLength;
	}

	public static void UpdateInstanceAddress(ref ulong currentPosition, IAddressHolder addressHolder, Type type, bool is64Bit)
	{
		addressHolder.Address = currentPosition;
		ulong instanceLength = StructureSizes.GetValueOrDefault(new(type, is64Bit));
		currentPosition += instanceLength;
	}

	public static void UpdateArrayAddress(ref ulong currentPosition, IAddressHolder addressHolder, ulong instanceLength,
		ulong instanceCount, ulong alignStart = 0x10, ulong alignEnd = 0x10)
	{
		currentPosition = Align(currentPosition, alignStart);
		addressHolder.Address = currentPosition;
		currentPosition += instanceLength * instanceCount;
		currentPosition = Align(currentPosition, alignEnd);
	}

	public static void UpdateArrayAddress(ref ulong currentPosition, IAddressHolder addressHolder, Type type, bool is64Bit,
		ulong instanceCount, ulong alignStart = 0x10, ulong alignEnd = 0x10)
	{
		currentPosition = Align(currentPosition, alignStart);
		addressHolder.Address = currentPosition;
		ulong instanceLength = StructureSizes.GetValueOrDefault(new(type, is64Bit));
		currentPosition += instanceLength * instanceCount;
		currentPosition = Align(currentPosition, alignEnd);
	}
}
