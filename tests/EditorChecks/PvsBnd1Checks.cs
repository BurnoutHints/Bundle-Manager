using System.Buffers.Binary;
using BundleFormat;
using BundleUtilities;
using PVSFormat;

internal static partial class Program
{
    private static void CheckPVSNeighbourPruning()
    {
        static Zone zone(ulong id, params ulong[] neighbours)
        {
            Zone result = new()
            {
                ZoneId = id
            };

            foreach (ulong neighbour in neighbours)
                result.UnsafeNeighbours.Add(new()
                {
                    ZoneId = neighbour,
                    Flags = NeighbourFlags.E_NEIGHBOURFLAG_RENDER
                });

            result.NumUnsafeNeighbours = (short)neighbours.Length;
            return result;
        }

        PVS pvs = new()
        {
            data = new()
        };

        // Zones 2 and 3 no longer exist, zone 1 lists them back to back
        pvs.data.Zones.AddRange([zone(1, 2, 3, 4), zone(4, 1)]);
        pvs.data.TotalZones = 2;
        BundleEntry entry = Entry(new byte[16]);
        Check(pvs.Write(entry), "A zone list with deleted neighbours saves");
        PVS reread = new();
        Check(reread.Read(entry), "The saved zone list reads back");
        Check(reread.data.Zones[0].UnsafeNeighbours.Select(n => n.ZoneId).SequenceEqual([4ul])
            && reread.data.Zones[1].UnsafeNeighbours.Select(n => n.ZoneId).SequenceEqual([1ul]),
            "Deleting two zones another zone lists back to back drops both links and keeps the rest");
    }

    // Bundle version 1 states its platform in a word whose position depends on the
    // pool count, anything but X360 layout this reader knows is refused
    private static void CheckBnd1PlatformRefusal()
    {
        byte[] bytes = Bnd1Bytes(false, Pattern(30, 1), Pattern(40, 2), Pattern(20, 3), Pattern(50, 4), []);
        string path = Path.Combine(output, "bnd1-other-platform.bndl");
        File.WriteAllBytes(path, bytes);
        Check(BundleArchive.Open(path).Entries.Count == 3, "An Xbox 360 version 1 bundle still opens");
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0x58), 3);
        File.WriteAllBytes(path, bytes);
        Check(Throws<ReadFailedError>(() => BundleArchive.Open(path)),
            "A version 1 bundle for another platform is refused rather than read with the Xbox 360 layout");
    }
}
