#:project ../WorldCollisionHandler/WorldCollisionHandler.csproj
#:project ../BaseHandlers/BaseHandlers.csproj
#:property TargetFramework=net10.0-windows
#:property UseWindowsForms=true
#:property PublishAot=false
#:property PublishTrimmed=false
using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using BaseHandlers;
using BundleFormat;
using WorldCollisionHandler;

// Rebuilds world collision from render meshes, preserving baseline surface data
// args: <build folder> <january WORLDCOL.BIN> <output file> [existing WORLDCOL.BIN to patch]
string feb = args[0].TrimEnd('\\') + "\\", janFile = args[1], output = args[2];

const ushort TypeRoad = 0x8000, TypeTerrain = 0x8400, TypeBuilding = 0x8800, TypeOther = 0x8C00;
const ushort NoSection = 0xFFFF, TopBit = 0x8000;
const int TrafficUnknown = 1, TrafficFirstAngle = 2, TrafficAngles = 14, TrafficVersion = 29, TrafficDataType = 0x10002;
const float LodError = 1f, LodSurface = 0.98f, MinDetailArea = 0.25f, MinBuildingEdge = 2f, MaxBuildingClearance = 30f, GroundCell = 8f, LodFootprint = 0.95f, LodHeight = 0.7f, ExactMatch = 0.04f, NearDistance = 2f, NearNormal = 0.6f, LaneCell = 8f, LaneHeight = 3f, TrafficStep = MathF.PI * 2 / 14, Cell = 4f, SectionCell = 50f;

uint U(byte[] b, int at) => BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(at));
ushort H(byte[] b, int at) => BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(at));
float F(byte[] b, int at) => BinaryPrimitives.ReadSingleBigEndian(b.AsSpan(at));
long Key(Vector3 v) => ((long)Math.Round(v.X * 20) & 0x1FFFFF) << 42 | ((long)Math.Round(v.Y * 20) & 0x1FFFFF) << 21 | ((long)Math.Round(v.Z * 20) & 0x1FFFFF);

var colNames = Regex.Matches(File.ReadAllText(feb + @"DEBUG\WORLDCOL.DBGINFO.XML"), "<entry name=\"TRK_COL_(\\d+)\" id=\"([0-9A-F]+)\"").ToDictionary(m => int.Parse(m.Groups[1].Value), m => Convert.ToUInt64(m.Groups[2].Value, 16));

// Baseline zone collision meshes
var jan = BundleArchive.Open(janFile);
var janZones = new Dictionary<int, List<Jan>>();
var janBox = new Dictionary<int, (Vector3 Lo, Vector3 Hi)>();
foreach (var (zone, id) in colNames)
{
    List<Jan> list = [];
    ClusteredMesh cm = new();
    cm.Read(jan.GetEntryByID(id), null);
    Vector3 lo = new(1e9f), hi = new(-1e9f);
    foreach (var c in cm.Clusters)
    {
        Vector3[] vs = [.. c.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z))];
        foreach (var f in c.Faces)
        {
            string[] tag = f.Material.Name.Split('_');
            list.Add(new(vs[f.Indices[0]], vs[f.Indices[1]], vs[f.Indices[2]], ushort.Parse(tag[1], NumberStyles.HexNumber)));
            foreach (int i in f.Indices) { lo = Vector3.Min(lo, vs[i]); hi = Vector3.Max(hi, vs[i]); }
        }
    }
    janZones[zone] = list;
    janBox[zone] = (lo, hi);
}
Console.WriteLine($"january: {janZones.Values.Sum(l => l.Count)} triangles");

// AI sections determine zone group tags
var ai = new AISections();
ai.Read(BundleArchive.Open(feb + "AI.DAT").Entries.First(e => e.Type == EntryType.AISections));
var sectionHeight = ai.maSections.Select(s => s.maPortals.Count > 0 ? s.maPortals.Average(p => p.mPosition.Y) : float.NaN).ToArray();
Dictionary<(int, int), List<int>> sectionGrid = [];
for (int s = 0; s < ai.maSections.Count; s++)
{
    var c = ai.maSections[s].maCorners;
    for (int x = (int)MathF.Floor(c.Min(v => v.X) / SectionCell); x <= (int)MathF.Floor(c.Max(v => v.X) / SectionCell); x++)
        for (int z = (int)MathF.Floor(c.Min(v => v.Y) / SectionCell); z <= (int)MathF.Floor(c.Max(v => v.Y) / SectionCell); z++)
        {
            if (!sectionGrid.TryGetValue((x, z), out var l)) sectionGrid[(x, z)] = l = [];
            l.Add(s);
        }
}

