using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;

namespace BaseHandlers;

// Decodes splicer samples and mixes splices the way SpliceSample::Play and Update do
public static class SplicerAudio
{
    public const int OutputRate = 48000;

    // FLT_EPSILON
    private const float Epsilon = 1.1920929e-7f;

    // A sample ref with this azimuth plays its sample in stereo instead of panning it
    public const float StereoAzimuth = -127;

    private static readonly string[] codecNames = ["none", "RELAH", "PCM", "EA-XMA", "EA-XAS", "EALayer3", "EALayer3 v2 PCM", "EALayer3 v2 Spike"];

    private static readonly int[][] mpegRates = [[11025, 12000, 8000], [], [22050, 24000, 16000], [44100, 48000, 32000]];

    public sealed record Decoded(float[] Pcm, int Channels, int Rate, int LoopStart)
    {
        public int Frames => Pcm.Length / Channels;
    }

    private sealed class BitReader(byte[] data, int position = 0)
    {
        public int Position = position;

        public uint Read(int count)
        {
            uint value = 0;

            for (int i = 0; i < count; i++, Position++)
            {
                if (Position >> 3 >= data.Length)
                    throw new InvalidDataException("The sample data ends early.");

                value = value << 1 | (uint)(data[Position >> 3] >> (7 - (Position & 7)) & 1);
            }

            return value;
        }
    }

    private sealed class BitWriter
    {
        public readonly List<byte> Bytes = [];
        private int pending, count;

        public void Write(int bits, uint value)
        {
            for (int i = bits - 1; i >= 0; i--)
            {
                pending = pending << 1 | (int)(value >> i & 1);

                if (++count < 8)
                    continue;

                Bytes.Add((byte)pending);
                pending = count = 0;
            }
        }

        public void Align()
        {
            while (count != 0)
                Write(1, 0);
        }
    }

    public readonly record struct Header(int Codec, int Channels, int Rate, int PlayType, bool Loops, int Samples, int LoopStart, int Size)
    {
        public string CodecName => Codec < codecNames.Length
            ? codecNames[Codec]
            : $"codec {Codec}";

        public float Seconds => Rate > 0
            ? Samples / (float)Rate
            : 0;

        public static Header Read(ReadOnlySpan<byte> data)
        {
            if (data.Length < 8)
                return default;

            BitReader bits = new(data[..Math.Min(data.Length, 24)].ToArray());
            bits.Read(4); // Version
            int codec = (int)bits.Read(4), channels = (int)bits.Read(6) + 1, rate = (int)bits.Read(18), playType = (int)bits.Read(2);
            bool loops = bits.Read(1) == 1;
            int samples = (int)bits.Read(29);
            int loopStart = loops
                ? (int)bits.Read(32)
                : -1;
            int resident = playType == 2
                ? (int)bits.Read(32)
                : 0;

            // A loop in the streamed part has its stream offset too
            if (loops && (playType == 1 || playType == 2 && loopStart >= resident))
                bits.Read(32);

            return new(codec, channels, rate, playType, loops, samples, loopStart, bits.Position / 8);
        }
    }

    public static Decoded Decode(ReadOnlySpan<byte> sample)
    {
        Header header = Header.Read(sample);

        if (header.Size == 0)
            throw new InvalidDataException("The sample is too short to have a header.");

        if (header.PlayType != 0)
            throw new NotSupportedException("Streamed samples aren't supported.");

        // SNS blocks
        List<byte[]> blocks = [];

        for (int at = header.Size; at + 8 <= sample.Length;)
        {
            int size = sample[at + 1] << 16 | sample[at + 2] << 8 | sample[at + 3];

            if (size < 8 || at + size > sample.Length)
                break;

            blocks.Add(sample[(at + 8)..(at + size)].ToArray());

            if ((sample[at] & 0x80) != 0)
                break;

            at += size;
        }

        short[] pcm;
        int discard = 0;

        if (header.Codec is 5 or 6 or 7)
        {
            (byte[] mp3, int frameSize, int bitrate, List<(int At, short[] Pcm)> spikes) = EALayer3ToMp3([.. blocks.SelectMany(b => b)], header.Codec != 5);
            pcm = DecodeMp3(mp3, header.Rate, header.Channels, frameSize, bitrate);

            // The encoder keeps each sample's start uncompressed at the end of the first frame, after the codec delay
            foreach ((int at, short[] spike) in spikes.Where(s => (s.At + s.Pcm.Length / header.Channels) * header.Channels <= pcm.Length))
                spike.CopyTo(pcm, at * header.Channels);

            discard = spikes.Select(s => s.At).DefaultIfEmpty(0).Min();
        }
        else if (header.Codec == 3 && header.Channels <= 2)
            pcm = Ffmpeg(Xma2Wav(blocks, header));
        else
            throw new NotSupportedException($"Previewing {header.CodecName} samples isn't supported.");

        float[] samples = new float[header.Samples * header.Channels];

        for (int i = 0; i < samples.Length && i + discard * header.Channels < pcm.Length; i++)
            samples[i] = pcm[i + discard * header.Channels] / 32768f;

        return new(samples, header.Channels, header.Rate, header.LoopStart);
    }

