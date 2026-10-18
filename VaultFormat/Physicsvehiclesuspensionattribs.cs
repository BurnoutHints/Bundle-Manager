using System;
using System.Collections.Generic;
using BundleUtilities;

namespace VaultFormat;

// To-Do: This is class is weird, because it's not divisible by 16 but has no padding...
public class Physicsvehiclesuspensionattribs : IAttribute
{
    public AttributeHeader header { get; set; }
    public SizeAndPositionInformation info { get; set; }

    public float UpwardMovement { get; set; }
    public float TimeToDampAfterLanding { get; set; }
    public float Strength { get; set; }
    public float SpringLength { get; set; }
    public float RearHeight { get; set; }
    public float MaxYawDampingOnLanding { get; set; }
    public float MaxVertVelocityDampingOnLanding { get; set; }
    public float MaxRollDampingOnLanding { get; set; }
    public float MaxPitchDampingOnLanding { get; set; }
    public float InAirDamping { get; set; }
    public float FrontHeight { get; set; }
    public float DownwardMovement { get; set; }
    public float Dampening { get; set; }

    public Physicsvehiclesuspensionattribs(SizeAndPositionInformation chunk, AttributeHeader dataChunk)
    {
        this.info = chunk;
        this.header = dataChunk;
    }
    // Hack, because of weird padding of suspension attribs
    public static byte[][] getDefaultBytes() {
        float value = 0;
        List<byte[]> bytes =
        [
            BitConverter.GetBytes(value), // UpwardMovement
            BitConverter.GetBytes(0), // TimeToDampAfterLanding
            BitConverter.GetBytes(0), // Strength
            BitConverter.GetBytes(0), // etc
            BitConverter.GetBytes(0),
            BitConverter.GetBytes(0),
            BitConverter.GetBytes(0),
            BitConverter.GetBytes(0),
            BitConverter.GetBytes(0),
            BitConverter.GetBytes(0),
            BitConverter.GetBytes(0),
            BitConverter.GetBytes(0),
            BitConverter.GetBytes(0)
        ];
        return [.. bytes];
    }

    public int getDataSize()
    {
        // Hack, because of weird padding of suspension attribs
        /**
        List<byte[]> bytes = new List<byte[]>();
        bytes.Add(BitConverter.GetBytes(UpwardMovement));
        bytes.Add(BitConverter.GetBytes(TimeToDampAfterLanding));
        bytes.Add(BitConverter.GetBytes(Strength));
        bytes.Add(BitConverter.GetBytes(SpringLength));
        bytes.Add(BitConverter.GetBytes(RearHeight));
        bytes.Add(BitConverter.GetBytes(MaxYawDampingOnLanding));
        bytes.Add(BitConverter.GetBytes(MaxVertVelocityDampingOnLanding));
        bytes.Add(BitConverter.GetBytes(MaxRollDampingOnLanding));
        bytes.Add(BitConverter.GetBytes(MaxPitchDampingOnLanding));
        bytes.Add(BitConverter.GetBytes(InAirDamping));
        bytes.Add(BitConverter.GetBytes(FrontHeight));
        bytes.Add(BitConverter.GetBytes(DownwardMovement));
        bytes.Add(BitConverter.GetBytes(Dampening));
        return bytes.SelectMany(i => i).Count();
        **/
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
        UpwardMovement = br.ReadSingle();
        TimeToDampAfterLanding = br.ReadSingle();
        Strength = br.ReadSingle();
        SpringLength = br.ReadSingle();
        RearHeight = br.ReadSingle();
        MaxYawDampingOnLanding = br.ReadSingle();
        MaxVertVelocityDampingOnLanding = br.ReadSingle();
        MaxRollDampingOnLanding = br.ReadSingle();
        MaxPitchDampingOnLanding = br.ReadSingle();
        InAirDamping = br.ReadSingle();
        FrontHeight = br.ReadSingle();
        DownwardMovement = br.ReadSingle();
        Dampening = br.ReadSingle();
    }

    public void Write(BinaryWriter2 bw)
    {
        bw.Write(UpwardMovement);
        bw.Write(TimeToDampAfterLanding);
        bw.Write(Strength);
        bw.Write(SpringLength);
        bw.Write(RearHeight);
        bw.Write(MaxYawDampingOnLanding);
        bw.Write(MaxVertVelocityDampingOnLanding);
        bw.Write(MaxRollDampingOnLanding);
        bw.Write(MaxPitchDampingOnLanding);
        bw.Write(InAirDamping);
        bw.Write(FrontHeight);
        bw.Write(DownwardMovement);
        bw.Write(Dampening);
    }
}
