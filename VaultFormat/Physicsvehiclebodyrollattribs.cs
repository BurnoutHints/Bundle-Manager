using System;
using System.Collections.Generic;
using System.Linq;
using BundleUtilities;

namespace VaultFormat;

public class Physicsvehiclebodyrollattribs : IAttribute
{
    public AttributeHeader header { get; set; }
    public SizeAndPositionInformation info { get; set; }

    public float WheelLongForceHeightOffset { get; set; }
    public float WheelLatForceHeightOffset { get; set; }
    public float WeightTransferDecayZ { get; set; }
    public float WeightTransferDecayX { get; set; }
    public float RollSpringStiffness { get; set; }
    public float RollSpringDampening { get; set; }
    public float PitchSpringStiffness { get; set; }
    public float PitchSpringDampening { get; set; }
    public float FactorOfWeightZ { get; set; }
    public float FactorOfWeightX { get; set; }
    public Physicsvehiclebodyrollattribs(SizeAndPositionInformation chunk, AttributeHeader dataChunk)
    {
        header = dataChunk;
        info = chunk;
    }

    public int getDataSize()
    {
        List<byte[]> bytes =
        [
            BitConverter.GetBytes(WheelLongForceHeightOffset),
            BitConverter.GetBytes(WheelLatForceHeightOffset),
            BitConverter.GetBytes(WeightTransferDecayZ),
            BitConverter.GetBytes(WeightTransferDecayX),
            BitConverter.GetBytes(RollSpringStiffness),
            BitConverter.GetBytes(RollSpringDampening),
            BitConverter.GetBytes(PitchSpringStiffness),
            BitConverter.GetBytes(PitchSpringDampening),
            BitConverter.GetBytes(FactorOfWeightZ),
            BitConverter.GetBytes(FactorOfWeightX),
            //4 bytes padding is Wrong!
            BitConverter.GetBytes((float)0)
        ];
        return bytes.SelectMany(i => i).Count();
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
        WheelLongForceHeightOffset = br.ReadSingle();
        WheelLatForceHeightOffset = br.ReadSingle();
        WeightTransferDecayZ = br.ReadSingle();
        WeightTransferDecayX = br.ReadSingle();
        RollSpringStiffness = br.ReadSingle();
        RollSpringDampening = br.ReadSingle();
        PitchSpringStiffness = br.ReadSingle();
        PitchSpringDampening = br.ReadSingle();
        FactorOfWeightZ = br.ReadSingle();
        FactorOfWeightX = br.ReadSingle();
        br.SkipUniquePadding(4);
    }

    public void Write(BinaryWriter2 bw)
    {
        bw.Write(WheelLongForceHeightOffset);
        bw.Write(WheelLatForceHeightOffset);
        bw.Write(WeightTransferDecayZ);
        bw.Write(WeightTransferDecayX);
        bw.Write(RollSpringStiffness);
        bw.Write(RollSpringDampening);
        bw.Write(PitchSpringStiffness);
        bw.Write(PitchSpringDampening);
        bw.Write(FactorOfWeightZ);
        bw.Write(FactorOfWeightX);
        bw.WriteUniquePadding(4);
    }
}
