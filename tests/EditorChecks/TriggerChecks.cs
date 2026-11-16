using System.Numerics;
using BaseHandlers;
using BundleFormat;

internal static partial class Program
{
    private static void CheckTriggerFiles(string[] args)
    {
        foreach (string argument in args.Where(argument => argument.StartsWith("--triggers-file=")))
        {
            string path = argument["--triggers-file=".Length..];
            BundleArchive bundle = BundleArchive.Read(path);
            string label = $"{Path.GetFileName(Path.GetDirectoryName(path))}/{Path.GetFileName(path)}";

            foreach (BundleEntry entry in bundle.Entries.Where(e => e.Type == EntryType.TriggerData))
            {
                TriggerData data = new();
                Check(data.Read(entry), $"{label}: TriggerData reads");
                int grids = data.mpLandmarks.Sum(l => l.mpaStartingGrids.Count);
                int[] elements = [.. data.mpSignatureStunts.Select(s => s.stuntElementRegions.Count)];

                Check(data.Write(entry), $"{label}: TriggerData saves");
                TriggerData reread = new();
                Check(reread.Read(entry), $"{label}: saved TriggerData reads back");
                Check(reread.mpLandmarks.Count == data.mpLandmarks.Count
                    && reread.mpLandmarks.Sum(l => l.mpaStartingGrids.Count) == grids
                    && reread.mpLandmarks.Zip(data.mpLandmarks).All(p => p.First.mpaStartingGrids.Zip(p.Second.mpaStartingGrids)
                        .All(g => g.First.StartingPositions.SequenceEqual(g.Second.StartingPositions) && g.First.StartingDirections.SequenceEqual(g.Second.StartingDirections))),
                    $"{label}: {data.mpLandmarks.Count} landmarks keep their {grids} starting grids");
                Check(reread.mpSignatureStunts.Select(s => s.stuntElementRegions.Count).SequenceEqual(elements),
                    $"{label}: {elements.Length} signature stunts keep their {elements.Sum()} elements");


                StartingGrid grid = new()
                {
                    StartingPositions = [.. Enumerable.Range(0, 8).Select(i => new Vector4(i, 1, 2, 0))],
                    StartingDirections = [.. Enumerable.Range(0, 8).Select(i => new Vector4(0, 0, 1, i))]
                };

                reread.mpLandmarks[0].mpaStartingGrids.Add(grid);
                int[] stunt = [reread.mpGenericRegions[0].mId, reread.mpGenericRegions[1].mId];
                reread.mpSignatureStunts.Add(new()
                {
                    mId = new()
                    {
                        ID = 0x5157_0001
                    },
                    miCamera = 7,
                    stuntElementRegions = [.. stunt]
                });
                Check(reread.Write(entry), $"{label}: TriggerData with a starting grid and a stunt saves");
                TriggerData added = new();
                Check(added.Read(entry), $"{label}: TriggerData with a starting grid and a stunt reads back");
                StartingGrid back = added.mpLandmarks[0].mpaStartingGrids.LastOrDefault();
                Check(back != null && back.StartingPositions.SequenceEqual(grid.StartingPositions) && back.StartingDirections.SequenceEqual(grid.StartingDirections),
                    $"{label}: an added starting grid reads back with all 8 positions and directions");
                Check(added.mpSignatureStunts.Count == elements.Length + 1 && added.mpSignatureStunts[^1].stuntElementRegions.SequenceEqual(stunt) && added.mpSignatureStunts[^1].miCamera == 7,
                    $"{label}: an added signature stunt keeps both of its elements");
            }
        }
    }
}