bool InSection(int s, float x, float z)
{
    var c = ai.maSections[s].maCorners;
    bool pos = false, neg = false;
    for (int i = 0; i < 4; i++)
    {
        var a = c[i]; var b = c[(i + 1) % 4];
        float cross = (b.X - a.X) * (z - a.Y) - (b.Y - a.Y) * (x - a.X);
        if (cross > 0) pos = true;
        if (cross < 0) neg = true;
    }
    return !(pos && neg);
}

ushort Section(Vector3 centre)
{
    if (!sectionGrid.TryGetValue(((int)MathF.Floor(centre.X / SectionCell), (int)MathF.Floor(centre.Z / SectionCell)), out var list)) return NoSection;
    int best = -1; float bestGap = float.MaxValue;
    foreach (int s in list)
    {
        if (!InSection(s, centre.X, centre.Z)) continue;
        float h = sectionHeight[s], gap = float.IsNaN(h) ? 1e6f : MathF.Abs(h - centre.Y);
        if (gap < bestGap) { bestGap = gap; best = s; }
    }
    return best < 0 ? NoSection : (ushort)(TopBit | best);
}

// Traffic lane flow directions for oncoming collision tags
byte[] laneData = BundleArchive.Open(feb + "B5TRAFFIC.BNDL").Entries.First(e => (uint)e.Type == TrafficDataType).GetPrimaryWithoutImports();
if (laneData[0] != TrafficVersion) throw new InvalidDataException($"Only traffic data version {TrafficVersion} is understood, not {laneData[0]}.");
Vector3 LanePoint(int at) => new(F(laneData, at), F(laneData, at + 4), F(laneData, at + 8));
List<Lane> lanes = [];
Dictionary<(int, int), List<int>> laneGrid = [];
for (int h = 0; h < H(laneData, 2); h++)
{
    int hull = (int)U(laneData, (int)U(laneData, 0xC) + h * 4), sections = (int)U(laneData, hull + 0xC), rungs = (int)U(laneData, hull + 0x10);
    for (int s = 0; s < laneData[hull]; s++)
    {
        int first = (int)U(laneData, sections + s * 44), count = laneData[sections + s * 44 + 4];
        for (int r = first; r + 1 < first + count; r++)
        {
            Vector3 l0 = LanePoint(rungs + r * 32), r0 = LanePoint(rungs + r * 32 + 16), l1 = LanePoint(rungs + r * 32 + 32), r1 = LanePoint(rungs + r * 32 + 48);
            Vector3 way = (l1 + r1 - l0 - r0) / 2;
            int index = lanes.Count;
            lanes.Add(new(l0, l1, r1, r0, (l0.Y + r0.Y + l1.Y + r1.Y) / 4, new Vector2(way.X, way.Z) / MathF.Max(new Vector2(way.X, way.Z).Length(), 1e-6f)));
            float[] xs = [l0.X, l1.X, r1.X, r0.X], zs = [l0.Z, l1.Z, r1.Z, r0.Z];
            for (int x = (int)MathF.Floor(xs.Min() / LaneCell); x <= (int)MathF.Floor(xs.Max() / LaneCell); x++)
                for (int z = (int)MathF.Floor(zs.Min() / LaneCell); z <= (int)MathF.Floor(zs.Max() / LaneCell); z++)
                {
                    if (!laneGrid.TryGetValue((x, z), out var l)) laneGrid[(x, z)] = l = [];
                    l.Add(index);
                }
        }
    }
}

static bool OnLane(Lane q, float x, float z)
{
    Vector3[] c = [q.A, q.B, q.C, q.D];
    bool pos = false, neg = false;
    for (int i = 0; i < 4; i++)
    {
        Vector3 a = c[i], b = c[(i + 1) % 4];
        float cross = (b.X - a.X) * (z - a.Z) - (b.Z - a.Z) * (x - a.X);
        if (cross > 0) pos = true;
        if (cross < 0) neg = true;
    }
    return !(pos && neg);
}

