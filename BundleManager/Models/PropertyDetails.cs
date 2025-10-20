using Serialization.Attributes;
using Serialization.Types;
using System;
using System.Collections;
using System.Reflection;

namespace BundleManager.Models;

public class PropertyDetails
{
	private readonly object topLevelObject;
	private readonly object targetObject;
	public PropertyInfo Info { get; }
	public IList? ParentArray { get; private set; }

	public Type Type { get => Info.PropertyType; }
	public object Value
	{
		get => Info.GetValue(targetObject)!;
		set => Info.SetValue(targetObject, value);
	}
	public string Name { get => Info.Name; }
	public bool IsReadOnly { get => Info.SetMethod == null; }
	public string? Tooltip
	{
		get
		{
			var attribute = Info.GetCustomAttribute<TooltipAttribute>();
			return attribute?.Text;
		}
	}

	public PropertyDetails(object topLevelObject, object target, PropertyInfo info)
	{
		this.topLevelObject = topLevelObject;
		targetObject = target;
		Info = info;

		var fromArrayAttribute = Info.GetCustomAttribute<FromArrayAttribute>();
		if (fromArrayAttribute == null)
			return;
		else
			FindParentArray(fromArrayAttribute);
	}

	private void FindParentArray(FromArrayAttribute fromArrayAttribute)
	{
		var topLevelType = topLevelObject.GetType();
		var member = (MemberInfo?)topLevelType.GetProperty(fromArrayAttribute.ArrayName, BindingFlags.Public | BindingFlags.Instance)
				 ?? topLevelType.GetField(fromArrayAttribute.ArrayName, BindingFlags.Public | BindingFlags.Instance);

		if (member == null)
			return;

		object? arrayObject = member switch
		{
			PropertyInfo p => p.GetValue(topLevelObject),
			FieldInfo f => f.GetValue(topLevelObject),
			_ => null
		};

		if (arrayObject != null)
		{
			Type arrayType = arrayObject.GetType();
			if (arrayType.IsGenericType && arrayType.GetGenericTypeDefinition() == typeof(CollectionDescriptor<>))
			{
				var list = (IList)arrayType.GetProperty("Collection")!.GetValue(arrayObject)!;
				ParentArray = list;
			}
		}
	}
}