    // EAXMA blocks are 4 bytes then 2048 byte XMA2 packets, ffmpeg decodes them from an XMA2 WAV
    private static byte[] Xma2Wav(List<byte[]> blocks, Header header)
    {
        byte[] packets = [.. blocks.SelectMany(b => b.Skip(4))];
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        writer.Write("RIFF"u8);
        writer.Write(4 + 8 + 52 + 8 + packets.Length);
        writer.Write("WAVEfmt "u8);
        writer.Write(52);
        writer.Write((short)0x166);
        writer.Write((short)header.Channels);
        writer.Write(header.Rate);
        writer.Write(header.Rate * header.Channels * 2);
        writer.Write((short)(header.Channels * 2));
        writer.Write((short)16);
        writer.Write((short)34);
        writer.Write((short)1); // Streams
        writer.Write(header.Channels == 1
            ? 4
            : 3); // Channel mask
        writer.Write(header.Samples);
        writer.Write(packets.Length); // Bytes per block
        writer.Write(0); // Play begin
        writer.Write(header.Samples);
        writer.Write(0L); // Loop begin and length
        writer.Write((byte)0);
        writer.Write((byte)4); // Encoder version
        writer.Write((short)1); // Blocks
        writer.Write("data"u8);
        writer.Write(packets.Length);
        writer.Write(packets);
        writer.Flush();
        return stream.ToArray();
    }

    private static short[] Ffmpeg(byte[] wav)
    {
        ProcessStartInfo start = new("ffmpeg", "-v error -f wav -i pipe:0 -f s16le pipe:1")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process process;

        try
        {
            process = Process.Start(start);
        }
        catch (Win32Exception)
        {
            throw new NotSupportedException("Previewing Xbox 360 samples needs ffmpeg, which isn't on the PATH.");
        }

        using (process)
        {
            Task<byte[]> output = Task.Run(() =>
            {
                using MemoryStream decoded = new();
                process.StandardOutput.BaseStream.CopyTo(decoded);
                return decoded.ToArray();
            });
            Task<string> errors = process.StandardError.ReadToEndAsync();

            try
            {
                process.StandardInput.BaseStream.Write(wav);
                process.StandardInput.Close();
            }
            catch (IOException) { } // ffmpeg stopped reading, its exit code says why

            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new InvalidDataException("ffmpeg couldn't decode the sample: " + errors.Result.Trim());

            byte[] bytes = output.Result;
            short[] pcm = new short[bytes.Length / 2];
            Buffer.BlockCopy(bytes, 0, pcm, 0, pcm.Length * 2);
            return pcm;
        }
    }

    private sealed class Granule
    {
        public int Version, RateIndex, ChannelMode, ModeExtension, Index, DataBit, SpikeAt;
        public uint[] Scfsi;
        public (uint Size, uint Side1, uint Side2)[] Side;
        public byte[] Source = [];

        // Uncompressed samples replacing the decoded ones from SpikeAt into the granule
        public short[] Spike = [];

        public int Channels => ChannelMode == 3
            ? 1
            : 2;

        public bool Mpeg1 => Version == 3;
    }

