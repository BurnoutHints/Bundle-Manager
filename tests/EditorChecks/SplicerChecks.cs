using System.Media;
using BaseHandlers;
using BundleFormat;
using BundleUtilities;

internal static partial class Program
{
    // Consoles leave junk in the sample refs' padding, Switch in the BinaryFile header's
    private static byte[] WithoutSplicerPadding(byte[] data, bool bigEndian)
    {
        byte[] copy = [.. data];
        Array.Clear(copy, 8, 8);

        using BinaryReader2 br = new(new MemoryStream(data), bigEndian);
        br.BaseStream.Position = 0x14;
        int sizeData = br.ReadInt32(), splices = br.ReadInt32();
        int refs = (sizeData - splices * 0x18) / 0x2C, start = 0x1C + splices * 0x18;

        for (int i = 0; i < refs; i++)
        {
            copy[start + i * 0x2C + 3] = 0;
            Array.Clear(copy, start + i * 0x2C + 0x2A, 2);
        }

        return copy;
    }

    private static void CheckSplicerFiles(string[] args)
    {
        foreach (string argument in args.Where(argument => argument.StartsWith("--splicer-file=")))
        {
            string path = argument["--splicer-file=".Length..];
            BundleArchive bundle = BundleArchive.Read(path);
            string label = $"{Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(path))))}/{Path.GetFileName(path)}";

            foreach (BundleEntry entry in bundle.Entries.Where(e => e.Type == EntryType.Splicer))
            {
                byte[] original = entry.EntryBlocks[0].Data;
                Splicer data = new();
                Check(data.Read(entry) && !data.Problems().Any(), $"{label}: {data.Splices.Count} splices and {data.Samples.Count} samples read without issues");

                byte[] saved = data.Serialize(entry);
                bool declared = BitConverter.ToUInt32(entry.Console ? [.. original[..4].Reverse()] : original[..4]) + 0x10 == original.Length;

                if (declared)
                    Equal(WithoutSplicerPadding(saved, entry.Console), WithoutSplicerPadding(original, entry.Console), $"{label}: saves the same bytes apart from padding");
                else
                    Check(saved.Length == (original.Length + 15) / 16 * 16 && WithoutSplicerPadding(saved, entry.Console)[4..original.Length].SequenceEqual(WithoutSplicerPadding(original, entry.Console)[4..]),
                        $"{label}: data past the declared size is kept, and the size is corrected");

                Dictionary<Splicer.Sample, SplicerAudio.Decoded> decoded = new(ReferenceEqualityComparer.Instance);
                int unsupported = 0, silent = 0;

                foreach (Splicer.Sample sample in data.Samples)
                {
                    try
                    {
                        SplicerAudio.Decoded sound = decoded[sample] = SplicerAudio.Decode(sample.Data.Span);
                        silent += sound.Pcm.All(s => Math.Abs(s) < 0.001f) ? 1 : 0;

                        if (sound.Frames != SplicerAudio.Header.Read(sample.Data.Span).Samples || sound.Pcm.Any(float.IsNaN))
                            throw new($"{label}: sample {data.Samples.IndexOf(sample)} decodes to {sound.Frames} samples, its header says {sample.Length * sample.SampleRate}");
                    }
                    catch (NotSupportedException)
                    {
                        unsupported++;
                    }
                }

                Check(decoded.Count + unsupported == data.Samples.Count, $"{label}: {decoded.Count} samples decode to their full length, {unsupported} use a codec without preview, {silent} are silent");

                if (decoded.Count == 0)
                    continue;

                Random random = new(5);
                int rendered = 0;

                foreach (Splicer.Splice splice in data.Splices.Where(s => s.SampleRefs.All(r => decoded.ContainsKey(r.Sample))))
                {
                    float[] mix = SplicerAudio.Render(splice, s => decoded[s], random);

                    if (mix.Any(float.IsNaN))
                        throw new($"{label}: splice {data.Splices.IndexOf(splice)} renders NaN");

                    // Without random pitch, the render lasts as long as the splice says
                    if (splice.SampleRefs.All(r => r.RND_Pitch == 0) && Math.Abs(mix.Length / 2f / SplicerAudio.OutputRate - Math.Min(splice.Length, 120)) > 0.01f)
                        throw new($"{label}: splice {data.Splices.IndexOf(splice)} renders {mix.Length / 2f / SplicerAudio.OutputRate} s, it lasts {splice.Length} s");

                    rendered++;
                }

                Check(rendered > 0, $"{label}: {rendered} splices render as long as they last");

                using SoundPlayer player = new(new MemoryStream(SplicerAudio.Wav(SplicerAudio.Render(data.Splices.First(s => s.SampleRefs.All(r => decoded.ContainsKey(r.Sample))), s => decoded[s], random), 2, SplicerAudio.OutputRate)));
                player.Load();
                Check(player.IsLoadCompleted, $"{label}: a rendered splice is a WAV Windows can play");

                CheckSplicerEditor(data, entry, label);
            }
        }
    }

