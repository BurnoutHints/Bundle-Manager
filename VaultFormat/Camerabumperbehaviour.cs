using System;
using System.Collections.Generic;
using System.Linq;
using BundleUtilities;

namespace VaultFormat;

public class Camerabumperbehaviour : IAttribute
{
    public AttributeHeader header { get; set; }

    public SizeAndPositionInformation info { get; set; }

    public float ZOffset { get; set; }

    public float YOffset { get; set; }

    public float YawSpring { get; set; }

    public float RollSpring { get; set; }

    public float PitchSpring { get; set; }

    public float FieldOfView { get; set; }

    public float BoostFieldOfView { get; set; }

    public float BodyRollScale { get; set; }

    public float BodyPitchScale { get; set; }

    public float AccelerationResponse { get; set; }

    public float AccelerationDampening { get; set; }

    public Camerabumperbehaviour(SizeAndPositionInformation chunk, AttributeHeader dataChunk)
    {
        header = dataChunk;
        info = chunk;
    }

    public int GetDataSize()
    {
        List<byte[]> bytes =
        [
            BitConverter.GetBytes(ZOffset),
            BitConverter.GetBytes(YOffset),
            BitConverter.GetBytes(YawSpring),
            BitConverter.GetBytes(RollSpring),
            BitConverter.GetBytes(PitchSpring),
            BitConverter.GetBytes(FieldOfView),
            BitConverter.GetBytes(BoostFieldOfView),
            BitConverter.GetBytes(BodyRollScale),
            BitConverter.GetBytes(BodyPitchScale),
            BitConverter.GetBytes(AccelerationResponse),
            BitConverter.GetBytes(AccelerationDampening)
        ];

        return bytes.SelectMany(i => i).Count();
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
        ZOffset = br.ReadSingle();
        YOffset = br.ReadSingle();
        YawSpring = br.ReadSingle();
        RollSpring = br.ReadSingle();
        PitchSpring = br.ReadSingle();
        FieldOfView = br.ReadSingle();
        BoostFieldOfView = br.ReadSingle();
        BodyRollScale = br.ReadSingle();
        BodyPitchScale = br.ReadSingle();
        AccelerationResponse = br.ReadSingle();
        AccelerationDampening = br.ReadSingle();
    }

    public void Write(BinaryWriter2 bw)
    {
        bw.Write(ZOffset);
        bw.Write(YOffset);
        bw.Write(YawSpring);
        bw.Write(RollSpring);
        bw.Write(PitchSpring);
        bw.Write(FieldOfView);
        bw.Write(BoostFieldOfView);
        bw.Write(BodyRollScale);
        bw.Write(BodyPitchScale);
        bw.Write(AccelerationResponse);
        bw.Write(AccelerationDampening);
    }
}