    private static Granule ReadGranule(BitReader bits, byte[] source)
    {
        int start = bits.Position;
        Granule granule = new()
        {
            Version = (int)bits.Read(2),
            RateIndex = (int)bits.Read(2),
            ChannelMode = (int)bits.Read(2),
            ModeExtension = (int)bits.Read(2),
            Index = (int)bits.Read(1),
            Source = source
        };

        if (granule.Version == 1 || granule.RateIndex == 3)
            throw new InvalidDataException("The sample has an invalid EALayer3 frame.");

        granule.Scfsi = [.. Enumerable.Range(0, granule.Channels).Select(_ => granule.Mpeg1 && granule.Index == 1
            ? bits.Read(4)
            : 0)];
        granule.Side = [.. Enumerable.Range(0, granule.Channels).Select(_ => (bits.Read(12), bits.Read(32), bits.Read(granule.Mpeg1
            ? 15
            : 19)))];
        granule.DataBit = bits.Position;
        bits.Position += (int)granule.Side.Sum(s => s.Size);
        bits.Position = start + (bits.Position - start + 7) / 8 * 8;
        return granule;
    }

    // Big endian, one channel after another
    private static short[] ReadSpike(byte[] data, int at, int count, int channels)
    {
        if (at + 2 * count * channels > data.Length)
            throw new InvalidDataException("The sample data ends early.");

        short[] pcm = new short[count * channels];

        for (int c = 0; c < channels; c++)
            for (int i = 0; i < count; i++)
                pcm[i * channels + c] = (short)(data[at + 2 * (c * count + i)] << 8 | data[at + 2 * (c * count + i) + 1]);

        return pcm;
    }

