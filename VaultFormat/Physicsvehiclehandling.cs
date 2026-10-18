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

    public int getDataSize()
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
        PhysicsVehicleSuspensionAttribs = new RefSpec(loader, br);
        PhysicsVehicleSteeringAttribs = new RefSpec(loader, br);
        PhysicsVehicleEngineAttribs = new RefSpec(loader, br);
        PhysicsVehicleDriftAttribs = new RefSpec(loader, br);
        PhysicsVehicleCollisionAttribs = new RefSpec(loader, br);
        PhysicsVehicleBoostAttribs = new RefSpec(loader, br);
        PhysicsVehicleBodyRollAttribs = new RefSpec(loader, br);
        PhysicsVehicleBaseAttribs = new RefSpec(loader, br);
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
