using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using BundleFormat;
using BundleUtilities;
using MathLib;
using OpenTK.Mathematics;
using PluginAPI;

namespace BaseHandlers
{
    // Layout reference: https://burnout.wiki/wiki/Street_Data
    // 32-bit PC only. 64-bit (Remastered) and big-endian (X360/PS3) are not
    // yet handled.

    public class ScoreList
    {
        public int[] maScores { get; set; } = new int[2]; // 0x0 0x8  int32_t[2] maScores

        public int Score0
        {
            get => maScores[0];
            set => maScores[0] = value;
        }
        public int Score1
        {
            get => maScores[1];
            set => maScores[1] = value;
        }

        public void Read(BinaryReader2 br)
        {
            maScores[0] = br.ReadInt32();
            maScores[1] = br.ReadInt32();
        }

        public void Write(BinaryWriter2 bw)
        {
            bw.Write(maScores[0]);
            bw.Write(maScores[1]);
        }

        public override string ToString() => "[" + maScores[0] + ", " + maScores[1] + "]";
    }

    public class ChallengeData
    {
        public byte[] mDirty { get; set; } = new byte[8];       // 0x0  0x8  BitArray<2u> mDirty
        public byte[] mValidScore { get; set; } = new byte[8];  // 0x8  0x8  BitArray<2u> mValidScores
        public ScoreList mScoreList { get; set; } = new ScoreList(); // 0x10 0x8

        public void Read(BinaryReader2 br)
        {
            mDirty = br.ReadBytes(8);
            mValidScore = br.ReadBytes(8);
            mScoreList.Read(br);
        }

        public void Write(BinaryWriter2 bw)
        {
            bw.Write(EnsureLen(mDirty, 8));
            bw.Write(EnsureLen(mValidScore, 8));
            mScoreList.Write(bw);
        }

        private static byte[] EnsureLen(byte[] a, int n)
        {
            if (a != null && a.Length == n) return a;
            byte[] r = new byte[n];
            if (a != null) Array.Copy(a, r, Math.Min(a.Length, n));
            return r;
        }
    }

    public class Exit
    {
        public short mSpan { get; set; }   // 0x0 0x2  SpanIndex mSpan
        [Browsable(false)]
        public byte[] padding { get; set; } = new byte[2]; // 0x2 0x2
        public float mrAngle { get; set; } // 0x4 0x4  float

        public override string ToString() => "Exit(span=" + mSpan + ", angle=" + mrAngle + ")";

        public void Read(BinaryReader2 br)
        {
            mSpan = br.ReadInt16();
            padding = br.ReadBytes(2);
            mrAngle = br.ReadSingle();
        }

        public void Write(BinaryWriter2 bw)
        {
            bw.Write(mSpan);
            bw.Write(padding != null && padding.Length == 2 ? padding : new byte[2]);
            bw.Write(mrAngle);
        }
    }

    public class AIInfo
    {
        public byte muMaxSpeedMPS { get; set; } // 0x0 0x1
        public byte muMinSpeedMPS { get; set; } // 0x1 0x1

        public override string ToString() => "AI(max=" + muMaxSpeedMPS + ", min=" + muMinSpeedMPS + ")";

        public void Read(BinaryReader2 br)
        {
            muMaxSpeedMPS = br.ReadByte();
            muMinSpeedMPS = br.ReadByte();
        }

        public void Write(BinaryWriter2 bw)
        {
            bw.Write(muMaxSpeedMPS);
            bw.Write(muMinSpeedMPS);
        }
    }

    public enum ESpanType
    {
        Street = 0,
        Junction = 1,
        Span_Type_Count = 2,
    }

    public class SpanBase
    {
        public int miRoadIndex { get; set; }     // 0x0 0x4
        public short miSpanIndex { get; set; }   // 0x4 0x2
        [Browsable(false)]
        public byte[] padding { get; set; } = new byte[2]; // 0x6 0x2
        public ESpanType meSpanType { get; set; }// 0x8 0x4