    private static (byte[] Mp3, int FrameSize, int Bitrate, List<(int At, short[] Pcm)> Spikes) EALayer3ToMp3(byte[] payload, bool version2)
    {
        List<Granule> granules = [];

        for (int offset = 0; offset < payload.Length;)
        {
            BitReader bits = new(payload, offset * 8);
            Granule granule = null;
            int size;

            if (version2)
            {
                bool extended = bits.Read(1) == 1;
                bits.Read(3); // Stereo flag, reserved
                size = (int)bits.Read(12);
                int before = 0, spike = 0, commonSize = 0;

                if (extended)
                {
                    bits.Read(2); // Offset mode
                    before = (int)bits.Read(10);
                    spike = (int)bits.Read(10);
                    commonSize = (int)bits.Read(10);
                }

                if (!extended || commonSize > 0)
                {
                    granule = ReadGranule(bits, payload);
                    granule.SpikeAt = before;
                    granule.Spike = ReadSpike(payload, offset + 6 + commonSize, spike, granule.Channels);
                }
                else if (spike > 0) // Not used in any splicer we have
                    throw new NotSupportedException("EALayer3 frames with only uncompressed samples aren't supported.");
            }
            else
            {
                uint flag = bits.Read(8);

                if (flag is not (0 or 0xEE))
                    throw new InvalidDataException("The sample has an invalid EALayer3 frame.");

                granule = ReadGranule(bits, payload);

                if (flag == 0xEE)
                {
                    bits.Read(16);
                    int spike = (int)bits.Read(16);
                    bits.Read(32);
                    granule.SpikeAt = 576 - spike;
                    granule.Spike = ReadSpike(payload, bits.Position / 8, spike, granule.Channels);
                }

                size = bits.Position / 8 + granule.Spike.Length * 2 - offset;
            }

            if (size <= 0)
                break;

            if (granule != null)
                granules.Add(granule);

            offset += size;
        }

        if (granules.Count == 0)
            return ([], 0, 0, []);

        List<(Granule First, Granule Second)> frames = [];

        for (int i = 0; i < granules.Count; i++)
        {
            Granule first = granules[i];
            Granule second = null;

            if (first.Mpeg1)
            {
                second = i + 1 < granules.Count && granules[i + 1].Index == 1
                    ? granules[++i]
                    : new()
                    {
                        Scfsi = new uint[first.Channels],
                        Side = new (uint, uint, uint)[first.Channels]
                    };
            }

            frames.Add((first, second));
        }

        // A silent frame to flush the Windows decoder since it holds back a frame
        Granule format = granules[0];
        Granule silence() => new()
        {
            Version = format.Version,
            RateIndex = format.RateIndex,
            ChannelMode = format.ChannelMode,
            Scfsi = new uint[format.Channels],
            Side = new (uint, uint, uint)[format.Channels]
        };
        frames.Add((silence(), format.Mpeg1
            ? silence()
            : null));

        bool mpeg1 = format.Mpeg1, mono = format.Channels == 1;
        int bitrate = mpeg1
            ? 320
            : 160;
        int frameSize = (mpeg1
            ? 144
            : 72) * bitrate * 1000 / mpegRates[format.Version][format.RateIndex];
        int sideSize = mpeg1
            ? mono
                ? 17
                : 32
            : mono
                ? 9
                : 17;
        int slots = frameSize - 4 - sideSize, reservoir = mpeg1
            ? 511
            : 255;
        byte[] mp3 = new byte[frames.Count * frameSize];
        List<(int At, short[] Pcm)> spikes = [];
        int position = 0; // Where the next main data goes in the frames' slots

        for (int k = 0; k < frames.Count; k++)
        {
            (Granule first, Granule second) = frames[k];
            Granule[] parts = second == null
                ? [first]
                : [first, second];

            for (int p = 0; p < parts.Length; p++)
                if (parts[p].Spike.Length > 0)
                    spikes.Add(((k * parts.Length + p) * 576 + parts[p].SpikeAt, parts[p].Spike));

            BitWriter main = new();

            foreach (Granule granule in parts)
            {
                BitReader data = new(granule.Source, granule.DataBit);

                foreach ((uint size, _, _) in granule.Side)
                    for (uint b = 0; b < size; b++)
                        main.Write(1, data.Read(1));
            }

            main.Align();

            int slotStart = k * slots;
            position = Math.Max(position, slotStart - reservoir);

            if (position + main.Bytes.Count > slotStart + slots)
                throw new InvalidDataException("An EALayer3 frame is too big for an MP3 frame.");

            BitWriter header = new();
            header.Write(11, 0x7FF);
            header.Write(2, (uint)first.Version);
            header.Write(2, 1); // Layer III
            header.Write(1, 1); // No CRC
            header.Write(4, 14); // 320 or 160 kbps
            header.Write(2, (uint)first.RateIndex);
            header.Write(2, 0); // Padding, private
            header.Write(2, (uint)first.ChannelMode);
            header.Write(2, (uint)first.ModeExtension);
            header.Write(4, 0b0100); // Not copyrighted, original, no emphasis
            header.Write(mpeg1
                ? 9
                : 8, (uint)(slotStart - position));
            header.Write(mpeg1
                ? mono
                    ? 5
                    : 3
                : mono
                    ? 1
                    : 2, 0);

            if (mpeg1)
                foreach (uint scfsi in second.Scfsi)
                    header.Write(4, scfsi);

            foreach (Granule granule in parts)
                foreach ((uint size, uint side1, uint side2) in granule.Side)
                {
                    header.Write(12, size);
                    header.Write(32, side1);
                    header.Write(mpeg1
                        ? 15
                        : 19, side2);
                }

            header.Bytes.CopyTo(mp3, k * frameSize);

            foreach (byte b in main.Bytes)
            {
                mp3[position / slots * frameSize + 4 + sideSize + position % slots] = b;
                position++;
            }
        }

        return (mp3, frameSize, bitrate * 1000, spikes);
    }

    private static short[] DecodeMp3(byte[] mp3, int rate, int channels, int frameSize, int bitrate)
    {
        if (mp3.Length == 0)
            return [];

        List<short> pcm = [];
        byte[] buffer = new byte[1152 * channels * 2 * 2];
        using AcmMp3FrameDecompressor decoder = new(new Mp3WaveFormat(rate, channels, frameSize, bitrate));
        using MemoryStream stream = new(mp3);

        while (Mp3Frame.LoadFromStream(stream) is Mp3Frame frame)
        {
            int length = decoder.DecompressFrame(frame, buffer, 0);

            for (int i = 0; i + 1 < length; i += 2)
                pcm.Add(BitConverter.ToInt16(buffer, i));
        }

        return [.. pcm];
    }

    private static bool Zero(float value) => Math.Abs(value) <= Epsilon;