// Computes triangle traffic tag value, or -1 if outside lanes
int TrafficValue(Vector3 a, Vector3 b, Vector3 c)
{
    Vector3 g = (a + b + c) / 3;
    Vector2 sum = Vector2.Zero, first = Vector2.Zero;
    bool any = false, opposed = false;
    foreach (Vector3 p in (Vector3[])[g, (g + a) / 2, (g + b) / 2, (g + c) / 2])
    {
        if (!laneGrid.TryGetValue(((int)MathF.Floor(p.X / LaneCell), (int)MathF.Floor(p.Z / LaneCell)), out var list)) continue;
        foreach (int i in list)
        {
            Lane q = lanes[i];
            if (MathF.Abs(q.Height - p.Y) > LaneHeight || !OnLane(q, p.X, p.Z)) continue;
            if (!any) { first = q.Way; any = true; }
            if (Vector2.Dot(first, q.Way) < 0) opposed = true;
            sum += q.Way;
        }
    }
    if (!any) return -1;
    if (opposed || sum.LengthSquared() < 1e-6f) return TrafficUnknown;
    float angle = MathF.Atan2(sum.Y, sum.X);
    if (angle < 0) angle += MathF.PI * 2;
    return TrafficFirstAngle + (int)(angle / TrafficStep) % TrafficAngles;
}

static float Distance2(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
{
    Vector3 ab = b - a, ac = c - a, ap = p - a;
    float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
    if (d1 <= 0 && d2 <= 0) return ap.LengthSquared();
    Vector3 bp = p - b; float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
    if (d3 >= 0 && d4 <= d3) return bp.LengthSquared();
    float vc = d1 * d4 - d3 * d2;
    if (vc <= 0 && d1 >= 0 && d3 <= 0) return (ap - ab * (d1 / (d1 - d3))).LengthSquared();
    Vector3 cp = p - c; float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
    if (d6 >= 0 && d5 <= d6) return cp.LengthSquared();
    float vb = d5 * d2 - d1 * d6;
    if (vb <= 0 && d2 >= 0 && d6 <= 0) return (ap - ac * (d2 / (d2 - d6))).LengthSquared();
    float va = d3 * d6 - d5 * d4;
    if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0) return (bp - (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)))).LengthSquared();
    float den = 1 / (va + vb + vc);
    return (ap - ab * (vb * den) - ac * (vc * den)).LengthSquared();
}

static int Rank(string name)
{
    // Unnamed models default to terrain/ground classification
    if (name.Length == 0 || name.Contains("/Terrain/") || name.Contains("Terrain_")) return 1;
    if (name.Contains("/Roads/") || name.Contains(".RoadScene")) return 0;
    if (name.Contains("/Buildings/")) return 2;
    return 3;
}

// Pass 1: Zone geometry extraction and baseline collision matching
var zones = new Dictionary<int, List<Tri>>();
var modelIds = new Dictionary<string, int>();
var modelNames = new List<string>();
var modelTotal = new List<long>();
var modelMatched = new List<long>();

int Model(string name)
{
    string key = name.Split('?')[0];
    if (modelIds.TryGetValue(key, out int id)) return id;
    id = modelNames.Count;
    modelIds[key] = id;
    modelNames.Add(key);
    modelTotal.Add(0);
    modelMatched.Add(0);
    return id;
}

(List<Jan> Pool, Dictionary<long, List<int>> ByVertex) Nearby(Vector3 lo, Vector3 hi)
{
    var pool = janZones.Where(k => k.Value.Count > 0 && janBox[k.Key].Lo.X <= hi.X + 5 && janBox[k.Key].Hi.X >= lo.X - 5 && janBox[k.Key].Lo.Z <= hi.Z + 5 && janBox[k.Key].Hi.Z >= lo.Z - 5).SelectMany(k => k.Value).ToList();
    Dictionary<long, List<int>> byVertex = [];
    for (int i = 0; i < pool.Count; i++)
        foreach (Vector3 v in (Vector3[])[pool[i].A, pool[i].B, pool[i].C])
        {
            if (!byVertex.TryGetValue(Key(v), out var l)) byVertex[Key(v)] = l = [];
            l.Add(i);
        }
    return (pool, byVertex);
}

