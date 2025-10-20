namespace Serialization.Interfaces;

public interface ICollectionDescriptor
{
	int Capacity { get; }
	bool VariableElementUsage { get; }
}