    // GetEnvelopeVolume in the way the game computes it
    public static float Envelope(Splicer.SampleRef sampleRef, float time)
    {
        if (!Zero(sampleRef.FadeIn) && time < sampleRef.FadeIn)
            return 1 - MathF.Cos(time / sampleRef.FadeIn);

        if (!Zero(sampleRef.FadeOut) && time > sampleRef.FadeOut)
            return Math.Max(0, 1 - MathF.Cos(1 - (time - sampleRef.FadeOut) / (sampleRef.Duration - sampleRef.FadeOut)));

        return 1;
    }

    public static float[] Render(Splicer.Splice splice, Func<Splicer.Sample, Decoded> decode, Random random, float maxSeconds = 120)
    {
        List<(Splicer.SampleRef Ref, Decoded Sound, float Volume, float Pitch, float Delay, float Cutoff, double Step)> voices = [];

        foreach (Splicer.SampleRef sampleRef in splice.SampleRefs.Where(r => r.Sample != null))
        {
            // rand() * (1 / 16383.5) - 1, and rand() / 32767
            float spread = random.Next(32768) / 16383.5f - 1;
            float volume = spread <= 0
                ? spread * (1 - sampleRef.RND_Vol) + 1
                : Zero(sampleRef.RND_Vol)
                    ? 4
                    : (1 / sampleRef.RND_Vol - 1) * spread + 1;
            float pitch = random.Next(32768) / 32767f * sampleRef.RND_Pitch + sampleRef.Pitch;
            Decoded sound = decode(sampleRef.Sample);
            float cutoff = pitch > 0
                ? sampleRef.Pitch / pitch * sampleRef.Duration
                : 0;
            voices.Add((sampleRef, sound, volume, pitch, Zero(sampleRef.Offset)
                ? 0
                : sampleRef.Offset, cutoff, (double)sound.Rate * pitch / OutputRate));
        }

        float seconds = voices.Select(v => v.Delay + (v.Sound.LoopStart >= 0 || v.Step <= 0
                ? v.Cutoff
                : Math.Min(v.Cutoff, (float)(v.Sound.Frames / v.Step / OutputRate))))
            .DefaultIfEmpty(0)
            .Max();
        float[] mix = new float[(int)(Math.Clamp(seconds, 0, maxSeconds) * OutputRate) * 2];

        foreach ((Splicer.SampleRef sampleRef, Decoded sound, float volume, float pitch, float delay, float cutoff, double step) in voices)
        {
            bool stereo = Math.Abs(sampleRef.Az - StereoAzimuth) < 0.000015258789f;
            float pan = (MathF.Sin(sampleRef.Az * MathF.PI / 180) + 1) * MathF.PI / 4;
            float left = stereo
                ? 1
                : MathF.Cos(pan), right = stereo
                ? 1
                : MathF.Sin(pan);
            float gain = splice.Volume * sampleRef.Volume * volume;
            int start = (int)(delay * OutputRate), frames = sound.Frames, channels = sound.Channels;

            for (int i = 0; 2 * (start + i) + 1 < mix.Length; i++)
            {
                float time = i / (float)OutputRate;

                if (time > cutoff)
                    break;

                double at = i * step;

                if (at >= frames - 1)
                {
                    if (sound.LoopStart < 0 || sound.LoopStart >= frames - 1)
                        break;

                    at = sound.LoopStart + (at - sound.LoopStart) % (frames - 1 - sound.LoopStart);
                }

                int whole = (int)at;
                float fraction = (float)(at - whole);

                float channel(int c) => sound.Pcm[whole * channels + c] * (1 - fraction) + sound.Pcm[(whole + 1) * channels + c] * fraction;

                float level = gain * Envelope(sampleRef, time);
                float first = channel(0), second = channels > 1
                    ? channel(1)
                    : first;

                if (!stereo)
                    first = second = (first + second) / 2;

                mix[2 * (start + i)] += first * left * level;
                mix[2 * (start + i) + 1] += second * right * level;
            }
        }

        return mix;
    }

    // 16-bit PCM WAV, clipping like the output would
    public static byte[] Wav(float[] samples, int channels, int rate)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + samples.Length * 2);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(rate);
        writer.Write(rate * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(samples.Length * 2);

        foreach (float sample in samples)
            writer.Write((short)(Math.Clamp(sample, -1, 1) * 32767));

        writer.Flush();
        return stream.ToArray();
    }
}
