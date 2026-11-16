using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

// Splicer (https://burnout.wiki/wiki/Splicer)
public class Splicer : IEntryData
{
    private const int DataOffset = 0x10, SpliceSize = 0x18, SampleRefSize = 0x2C;

    // Splice holds 15 SpliceSamples and plays every sample ref without checking
    public const int MaxSampleRefs = 15;

    // EA's packed sample header, then the codec data
    public class Sample
    {
        [Browsable(false)]
        public ReadOnlyMemory<byte> Data { get; set; }

        private SplicerAudio.Header Header => SplicerAudio.Header.Read(Data.Span);

        [Category("Properties"), Description("How the sample is compressed. PC, PS3 and Remastered use EALayer3, Xbox 360 uses EA-XMA.")]
        public string Codec => Header.CodecName;

        [Category("Properties")]
        public int Channels => Header.Channels;

        [Category("Properties"), Description("Samples per second.")]
        public int SampleRate => Header.Rate;

        [Category("Properties"), Description("Length in seconds, at a pitch of 1.")]
        public float Length => Header.Seconds;

        [Category("Properties"), Description("Whether the sample loops until its sample ref's duration ends.")]
        public bool Loops => Header.Loops;

        [Category("Properties"), Description("Bytes of sample data.")]
        public int Size => Data.Length;

        public override string ToString() => Header.Size == 0
            ? $"{Data.Length} bytes"
            : $"{Header.CodecName}, {Header.Rate} Hz {(Header.Channels == 1 ? "mono" : Header.Channels == 2 ? "stereo" : $"{Header.Channels} channels")}, {Header.Seconds:0.00} s";
    }

    // SPLICE_SampleRef
    public class SampleRef
    {
        [Category("Properties"), Description("The sample to play."), TypeConverter(typeof(TrafficReferenceConverter))]
        public Sample Sample { get; set; }

        [Category("Properties"), Description("Volume, as a multiplier.")]
        public float Volume { get; set; } = 1;

        [Category("Properties"), Description("Playback speed, as a multiplier. 2 plays an octave higher and twice as fast.")]
        public float Pitch { get; set; } = 1;

        [Category("Timing"), Description("Seconds after the splice starts before this sample plays.")]
        public float Offset { get; set; }

        [Category("Properties"), Description("Pan in degrees: 0 is centre, -90 left, 90 right. -127 plays a stereo sample in stereo.")]
        public float Az { get; set; }

        [Category("Timing"), Description("Seconds the sample plays for before it's cut off, if it hasn't ended. A looping sample plays until this.")]
        public float Duration { get; set; } = 1;

        [Category("Timing"), Description("Seconds the sample fades in over; 0 for none. The game's curve reaches only 46% before jumping to full volume.")]
        public float FadeIn { get; set; }

        [Category("Timing"), Description("Seconds into the sample that it starts fading out, until the duration ends; 0 for none. The game's curve starts at 46%.")]
        public float FadeOut { get; set; }

        [Category("Randomness"), Description("Each play takes a volume between this and 1 / this, as a multiplier. 1 for none.")]
        public float RND_Vol { get; set; } = 1;

        [Category("Randomness"), Description("Each play adds between 0 and this to the pitch.")]
        public float RND_Pitch { get; set; }

        [Category("Advanced"), Description("Not used by the game's splice player.")]
        public byte Priority { get; set; }

        [Category("Advanced"), Description("Not used by the game's splice player.")]
        public byte eRollOffType { get; set; }

        public override string ToString() => $"{(Sample == null ? "no sample" : $"sample {TrafficReferences.SplicerData?.Samples.IndexOf(Sample)}")}{(Offset > 0 ? $", after {Offset:0.###} s" : "")}";
    }

    // SPLICE_Data
    public class Splice
    {
        [Browsable(false)]
        public List<SampleRef> SampleRefs { get; set; } = [];

        [Category("Properties"), Description("Volume of the whole splice, as a multiplier on each sample's.")]
        public float Volume { get; set; } = 1;

        [Category("Advanced"), Description("Not used by the game's splice player.")]
        public float RND_Pitch { get; set; }

        [Category("Advanced"), Description("Not used by the game's splice player.")]
        public float RND_Vol { get; set; } = 1;

        [Category("Properties"), Description("Seconds the splice plays for, leaving out random pitch.")]
        public float Length => SampleRefs.Where(r => r.Sample != null && r.Pitch > 0)
            .Select(r => r.Offset + (r.Sample.Loops
                ? r.Duration
                : Math.Min(r.Duration, r.Sample.Length / r.Pitch)))
            .DefaultIfEmpty(0)
            .Max();

        public override string ToString() => $"{SampleRefs.Count} sample{(SampleRefs.Count == 1 ? "" : "s")}, {Length:0.00} s";
    }

    [Browsable(false)]
    public List<Splice> Splices { get; set; } = [];

    [Browsable(false)]
    public List<Sample> Samples { get; set; } = [];