        public override string ToString() =>
            meSpanType + "(road=" + miRoadIndex + ", span=" + miSpanIndex + ")";

        public void Read(BinaryReader2 br)
        {
            miRoadIndex = br.ReadInt32();
            miSpanIndex = br.ReadInt16();
            padding = br.ReadBytes(2);
            meSpanType = (ESpanType)br.ReadInt32();
        }

        public void Write(BinaryWriter2 bw)
        {
            bw.Write(miRoadIndex);
            bw.Write(miSpanIndex);
            bw.Write(padding != null && padding.Length == 2 ? padding : new byte[2]);
            bw.Write((int)meSpanType);
        }
    }

    public class Street
    {
        public SpanBase super_SpanBase { get; set; } = new SpanBase(); // 0x0 0xC
        public AIInfo mAiInfo { get; set; } = new AIInfo();            // 0xC 0x2
        [Browsable(false)]
        public byte[] padding { get; set; } = new byte[2];             // 0xE 0x2

        public int RoadIndex
        {
            get => super_SpanBase.miRoadIndex;
            set => super_SpanBase.miRoadIndex = value;
        }
        public short SpanIndex
        {
            get => super_SpanBase.miSpanIndex;
            set => super_SpanBase.miSpanIndex = value;
        }
        public ESpanType SpanType
        {
            get => super_SpanBase.meSpanType;
            set => super_SpanBase.meSpanType = value;
        }
        public byte MaxSpeedMPS
        {
            get => mAiInfo.muMaxSpeedMPS;
            set => mAiInfo.muMaxSpeedMPS = value;
        }
        public byte MinSpeedMPS
        {
            get => mAiInfo.muMinSpeedMPS;
            set => mAiInfo.muMinSpeedMPS = value;
        }

        public void Read(BinaryReader2 br)
        {
            super_SpanBase.Read(br);
            mAiInfo.Read(br);
            padding = br.ReadBytes(2);
        }

        public void Write(BinaryWriter2 bw)
        {
            super_SpanBase.Write(bw);
            mAiInfo.Write(bw);
            bw.Write(padding != null && padding.Length == 2 ? padding : new byte[2]);
        }
    }

    public class Junction
    {
        public SpanBase super_SpanBase { get; set; } = new SpanBase(); // 0x0 0xC
        [Browsable(false)]
        public int mpaExits { get; set; }                              // 0xC 0x4 (32-bit)
        public int miExitCount { get; set; }                           // 0x10 0x4
        public string macName { get; set; }                            // 0x14 0x10

        public List<Exit> exits { get; set; } = new List<Exit>();

        public int RoadIndex
        {
            get => super_SpanBase.miRoadIndex;
            set => super_SpanBase.miRoadIndex = value;
        }
        public short SpanIndex
        {
            get => super_SpanBase.miSpanIndex;
            set => super_SpanBase.miSpanIndex = value;
        }
        public ESpanType SpanType
        {
            get => super_SpanBase.meSpanType;
            set => super_SpanBase.meSpanType = value;
        }
        public string Name
        {
            get => (macName ?? string.Empty).TrimEnd('\0');
            set => macName = value ?? string.Empty;
        }

        public void Read(BinaryReader2 br)
        {
            super_SpanBase.Read(br);
            mpaExits = br.ReadInt32();
            miExitCount = br.ReadInt32();
            macName = Encoding.ASCII.GetString(br.ReadBytes(16));

            long savedPosition = br.BaseStream.Position;
            br.BaseStream.Position = mpaExits;
            exits.Clear();
            for (int j = 0; j < miExitCount; j++)
            {
                Exit exit = new Exit();
                exit.Read(br);
                exits.Add(exit);
            }
            br.BaseStream.Position = savedPosition;
        }

