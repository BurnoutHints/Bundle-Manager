using System;
using System.Collections.Generic;
using System.Numerics;
using BundleFormat;
using BundleUtilities;

namespace VaultFormat;

public class Physicsvehicleengineattribs : IAttribute
{
    public AttributeHeader header { get; set; }
    public SizeAndPositionInformation info { get; set; }

    public Vector4 TorqueScales2 { get; set; }
    public Vector4 TorqueScales1 { get; set; }
    public Vector4 GearUpRPMs2 { get; set; }
    public Vector4 GearUpRPMs1 { get; set; }
    public Vector4 GearRatios2 { get; set; }
    public Vector4 GearRatios1 { get; set; }
    public float TransmissionEfficiency { get; set; }
    public float TorqueFallOffRPM { get; set; }
    public float MaxTorque { get; set; }
    public float MaxRPM { get; set; }
    public float LSDMGearUpSpeed { get; set; }
    public float GearChangeTime { get; set; }
    public float FlyWheelInertia { get; set; }
    public float FlyWheelFriction { get; set; }
    public float EngineResistance { get; set; }
    public float EngineLowEndTorqueFactor { get; set; }
    public float EngineBraking { get; set; }
    public float Differential { get; set; }

    public Physicsvehicleengineattribs(SizeAndPositionInformation chunk, AttributeHeader dataChunk)
    {
        header = dataChunk;
        info = chunk;
    }

    public int getDataSize()
    {
        List<byte[]> bytes =
        [
            TorqueScales2.toBytes(),
            TorqueScales1.toBytes(),
            GearUpRPMs2.toBytes(),
            GearUpRPMs1.toBytes(),
            GearRatios2.toBytes(),
            GearRatios1.toBytes(),
            BitConverter.GetBytes(TransmissionEfficiency),
            BitConverter.GetBytes(TorqueFallOffRPM),
            BitConverter.GetBytes(MaxTorque),
            BitConverter.GetBytes(MaxRPM),
            BitConverter.GetBytes(LSDMGearUpSpeed),
            BitConverter.GetBytes(GearChangeTime),
            BitConverter.GetBytes(FlyWheelInertia),
            BitConverter.GetBytes(FlyWheelFriction),
            BitConverter.GetBytes(EngineResistance),
            BitConverter.GetBytes(EngineLowEndTorqueFactor),
            BitConverter.GetBytes(EngineBraking),
            BitConverter.GetBytes(Differential),
        ];
        return CountingUtilities.AddPadding(bytes);
    }

    public void Write(BinaryWriter2 bw)
    {
        bw.Write(TorqueScales2.toBytes(bw.BigEndian));
        bw.Write(TorqueScales1.toBytes(bw.BigEndian));
        bw.Write(GearUpRPMs2.toBytes(bw.BigEndian));
        bw.Write(GearUpRPMs1.toBytes(bw.BigEndian));
        bw.Write(GearRatios2.toBytes(bw.BigEndian));
        bw.Write(GearRatios1.toBytes(bw.BigEndian));
        bw.Write(TransmissionEfficiency);
        bw.Write(TorqueFallOffRPM);
        bw.Write(MaxTorque);
        bw.Write(MaxRPM);
        bw.Write(LSDMGearUpSpeed);
        bw.Write(GearChangeTime);
        bw.Write(FlyWheelInertia);
        bw.Write(FlyWheelFriction);
        bw.Write(EngineResistance);
        bw.Write(EngineLowEndTorqueFactor);
        bw.Write(EngineBraking);
        bw.Write(Differential);
    }

    public void Read(ILoader loader, BinaryReader2 br)
    {
        TorqueScales2 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        TorqueScales1 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        GearUpRPMs2 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        GearUpRPMs1 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        GearRatios2 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        GearRatios1 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        TransmissionEfficiency = br.ReadSingle();
        TorqueFallOffRPM = br.ReadSingle();
        MaxTorque = br.ReadSingle();
        MaxRPM = br.ReadSingle();
        LSDMGearUpSpeed = br.ReadSingle();
        GearChangeTime = br.ReadSingle();
        FlyWheelInertia = br.ReadSingle();
        FlyWheelFriction = br.ReadSingle();
        EngineResistance = br.ReadSingle();
        EngineLowEndTorqueFactor = br.ReadSingle();
        EngineBraking = br.ReadSingle();
        Differential = br.ReadSingle();
    }

    public AttributeHeader getHeader()
    {
        return header;
    }

    public SizeAndPositionInformation getInfo()
    {
        return info;
    }

}