    public IEnumerable<TrafficRules.Issue> Problems()
    {
        HashSet<Sample> samples = new(Samples, ReferenceEqualityComparer.Instance);

        for (int i = 0; i < Splices.Count; i++)
        {
            Splice splice = Splices[i];

            if (splice.SampleRefs.Count > MaxSampleRefs)
                yield return new(splice.SampleRefs, null, $"Splice {i} has {splice.SampleRefs.Count} samples, the game plays up to {MaxSampleRefs}.");

            foreach (SampleRef sampleRef in splice.SampleRefs.Where(r => r.Sample == null || !samples.Contains(r.Sample)))
                yield return new(sampleRef, nameof(SampleRef.Sample), $"Splice {i} has a sample ref without a sample.");
        }
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        byte[] data = entry.GetPrimaryWithoutImports();
        using BinaryReader2 br = new(new MemoryStream(data), entry.Console);

        // mu32DataSize isn't trusted: a modded PRESENTATIONASSET has sample data past it
        br.ReadUInt32();
        int offset = br.ReadInt32();
        br.BaseStream.Position = offset;
        int version = br.ReadInt32();

        if (version != 1)
            throw new ReadFailedError($"Unsupported splicer version {version}, expected 1.");

        int sizeData = br.ReadInt32(), spliceCount = br.ReadInt32();
        long records = br.BaseStream.Position;
        List<int> refCounts = [];
        Splices = [];

        for (int i = 0; i < spliceCount; i++)
        {
            br.ReadUInt32(); // NameHash, always 0
            br.ReadUInt16(); // SpliceIndex, always the position
            br.ReadByte(); // eSpliceType, set by the game when loading
            refCounts.Add(br.ReadByte());
            Splices.Add(new()
            {
                Volume = br.ReadSingle(),
                RND_Pitch = br.ReadSingle(),
                RND_Vol = br.ReadSingle()
            });
            br.ReadUInt32(); // pSampleRefList, set by the game when loading
        }

        if (spliceCount * SpliceSize + refCounts.Sum() * SampleRefSize != sizeData)
            throw new ReadFailedError($"The splicer's {spliceCount} splices and {refCounts.Sum()} sample refs don't fill its {sizeData} bytes of splice data.");

        br.BaseStream.Position = records + sizeData;
        int sampleCount = br.ReadInt32();
        int[] contents = [.. Enumerable.Range(0, sampleCount).Select(_ => br.ReadInt32())];
        int arena = (int)br.BaseStream.Position;

        // The last sample runs to the end, taking the padding with it
        Samples = [.. contents.Select((start, i) =>
        {
            int end = i + 1 < contents.Length
                ? contents[i + 1]
                : data.Length - arena;

            if (start < 0 || start > end || arena + end > data.Length)
                throw new ReadFailedError($"Splicer sample {i} is out of order or out of range.");

            return new Sample
            {
                Data = data[(arena + start)..(arena + end)]
            };
        })];

        br.BaseStream.Position = records + spliceCount * SpliceSize;

        for (int i = 0; i < spliceCount; i++)
            for (int r = 0; r < refCounts[i]; r++)
            {
                int sample = br.ReadUInt16();
                br.ReadUInt16(); // eSpliceType, then padding with leftover bytes on consoles

                if (sample >= Samples.Count)
                    throw new ReadFailedError($"Splice {i} uses sample {sample}, the splicer has {Samples.Count}.");

                Splices[i].SampleRefs.Add(new()
                {
                    Sample = Samples[sample],
                    Volume = br.ReadSingle(),
                    Pitch = br.ReadSingle(),
                    Offset = br.ReadSingle(),
                    Az = br.ReadSingle(),
                    Duration = br.ReadSingle(),
                    FadeIn = br.ReadSingle(),
                    FadeOut = br.ReadSingle(),
                    RND_Vol = br.ReadSingle(),
                    RND_Pitch = br.ReadSingle(),
                    Priority = br.ReadByte(),
                    eRollOffType = br.ReadByte()
                });
                br.ReadUInt16(); // Padding
            }

        return true;
    }

    public byte[] Serialize(BundleEntry entry)
    {
        if (Problems().FirstOrDefault() is TrafficRules.Issue problem)
            throw new InvalidDataException(problem.Message);

        Dictionary<Sample, int> indices = new(ReferenceEqualityComparer.Instance);

        for (int i = 0; i < Samples.Count; i++)
            indices[Samples[i]] = i;

        using MemoryStream ms = new();
        using BinaryWriter2 bw = new(ms, entry.Console);
        bw.Write(0); // mu32DataSize, filled in below
        bw.Write(DataOffset);
        bw.Write(0L);
        bw.Write(1);
        bw.Write(Splices.Count * SpliceSize + Splices.Sum(s => s.SampleRefs.Count) * SampleRefSize);
        bw.Write(Splices.Count);

        for (int i = 0; i < Splices.Count; i++)
        {
            bw.Write(0); // NameHash
            bw.Write((ushort)i);
            bw.Write((byte)0);
            bw.Write((byte)Splices[i].SampleRefs.Count);
            bw.Write(Splices[i].Volume);
            bw.Write(Splices[i].RND_Pitch);
            bw.Write(Splices[i].RND_Vol);
            bw.Write(0);
        }

        foreach (SampleRef sampleRef in Splices.SelectMany(s => s.SampleRefs))
        {
            bw.Write((ushort)indices[sampleRef.Sample]);
            bw.Write((ushort)0);

            foreach (float value in (float[])[sampleRef.Volume, sampleRef.Pitch, sampleRef.Offset, sampleRef.Az, sampleRef.Duration,
                sampleRef.FadeIn, sampleRef.FadeOut, sampleRef.RND_Vol, sampleRef.RND_Pitch])
                bw.Write(value);

            bw.Write(sampleRef.Priority);
            bw.Write(sampleRef.eRollOffType);
            bw.Write((ushort)0);
        }

        bw.Write(Samples.Count);
        int at = 0;

        foreach (Sample sample in Samples)
        {
            bw.Write(at);
            at += sample.Data.Length;
        }

        foreach (Sample sample in Samples)
            bw.Write(sample.Data.Span);

        while (ms.Length % 16 != 0)
            bw.Write((byte)0);

        ms.Position = 0;
        bw.Write((int)ms.Length - DataOffset);
        bw.Flush();
        return ms.ToArray();
    }

