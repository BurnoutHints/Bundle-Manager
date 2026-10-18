using System;
using System.Collections.Generic;
using System.Linq;
using BundleUtilities;

namespace VaultFormat;

public class Burnoutcarasset : IAttribute
{
    public RefSpec[] Offences { get; set; } //RefSpec[12]
    public RefSpec SoundExhaustAsset { get; set; }
    public RefSpec SoundEngineAsset { get; set; }
    public RefSpec PhysicsVehicleHandlingAsset { get; set; }
    public RefSpec GraphicsAsset { get; set; }
    public RefSpec CarUnlockShot { get; set; }
    public RefSpec CameraExternalBehaviourAsset { get; set; }
    public RefSpec CameraBumperBehaviourAsset { get; set; }
    public byte[] VehicleID;//See StrE	
    public long PhysicsAsset { get; set; }
    public long MasterSceneMayaBinaryFile { get; set; }
    public byte[] InGameName;// See StrE
    public long GameplayAsset { get; set; }
    public byte[] ExhaustName;//See StrE
    public long ExhaustEntityKey { get; set; }
    public byte[] EngineName; //See StrE
    public long EngineEntityKey { get; set; }
    public long DefaultWheel { get; set; }
    public bool BuildThisVehicle { get; set; }

    public AttributeHeader header { get; set; }
    public SizeAndPositionInformation info { get; set; }

    public Burnoutcarasset(SizeAndPositionInformation chunk, AttributeHeader dataChunk)
    {
        header = dataChunk;
        info = chunk;
    }

    public int getDataSize()
    {
        List<byte[]> bytes = [];
        foreach (RefSpec r in Offences)
        {
            bytes.Add(r.ToBytes()); //RefSpec[12]
        } // 288
        bytes.Add(SoundExhaustAsset.ToBytes());
        bytes.Add(SoundEngineAsset.ToBytes());
        bytes.Add(PhysicsVehicleHandlingAsset.ToBytes());
        bytes.Add(GraphicsAsset.ToBytes());
        bytes.Add(CarUnlockShot.ToBytes());
        bytes.Add(CameraExternalBehaviourAsset.ToBytes());
        bytes.Add(CameraBumperBehaviourAsset.ToBytes()); // 168 + 80 + 4
        bytes.Add(VehicleID);
        bytes.Add(BitConverter.GetBytes(PhysicsAsset));
        bytes.Add(BitConverter.GetBytes(MasterSceneMayaBinaryFile));
        bytes.Add(InGameName);
        bytes.Add(BitConverter.GetBytes(GameplayAsset));
        bytes.Add(ExhaustName);
        bytes.Add(BitConverter.GetBytes(ExhaustEntityKey));
        bytes.Add(EngineName);
        bytes.Add(BitConverter.GetBytes(EngineEntityKey));
        bytes.Add(BitConverter.GetBytes(DefaultWheel));
        bytes.Add(BitConverter.GetBytes(BuildThisVehicle));
        // To-Do: The program want 3 bytes for some reason
        Console.WriteLine("Before " + bytes.SelectMany(i => i).Count());
        int newValue = AddPadding(bytes).Count();
        Console.WriteLine("After " + newValue);
        // Should be 540 is 514 -> probably the text tbh
        return newValue;
    }

    private byte[] AddPadding(List<byte[]> bytes)
    {
        List<byte> padding =
        [
            0,
            0,
            0
        ];
        bytes.Add([.. padding]);
        return [.. bytes.SelectMany(i => i)];
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
        Offences = new RefSpec[12];
        for (int i = 0; i < 12; i++)
            Offences[i] = new RefSpec(loader, br);
        SoundExhaustAsset = new RefSpec(loader, br);
        SoundEngineAsset = new RefSpec(loader, br);
        PhysicsVehicleHandlingAsset = new RefSpec(loader, br);
        GraphicsAsset = new RefSpec(loader, br);
        CarUnlockShot = new RefSpec(loader, br);
        CameraExternalBehaviourAsset = new RefSpec(loader, br);
        CameraBumperBehaviourAsset = new RefSpec(loader, br);
        VehicleID = br.ReadBytes(8);
        PhysicsAsset = br.ReadInt64();
        MasterSceneMayaBinaryFile = br.ReadInt64();
        InGameName = br.ReadBytes(8);
        GameplayAsset = br.ReadInt64();
        ExhaustName = br.ReadBytes(8);
        ExhaustEntityKey = br.ReadInt64();
        EngineName = br.ReadBytes(8);
        EngineEntityKey = br.ReadInt64();
        DefaultWheel = br.ReadInt64();
        BuildThisVehicle = br.ReadBoolean();
        br.SkipUniquePadding(3);
    }

    public void Write(BinaryWriter2 bw)
    {
        foreach (RefSpec r in Offences)
            r.Write(bw);
        SoundExhaustAsset.Write(bw);
        SoundEngineAsset.Write(bw);
        PhysicsVehicleHandlingAsset.Write(bw);
        GraphicsAsset.Write(bw);
        CarUnlockShot.Write(bw);
        CameraExternalBehaviourAsset.Write(bw);
        CameraBumperBehaviourAsset.Write(bw);
        bw.Write(VehicleID);
        bw.Write(PhysicsAsset);
        bw.Write(MasterSceneMayaBinaryFile);
        bw.Write(InGameName);
        bw.Write(GameplayAsset);
        bw.Write(ExhaustName);
        bw.Write(ExhaustEntityKey);
        bw.Write(EngineName);
        bw.Write(EngineEntityKey);
        bw.Write(DefaultWheel);
        bw.Write(BuildThisVehicle);
        bw.WriteUniquePadding(3);
    }
}
