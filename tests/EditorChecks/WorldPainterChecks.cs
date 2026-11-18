using BaseHandlers;
using BundleFormat;

internal static partial class Program
{
    // DISTRICTS.DAT and SOUND/REGIONS/AMBIENCES.DAT from each --painter-dir= install
    private static void CheckWorldPainterFiles(string[] args)
    {
        foreach (string root in args.Where(a => a.StartsWith("--painter-dir=")).Select(a => a["--painter-dir=".Length..]))
            foreach (string path in new[]
            {
                "DISTRICTS.DAT",
                "SOUND/REGIONS/AMBIENCES.DAT"
            }.Select(p => Path.Combine(root, p)).Where(File.Exists))
                foreach (BundleEntry entry in BundleArchive.Read(path).Entries.Where(e => e.Type == EntryType.WorldPainter2D))
                {
                    string label = $"{path} {entry.ID:X8}";
                    byte[] before = entry.GetPrimaryWithoutImports();
                    WorldPainter2D map = new();
                    map.Read(entry);
                    Check(map.Width > 0 && map.Height > 0 && map.Name is "Districts" or "Ambiences", $"{label}: reads a {map.Width}x{map.Height} {map.Name} grid");
                    Check(Enumerable.Range(0, map.Width * map.Height).All(i => map[i % map.Width, i / map.Width] is var v && (v == WorldPainter2D.Outside || map.Label(v).Contains(':'))),
                        $"{label}: every value in the grid has a name");
                    Console.WriteLine("  " + string.Join(", ", Enumerable.Range(0, map.Width * map.Height).GroupBy(i => map[i % map.Width, i / map.Width])
                        .OrderBy(g => g.Key).Select(g => $"{map.Label(g.Key)} ×{g.Count()}")));

                    // Each region sits where the game reads it: (nearly) every AI section is in some district
                    if (map.Name == "Districts" && File.Exists(Path.Combine(root, "AI.DAT")))
                    {
                        AISections ai = new();

                        try
                        {
                            ai.Read(BundleArchive.Read(Path.Combine(root, "AI.DAT")).Entries.First(e => e.Type == EntryType.AISections));
                        }
                        catch (BundleUtilities.ReadFailedError)
                        {
                            // The 64-bit AI layout (Switch, Xbox One) isn't readable yet
                            Console.WriteLine($"  {label}: skipped the AI section check, the AI data isn't readable");
                        }

                        // A newer AI.DAT can reach past an older map (the decomp build has island roads but 1.0's districts)
                        List<(float X, float Z)> centres = ai.maSections.Select(s => (X: s.maCorners.Average(c => c.X), Z: s.maCorners.Average(c => c.Y)))
                            .Where(c => map.Regions.Any(r => r.World.Contains(c.X, c.Z))).ToList();
                        int outside = centres.Count(c => map.Cell(c.X, c.Z) is not Point cell || map[cell.X, cell.Y] == WorldPainter2D.Outside);
                        Check(centres.Count == 0 || outside * 100 <= centres.Count, $"{label}: {centres.Count - outside} of {centres.Count} AI sections on the map are in a district");
                    }

                    map.Write(entry);
                    Check(entry.GetPrimaryWithoutImports().SequenceEqual(before), $"{label}: saves unchanged");

                    using WorldPainter2DView view = new(map, entry);
                    Hidden(view);
                    Application.DoEvents();
                    Capture(view, $"painter-{Path.GetFileName(root.TrimEnd('/', '\\')).Replace(' ', '_')}-{map.Name}.png");
                }
    }
}