int Exact(List<Jan> pool, Dictionary<long, List<int>> byVertex, Vector3 a, Vector3 b, Vector3 c)
{
    bool Close(Vector3 p, Vector3 q) => Vector3.DistanceSquared(p, q) < ExactMatch * ExactMatch;
    for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
            for (int dz = -1; dz <= 1; dz++)
            {
                if (!byVertex.TryGetValue(Key(a + new Vector3(dx, dy, dz) * 0.05f), out var cand)) continue;
                foreach (int i in cand)
                {
                    Jan t = pool[i];
                    if ((Close(a, t.A) && Close(b, t.B) && Close(c, t.C)) || (Close(a, t.B) && Close(b, t.C) && Close(c, t.A)) || (Close(a, t.C) && Close(b, t.A) && Close(c, t.B))) return i;
                }
            }
    return -1;
}

void Extract(BundleEntry r, float[] m, int mid, List<Tri> into)
{
    byte[] rp = r.GetPrimaryWithoutImports(), s = r.EntryBlocks[1]?.Data;
    if (s == null) return;
    int meshes = H(rp, 0x12), mt = (int)U(rp, 0x14);
    Vector3 At(int vb, int stride, int idx)
    {
        int o = vb + idx * stride;
        float lx = F(s, o), ly = F(s, o + 4), lz = F(s, o + 8);
        return new(lx * m[0] + ly * m[4] + lz * m[8] + m[12], lx * m[1] + ly * m[5] + lz * m[9] + m[13], lx * m[2] + ly * m[6] + lz * m[10] + m[14]);
    }
    for (int j = 0; j < meshes; j++)
    {
        int me = (int)U(rp, mt + j * 4);
        int bv = (int)U(rp, me + 0x74), si = (int)U(rp, me + 0x78), ni = (int)U(rp, me + 0x7C), ib = (int)U(rp, me + 0xAC);
        int vb = (int)(U(rp, me + 0xD0) & ~3u), vs = (int)U(rp, me + 0xD8), stride = (int)(U(rp, me + 0xDC) >> 16);
        int nv = stride == 0 ? 0 : vs / stride;
        int a0 = 0, b0 = 0, run = 0;
        for (int t = 0; t < ni; t++)
        {
            int o = ib + (si + t) * 2;
            if (o + 2 > s.Length) break;
            int idx = H(s, o);
            if (idx == 0xFFFF) { run = 0; continue; }
            idx += bv;
            if (idx >= nv) continue;
            if (run >= 2 && a0 != b0 && b0 != idx && a0 != idx)
            {
                var (x, y, z) = run % 2 == 0 ? (a0, b0, idx) : (b0, a0, idx);
                Vector3 pa = At(vb, stride, x), pb = At(vb, stride, y), pc = At(vb, stride, z);
                if (Vector3.Cross(pb - pa, pc - pa).LengthSquared() > 1e-8f) into.Add(new(pa, pb, pc, mid));
            }
            a0 = b0; b0 = idx; run++;
        }
    }
}

static Vector3 Extent(IEnumerable<Tri> tris)
{
    Vector3 lo = new(1e9f), hi = new(-1e9f);
    foreach (var t in tris) { lo = Vector3.Min(lo, Vector3.Min(t.A, Vector3.Min(t.B, t.C))); hi = Vector3.Max(hi, Vector3.Max(t.A, Vector3.Max(t.B, t.C))); }
    return hi - lo;
}

static bool Covers(IEnumerable<Tri> full, List<Tri> low)
{
    Vector3 a = Extent(full), b = Extent(low);
    return b.X >= a.X * LodFootprint && b.Z >= a.Z * LodFootprint && b.Y >= a.Y * LodHeight;
}

