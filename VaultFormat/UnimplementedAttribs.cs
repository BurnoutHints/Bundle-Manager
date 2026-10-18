using BundleUtilities;

namespace VaultFormat;

public class UnimplementedAttribs : IAttribute
{
    public AttributeHeader header { get; set; }
    public SizeAndPositionInformation info { get; set; }
    public byte[] data { get; set; }

    public UnimplementedAttribs(SizeAndPositionInformation chunk, AttributeHeader dataChunk)
    {
        info = chunk;
        header = dataChunk;
    }

    public int getDataSize()
    {
        return 0;
    }

    public AttributeHeader getHeader()
    {
        return header;
    }

    public SizeAndPositionInformation getInfo()
    {
        return info;
    }

    public void Read(ILoader loader, BinaryReader2 br)
    {

    }

    public void Write(BinaryWriter2 bw)
    {

    }
}
