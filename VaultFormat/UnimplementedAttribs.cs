using BundleUtilities;

namespace VaultFormat;

public class UnimplementedAttribs : IAttribute
{
    public AttributeHeader Header { get; set; }

    public SizeAndPositionInformation Info { get; set; }

    public byte[] Data { get; set; }

    public UnimplementedAttribs(SizeAndPositionInformation chunk, AttributeHeader dataChunk)
    {
        Info = chunk;
        Header = dataChunk;
    }

    public int GetDataSize()
    {
        return 0;
    }

    public AttributeHeader GetHeader()
    {
        return Header;
    }

    public SizeAndPositionInformation GetInfo()
    {
        return Info;
    }

    public void Read(ILoader loader, BinaryReader2 br)
    {

    }

    public void Write(BinaryWriter2 bw)
    {

    }
}
