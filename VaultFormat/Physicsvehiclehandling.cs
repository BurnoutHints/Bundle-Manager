using System;
using System.Collections.Generic;
using System.Linq;
using BundleUtilities;

namespace VaultFormat;

public class Physicsvehiclehandling : IAttribute
{
    public AttributeHeader header { get; set; }

    public SizeAndPositionInformation info { get; set; }

    public RefSpec PhysicsVehicleSuspensionAttribs { get; set; }

    public RefSpec PhysicsVehicleSteeringAttribs { get; set; }

    public RefSpec PhysicsVehicleEngineAttribs { get; set; }

    public RefSpec PhysicsVehicleDriftAttribs { get; set; }

    public RefSpec PhysicsVehicleCollisionAttribs { get; set; }

    public RefSpec PhysicsVehicleBoostAttribs { get; set; }

    public RefSpec PhysicsVehicleBodyRollAttribs { get; set; }

    public RefSpec PhysicsVehicleBaseAttribs { get; set; }

    public Physicsvehiclehandling(SizeAndPositionInformation chunk, AttributeHeader dataChunk)
    {
        header = dataChunk;
        info = chunk;
    }

    public int GetDataSize()
    {
        List<byte[]> bytes =
        [
            PhysicsVehicleSuspensionAttribs.ToBytes(),
            PhysicsVehicleSteeringAttribs.ToBytes(),
            PhysicsVehicleEngineAttribs.ToBytes(),
            PhysicsVehicleDriftAttribs.ToBytes(),
            PhysicsVehicleCollisionAttribs.ToBytes(),
            PhysicsVehicleBoostAttribs.ToBytes(),
            PhysicsVehicleBodyRollAttribs.ToBytes(),
            PhysicsVehicleBaseAttribs.ToBytes()
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
        PhysicsVehicleSuspensionAttribs = new(loader, br);
        PhysicsVehicleSteeringAttribs = new(loader, br);
        PhysicsVehicleEngineAttribs = new(loader, br);
        PhysicsVehicleDriftAttribs = new(loader, br);
        PhysicsVehicleCollisionAttribs = new(loader, br);
        PhysicsVehicleBoostAttribs = new(loader, br);
        PhysicsVehicleBodyRollAttribs = new(loader, br);
        PhysicsVehicleBaseAttribs = new(loader, br);
    }

    public void Write(BinaryWriter2 bw)
    {
        PhysicsVehicleSuspensionAttribs.Write(bw);
        PhysicsVehicleSteeringAttribs.Write(bw);
        PhysicsVehicleEngineAttribs.Write(bw);
        PhysicsVehicleDriftAttribs.Write(bw);
        PhysicsVehicleCollisionAttribs.Write(bw);
        PhysicsVehicleBoostAttribs.Write(bw);
        PhysicsVehicleBodyRollAttribs.Write(bw);
        PhysicsVehicleBaseAttribs.Write(bw);
    }
}
