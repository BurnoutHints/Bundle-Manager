using BundleUtilities;

namespace VaultFormat;

public interface IAttribute
{
    AttributeHeader getHeader();

    SizeAndPositionInformation getInfo();

    int getDataSize();

    void Read(ILoader loader, BinaryReader2 br);

    void Write(BinaryWriter2 bw);
}
