using System;
using System.Collections.Generic;
using System.Linq;
using BundleUtilities;

namespace VaultFormat;

//To-Do: Weird, because one RefSpec equals 0x18 aka 24 and is not divisible by 16
public class RefSpec
{
    public ulong ClassKey;
    public ulong CollectionKey;
    public uint CollectionPtr;

    public RefSpec(ILoader loader, BinaryReader2 br)
    {
        ClassKey = br.ReadUInt64();
        CollectionKey = br.ReadUInt64();
        CollectionPtr = br.ReadUInt32();
        br.SkipUniquePadding(4);
    }

    public byte[] ToBytes()
    {
        List<byte[]> bytes =
        [
            BitConverter.GetBytes(ClassKey),
            BitConverter.GetBytes(CollectionKey),
            BitConverter.GetBytes(CollectionPtr)
        ];
        return AddPadding(bytes);
    }

    private byte[] AddPadding(List<byte[]> bytes)
    {
        List<byte> padding =
        [
            0,
            0,
            0,
            0
        ];
        bytes.Add([.. padding]);
        return [.. bytes.SelectMany(i => i)];
    }
    public void Write(BinaryWriter2 bw)
    {
        bw.Write(ClassKey);
        bw.Write(CollectionKey);
        bw.Write(CollectionPtr);
        bw.WriteUniquePadding(4);
    }


}