    // The same sounds encoded per platform should decode to the same audio, lined up to the sample
    private static void CheckSplicerPlatforms(string[] args)
    {
        foreach (string argument in args.Where(argument => argument.StartsWith("--splicer-pair=")))
        {
            string[] paths = argument["--splicer-pair=".Length..].Split('|');
            Splicer[] pair = [.. paths.Select(path =>
            {
                Splicer data = new();
                data.Read(BundleArchive.Read(path).Entries.First(e => e.Type == EntryType.Splicer));
                return data;
            })];
            List<double> correlations = [];

            for (int i = 0; i < Math.Min(pair[0].Samples.Count, pair[1].Samples.Count); i++)
            {
                SplicerAudio.Decoded a = SplicerAudio.Decode(pair[0].Samples[i].Data.Span), b = SplicerAudio.Decode(pair[1].Samples[i].Data.Span);

                if (a.Frames != b.Frames || a.Channels != b.Channels || a.Rate != b.Rate)
                    continue;

                int length = Math.Min(a.Pcm.Length, 48000 * a.Channels);
                double meanA = a.Pcm.Take(length).Average(), meanB = b.Pcm.Take(length).Average(), product = 0, squaresA = 0, squaresB = 0;

                for (int s = 0; s < length; s++)
                {
                    product += (a.Pcm[s] - meanA) * (b.Pcm[s] - meanB);
                    squaresA += (a.Pcm[s] - meanA) * (a.Pcm[s] - meanA);
                    squaresB += (b.Pcm[s] - meanB) * (b.Pcm[s] - meanB);
                }

                if (squaresA > 0 && squaresB > 0)
                    correlations.Add(product / Math.Sqrt(squaresA * squaresB));
            }

            correlations.Sort();
            Check(correlations.Count > 0 && correlations[correlations.Count / 2] > 0.9,
                $"{string.Join(" vs ", paths.Select(p => Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(p)))) + "/" + Path.GetFileName(p)))}: {correlations.Count} matching samples line up, median correlation {correlations[correlations.Count / 2]:0.000}, lowest {correlations[0]:0.000}");
        }
    }

    private static void CheckSplicerEditor(Splicer source, BundleEntry entry, string label)
    {
        byte[] before = [.. entry.EntryBlocks[0].Data];
        using SplicerEditor editor = new(source, entry);
        Hidden(editor);
        Splicer draft = Field<Splicer>(editor, "data");
        ObjectTreeEditor tree = Field<ObjectTreeEditor>(editor, "tree");
        int spliceIndex = draft.Splices.FindIndex(s => s.SampleRefs.Count > 0);
        Splicer.SampleRef sampleRef = draft.Splices[spliceIndex].SampleRefs[0];

        tree.SelectObject(sampleRef);
        Check(Field<ToolStripButton>(editor, "playSplice").Enabled && Field<ToolStripButton>(editor, "playSample").Enabled, $"{label}: a selected sample ref can play its splice or its sample");
        Check(TrafficReferences.Candidates(sampleRef, nameof(Splicer.SampleRef.Sample), typeof(Splicer.Sample)).Count == draft.Samples.Count, $"{label}: a sample ref picks from the splicer's samples");
        tree.SelectObject(draft.Samples[^1]);
        Check(!Field<ToolStripButton>(editor, "playSplice").Enabled, $"{label}: a sample on its own has no splice to play");
        Check(tree.CollectionLimit(draft.Splices[spliceIndex], nameof(Splicer.Splice.SampleRefs)) == Splicer.MaxSampleRefs && tree.PositionsFixed(draft, nameof(Splicer.Splices)),
            $"{label}: splices hold up to 15 samples and keep their positions");

        editor.ClientSize = new(1000, 700);
        tree.SelectObject(sampleRef);
        Application.DoEvents();
        Capture(editor, $"splicer-{entry.ID:X8}.png");

        Splicer.Sample other = draft.Samples[^1];
        sampleRef.Sample = other;
        sampleRef.Offset = 0.25f;
        tree.RecordChange();
        Check(entry.EntryBlocks[0].Data.SequenceEqual(before) && (bool)Call(editor, "ApplyChanges"), $"{label}: edits stay in the draft until Apply writes them");
        Splicer applied = new();
        applied.Read(entry);
        Check(applied.Splices[spliceIndex].SampleRefs[0].Offset == 0.25f && applied.Samples.IndexOf(applied.Splices[spliceIndex].SampleRefs[0].Sample) == draft.Samples.Count - 1,
            $"{label}: Apply writes the new sample and offset");

        draft.Splices[spliceIndex].SampleRefs.AddRange(Enumerable.Range(0, Splicer.MaxSampleRefs).Select(_ => new Splicer.SampleRef { Sample = other }));
        tree.RecordChange();
        Check(Field<ListBox>(editor, "issues").Items.Count > 0 && !(bool)Call(editor, "ApplyChanges"), $"{label}: more than 15 samples in a splice is listed and blocks Apply");
        entry.EntryBlocks[0].Data = before;
    }
}
