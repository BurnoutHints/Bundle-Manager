using System.Buffers.Binary;
using BundleFormat;

internal static partial class Program
{
    private static byte[] Pattern(int length, byte seed) => [.. Enumerable.Range(0, length).Select(i => (byte)(seed + i * 7))];

    // Creates test BND1 archive with three resources (memory type 1 imports and secondary data)
    private static byte[] Bnd1Bytes(bool compressed, byte[] a, byte[] b, byte[] c, byte[] secondary, byte[] unread)
    {
        byte[] stored(byte[] data) => compressed
            ? data.Compress()
            : data;

        const int count = 3, table = 0x70 + count * 8, sizes = table + count * 0x70;
        int imports = sizes + (compressed
            ? count * 0x28
            : 0), start = (imports + 0x20 + 15) & ~15, end = start;
        byte[][] primary = [stored(a), stored(b), stored(c)];
        byte[] storedSecondary = stored(secondary);
        int[] at = new int[count];

        for (int i = 0; i < count; i++)
        {
            at[i] = end;
            end += primary[i].Length;

            if (i < count - 1)
                end = (end + 15) & ~15;
        }

        byte[] file = new byte[end + unread.Length + storedSecondary.Length];

        void u(int offset, uint value) => BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(offset), value);

        "bndl"u8.CopyTo(file);
        u(0x04, 5);
        u(0x08, count);
        u(0x0C, (uint)end);
        u(0x10, 16);
        u(0x14, (uint)unread.Length);
        u(0x18, 1);
        u(0x1C, (uint)storedSecondary.Length);
        u(0x20, 16);
        u(0x28, 1);
        u(0x30, 1);
        u(0x48, 0x70);
        u(0x4C, table);
        u(0x50, (uint)imports);
        u(0x54, (uint)start);
        u(0x58, 2);
        u(0x5C, compressed
            ? 1u
            : 0);
        u(0x60, compressed
            ? count
            : 0u);
        u(0x64, compressed
            ? sizes
            : 0u);

        for (int i = 0; i < count; i++)
        {
            int record = table + i * 0x70;
            BinaryPrimitives.WriteUInt64BigEndian(file.AsSpan(0x70 + i * 8), 0x1111UL * (ulong)(i + 1));
            u(record + 4, i == 1
                ? (uint)imports
                : 0);
            u(record + 8, (uint)EntryType.AISections);
            u(record + 0x0C, (uint)primary[i].Length);
            u(record + 0x10, compressed
                ? 1u
                : 16);
            u(record + 0x34, (uint)at[i]);

            for (int j = 0; j < 5; j++)
                u(record + 0x38 + j * 8, 1);
        }

        u(table + 0x70 + 0x14, (uint)unread.Length);
        u(table + 0xE0 + 0x1C, (uint)storedSecondary.Length);

        if (compressed)
        {
            u(sizes, (uint)a.Length);
            u(sizes + 0x28, (uint)b.Length);
            u(sizes + 0x50, (uint)c.Length);
            u(sizes + 0x50 + 0x10, (uint)secondary.Length);
        }

        u(imports, 1);
        BinaryPrimitives.WriteUInt64BigEndian(file.AsSpan(imports + 8), 0xABCD);
        u(imports + 0x10, 0x20);
        u(imports + 0x14, 7);
        primary[0].CopyTo(file, at[0]);
        primary[1].CopyTo(file, at[1]);
        primary[2].CopyTo(file, at[2]);
        unread.CopyTo(file, end);
        storedSecondary.CopyTo(file, end + unread.Length);
        return file;
    }

    private static void CheckBnd1Saving()
    {
        foreach (bool compressed in (bool[])[true, false])
        {
            string mode = compressed
                ? "compressed"
                : "uncompressed", path = Path.Combine(output, $"bnd1-{mode}.bndl");
            byte[] a = Pattern(300, 1), b = Pattern(500, 2), c = Pattern(90, 3), secondary = Pattern(700, 4), unread = Pattern(32, 5), bigger = Pattern(9000, 6);
            byte[] original = Bnd1Bytes(compressed, a, b, c, secondary, unread);
            File.WriteAllBytes(path, original);

            BundleArchive archive = BundleArchive.Open(path);
            archive.Write(path);
            Equal(File.ReadAllBytes(path), original, $"An unchanged {mode} BND1 bundle saves byte for byte");

            archive.Entries[0].EntryBlocks[0].Data = bigger;
            archive.Write(path);
            byte[] saved = File.ReadAllBytes(path);

            uint u(byte[] file, int at) => BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(at));

            BundleArchive grown = BundleArchive.Open(path);
            Equal(grown.Entries[0].EntryBlocks[0].Data, bigger, $"A changed block of a {mode} BND1 bundle is saved");
            Equal(grown.Entries[1].EntryBlocks[0].Data, b, $"The resource after a block that grew is intact in a {mode} BND1 bundle");
            Equal(grown.Entries[2].EntryBlocks[0].Data, c, $"The last primary block survives in a {mode} BND1 bundle");
            Equal(grown.Entries[2].EntryBlocks[1].Data, secondary, $"Secondary data follows the move in a {mode} BND1 bundle");
            Equal(saved[(int)u(saved, 0x0C)..((int)u(saved, 0x0C) + 32)], unread, $"Memory types Bundle Manager doesn't read survive in a {mode} BND1 bundle");
            Check(u(saved, 0x0C) + u(saved, 0x14) + u(saved, 0x1C) == saved.Length, $"The block sizes in a {mode} BND1 header cover the file");
            Check(u(saved, 0x88 + 0x70 + 0x34) % 16 == 0, $"Blocks stay aligned after a move in a {mode} BND1 bundle");
            Check(grown.Entries[1].Dependencies is [{ ID: 0xABCD, EntryPointerOffset: 0x20 }], $"Imports survive in a {mode} BND1 bundle");

            grown.Entries[0].EntryBlocks[0].Data = c;
            grown.Entries[1].Dependencies[0] = new()
            {
                ID = 0xBEEF,
                EntryPointerOffset = 0x40
            };
            grown.Write(path);
            BundleArchive shrunk = BundleArchive.Open(path);
            Equal(shrunk.Entries[0].EntryBlocks[0].Data, c, $"A block can shrink in a {mode} BND1 bundle");
            Equal(shrunk.Entries[2].EntryBlocks[1].Data, secondary, $"Secondary data survives a shrink in a {mode} BND1 bundle");
            Check(shrunk.Entries[1].Dependencies is [{ ID: 0xBEEF, EntryPointerOffset: 0x40 }], $"A changed import is saved in a {mode} BND1 bundle");

            shrunk.Entries.Add(new(shrunk)
            {
                ID = 9
            });
            bool refused = false;

            try
            {
                shrunk.Write(path);
            }
            catch (NotSupportedException)
            {
                refused = true;
            }

            Check(refused, $"Adding a resource to a {mode} BND1 bundle is refused rather than lost");
        }
    }
}
