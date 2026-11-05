using BundleUtilities;

namespace VaultFormat;

public interface IAttribute
{
    AttributeHeader GetHeader();

    SizeAndPositionInformation GetInfo();

    int GetDataSize();

    void Read(ILoader loader, BinaryReader2 br);

    void Write(BinaryWriter2 bw);
}
