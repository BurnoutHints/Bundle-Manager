namespace BundleFormat.BundleV1;

internal class BundleV5ExtendedData : BundleV4ExtendedData
{
	internal int mainMemAlignment;
	internal int graphicsMemAlignment;

	public BundleV5ExtendedData()
	{

	}

	public BundleV5ExtendedData(BundleBase original)
	{
		magicNumber = original.magicNumber;
		version = original.version;
		numResources = original.numResources;
		bundleResourceDescriptor = original.bundleResourceDescriptor;
		allocatedResource = original.allocatedResource;
		hashTableOffset = original.hashTableOffset;
		resourceEntriesOffset = original.resourceEntriesOffset;
		importTablesOffset = original.importTablesOffset;
		resourceDataOffset = original.resourceDataOffset;
		platform = original.platform;
	}

	public BundleV5ExtendedData(BundleV4ExtendedData original)
	{
		magicNumber = original.magicNumber;
		version = original.version;
		numResources = original.numResources;
		bundleResourceDescriptor = original.bundleResourceDescriptor;
		allocatedResource = original.allocatedResource;
		hashTableOffset = original.hashTableOffset;
		resourceEntriesOffset = original.resourceEntriesOffset;
		importTablesOffset = original.importTablesOffset;
		resourceDataOffset = original.resourceDataOffset;
		platform = original.platform;
		flags = original.flags;
		uncompressedDescriptorCount = original.uncompressedDescriptorCount;
		uncompressedResourceDescriptors = original.uncompressedResourceDescriptors;
	}
}
