using Serialization.Attributes;
using Serialization.Types;
using System.Collections;
using System.Reflection;

namespace Serialization;

public static class Validator
{
	public static string? ValidateRequiredProperties(Type type, object instance)
	{
		string? error = null;

		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(CollectionDescriptor<>))
		{
			Type elementType = type.GetGenericArguments()[0];
			var list = (IList)type.GetProperty("Collection")!.GetValue(instance)!;
			foreach (var element in list)
			{
				error = ValidateRequiredProperties(elementType, element);
				if (error != null)
					break;
			}
		}
		else
		{
			foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				var requiresValueAttribute = propertyInfo.GetCustomAttribute<RequiresValueAttribute>();
				if (requiresValueAttribute == null)
					continue;

				var value = propertyInfo.GetValue(instance);
				if (value == null)
				{
					error = requiresValueAttribute.ErrorMessage;
					break;
				}
			}
		}

		return error;
	}
}
