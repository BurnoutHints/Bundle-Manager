using Configuration;
using Serialization.Attributes;
using Serialization.Types;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Serialization;

public static class Deserializer
{
	private static readonly Dictionary<Tuple<ulong, Type>, object> objectDictionary = [];

	public static T Deserialize<T>(Stream stream, FileConfig config) where T : new()
	{
		stream.Seek(0, SeekOrigin.Begin);
		EndianAwareBinaryReader reader = new(stream, config.Platform.Endianness, config.Platform.Is64Bit);
		T instance = (T)DeserializeObject(typeof(T), reader);
		objectDictionary.Clear();
		return instance;
	}

	public static object Deserialize(Type type, Stream stream, FileConfig config)
	{
		stream.Seek(0, SeekOrigin.Begin);
		EndianAwareBinaryReader reader = new(stream, config.Platform.Endianness, config.Platform.Is64Bit);
		object instance = DeserializeObject(type, reader);
		objectDictionary.Clear();
		return instance;
	}

	public static T Deserialize<T>(EndianAwareBinaryReader reader) where T : new()
	{
		T instance = (T)DeserializeObject(typeof(T), reader);
		objectDictionary.Clear();
		return instance;
	}

	public static object Deserialize(Type type, EndianAwareBinaryReader reader)
	{
		object instance = DeserializeObject(type, reader);
		objectDictionary.Clear();
		return instance;
	}

	private static object DeserializeObject(Type type, EndianAwareBinaryReader reader)
	{
		long startPosition = reader.BaseStream.Position;
		
		object obj = Activator.CreateInstance(type)
			?? throw new Exception($"Could not create type {type.Name}.");