// Verifies that simplified terrain LOD conforms to full detail surface
static bool CoversSurface(List<Tri> full, List<Tri> low)
{
    Dictionary<(int, int), List<int>> cells = [];
    for (int i = 0; i < low.Count; i++)
    {
        Tri t = low[i];
        int x0 = (int)MathF.Floor(MathF.Min(t.A.X, MathF.Min(t.B.X, t.C.X)) / Cell), x1 = (int)MathF.Floor(MathF.Max(t.A.X, MathF.Max(t.B.X, t.C.X)) / Cell);
        int z0 = (int)MathF.Floor(MathF.Min(t.A.Z, MathF.Min(t.B.Z, t.C.Z)) / Cell), z1 = (int)MathF.Floor(MathF.Max(t.A.Z, MathF.Max(t.B.Z, t.C.Z)) / Cell);
        for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
            {
                if (!cells.TryGetValue((x, z), out var l)) cells[(x, z)] = l = [];
                l.Add(i);
            }
    }

    bool OnLod(Vector3 p)
    {
        for (int x = (int)MathF.Floor((p.X - LodError) / Cell); x <= (int)MathF.Floor((p.X + LodError) / Cell); x++)
            for (int z = (int)MathF.Floor((p.Z - LodError) / Cell); z <= (int)MathF.Floor((p.Z + LodError) / Cell); z++)
                if (cells.TryGetValue((x, z), out var l))
                    foreach (int i in l)
                        if (Distance2(p, low[i].A, low[i].B, low[i].C) <= LodError * LodError) return true;
        return false;
    }

    double total = full.Sum(t => Vector3.Cross(t.B - t.A, t.C - t.A).Length() / 2), missing = 0;
    foreach (Tri t in full)
    {
        double quarter = Vector3.Cross(t.B - t.A, t.C - t.A).Length() / 8;
        Vector3 g = (t.A + t.B + t.C) / 3;
        foreach (Vector3 p in (Vector3[])[g, (g + t.A) / 2, (g + t.B) / 2, (g + t.C) / 2])
            if (!OnLod(p) && (missing += quarter) > total * (1 - LodSurface)) return false;
    }
    return true;
}

(long, long, long) WindingKey(Tri t)
{
    long a = Key(t.A), b = Key(t.B), c = Key(t.C);
    return a <= b && a <= c ? (a, b, c) : b <= c ? (b, c, a) : (c, a, b);
}

long lodSwaps = 0, lodTrianglesSaved = 0;

foreach (int zone in colNames.Keys.OrderBy(z => z))
{
    string path = feb + $"TRK_UNIT{zone}_GR.BNDL";
    List<Tri> tris = [];
    zones[zone] = tris;
    if (!File.Exists(path)) continue;
    var names = Regex.Matches(File.ReadAllText(feb + $@"DEBUG\TRK_UNIT{zone}_GR.DBGINFO.XML"), "<entry name=\"([^\"]+)\" id=\"([0-9A-F]+)\"").GroupBy(m => Convert.ToUInt64(m.Groups[2].Value, 16)).ToDictionary(g => g.Key, g => g.First().Groups[1].Value);
    var gr = BundleArchive.Open(path);
    var il = gr.Entries.First(e => e.Type == EntryType.InstanceList);
    byte[] p = il.GetPrimaryWithoutImports();
    int table = (int)U(p, 0), count = (int)U(p, 8);
    var imports = il.GetImports();
    for (int i = 0; i < count; i++)
    {
        var imp = imports.FirstOrDefault(x => x.EntryPointerOffset == table + i * 80);
        var model = imp.ID == 0 ? null : gr.GetEntryByID(imp.ID);
        if (model == null) continue;
        float[] m = new float[16];
        for (int k = 0; k < 16; k++) m[k] = F(p, table + i * 80 + 0x10 + k * 4);
        var lods = model.GetImports().Where(x => gr.GetEntryByID(x.ID)?.Type == EntryType.Renderable).OrderBy(x => x.EntryPointerOffset).Select(x => gr.GetEntryByID(x.ID)).ToList();
        if (lods.Count == 0) continue;
        string name = names.GetValueOrDefault(imp.ID, "");
        int mid = Model(name), before = tris.Count;
        Extract(lods[0], m, mid, tris);
        // Substitute lowest LOD for buildings and terrain when footprint is preserved
        if (lods.Count > 1 && Rank(name) is 1 or 2)
        {
            List<Tri> low = [];
            Extract(lods[^1], m, mid, low);
            var full = tris.GetRange(before, tris.Count - before);
            if (low.Count > 0 && (Rank(name) == 1 ? CoversSurface(full, low) : Covers(full, low)))
            {
                lodTrianglesSaved += tris.Count - before - low.Count;
                lodSwaps++;
                tris.RemoveRange(before, tris.Count - before);
                tris.AddRange(low);
            }
        }
    }
    Vector3 lo = new(1e9f), hi = new(-1e9f);
    foreach (var t in tris) { lo = Vector3.Min(lo, Vector3.Min(t.A, Vector3.Min(t.B, t.C))); hi = Vector3.Max(hi, Vector3.Max(t.A, Vector3.Max(t.B, t.C))); }
    if (tris.Count == 0 || janZones[zone].Count == 0) continue;
    var (pool, byVertex) = Nearby(lo, hi);
    foreach (var t in tris)
    {
        modelTotal[t.Model]++;
        if (Exact(pool, byVertex, t.A, t.B, t.C) >= 0) modelMatched[t.Model]++;
    }
}

