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
        // mpaExits / miExitCount are written as 0 by the writer because the
        // game's StreetData::FixUp() corrupts the per-junction exit arrays
        // (see Write() for details), so the on-disk values are unreliable.
        // Kept on the type so the reader can still inspect the original
        // pointer if a tool wants to.
        [Browsable(false)]
        public int mpaExits { get; set; }                              // 0xC 0x4 (32-bit)
        [Browsable(false)]
        public int miExitCount { get; set; }                           // 0x10 0x4
        public string macName { get; set; }                            // 0x14 0x10

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
            // Intentionally NOT reading the exit array. StreetData::FixUp()
            // in retail Burnout Paradise overwrites large parts of that
            // region (see StreetData.Write for the writeup), so the on-disk
            // bytes are not reliable Exit structs.
        }

        public void Write(BinaryWriter2 bw)
        {
            super_SpanBase.Write(bw);
            // Always emit zeroed pointer + count. The game ignores these
            // (the responsibilities live in AI Sections / Traffic Data /
            // Trigger Data / collision resources) and Burnout's FixUp bug
            // means the original on-disk values point at corrupt memory.
            bw.Write(0);
            bw.Write(0);
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
        // miSpanCount is written as 0; see Write() for the rationale.
        [Browsable(false)]
        public int miSpanCount { get; set; }            // 0x3C 0x4
        public int unknown { get; set; }                // 0x40 0x4  (spec: uint32, always 1)
        [Browsable(false)]
        public byte[] padding { get; set; } = new byte[4]; // 0x44 0x4

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
            // Intentionally NOT reading the per-road span array. See
            // StreetData.Write for the FixUp corruption details.
        }

        public void Write(BinaryWriter2 bw)
        {
            bw.Write(mReferencePosition.X);
            bw.Write(mReferencePosition.Y);
            bw.Write(mReferencePosition.Z);
            // Always emit zeroed pointer + count; see Junction.Write.
            bw.Write(0);
            bw.Write(mId);
            bw.Write(miRoadLimitId0);
            bw.Write(miRoadLimitId1);
            bw.Write(Junction.PadName(macDebugName, 16));
            bw.Write(mChallenge);
            bw.Write(0);
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

        private const int SIZEOF_CHALLENGE_PAR_SCORES_ENTRY = 0x28;

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
        }

        public bool Read(BundleEntry entry, ILoader loader = null)
        {
            Clear();

            MemoryStream ms = new MemoryStream(entry.EntryBlocks[0].Data);
            BinaryReader2 br = new BinaryReader2(ms);
            br.BigEndian = entry.Console;

            miVersion = br.ReadInt32();
            br.ReadInt32(); // miSize, recomputed on write
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

            // The header has no miChallengeCount; FixUp() in retail Burnout
            // Paradise constructs miJunctionCount BitArrays here even though
            // there are only miRoadCount real entries (this is a game bug -
            // it should iterate miRoadCount times). The on-disk allocation
            // matches the game allocation, so the file holds miRoadCount
            // entries here followed by partially-corrupt bytes.
            br.BaseStream.Position = mpaChallengeParScores;
            for (int i = 0; i < miRoadCount; i++)
            {
                ChallengeParScores c = new ChallengeParScores();
                c.Read(br);
                challenges.Add(c);
            }

            br.Close();
            ms.Close();
            return true;
        }

        public bool Write(BundleEntry entry)
        {
            // The spec has no explicit challenge count and the reader uses
            // miRoadCount, so an unbalanced model would silently corrupt.
            if (challenges.Count != roads.Count)
                throw new InvalidOperationException(
                    "StreetData.Write: challenges.Count (" + challenges.Count +
                    ") must equal roads.Count (" + roads.Count + ").");

            MemoryStream ms = new MemoryStream();
            BinaryWriter2 bw = new BinaryWriter2(ms);
            bw.BigEndian = entry.Console;

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

            bw.WritePadding();
            int streetsOffset = (int)ms.Position;
            for (int i = 0; i < streets.Count; i++) streets[i].Write(bw);

            bw.WritePadding();
            int junctionsOffset = (int)ms.Position;
            for (int i = 0; i < junctions.Count; i++) junctions[i].Write(bw);

            bw.WritePadding();
            int roadsOffset = (int)ms.Position;
            for (int i = 0; i < roads.Count; i++) roads[i].Write(bw);

            bw.WritePadding();
            int challengesOffset = (int)ms.Position;
            for (int i = 0; i < challenges.Count; i++) challenges[i].Write(bw);

            // The game's StreetData::FixUp() iterates miJunctionCount times
            // (a Burnout bug; it should be miRoadCount) when constructing
            // mDirty / mValidScores BitArrays inside ChallengeParScoresEntry.
            // The extra iterations write past the real challenge entries, so
            // the resource MUST extend at least to
            //
            //     mpaChallengeParScores + miJunctionCount * 0x28
            //
            // or FixUp() will trample whatever lives after the resource at
            // runtime. We pad with zeroes here to keep that memory writeable.
            //
            // Road span and junction exit arrays used to live in this region
            // in retail data, but the FixUp bug overwrites them, so the
            // values are unreliable and the game ignores them. We omit them
            // entirely - per-road / per-junction mpaSpans, miSpanCount,
            // mpaExits, miExitCount are all written as 0.
            int fixUpEnd = challengesOffset + junctions.Count * SIZEOF_CHALLENGE_PAR_SCORES_ENTRY;
            while (ms.Position < fixUpEnd) bw.Write((byte)0);
            int miSizeOut = (int)ms.Position;
            bw.WritePadding();

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

        public EntryType GetEntryType(BundleEntry entry)
        {
            return EntryType.StreetData;
        }

        public IEntryEditor GetEditor(BundleEntry entry)
        {
            StreetDataEditor editor = new StreetDataEditor();
            editor.Model = this;
            editor.EditEvent += () => Write(entry);
            return editor;
        }
    }
}
