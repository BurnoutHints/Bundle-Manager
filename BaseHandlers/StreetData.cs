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
    public class ScoreList
    {
        public int[] maScores; //0x0	0x8	int32_t[2] maScores

        public void Read(BinaryReader2 br)
        {
            maScores = new int[2];
            maScores[0] = br.ReadInt32();
            maScores[1] = br.ReadInt32();
        }
    }

    public class ChallengeData
    {
        public byte[] mDirty;//0x0	0x8	BitArray<2u> mDirty		
        public byte[] mValidScore;// 0x8	0x8	BitArray<2u> mValidScores		
        public ScoreList mScoreList; //0x10	0x8	ScoreList mScoreList      ScoreList format

        public void Read(BinaryReader2 br)
        {
            mDirty = br.ReadBytes(8);
            mValidScore = br.ReadBytes(8);
            mScoreList = new ScoreList();
            mScoreList.Read(br);
        }
    }

    public class Exit
    {
        public short mSpan; //0x0	0x2	SpanIndex	mSpan
        public byte[] padding; //0x2	0x2			padding
        public float mrAngle; //0x4	0x4	float_t	mrAngle	

        public void Read(BinaryReader2 br)
        {
            mSpan = br.ReadInt16();
            padding = br.ReadBytes(2);
            mrAngle = br.ReadSingle();
        }

    }

    public class AIInfo
    {
        public byte muMaxSpeedMPS;
        public byte muMinSpeedMPS;

        public void Read(BinaryReader2 br)
        {
            muMaxSpeedMPS = br.ReadByte();
            muMinSpeedMPS = br.ReadByte();
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
    }

    public class Street
    {

        public SpanBase super_SpanBase; //0x0	0xC	SpanBase super_SpanBase      SpanBase format
        public AIInfo mAiInfo; //0xC	0x2	AIInfo mAIInfo     AIInfo format
        public byte[] padding; //0xE	0x2			padding

    }

    public class Junction
    {
        public SpanBase super_SpanBase;  //0x0	0xC	SpanBase super_SpanBase      SpanBase format
        public int mpaExits; //0xC	0x4	Exit* mpaExits        Exit format
        public int miExitCount; //0x10	0x4	int32_t miExitCount		
        public string macName; //0x14	0x10	char[16] macName

        public List<Exit> exits;
        public Junction()
        {
            exits = new List<Exit>();
        }
    }

    public class Road
    {
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
    }

    public class ChallengeParScores
    {
        public ChallengeData challengeData; //0x0	0x18	ChallengeData	super_ChallengeData		ChallengeData format
        public long[] mRivals; //0x18	0x10	CgsID[2]	mRivals
    }

    public class StreetData : IEntryData
    {
        public int miVersion; // 0x0	0x4	int32_t miVersion		6 in 1.4+
        private int miSize; //0x4	0x4	int32_t miSize	
        public int mpaStreets; //0x8	0x4	Street* mpaStreets      Street format
        private int mpaJunctions; // 0xC	0x4	Junction* mpaJunctions        Junction format
        private int mpaRoads; //0x10	0x4	Road* mpaRoads        Road format
        private int mpaChallengeParScores; //0x14	0x4	ChallengeParScoresEntry* mpaChallengeParScores       ChallengeParScoresEntry format
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
                SpanBase spanBase = new SpanBase();
                spanBase.Read(br);
                AIInfo info = new AIInfo();
                info.Read(br);
                street.super_SpanBase = spanBase;
                street.mAiInfo = info;
                street.padding = br.ReadBytes(2);
                streets.Add(street);
            }

            br.BaseStream.Position = mpaJunctions;

            for (int i = 0; i < miJunctionCount; i++)
            {
                Junction junction = new Junction();
                SpanBase spanBase = new SpanBase();
                spanBase.Read(br);
                junction.mpaExits = br.ReadInt32();
                junction.miExitCount = br.ReadInt32();
                junction.macName = new string(br.ReadChars(16));
                long oldPosition = br.BaseStream.Position;
                br.BaseStream.Position = junction.mpaExits;
                Console.Out.WriteLine(junction.miExitCount);

                for (int j = 0; j < junction.miExitCount; j++)
                {
                    Exit exit = new Exit();
                    exit.Read(br);
                    junction.exits.Add(exit);
                }
                br.BaseStream.Position = oldPosition;
                junction.super_SpanBase = spanBase;
                junctions.Add(junction);
            }

            br.BaseStream.Position = mpaRoads;

            for (int i = 0; i < miRoadCount; i++)
            {
                Road road = new Road();
                road.mReferencePosition = br.ReadVector3F();
                road.mpaSpans = br.ReadInt32();
                road.mId = br.ReadInt64();
                road.miRoadLimitId0 = br.ReadInt64();
                road.miRoadLimitId1 = br.ReadInt64();
                road.macDebugName = Encoding.ASCII.GetString(br.ReadBytes(16));
                road.mChallenge = br.ReadInt32();
                road.miSpanCount = br.ReadInt32();
                road.unknown = br.ReadInt32();
                road.padding = br.ReadBytes(4);
                long oldPosition = br.BaseStream.Position;
                br.BaseStream.Position = road.mpaSpans;
                for (int j = 0; j < road.miSpanCount; j++)
                {
                    road.spans.Add(br.ReadInt32());
                }
                br.BaseStream.Position = oldPosition;
                roads.Add(road);
            }

            br.BaseStream.Position = mpaChallengeParScores;

            for (int i = 0; i < miJunctionCount; i++)
            {
                ChallengeParScores score = new ChallengeParScores();
                ChallengeData data = new ChallengeData();
                data.Read(br);
                score.mRivals = new long[2];
                score.mRivals[0] = br.ReadInt64();
                score.mRivals[1] = br.ReadInt64();
                challenges.Add(score);
            }

            return true;
        }

        public bool Write(BundleEntry entry)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

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
            return null;
        }
    }
}