		List<Tuple<long, FieldInfo>> deferredLists = [];

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
				if (fieldInfo.FieldType.IsGenericType
					&& (fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(ArrayPointer<>)
					|| fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(CollectionDescriptor<>)))
					deferredLists.Add(new(reader.BaseStream.Position, fieldInfo));
				fieldInfo.SetValue(obj, ReadField(fieldInfo.FieldType, reader, fieldInfo, obj));
			}
		}

		long pos = reader.BaseStream.Position;
		foreach (var arrayPointerInfo in deferredLists)
		{
			reader.Seek(arrayPointerInfo.Item1, SeekOrigin.Begin);
			arrayPointerInfo.Item2.SetValue(obj, ReadField(arrayPointerInfo.Item2.FieldType, reader, arrayPointerInfo.Item2, obj));
		}
		reader.Seek(pos, SeekOrigin.Begin);

		var alignEndAttribute = type.GetCustomAttribute<AlignEndAttribute>();
		if (alignEndAttribute != null)
		{
			if (alignEndAttribute.Alignment == 0)
				reader.Align(GetAlignment(type, reader));
			else
				reader.Align(alignEndAttribute.Alignment);
		}

		long endPosition = reader.BaseStream.Position;
		ulong structSize = (ulong)(endPosition - startPosition);
		if (!type.IsGenericType)
			Serializer.StructureSizes[new(type, reader.Is64Bit)] = structSize;

		return obj;
	}

	private static object ReadField(Type type, EndianAwareBinaryReader reader, FieldInfo fieldInfo, object parent)
	{
		var unalignedAttribute = fieldInfo.GetCustomAttribute<UnalignedAttribute>();
		if (unalignedAttribute == null)
			reader.Align(GetAlignment(type, reader));
		
		if (type.IsPrimitive)
			return ReadPrimitive(type, reader);

		if (type.IsEnum)
			return ReadPrimitive(type.GetEnumUnderlyingType(), reader);

		if (type == typeof(VoidPointer))
			return ReadVoidPointer(reader);

		if (type.IsGenericType)
		{
			if (type.GetGenericTypeDefinition() == typeof(InstancePointer<>))
				return ReadInstancePointer(type, reader);
			if (type.GetGenericTypeDefinition() == typeof(ArrayPointer<>))
				return ReadArrayPointer(type, reader, fieldInfo, parent);
			if (type.GetGenericTypeDefinition() == typeof(CollectionDescriptor<>))
				return ReadCollectionDescriptor(type, reader, fieldInfo, parent);
			throw new Exception($"Unsupported generic field type {type.FullName}");
		}

		if (type == typeof(NullTerminatedStringPointer))
			return ReadString(reader);

		if (type.IsArray)
			return ReadArray(type, reader, fieldInfo, parent);

		if (type.IsClass)
		{
			ulong address = (ulong)reader.BaseStream.Position;
			if (objectDictionary.TryGetValue(new(address, type), out object? instance))
			{
				_ = DeserializeObject(type, reader); // Necessary for ensuring the position is correct.
				return instance!;
			}
			object deserializedClass = DeserializeObject(type, reader);
			objectDictionary.Add(new(address, type), deserializedClass);
			return deserializedClass;
		}

		throw new Exception($"Unsupported field type {type.FullName}");
	}

	private static uint GetAlignment(Type type, EndianAwareBinaryReader reader)
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
			return GetAlignment(type.GetEnumUnderlyingType(), reader);

		if (type == typeof(VoidPointer) || type == typeof(NullTerminatedStringPointer))
			return (uint)(reader.Is64Bit ? 8 : 4);

		if (type.IsGenericType)
		{
			if (type.GetGenericTypeDefinition() == typeof(InstancePointer<>))
				return (uint)(reader.Is64Bit ? 8 : 4);
			if (type.GetGenericTypeDefinition() == typeof(ArrayPointer<>))
				return (uint)(reader.Is64Bit ? 8 : 4);
			if (type.GetGenericTypeDefinition() == typeof(CollectionDescriptor<>))
				return GetAlignment(type.GenericTypeArguments[0], reader);
		}

		if (type.IsArray)
			return GetAlignment(type.GetElementType()!, reader);

		if (type.IsClass)
		{
			uint maxSize = 1;
			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
			{
				uint fieldAlignment = GetAlignment(fieldInfo.FieldType, reader);
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

	private static object ReadPrimitive(Type type, EndianAwareBinaryReader reader)
	{
		if (type == typeof(bool)) return reader.ReadBoolean();
		if (type == typeof(sbyte)) return reader.ReadSByte();
		if (type == typeof(byte)) return reader.ReadByte();
		if (type == typeof(short)) return reader.ReadInt16();
		if (type == typeof(ushort)) return reader.ReadUInt16();
		if (type == typeof(int)) return reader.ReadInt32();
		if (type == typeof(uint)) return reader.ReadUInt32();
		if (type == typeof(long)) return reader.ReadInt64();
		if (type == typeof(ulong)) return reader.ReadUInt64();
		if (type == typeof(float)) return reader.ReadSingle();
		if (type == typeof(double)) return reader.ReadDouble();
		if (type == typeof(nint) || type == typeof(nuint) || type == typeof(char))
			throw new Exception($"Encountered unsupported type {type.FullName}.");
		throw new Exception($"Encountered unhandled primitive type {type.FullName}.");
	}

	private static VoidPointer ReadVoidPointer(EndianAwareBinaryReader reader)
	{
		ulong address = reader.Is64Bit ? reader.ReadUInt64() : reader.ReadUInt32();
		return new VoidPointer { Address = address };
	}

	private static object ReadInstancePointer(Type type, EndianAwareBinaryReader reader)
	{
		Type pointedToType = type.GetGenericArguments()[0];
		object instancePointer = Activator.CreateInstance(type)
			?? throw new Exception($"Could not create InstancePointer<{pointedToType.Name}> instance.");

		ulong address = reader.Is64Bit ? reader.ReadUInt64() : reader.ReadUInt32();
		type.GetProperty("Address")!.SetValue(instancePointer, address);
		if (address == 0)
			return instancePointer;
		if (objectDictionary.TryGetValue(new(address, pointedToType), out object? existingInstance))
		{
			type.GetProperty("Data")!.SetValue(instancePointer, existingInstance);
			return instancePointer;
		}

		long pos = reader.BaseStream.Position;
		reader.Seek((long)address, SeekOrigin.Begin);
		var data = DeserializeObject(pointedToType, reader);
		type.GetProperty("Data")!.SetValue(instancePointer, data);
		reader.Seek(pos, SeekOrigin.Begin);

		objectDictionary.Add(new(address, pointedToType), data);
		return instancePointer;
	}

	private static object ReadArrayPointer(Type type, EndianAwareBinaryReader reader, FieldInfo fieldInfo, object parent)
	{
		Type elementType = type.GetGenericArguments()[0];
		object arrayPointer = Activator.CreateInstance(type)
			?? throw new Exception($"Could not create ArrayPointer<{elementType.Name}> instance.");

		ulong address = reader.Is64Bit ? reader.ReadUInt64() : reader.ReadUInt32();
		type.GetProperty("Address")!.SetValue(arrayPointer, address);
		if (address == 0)
			return arrayPointer;

		var lengthFieldAttribute = fieldInfo.GetCustomAttribute<ArrayLengthFieldAttribute>()
			?? throw new Exception($"ArrayPointer<{elementType.Name}>: no ArrayLengthField attribute found.");
		var externalLengthField = fieldInfo.DeclaringType!.GetField(lengthFieldAttribute.FieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;
		int length = Convert.ToInt32(externalLengthField.GetValue(parent)!);
		var internalLengthProperty = type.GetProperty("Length")!;
		internalLengthProperty.SetValue(arrayPointer, length);

		long pos = reader.BaseStream.Position;
		reader.Seek((long)address, SeekOrigin.Begin);
		var listRefType = typeof(CollectionDescriptor<>).MakeGenericType(elementType);
		var listRef = Activator.CreateInstance(listRefType)!;
		var list = (IList)listRefType.GetProperty("Collection")!.GetValue(listRef)!;
		for (int i = 0; i < length; i++)
			list.Add(ReadField(elementType, reader, fieldInfo, parent));

		var notifier = (INotifyCollectionChanged)listRefType.GetProperty("Collection")!.GetValue(listRef)!;
		notifier.CollectionChanged += (s, e) =>
		{
			internalLengthProperty.SetValue(arrayPointer, Convert.ChangeType(list.Count, internalLengthProperty.PropertyType));
			externalLengthField.SetValue(parent, Convert.ChangeType(list.Count, externalLengthField.FieldType));
		};

		type.GetProperty("Data")!.SetValue(arrayPointer, listRef);
		reader.Seek(pos, SeekOrigin.Begin);

		return arrayPointer;
	}

	private static NullTerminatedStringPointer ReadString(EndianAwareBinaryReader reader)
	{
		NullTerminatedStringPointer cString = new()
		{
			Address = reader.Is64Bit ? reader.ReadUInt64() : reader.ReadUInt32()
		};
		if (cString.Address == 0)
			return cString;

		long pos = reader.BaseStream.Position;
		reader.Seek((long)cString.Address, SeekOrigin.Begin);
		StringBuilder stringBuilder = new();
		sbyte character;
		while ((character = reader.ReadSByte()) != 0)
			stringBuilder.Append((char)character);
		cString.Data = stringBuilder.ToString();
		reader.Seek(pos, SeekOrigin.Begin);

		return cString;
	}

	private static Array ReadArray(Type type, EndianAwareBinaryReader reader, FieldInfo fieldInfo, object parent)
	{
		Type elementType = type.GetElementType()
			?? throw new Exception($"{fieldInfo.Name}: Element type was null.");

		var sizeInfo = fieldInfo.GetCustomAttribute<ArrayLengthAttribute>();
		var platformSpecificSizeInfo = fieldInfo.GetCustomAttribute<PlatformSpecificArrayLengthAttribute>();
		int[] length = [0];
		if (sizeInfo != null)
			length = sizeInfo.Length;
		else if (platformSpecificSizeInfo != null)
			length[0] = platformSpecificSizeInfo.GetLength(UserConfig.Config.Platform);
		else
			throw new Exception($"Array {fieldInfo.Name} was not annotated with size information.");

		if (length.Length == 1)
		{
			Array array = Array.CreateInstance(elementType, length[0]);
			for (int i = 0; i < array.Length; i++)
				array.SetValue(ReadField(elementType, reader, fieldInfo, parent), i);
			return array;
		}
		else if (length.Length == 2)
		{
			// SetValue()'s current overload can't handle more than two dimensions.
			// >2 dimensional arrays will remain unsupported unless they occur in a resource somewhere.
			Array array = Array.CreateInstance(elementType, length);
			for (int dim0 = 0; dim0 < array.GetLength(0); dim0++)
			{
				for (int dim1 = 0; dim1 < array.GetLength(1); dim1++)
					array.SetValue(ReadField(elementType, reader, fieldInfo, parent), dim0, dim1);
			}
			return array;
		}
		else
			throw new Exception("Only one- and two-dimensional arrays are supported.");
	}

	private static object ReadCollectionDescriptor(Type type, EndianAwareBinaryReader reader, FieldInfo fieldInfo, object parent)
	{
		Type elementType = type.GetGenericArguments()[0];

		var listRef = Activator.CreateInstance(type)!;
		var listProperty = type.GetProperty("Collection")!;
		var list = (IList)listProperty.GetValue(listRef)!;

		var sizeInfo = fieldInfo.GetCustomAttribute<ArrayLengthAttribute>();
		var platformSpecificSizeInfo = fieldInfo.GetCustomAttribute<PlatformSpecificArrayLengthAttribute>();
		int length;
		if (sizeInfo != null)
			length = sizeInfo.Length[0];
		else if (platformSpecificSizeInfo != null)
			length = platformSpecificSizeInfo.GetLength(UserConfig.Config.Platform);
		else
			throw new Exception($"Array {fieldInfo.Name} was not annotated with size information.");

		int usedLength = length;
		var usedLengthAttribute = fieldInfo.GetCustomAttribute<ArrayLengthFieldAttribute>();
		if (usedLengthAttribute != null)
		{
			type.GetProperty("VariableElementUsage")!.SetValue(listRef, true);
			var usedLengthField = parent.GetType().GetField(usedLengthAttribute.FieldName, BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new Exception($"Array {fieldInfo.Name}: Used-length field '{usedLengthAttribute.FieldName}' not found.");

			usedLength = Convert.ToInt32(usedLengthField.GetValue(parent));
			if (usedLength < 0 || usedLength > length)
				throw new Exception($"Array {fieldInfo.Name}: Used length {usedLength} exceeds allocated length {length}.");
		}

		type.GetProperty("Capacity")!.SetValue(listRef, length);

		for (int i = 0; i < usedLength; i++)
			list.Add(ReadField(elementType, reader, fieldInfo, parent));

		for (int i = usedLength; i < length; i++)
			_ = ReadField(elementType, reader, fieldInfo, parent);

		if (usedLengthAttribute != null)
		{
			var usedLengthField = parent.GetType().GetField(usedLengthAttribute.FieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;
			var notifier = (INotifyCollectionChanged)type.GetProperty("Collection")!.GetValue(listRef)!;
			notifier.CollectionChanged += (s, e) =>
			{
				usedLengthField.SetValue(parent, Convert.ChangeType(list.Count, usedLengthField.FieldType));
			};
		}

		return listRef;
	}
}
