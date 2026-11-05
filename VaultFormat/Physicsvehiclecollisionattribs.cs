using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BundleFormat;
using BundleUtilities;

namespace VaultFormat;

public class Physicsvehiclecollisionattribs : IAttribute
{
    public AttributeHeader header { get; set; }

    public SizeAndPositionInformation info { get; set; }

    public Vector4 BodyBox { get; set; }

    public Physicsvehiclecollisionattribs(SizeAndPositionInformation chunk, AttributeHeader dataChunk)
    {
        header = dataChunk;
        info = chunk;
    }

    public int GetDataSize()
    {
        List<byte[]> bytes =
        [
            BodyBox.ToBytes()
        ];
        Console.WriteLine(bytes.SelectMany(i => i).Count());
        return CountingUtilities.AddPadding(bytes);
    }

    public AttributeHeader GetHeader()
    {
        return header;
    }

    public SizeAndPositionInformation GetInfo()
    {
        return info;
    }

    public void Read(ILoader loader, BinaryReader2 br)
    {
        BodyBox = new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
    }

    public void Write(BinaryWriter2 bw)
    {
        bw.Write(BodyBox.ToBytes(bw.BigEndian));
    }
}
