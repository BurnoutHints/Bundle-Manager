using System.Collections;
using System.Reflection;

namespace Serialization;

public class ReferenceFinder
{
	public static List<string> FindReferences(object root, object target)
	{
		var results = new List<string>();
		var visited = new HashSet<object>();
		Recurse(root, "root", visited, target, results);
		return results;
	}

	private static void Recurse(object? current, string path, HashSet<object> visited, object target, List<string> results)
	{
		if (current == null)
			return;
		if (ReferenceEquals(current, target))
		{
			results.Add(path);
			return;
		}

		// Do not check primitives, strings, and enums
		var type = current.GetType();
		if (type.IsPrimitive || current is string || type.IsEnum)
			return;

		// Do not cover already-covered objects
		if (!visited.Add(current))
			return;

		// If list, iterate over elements
		if (current is IList list)
		{
			for (int i = 0; i < list.Count; ++i)
			{
				var item = list[i];
				Recurse(item, $"{path}[{i}]", visited, target, results);
			}
			return;
		}

		// Handle CollectionDescriptor<T>
		var CollectionDescriptor = type.GetProperty("Collection", BindingFlags.Public | BindingFlags.Instance);
		if (CollectionDescriptor != null)
		{
			var CollectionDescriptorList = (IList)CollectionDescriptor.GetValue(current)!;
			for (int i = 0; i < CollectionDescriptorList.Count; ++i)
				Recurse(CollectionDescriptorList[i], AppendPath(path, $"List[{i}]"), visited, target, results);
			return;
		}

		// Otherwise, read properties
		var propertiesInfo = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		foreach (var propertyInfo in propertiesInfo)
		{
			if (propertyInfo.GetIndexParameters().Length > 0)
				continue;
			object? propertyValue = null;
			try
			{
				propertyValue = propertyInfo.GetValue(current);
			}
			catch
			{
				continue;
			}
			Recurse(propertyValue, AppendPath(path, propertyInfo.Name), visited, target, results);
		}
	}

	private static string AppendPath(string basePath, string next)
	{
		return string.IsNullOrEmpty(basePath) ? next : $"{basePath}.{next}";
	}
}