bool[] included = new bool[modelNames.Count];
long excludedTris = 0, includedTris = 0, propsKept = 0, propsDropped = 0;
for (int m = 0; m < modelNames.Count; m++)
{
    int rank = Rank(modelNames[m]);
    double ratio = modelTotal[m] == 0 ? 0 : (double)modelMatched[m] / modelTotal[m];
    included[m] = rank < 3 || ratio >= 0.5;
    if (rank == 3) { if (included[m]) propsKept++; else propsDropped++; }
}
Console.WriteLine($"low detail LODs on {lodSwaps} buildings and terrain objects, {lodTrianglesSaved} fewer triangles");
Console.WriteLine($"props and scenery: {propsKept} objects kept as January collided with them, {propsDropped} left out");

// Filter out small geometric details and high out-of-reach building faces
int[] ranks = [.. modelNames.Select(Rank)];
Dictionary<(int, int), float> ground = [];
foreach (var t in zones.Values.SelectMany(l => l).Where(t => included[t.Model] && ranks[t.Model] <= 1))
    foreach (Vector3 v in (Vector3[])[t.A, t.B, t.C])
    {
        var k = ((int)MathF.Floor(v.X / GroundCell), (int)MathF.Floor(v.Z / GroundCell));
        if (!ground.TryGetValue(k, out float y) || v.Y > y) ground[k] = v.Y;
    }

bool Unreachable(Tri t)
{
    if (ranks[t.Model] < 2) return false;
    if (Vector3.Cross(t.B - t.A, t.C - t.A).Length() / 2 < MinDetailArea) return true;
    if (ranks[t.Model] == 3) return false;
    if (MathF.Max((t.B - t.A).LengthSquared(), MathF.Max((t.C - t.B).LengthSquared(), (t.A - t.C).LengthSquared())) < MinBuildingEdge * MinBuildingEdge) return true;
    int cx = (int)MathF.Floor(t.A.X / GroundCell), cz = (int)MathF.Floor(t.A.Z / GroundCell);
    float top = float.MinValue;
    for (int x = cx - 3; x <= cx + 3; x++)
        for (int z = cz - 3; z <= cz + 3; z++)
            if (ground.TryGetValue((x, z), out float y)) top = MathF.Max(top, y);
    return top > float.MinValue && MathF.Min(t.A.Y, MathF.Min(t.B.Y, t.C.Y)) > top + MaxBuildingClearance;
}