        public void Write(BinaryWriter2 bw)
        {
            super_SpanBase.Write(bw);
            bw.Write(mpaExits);
            bw.Write(miExitCount);
            bw.Write(PadName(macName, 16));
        }

        internal static byte[] PadName(string name, int len)
        {
            byte[] buf = new byte[len];
            if (name != null)
            {
                byte[] src = Encoding.ASCII.GetBytes(name);
                Array.Copy(src, buf, Math.Min(src.Length, len));
            }
            return buf;
        }
    }

    public class Road
    {
        public Vector3 mReferencePosition { get; set; } // 0x0 0xC
        [Browsable(false)]
        public int mpaSpans { get; set; }               // 0xC 0x4 (32-bit)
        public long mId { get; set; }                   // 0x10 0x8
        public long miRoadLimitId0 { get; set; }        // 0x18 0x8
        public long miRoadLimitId1 { get; set; }        // 0x20 0x8
        public string macDebugName { get; set; }        // 0x28 0x10
        public int mChallenge { get; set; }             // 0x38 0x4
        public int miSpanCount { get; set; }            // 0x3C 0x4
        public int unknown { get; set; }                // 0x40 0x4  (spec: uint32, always 1)
        [Browsable(false)]
        public byte[] padding { get; set; } = new byte[4]; // 0x44 0x4

        public List<int> spans { get; set; } = new List<int>();

        public string DebugName
        {
            get => (macDebugName ?? string.Empty).TrimEnd('\0');
            set => macDebugName = value ?? string.Empty;
        }
        public float RefX
        {
            get => mReferencePosition.X;
            set
            {
                Vector3 v = mReferencePosition;
                v.X = value;
                mReferencePosition = v;
            }
        }
        public float RefY
        {
            get => mReferencePosition.Y;
            set
            {
                Vector3 v = mReferencePosition;
                v.Y = value;
                mReferencePosition = v;
            }
        }
        public float RefZ
        {
            get => mReferencePosition.Z;
            set
            {
                Vector3 v = mReferencePosition;
                v.Z = value;
                mReferencePosition = v;
            }
        }

        public void Read(BinaryReader2 br)
        {
            mReferencePosition = br.ReadVector3F();
            mpaSpans = br.ReadInt32();
            mId = br.ReadInt64();
            miRoadLimitId0 = br.ReadInt64();
            miRoadLimitId1 = br.ReadInt64();
            macDebugName = Encoding.ASCII.GetString(br.ReadBytes(16));
            mChallenge = br.ReadInt32();
            miSpanCount = br.ReadInt32();
            unknown = br.ReadInt32();
            padding = br.ReadBytes(4);

            long savedPosition = br.BaseStream.Position;
            br.BaseStream.Position = mpaSpans;
            spans.Clear();
            for (int j = 0; j < miSpanCount; j++)
                spans.Add(br.ReadInt32());
            br.BaseStream.Position = savedPosition;
        }

        public void Write(BinaryWriter2 bw)
        {
            bw.Write(mReferencePosition.X);
            bw.Write(mReferencePosition.Y);
            bw.Write(mReferencePosition.Z);
            bw.Write(mpaSpans);
            bw.Write(mId);
            bw.Write(miRoadLimitId0);
            bw.Write(miRoadLimitId1);
            bw.Write(Junction.PadName(macDebugName, 16));
            bw.Write(mChallenge);
            bw.Write(miSpanCount);
            bw.Write(unknown);
            bw.Write(padding != null && padding.Length == 4 ? padding : new byte[4]);
        }
    }

    public class ChallengeParScores
    {
        public ChallengeData challengeData { get; set; } = new ChallengeData(); // 0x0 0x18
        public long[] mRivals { get; set; } = new long[2];                       // 0x18 0x10

        public int Score0
        {
            get => challengeData.mScoreList.maScores[0];
            set => challengeData.mScoreList.maScores[0] = value;
        }
        public int Score1
        {
            get => challengeData.mScoreList.maScores[1];
            set => challengeData.mScoreList.maScores[1] = value;
        }
        public long Rival0
        {
            get => mRivals[0];
            set => mRivals[0] = value;
        }
        public long Rival1
        {
            get => mRivals[1];
            set => mRivals[1] = value;
        }

