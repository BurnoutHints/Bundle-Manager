using Information.Enums;
using System.Collections.Generic;

namespace BundleManager.Registry;

internal class ResourceTypeViewRegistry
{
	public static Dictionary<ResourceType, CustomView[]> Registry { get; } = new()
	{

	};

	public static Dictionary<ResourceTypeV2, CustomView[]> RegistryV2 { get; } = new()
	{

	};
}