// Pass 2: Generate material tags and mesh clusters
var outBundle = BundleArchive.Open(args.Length > 3 ? args[3] : feb + "WORLDCOL.BIN");
var tagSource = new long[3];
long valid = 0, total = 0, filled = 0, stillEmpty = 0, laneValues = 0, laneMixed = 0, oldBytes = 0, newBytes = 0, failed = 0;
foreach (int zone in colNames.Keys.OrderBy(z => z))
{
    var entry = outBundle.GetEntryByID(colNames[zone]);
    oldBytes += entry.EntryBlocks[0].Data.Length;
    var tris = zones[zone].Where(t => included[t.Model] && !Unreachable(t)).DistinctBy(WindingKey).ToList();
    if (tris.Count == 0) { newBytes += entry.EntryBlocks[0].Data.Length; stillEmpty++; continue; }
    Vector3 lo = new(1e9f), hi = new(-1e9f);
    foreach (var t in tris) { lo = Vector3.Min(lo, Vector3.Min(t.A, Vector3.Min(t.B, t.C))); hi = Vector3.Max(hi, Vector3.Max(t.A, Vector3.Max(t.B, t.C))); }
    var (pool, byVertex) = Nearby(lo, hi);
    Dictionary<(int, int), List<int>> cells = [];
    for (int i = 0; i < pool.Count; i++)
    {
        Jan t = pool[i];
        int x0 = (int)MathF.Floor(MathF.Min(t.A.X, MathF.Min(t.B.X, t.C.X)) / Cell), x1 = (int)MathF.Floor(MathF.Max(t.A.X, MathF.Max(t.B.X, t.C.X)) / Cell);
        int z0 = (int)MathF.Floor(MathF.Min(t.A.Z, MathF.Min(t.B.Z, t.C.Z)) / Cell), z1 = (int)MathF.Floor(MathF.Max(t.A.Z, MathF.Max(t.B.Z, t.C.Z)) / Cell);
        if ((x1 - x0 + 1) * (z1 - z0 + 1) > 400) { x1 = x0; z1 = z0; }
        for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
            {
                if (!cells.TryGetValue((x, z), out var l)) cells[(x, z)] = l = [];
                l.Add(i);
            }
    }
    List<CollisionTriangle> list = new(tris.Count);
    foreach (var t in tris)
    {
        Vector3 centre = (t.A + t.B + t.C) / 3, normal = Vector3.Normalize(Vector3.Cross(t.B - t.A, t.C - t.A));
        int rank = Rank(modelNames[t.Model]);
        ushort material;
        int ex = Exact(pool, byVertex, t.A, t.B, t.C);
        if (ex >= 0)
        {
            material = pool[ex].Material;
            tagSource[0]++;
        }
        else
        {
            int cx = (int)MathF.Floor(centre.X / Cell), cz = (int)MathF.Floor(centre.Z / Cell), best = -1;
            float bestDist = NearDistance * NearDistance;
            for (int x = cx - 1; x <= cx + 1; x++)
                for (int z = cz - 1; z <= cz + 1; z++)
                {
                    if (!cells.TryGetValue((x, z), out var l)) continue;
                    foreach (int i in l)
                    {
                        Jan c = pool[i];
                        Vector3 n2 = Vector3.Cross(c.B - c.A, c.C - c.A);
                        if (n2.LengthSquared() < 1e-8f || MathF.Abs(Vector3.Dot(normal, Vector3.Normalize(n2))) < NearNormal) continue;
                        float d = Distance2(centre, c.A, c.B, c.C);
                        if (d < bestDist) { bestDist = d; best = i; }
                    }
                }
            if (best >= 0)
            {
                material = pool[best].Material;
                tagSource[1]++;
            }
            else
            {
                material = rank switch { 0 => TypeRoad, 1 => TypeTerrain, 2 => TypeBuilding, _ => TypeOther };
                tagSource[2]++;
            }
        }
        // Evaluate traffic lane tags for driveable surfaces (normal.Y > 0.5)
        if (MathF.Abs(normal.Y) > 0.5f)
        {
            int lane = TrafficValue(t.A, t.B, t.C);
            if (lane >= 0)
            {
                material = (ushort)((material & ~0xF) | lane);
                if (lane == TrafficUnknown) laneMixed++; else laneValues++;
            }
        }
        ushort group = Section(centre);
        if (group != NoSection) valid++;
        total++;
        list.Add(new(t.A, t.B, t.C, group, material));
    }
    byte[] built = ClusteredMeshBuilder.Build(list);
    if (built == null) { newBytes += entry.EntryBlocks[0].Data.Length; stillEmpty++; continue; }
    string problem = ClusteredMeshBuilder.Validate(built);
    if (problem != null) { failed++; Console.WriteLine($"zone {zone}: {problem}"); newBytes += entry.EntryBlocks[0].Data.Length; continue; }
    entry.EntryBlocks[0].Data = built;
    newBytes += built.Length;
    filled++;
}
Console.WriteLine($"zones built {filled}, empty {stillEmpty}, failed {failed}");
Console.WriteLine($"triangles {total}; surface data: exact January {tagSource[0]} ({100.0 * tagSource[0] / total:0.0}%), nearby January {tagSource[1]} ({100.0 * tagSource[1] / total:0.0}%), default for object type {tagSource[2]} ({100.0 * tagSource[2] / total:0.0}%); road direction from lanes on {laneValues}, across opposing lanes {laneMixed}");
Console.WriteLine($"AI section assigned {100.0 * valid / total:0.0}%; resource bytes {oldBytes} -> {newBytes}");
if (failed == 0)
{
    outBundle.Write(output);
    Console.WriteLine($"wrote {output}: {new FileInfo(output).Length} bytes");
}

record struct Lane(Vector3 A, Vector3 B, Vector3 C, Vector3 D, float Height, Vector2 Way);
record struct Jan(Vector3 A, Vector3 B, Vector3 C, ushort Material);
record struct Tri(Vector3 A, Vector3 B, Vector3 C, int Model);