        public void Read(BinaryReader2 br)
        {
            challengeData.Read(br);
            mRivals[0] = br.ReadInt64();
            mRivals[1] = br.ReadInt64();
        }

        public void Write(BinaryWriter2 bw)
        {
            challengeData.Write(bw);
            bw.Write(mRivals[0]);
            bw.Write(mRivals[1]);
        }
    }

    public class StreetData : IEntryData
    {
        public int miVersion;              // 0x0 0x4  (expected 6)
        public int mpaStreets;             // 0x8 0x4
        public int mpaJunctions;           // 0xC 0x4
        public int mpaRoads;               // 0x10 0x4
        public int mpaChallengeParScores;  // 0x14 0x4

        public List<Street> streets = new List<Street>();
        public List<Junction> junctions = new List<Junction>();
        public List<Road> roads = new List<Road>();
        public List<ChallengeParScores> challenges = new List<ChallengeParScores>();

        // Captured during Read so the writer can reproduce the spans + exits
        // tail verbatim, preserving shared span arrays.
        private byte[] _rawTail;
        private int _tailOffset;

        // Paradise stores miSize excluding trailing 16-byte alignment padding,
        // so we keep the original value rather than recompute it.
        private int _origMiSize;

        // Counts captured at Read time so the writer can detect a list edit
        // and force a tail rebuild.
        private int[] _origJunctionExitCounts;
        private int[] _origRoadSpanCounts;

        private const int SIZEOF_CHALLENGE = 0x28;

        private void Clear()
        {
            miVersion = 0;
            mpaStreets = 0;
            mpaJunctions = 0;
            mpaRoads = 0;
            mpaChallengeParScores = 0;
            streets.Clear();
            junctions.Clear();
            roads.Clear();
            challenges.Clear();
            _rawTail = null;
            _tailOffset = 0;
            _origMiSize = 0;
            _origJunctionExitCounts = null;
            _origRoadSpanCounts = null;
        }

        public bool Read(BundleEntry entry, ILoader loader = null)
        {
            Clear();

            byte[] raw = entry.EntryBlocks[0].Data;
            MemoryStream ms = new MemoryStream(raw);
            BinaryReader2 br = new BinaryReader2(ms);
            br.BigEndian = entry.Console;

            miVersion = br.ReadInt32();
            _origMiSize = br.ReadInt32();
            mpaStreets = br.ReadInt32();
            mpaJunctions = br.ReadInt32();
            mpaRoads = br.ReadInt32();
            mpaChallengeParScores = br.ReadInt32();
            int miStreetCount = br.ReadInt32();
            int miJunctionCount = br.ReadInt32();
            int miRoadCount = br.ReadInt32();

            br.BaseStream.Position = mpaStreets;
            for (int i = 0; i < miStreetCount; i++)
            {
                Street s = new Street();
                s.Read(br);
                streets.Add(s);
            }

            br.BaseStream.Position = mpaJunctions;
            for (int i = 0; i < miJunctionCount; i++)
            {
                Junction j = new Junction();
                j.Read(br);
                junctions.Add(j);
            }

            br.BaseStream.Position = mpaRoads;
            for (int i = 0; i < miRoadCount; i++)
            {
                Road r = new Road();
                r.Read(br);
                roads.Add(r);
            }

            // No explicit miChallengeCount in the spec; the count matches
            // miRoadCount on every real file we've seen.
            br.BaseStream.Position = mpaChallengeParScores;
            for (int i = 0; i < miRoadCount; i++)
            {
                ChallengeParScores c = new ChallengeParScores();
                c.Read(br);
                challenges.Add(c);
            }

            _origJunctionExitCounts = new int[junctions.Count];
            for (int i = 0; i < junctions.Count; i++)
                _origJunctionExitCounts[i] = junctions[i].exits.Count;
            _origRoadSpanCounts = new int[roads.Count];
            for (int i = 0; i < roads.Count; i++)
                _origRoadSpanCounts[i] = roads[i].spans.Count;

            int tailStart = ComputeTailStart();
            if (tailStart >= 0 && tailStart < raw.Length)
            {
                _tailOffset = tailStart;
                _rawTail = new byte[raw.Length - tailStart];
                Array.Copy(raw, tailStart, _rawTail, 0, _rawTail.Length);
            }
            else
            {
                _tailOffset = mpaChallengeParScores + challenges.Count * SIZEOF_CHALLENGE;
                _rawTail = new byte[0];
            }

            br.Close();
            ms.Close();
            return true;
        }

