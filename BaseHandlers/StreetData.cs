using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BundleFormat;
using BundleUtilities;
using MathLib;
using OpenTK;
using PluginAPI;

namespace BaseHandlers
{

    public interface ByteSerializable
    {
        void Read(BinaryReader2 br);

        byte[] ToBytes();

    }

    //48-4992 Street
    //5020-12796 Junction
    //12832-18232 Road
    //18304-21304 ChallengePar
    //21344-23840 Spans
    //23856-29552 Exits(8) - Array Padding (Must be dividable by 16)
    public class ScoreList : ByteSerializable
    {
        public int[] maScores; //0x0	0x8	int32_t[2] maScores

        public void Read(BinaryReader2 br)
        {
            maScores = new int[2];
            maScores[0] = br.ReadInt32();
            maScores[1] = br.ReadInt32();
        }

        public byte[] ToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(BitConverter.GetBytes(maScores[0]));
            bytes.Add(BitConverter.GetBytes(maScores[1]));
            return bytes.SelectMany(i => i).ToArray();
        }
    }

    public class ChallengeData : ByteSerializable
    {
        public long position;
        public byte[] mDirty;//0x0	0x8	BitArray<2u> mDirty		
        public byte[] mValidScore;// 0x8	0x8	BitArray<2u> mValidScores		
        public ScoreList mScoreList; //0x10	0x8	ScoreList mScoreList      ScoreList format

        public void Read(BinaryReader2 br)
        {
            position = br.BaseStream.Position;
            mDirty = br.ReadBytes(8);
            mValidScore = br.ReadBytes(8);
            mScoreList = new ScoreList();
            mScoreList.Read(br);
        }
        public byte[] ToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(mDirty);
            bytes.Add(mValidScore);
            bytes.Add(mScoreList.ToBytes());
            return bytes.SelectMany(i => i).ToArray();
        }

    }

    public class Exit : ByteSerializable
    {
        public long position;
        public int byteLength = 0;
        public short mSpan; //0x0	0x2	SpanIndex	mSpan
        public byte[] padding; //0x2	0x2			padding
        public float mrAngle; //0x4	0x4	float_t	mrAngle	

        public void Read(BinaryReader2 br)
        {
            position = br.BaseStream.Position;
            mSpan = br.ReadInt16();
            padding = br.ReadBytes(2);
            mrAngle = br.ReadSingle();
            byteLength = ToBytes().Count();
        }

        public byte[] ToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(BitConverter.GetBytes(mSpan));
            bytes.Add(padding);
            bytes.Add(BitConverter.GetBytes(mrAngle));
            return bytes.SelectMany(i => i).ToArray();
        }

    }

    public class AIInfo : ByteSerializable
    {
        public byte muMaxSpeedMPS;
        public byte muMinSpeedMPS;

        public void Read(BinaryReader2 br)
        {
            muMaxSpeedMPS = br.ReadByte();
            muMinSpeedMPS = br.ReadByte();
        }

        public byte[] ToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(BitConverter.GetBytes(muMaxSpeedMPS));
            bytes.Add(BitConverter.GetBytes(muMinSpeedMPS));
            return bytes.SelectMany(i => i).ToArray();
        }

    }

    public enum ESpanType
    {
        Street = 0,
        Junction = 1,
        Span_Type_Count = 2,
    }
    public class SpanBase : ByteSerializable
    {
        public int miRoadIndex;// 0x0	0x4	RoadIndex	miRoadIndex					
        public short miSpanIndex; //0x4	0x2	SpanIndex	miSpanIndex	
        public byte[] padding; //0x6	0x2			padding
        public ESpanType meSpanType; //0x8	0x4	ESpanType	meSpanType	

        public void Read(BinaryReader2 br)
        {
            miRoadIndex = br.ReadInt32();
            miSpanIndex = br.ReadInt16();
            padding = br.ReadBytes(2);
            meSpanType = (ESpanType)br.ReadInt32();
        }

        public byte[] ToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(BitConverter.GetBytes(miRoadIndex));
            bytes.Add(BitConverter.GetBytes(miSpanIndex));
            bytes.Add(padding);
            bytes.Add(BitConverter.GetBytes((int)meSpanType));
            return bytes.SelectMany(i => i).ToArray();
        }

    }

    public class Street : ByteSerializable
    {
        public long position;
        public SpanBase super_SpanBase; //0x0	0xC	SpanBase super_SpanBase      SpanBase format
        public AIInfo mAiInfo; //0xC	0x2	AIInfo mAIInfo     AIInfo format
        public byte[] padding; //0xE	0x2			padding

        public void Read(BinaryReader2 br)
        {
            position = br.BaseStream.Position;
            SpanBase spanBase = new SpanBase();
            spanBase.Read(br);
            AIInfo info = new AIInfo();
            info.Read(br);
            super_SpanBase = spanBase;
            mAiInfo = info;
            padding = br.ReadBytes(2);
        }
        public byte[] ToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(super_SpanBase.ToBytes());
            bytes.Add(mAiInfo.ToBytes());
            bytes.Add(padding);
            return bytes.SelectMany(i => i).ToArray();
        }

    }

    public class Junction : ByteSerializable
    {
        public long position;
        public SpanBase super_SpanBase;  //0x0	0xC	SpanBase super_SpanBase      SpanBase format
        public int mpaExits; //0xC	0x4	Exit* mpaExits        Exit format
        public int miExitCount; //0x10	0x4	int32_t miExitCount		
        public string macName; //0x14	0x10	char[16] macName

        public List<Exit> exits;
        public Junction()
        {
            exits = new List<Exit>();
        }

        public void Read(BinaryReader2 br)
        {
            SpanBase spanBase = new SpanBase();
            spanBase.Read(br);
            position = br.BaseStream.Position;
            mpaExits = br.ReadInt32();
            miExitCount = br.ReadInt32();
            macName = new string(br.ReadChars(16));
            long oldPosition = br.BaseStream.Position;
            br.BaseStream.Position = mpaExits;
            for (int j = 0; j < miExitCount; j++)
            {
                Exit exit = new Exit();
                exit.Read(br);
                exits.Add(exit);
            }
            br.BaseStream.Position = oldPosition;
            super_SpanBase = spanBase;
        }
        public byte[] ToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(super_SpanBase.ToBytes());
            bytes.Add(BitConverter.GetBytes(mpaExits)); //Calculate this 
            bytes.Add(BitConverter.GetBytes(miExitCount));
            bytes.Add(Encoding.ASCII.GetBytes(macName.PadRight(16).Substring(0, 16).ToCharArray()));
            bytes.Add(BitConverter.GetBytes(miExitCount));
            return bytes.SelectMany(i => i).ToArray();
        }

        public byte[] ExitsToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            foreach (Exit exit in exits)
            {
                bytes.Add(exit.ToBytes());
            }
            //Add padding
            if (bytes.SelectMany(i => i).Count() % 16 != 0)
            {
                bytes.Add(new byte[bytes.SelectMany(i => i).Count() + (16 - (bytes.SelectMany(i => i).Count() % 16))]);
            }
            return bytes.SelectMany(i => i).ToArray();
        }

    }

    public class Road : ByteSerializable
    {
        public long position;
        public Vector3 mReferencePosition; //0x0	0xC	Vector3	mReferencePosition
        public int mpaSpans; //0xC	0x4	SpanIndex* mpaSpans	
        public long mId; //0x10	0x8	GameDB ID   mId	
        public long miRoadLimitId0; //0x18	0x8	GameDB ID   miRoadLimitId0	
        public long miRoadLimitId1; //0x20	0x8	GameDB ID   miRoadLimitId1	
        public string macDebugName; //0x28	0x10	char[16] macDebugName	
        public int mChallenge; //0x38	0x4	ChallengeIndex mChallenge	
        public int miSpanCount; //0x3C	0x4	int32_t miSpanCount
        public int unknown;  // PC Only, 0x40	0x4	int32_t Always 1
        public byte[] padding; //0x40	0x4			padding


        public List<int> spans;

        public Road()
        {
            spans = new List<int>();
        }

        public byte[] ToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(BitConverter.GetBytes(mReferencePosition.X));
            bytes.Add(BitConverter.GetBytes(mReferencePosition.Y));
            bytes.Add(BitConverter.GetBytes(mReferencePosition.Z));
            bytes.Add(BitConverter.GetBytes(mpaSpans)); //Calculate this
            bytes.Add(BitConverter.GetBytes(mId));
            bytes.Add(BitConverter.GetBytes(miRoadLimitId0));
            bytes.Add(BitConverter.GetBytes(miRoadLimitId1));
            bytes.Add(Encoding.ASCII.GetBytes(macDebugName.PadRight(16).Substring(0, 16).ToCharArray()));
            bytes.Add(BitConverter.GetBytes(mChallenge));
            bytes.Add(BitConverter.GetBytes(miSpanCount));
            bytes.Add(BitConverter.GetBytes(unknown));
            bytes.Add(padding);
            return bytes.SelectMany(i => i).ToArray();
        }

        public byte[] SpansToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            foreach (int span in spans)
            {
                bytes.Add(BitConverter.GetBytes(span));
            }
            return bytes.SelectMany(i => i).ToArray();
        }

        public void Read(BinaryReader2 br)
        {
            position = br.BaseStream.Position;
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
            long oldPosition = br.BaseStream.Position;
            br.BaseStream.Position = mpaSpans;
            for (int j = 0; j < miSpanCount; j++)
            {
                spans.Add(br.ReadInt32());
            }
            br.BaseStream.Position = oldPosition;
        }
    }

    public class ChallengeParScores : ByteSerializable
    {
        public long position;
        public ChallengeData challengeData; //0x0	0x18	ChallengeData	super_ChallengeData		ChallengeData format
        public long[] mRivals; //0x18	0x10	CgsID[2]	mRivals

        public void Read(BinaryReader2 br)
        {
            position = br.BaseStream.Position;
            ChallengeData data = new ChallengeData();
            challengeData = data;
            data.Read(br);
            mRivals = new long[2];
            mRivals[0] = br.ReadInt64();
            mRivals[1] = br.ReadInt64();
        }

        public byte[] ToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(challengeData.ToBytes());
            bytes.Add(BitConverter.GetBytes(mRivals[0]));
            bytes.Add(BitConverter.GetBytes(mRivals[1]));
            return bytes.SelectMany(i => i).ToArray();
        }
    }

    public class StreetData : IEntryData
    {
        public int miVersion; // 0x0	0x4	int32_t miVersion		6 in 1.4+
        private int miSize; //0x4	0x4	int32_t miSize	
        public int mpaStreets; //0x8	0x4	Street* mpaStreets      Street format
        public int mpaJunctions; // 0xC	0x4	Junction* mpaJunctions        Junction format
        public int mpaRoads; //0x10	0x4	Road* mpaRoads        Road format
        public int mpaChallengeParScores; //0x14	0x4	ChallengeParScoresEntry* mpaChallengeParScores       ChallengeParScoresEntry format
        private int miStreetCount; //0x18	0x4	int32_t miStreetCount	
        private int miJunctionCount; //0x1C	0x4	int32_t miJunctionCount		
        private int miRoadCount; //0x20	0x4	int32_t miRoadCount
        public List<Street> streets;
        public List<Junction> junctions;
        public List<Road> roads;
        public List<ChallengeParScores> challenges;

        public StreetData()
        {
            streets = new List<Street>();
            junctions = new List<Junction>();
            roads = new List<Road>();
            challenges = new List<ChallengeParScores>();
        }

        public int GetSizeHeader()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(BitConverter.GetBytes(miVersion));
            bytes.Add(BitConverter.GetBytes(miSize));
            bytes.Add(BitConverter.GetBytes(mpaStreets)); 
            bytes.Add(BitConverter.GetBytes(mpaJunctions)); 
            bytes.Add(BitConverter.GetBytes(mpaRoads)); 
            bytes.Add(BitConverter.GetBytes(mpaChallengeParScores));
            bytes.Add(BitConverter.GetBytes(streets.Count()));
            bytes.Add(BitConverter.GetBytes(junctions.Count()));
            bytes.Add(BitConverter.GetBytes(roads.Count()));
            return bytes.SelectMany(i => i).ToArray().Count();
        }

        public int GetSize()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(BitConverter.GetBytes(miVersion));
            bytes.Add(BitConverter.GetBytes(miSize));
            bytes.Add(BitConverter.GetBytes(mpaStreets)); 
            bytes.Add(BitConverter.GetBytes(mpaJunctions)); 
            bytes.Add(BitConverter.GetBytes(mpaRoads)); 
            bytes.Add(BitConverter.GetBytes(mpaChallengeParScores));
            bytes.Add(BitConverter.GetBytes(streets.Count()));
            bytes.Add(BitConverter.GetBytes(junctions.Count()));
            bytes.Add(BitConverter.GetBytes(roads.Count()));
            bytes.Add(streets.SelectMany(i => i.ToBytes()).ToArray());
            bytes.Add(junctions.SelectMany(i => i.ToBytes()).ToArray());
            bytes.Add(roads.SelectMany(i => i.ToBytes()).ToArray());
            bytes.Add(challenges.SelectMany(i => i.ToBytes()).ToArray());
            bytes.Add(roads.SelectMany(i => i.SpansToBytes()).ToArray());
            bytes.Add(junctions.SelectMany(i => i.ExitsToBytes()).ToArray());
            return bytes.SelectMany(i => i).ToArray().Count();
        }

        public int getSizeOf(IEnumerable<ByteSerializable> bytes)
        {
            return bytes.ToList().SelectMany(i => i.ToBytes()).ToArray().Count();
        }

        public byte[] ToBytes()
        {
            List<byte[]> bytes = new List<byte[]>();
            bytes.Add(BitConverter.GetBytes(miVersion));
            bytes.Add(BitConverter.GetBytes(GetSize())); //miSize
            bytes.Add(BitConverter.GetBytes(GetSizeHeader())); // mpaStreets
            bytes.Add(BitConverter.GetBytes(GetSizeHeader() + getSizeOf(streets.Cast<ByteSerializable>()))); //mpaJunctions
            bytes.Add(BitConverter.GetBytes(GetSizeHeader() + getSizeOf(streets.Cast<ByteSerializable>()) + getSizeOf(junctions.Cast<ByteSerializable>()))); //mpaRoads
            bytes.Add(BitConverter.GetBytes(GetSizeHeader() + getSizeOf(streets.Cast<ByteSerializable>()) + getSizeOf(junctions.Cast<ByteSerializable>()) + getSizeOf(roads.Cast<ByteSerializable>()))); //mpaChallengeParScores
            bytes.Add(BitConverter.GetBytes(streets.Count()));
            bytes.Add(BitConverter.GetBytes(junctions.Count()));
            bytes.Add(BitConverter.GetBytes(roads.Count()));
            bytes.Add(streets.SelectMany(i => i.ToBytes()).ToArray());
            bytes.Add(junctions.SelectMany(i => i.ToBytes()).ToArray());
            bytes.Add(roads.SelectMany(i => i.ToBytes()).ToArray());
            bytes.Add(challenges.SelectMany(i => i.ToBytes()).ToArray());
            bytes.Add(roads.SelectMany(i => i.SpansToBytes()).ToArray());
            bytes.Add(junctions.SelectMany(i => i.ExitsToBytes()).ToArray());
            return bytes.SelectMany(i => i).ToArray();
        }

        private void Clear()
        {
            miVersion = default;
            miSize = default;
            mpaStreets = default;
            mpaJunctions = default;
            mpaRoads = default;
            mpaChallengeParScores = default;
            miStreetCount = default;
            miJunctionCount = default;
            miRoadCount = default;

            streets.Clear();
            junctions.Clear();
            roads.Clear();
            challenges.Clear();
        }

        public bool Read(BundleEntry entry, ILoader loader = null)
        {
            Clear();

            MemoryStream ms = entry.MakeStream();
            BinaryReader2 br = new BinaryReader2(ms);
            br.BigEndian = entry.Console;

            miVersion = br.ReadInt32();
            miSize = br.ReadInt32();
            mpaStreets = br.ReadInt32();
            mpaJunctions = br.ReadInt32();
            mpaRoads = br.ReadInt32();
            mpaChallengeParScores = br.ReadInt32();
            miStreetCount = br.ReadInt32();
            miJunctionCount = br.ReadInt32();
            miRoadCount = br.ReadInt32();


            br.BaseStream.Position = mpaStreets;

            for (int i = 0; i < miStreetCount; i++)
            {
                Street street = new Street();
                street.Read(br);
                streets.Add(street);
            }

            br.BaseStream.Position = mpaJunctions;

            for (int i = 0; i < miJunctionCount; i++)
            {
                Junction junction = new Junction();
                junction.Read(br);
                junctions.Add(junction);
            }

            br.BaseStream.Position = mpaRoads;

            for (int i = 0; i < miRoadCount; i++)
            {
                Road road = new Road();
                road.Read(br);
                roads.Add(road);
            }

            br.BaseStream.Position = mpaChallengeParScores;

            for (int i = 0; i < miRoadCount; i++)
            {
                ChallengeParScores score = new ChallengeParScores();
                score.Read(br);
                challenges.Add(score);
            }

            return true;
        }

        public bool Write(BundleEntry entry)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Flush();

            bw.Write(ToBytes());

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
            return null;
        }
    }
}