    public bool Write(BundleEntry entry)
    {
        entry.SetPrimary(Serialize(entry), entry.GetImports());
        return true;
    }

    public EntryType GetEntryType(BundleEntry entry) => EntryType.Splicer;

    public IEntryEditor GetEditor(BundleEntry entry) => new SplicerEditor(this, entry);
}

public sealed class SplicerEditor : ResourceEditor
{
    private readonly Splicer data;
    private readonly Dictionary<Splicer.Sample, SplicerAudio.Decoded> decoded = new(ReferenceEqualityComparer.Instance);
    private readonly Random random = new();
    private readonly ToolStripButton playSplice = new("Play splice"), playSample = new("Play sample"), stop = new("Stop");
    private SoundPlayer player;

    public SplicerEditor(Splicer source, BundleEntry initialEntry) : base(initialEntry, "Splicer", "splicer")
    {
        data = (Splicer)ObjectTreeEditor.Clone(source);
        TrafficLabels.Install();
        Splicer previous = TrafficReferences.SplicerData;
        TrafficReferences.SplicerData = data;
        Disposed += (_, _) => TrafficReferences.SplicerData = previous;
        Initialize(data, "Splicer", null);
        Tree.SearchPrompt = "Search splices and samples";
        Tree.CollectionLimit = (_, list) => list switch
        {
            nameof(Splicer.Splice.SampleRefs) => Splicer.MaxSampleRefs,
            nameof(Splicer.Samples) => data.Samples.Count, // There's no way to make sample data here
            _ => ushort.MaxValue + 1
        };

        // The game plays splices by index
        Tree.PositionsFixed = (_, list) => list is nameof(Splicer.Splices);

        playSplice.ToolTipText = "Play the selected splice as the game would, with its random volume and pitch rolled again each time";
        playSample.ToolTipText = "Play the selected sample as it's stored";
        playSplice.Click += (_, _) => Play(() => SplicerAudio.Wav(SplicerAudio.Render(SelectedSplice, Decode, random), 2, SplicerAudio.OutputRate));
        playSample.Click += (_, _) => Play(() =>
        {
            SplicerAudio.Decoded sound = Decode(SelectedSample);
            return SplicerAudio.Wav(sound.Pcm, sound.Channels, sound.Rate);
        });
        stop.Click += (_, _) => player?.Stop();
        Tree.AddPlacementCommands(playSplice, playSample, new ToolStripSeparator(), stop);
        Tree.SelectionChanged += _ => UpdateCommands();
        FormClosed += (_, _) => player?.Dispose();
        UpdateCommands();
    }

    private Splicer.Splice SelectedSplice => Tree.SelectedObject switch
    {
        Splicer.Splice splice => splice,
        Splicer.SampleRef sampleRef => data.Splices.FirstOrDefault(s => s.SampleRefs.Contains(sampleRef)),
        List<Splicer.SampleRef> list => data.Splices.FirstOrDefault(s => s.SampleRefs == list),
        _ => null
    };

    private Splicer.Sample SelectedSample => Tree.SelectedObject switch
    {
        Splicer.Sample sample => sample,
        Splicer.SampleRef sampleRef => sampleRef.Sample,
        _ => null
    };

    private void UpdateCommands()
    {
        playSplice.Enabled = SelectedSplice != null;
        playSample.Enabled = SelectedSample != null;
    }

    private SplicerAudio.Decoded Decode(Splicer.Sample sample)
    {
        if (!decoded.TryGetValue(sample, out SplicerAudio.Decoded sound))
            decoded[sample] = sound = SplicerAudio.Decode(sample.Data.Span);

        return sound;
    }

    private void Play(Func<byte[]> render)
    {
        player?.Stop();
        player?.Dispose();
        player = null;

        try
        {
            player = new(new MemoryStream(render()));
            player.Play();
        }
        // Decoding goes through the Windows MP3 codec, which can fail in its own ways
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Can't play this", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    protected override List<TrafficRules.Issue> FindIssues() => [.. data.Problems()];

    protected override void WriteResource() => data.Write(ResourceEntry);
}