        // The tail starts at the lowest referenced offset across mpaSpans and
        // mpaExits, clamped to the end of the challenge section.
        private int ComputeTailStart()
        {
            int lower = int.MaxValue;
            for (int i = 0; i < roads.Count; i++)
                if (roads[i].mpaSpans > 0 && roads[i].mpaSpans < lower) lower = roads[i].mpaSpans;
            for (int i = 0; i < junctions.Count; i++)
                if (junctions[i].mpaExits > 0 && junctions[i].mpaExits < lower) lower = junctions[i].mpaExits;
            int minTail = mpaChallengeParScores + challenges.Count * SIZEOF_CHALLENGE;
            if (lower == int.MaxValue) return minTail;
            return Math.Max(lower, minTail);
        }

        public bool Write(BundleEntry entry)
        {
            // The spec has no explicit challenge count; the reader derives it
            // from miRoadCount, so an unbalanced model would silently corrupt.
            if (challenges.Count != roads.Count)
                throw new InvalidOperationException(
                    "StreetData.Write: challenges.Count (" + challenges.Count +
                    ") must equal roads.Count (" + roads.Count + ").");

            MemoryStream ms = new MemoryStream();
            BinaryWriter2 bw = new BinaryWriter2(ms);
            bw.BigEndian = entry.Console;

            // Header is written with placeholders and patched at the end.
            long headerPos = ms.Position;
            bw.Write(miVersion);
            bw.Write(0); // miSize
            bw.Write(0); // mpaStreets
            bw.Write(0); // mpaJunctions
            bw.Write(0); // mpaRoads
            bw.Write(0); // mpaChallengeParScores
            bw.Write(streets.Count);
            bw.Write(junctions.Count);
            bw.Write(roads.Count);

            PadTo(bw, 16);
            int streetsOffset = (int)ms.Position;
            for (int i = 0; i < streets.Count; i++) streets[i].Write(bw);

            // Junction.mpaExits lives at offset 0xC; track the absolute
            // stream position for each junction so it can be patched later.
            PadTo(bw, 16);
            int junctionsOffset = (int)ms.Position;
            long[] junctionExitFieldPos = new long[junctions.Count];
            for (int i = 0; i < junctions.Count; i++)
            {
                long start = ms.Position;
                junctionExitFieldPos[i] = start + 0xC;
                junctions[i].Write(bw);
            }

            // Same for Road.mpaSpans, also at offset 0xC.
            PadTo(bw, 16);
            int roadsOffset = (int)ms.Position;
            long[] roadSpanFieldPos = new long[roads.Count];
            for (int i = 0; i < roads.Count; i++)
            {
                long start = ms.Position;
                roadSpanFieldPos[i] = start + 0xC;
                roads[i].Write(bw);
            }

            PadTo(bw, 16);
            int challengesOffset = (int)ms.Position;
            for (int i = 0; i < challenges.Count; i++) challenges[i].Write(bw);

            // Tail (spans + exits). Preserve the captured raw blob when
            // no list mutations have happened (byte-exact, shared arrays
            // preserved); otherwise pack each road/junction array
            // sequentially and patch the sub-pointers.
            PadTo(bw, 16);

            int miSizeOut;

            bool canPreserveTail =
                _rawTail != null &&
                _rawTail.Length > 0 &&
                ms.Position <= _tailOffset;

            // Compare against the Read-time snapshot, not against
            // miExitCount / miSpanCount, which the caller could update
            // independently of the list.
            if (canPreserveTail &&
                (_origJunctionExitCounts == null ||
                 _origJunctionExitCounts.Length != junctions.Count))
            {
                canPreserveTail = false;
            }
            if (canPreserveTail)
            {
                for (int i = 0; i < junctions.Count; i++)
                {
                    if (junctions[i].exits.Count != _origJunctionExitCounts[i] ||
                        junctions[i].miExitCount != _origJunctionExitCounts[i])
                    {
                        canPreserveTail = false;
                        break;
                    }
                }
            }
            if (canPreserveTail &&
                (_origRoadSpanCounts == null ||
                 _origRoadSpanCounts.Length != roads.Count))
            {
                canPreserveTail = false;
            }
            if (canPreserveTail)
            {
                for (int i = 0; i < roads.Count; i++)
                {
                    if (roads[i].spans.Count != _origRoadSpanCounts[i] ||
                        roads[i].miSpanCount != _origRoadSpanCounts[i])
                    {
                        canPreserveTail = false;
                        break;
                    }
                }
            }

            if (canPreserveTail)
            {
                while (ms.Position < _tailOffset) bw.Write((byte)0);
                bw.Write(_rawTail);
                int totalSize = (int)ms.Position;
                miSizeOut = _origMiSize > 0 ? _origMiSize : totalSize;
            }
            else
            {
                int[] newRoadSpanOffsets = new int[roads.Count];
                for (int i = 0; i < roads.Count; i++)
                {
                    newRoadSpanOffsets[i] = (int)ms.Position;
                    for (int j = 0; j < roads[i].spans.Count; j++)
                        bw.Write(roads[i].spans[j]);
                }

                PadTo(bw, 16);
                int[] newJunctionExitOffsets = new int[junctions.Count];
                for (int i = 0; i < junctions.Count; i++)
                {
                    newJunctionExitOffsets[i] = (int)ms.Position;
                    for (int j = 0; j < junctions[i].exits.Count; j++)
                        junctions[i].exits[j].Write(bw);
                }

                // miSize excludes the trailing alignment pad.
                int tailEnd = (int)ms.Position;
                PadTo(bw, 16);
                miSizeOut = tailEnd;

                long savedPos = ms.Position;
                for (int i = 0; i < roads.Count; i++)
                {
                    ms.Position = roadSpanFieldPos[i];
                    bw.Write(newRoadSpanOffsets[i]);
                }
                for (int i = 0; i < junctions.Count; i++)
                {
                    ms.Position = junctionExitFieldPos[i];
                    bw.Write(newJunctionExitOffsets[i]);
                }
                ms.Position = savedPos;
            }

            ms.Position = headerPos + 4;
            bw.Write(miSizeOut);
            bw.Write(streetsOffset);
            bw.Write(junctionsOffset);
            bw.Write(roadsOffset);
            bw.Write(challengesOffset);

            bw.Flush();
            byte[] data = ms.ToArray();
            bw.Close();
            ms.Close();
            entry.EntryBlocks[0].Data = data;
            entry.Dirty = true;
            return true;
        }

        private static void PadTo(BinaryWriter2 bw, int alignment)
        {
            long pos = bw.BaseStream.Position;
            long pad = (alignment - (pos % alignment)) % alignment;
            for (long i = 0; i < pad; i++) bw.Write((byte)0);
        }

        public EntryType GetEntryType(BundleEntry entry)
        {
            return EntryType.StreetData;
        }

        public IEntryEditor GetEditor(BundleEntry entry)
        {
            return null;
        }
    }
}
