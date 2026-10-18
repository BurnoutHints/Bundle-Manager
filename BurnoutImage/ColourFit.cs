using BCnEncoder.Shared;

namespace BurnoutImage;

internal abstract class ColourFit
{
    protected ColourSet Colours;
    protected CompressionFormat Compression;

    public ColourFit(ColourSet colours, CompressionFormat compression)
    {
        Colours = colours;
        Compression = compression;
    }

    public void Compress(byte[] block, int offset)
    {
        bool isDxt1 = Compression == CompressionFormat.Bc1;
        if (isDxt1)
        {
            Compress3(block, offset);
            if (!Colours.Transparent)
                Compress4(block, offset);
        }
        else
        {
            Compress4(block, offset);
        }
    }

    protected abstract void Compress3(byte[] block, int offset);
    protected abstract void Compress4(byte[] block, int offset);
}
