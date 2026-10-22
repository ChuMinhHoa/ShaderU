// Holo card girl (Assets/CardDemo/Textures/Holo/1 (4).png, 1024x1536) Spine generator (C# 5, .NET Framework).
// Adapted from Defense/Tools/SpineRigGen/RookieCharGen.cs. Seated girl in a red dress, the whole picture is ONE
// weighted mesh: chest (breathing) + bust (bounce), head with earrings / hairpin tassel, long hair on both sides,
// sleeve ribbons and the waist tassel. Hips, skirt, legs and the resting hand stay on the root (never move).
// The atlas page IS the source picture (1:1, region at 0,0, no padding), so mesh UVs equal the picture UVs:
// HoloCardView's pop-out zones / mask (in picture UV) keep working on the Spine mesh.
// Usage: HoloGirlGen.exe <source.png> <outDir>   (or: build.ps1 -Gen HoloGirlGen)
// Guide: Defense/Docs/HUONG_DAN_TAO_SPINE_ANIMATION.md
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

class Img
{
    public int W, H;
    public float[] R, G, B, A;
    public Img(int w, int h) { W = w; H = h; R = new float[w * h]; G = new float[w * h]; B = new float[w * h]; A = new float[w * h]; }

    public static Img Load(string path)
    {
        using (Bitmap bmp = new Bitmap(path))
        {
            Img img = new Img(bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] buf = new byte[data.Stride * bmp.Height];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            bmp.UnlockBits(data);
            for (int y = 0; y < img.H; y++)
                for (int x = 0; x < img.W; x++)
                {
                    int o = y * data.Stride + x * 4, i = y * img.W + x;
                    img.B[i] = buf[o] / 255f; img.G[i] = buf[o + 1] / 255f; img.R[i] = buf[o + 2] / 255f; img.A[i] = buf[o + 3] / 255f;
                }
            return img;
        }
    }
}

class Bone
{
    public string Name, ParentName, Color, Inherit; // Inherit: null = normal, e.g. "noScale"
    public Bone Parent;
    public int Index;
    public bool HasEnd;
    public double PSX, PSY, PEX, PEY; // source-pixel joints
    public double WX, WY, WRot, Len;  // setup world (spine units, y up)
    public double LX, LY, LRot;
}

class Piece
{
    public string Name;
    public bool[] Mask;
    public bool[] Cells;
    public List<int> Verts = new List<int>();
    public int Hull;
    public List<int> Tris = new List<int>();
    public Dictionary<int, double[]> VW = new Dictionary<int, double[]>();
}

class Key
{
    public double T;
    public double[] V;
    public bool Herm;       // hermite using Slope
    public double[] Slope;
}

class Tl
{
    public string Kind;
    public List<Key> Keys = new List<Key>();
}

class Anim
{
    public string Name;
    public List<KeyValuePair<string, List<Tl>>> Bones = new List<KeyValuePair<string, List<Tl>>>();
    public Anim(string n) { Name = n; }

    public Tl Get(string target, string kind)
    {
        List<Tl> tls = null;
        foreach (KeyValuePair<string, List<Tl>> kv in Bones) if (kv.Key == target) tls = kv.Value;
        if (tls == null) { tls = new List<Tl>(); Bones.Add(new KeyValuePair<string, List<Tl>>(target, tls)); }
        foreach (Tl t in tls) if (t.Kind == kind) return t;
        Tl nt = new Tl(); nt.Kind = kind; tls.Add(nt); return nt;
    }
}

static class Program
{
    static readonly CultureInfo IC = CultureInfo.InvariantCulture;
    const double OX = 512, OY = 768; // source px -> spine: x = px-OX, y = OY-py (picture centre at 0,0, like the card quad)
    const int CELL = 16;             // big picture: 16px is fine enough for earrings / tassel and keeps ~4k vertices
    const string NAME = "HoloGirl", SLOT = "hologirl_main";

    static Img Src;
    static int W, H;
    static List<Bone> Bones = new List<Bone>();
    static Dictionary<string, Bone> BM = new Dictionary<string, Bone>();
    static int[] XS, YS;
    static int NX, NY; // cell counts
    static int GW;     // gridpoints per row = NX+1

    // ---------------- regions (source px) ----------------
    static double[] EarLPoly = { 352,266, 388,266, 388,364, 352,364 };
    static double[] EarRPoly = { 553,334, 588,334, 588,414, 553,414 };
    // red/gold dangle hanging under the flower hairpin
    static double[] PinPoly = { 298,178, 348,172, 346,292, 298,292 };
    static double[] RibbonLPoly = { 183,752, 217,752, 217,858, 183,858 };
    static double[] RibbonRPoly = { 733,662, 762,662, 780,790, 748,790 };
    // waist tassel: upper tassel, beaded cord, big lower tassel
    static double[] Tassel1Poly = { 670,938, 728,938, 730,1062, 670,1062 };
    static double[] CordPoly = { 692,1060, 732,1060, 738,1150, 698,1150 };
    static double[] Tassel2Poly = { 712,1146, 742,1146, 808,1394, 722,1398, 706,1250 };
    // bust ellipses (centre x, y, radius x, y): from the upper slope of the breast (skin above the neckline) to the
    // underside, so the whole breast moves, not only the part inside the dress
    static double[] BustL = { 334, 758, 86, 122 };
    static double[] BustR = { 566, 742, 94, 126 };
    // upper slope of each breast (skin above the neckline frill): own bone, jiggles a beat after the breast
    static double[] BustTopL = { 330, 668, 78, 48 };
    static double[] BustTopR = { 550, 616, 88, 62 };   // right breast sits higher: its upper slope starts ~y 555
    // face + top hair + flower + the hair beside the face; bottom edge follows the chin / jaw line
    static double[] HeadPoly = { 225,0, 805,0, 765,250, 722,380, 640,398, 560,402, 520,398, 482,392, 440,372, 382,382, 330,420, 268,430, 240,330, 218,150 };
    // long hair falling left of the body (outside the shoulder and the sleeve)
    static double[] HairLPoly = { 218,330, 268,430, 330,420, 300,470, 230,515, 162,560, 150,690, 184,900, 196,1098, 138,1104, 92,950, 92,700, 118,560, 168,430 };
    // hair hanging between the right breast and the right sleeve
    static double[] HairRPoly = { 640,398, 722,380, 702,470, 692,560, 702,700, 704,838, 656,834, 640,700, 620,560, 606,450 };
    const double WaistY = 945; // above: chest family (breathes), below: root (hips, skirt, legs, resting hand)

    static string[] HairLSet = { "hairL1", "hairL2", "hairL3" };
    static string[] HairRSet = { "hairR1", "hairR2" };
    static string[] TasselSet = { "tassel1", "cord", "tassel2" };

    static string OutDir, DebugDir;

    static int Main(string[] args)
    {
        string srcPath = args[0];
        OutDir = args[1];
        Directory.CreateDirectory(OutDir);
        DebugDir = Path.Combine(OutDir, "debug");
        Directory.CreateDirectory(DebugDir);
        Src = Img.Load(srcPath);
        W = Src.W; H = Src.H;
        Console.WriteLine("source " + W + "x" + H);

        BuildBones();
        BuildGrid();

        // ---- single piece over every visible pixel ----
        Piece main = new Piece(); main.Name = SLOT;
        main.Mask = new bool[W * H];
        for (int i = 0; i < W * H; i++) main.Mask[i] = Src.A[i] > 0.004f;
        main.Cells = CellsFromMask(main.Mask);
        int lost = 0, lostSolid = 0;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (main.Mask[y * W + x] && !main.Cells[CellOf(x, y)]) { lost++; if (Src.A[y * W + x] > 0.5f) lostSolid++; }
        Console.WriteLine("CHECK pixels outside the mesh: " + lost + " (alpha > 0.5: " + lostSolid + ")");

        // ---- gridpoint labels & weights ----
        int nG = (NX + 1) * (NY + 1);
        bool[] used = new bool[nG];
        for (int c = 0; c < NX * NY; c++) if (main.Cells[c]) foreach (int g in CellCorners(c)) used[g] = true;
        int[] gLabel = new int[nG];
        for (int g = 0; g < nG; g++) gLabel[g] = used[g] ? Label(XS[g % GW], YS[g / GW]) : -1;
        List<int>[] adj = new List<int>[nG];
        for (int g = 0; g < nG; g++) adj[g] = new List<int>();
        for (int c = 0; c < NX * NY; c++)
        {
            if (!main.Cells[c]) continue;
            int[] cc = CellCorners(c);
            for (int a = 0; a < 4; a++)
                for (int b = a + 1; b < 4; b++)
                {
                    int ga = cc[a], gb = cc[b];
                    if (!adj[ga].Contains(gb)) adj[ga].Add(gb);
                    if (!adj[gb].Contains(ga)) adj[gb].Add(ga);
                }
        }
        // one painted layer: nothing has to separate, weights blend everywhere (4 passes ~ 3 cells of falloff)
        int nb = Bones.Count;
        double[][] Wt = new double[nG][];
        for (int g = 0; g < nG; g++) { Wt[g] = new double[nb]; if (used[g]) Wt[g][gLabel[g]] = 1; }
        for (int it = 0; it < 4; it++)
        {
            double[][] nw = new double[nG][];
            for (int g = 0; g < nG; g++)
            {
                nw[g] = new double[nb];
                if (!used[g]) continue;
                if (adj[g].Count == 0) { Array.Copy(Wt[g], nw[g], nb); continue; }
                for (int b = 0; b < nb; b++)
                {
                    double s = 0;
                    foreach (int n in adj[g]) s += Wt[n][b];
                    nw[g][b] = 0.5 * Wt[g][b] + 0.5 * s / adj[g].Count;
                }
            }
            Wt = nw;
        }
        BuildPieceMesh(main);
        foreach (int g in main.Verts) main.VW[g] = Wt[g];

        // ---- texture + atlas: the source picture itself is the page (straight alpha, region = whole page) ----
        File.Copy(srcPath, Path.Combine(OutDir, NAME + ".png"), true);
        StringBuilder atlas = new StringBuilder();
        atlas.Append(NAME + ".png\n");
        atlas.Append("size:" + W + "," + H + "\n");
        atlas.Append("filter:Linear,Linear\n");
        atlas.Append(SLOT + "\n");
        atlas.Append("bounds:0,0," + W + "," + H + "\n");
        File.WriteAllText(Path.Combine(OutDir, NAME + ".atlas.txt"), atlas.ToString());

        // ---- skeleton json ----
        string json = BuildJson(main);
        File.WriteAllText(Path.Combine(OutDir, NAME + ".json"), json);
        Console.WriteLine("json bytes " + json.Length);

        DebugOverlay(gLabel, main, Path.Combine(DebugDir, "debug_labels.png"));
        Console.WriteLine(main.Name + ": verts " + main.Verts.Count + " hull " + main.Hull + " tris " + main.Tris.Count / 3);
        return 0;
    }

    // ===================== bones =====================
    static Bone AddBone(string name, string parent, double sx, double sy, double ex, double ey, string color)
    {
        Bone b = new Bone();
        b.Name = name; b.ParentName = parent; b.Color = color;
        b.PSX = sx; b.PSY = sy; b.PEX = ex; b.PEY = ey;
        b.HasEnd = !double.IsNaN(ex);
        b.Index = Bones.Count;
        b.Parent = parent == null ? null : BM[parent];
        b.WX = sx - OX; b.WY = OY - sy;
        if (b.HasEnd)
        {
            double dx = ex - sx, dy = sy - ey;
            b.WRot = Math.Atan2(dy, dx) * 180 / Math.PI;
            b.Len = Math.Sqrt(dx * dx + dy * dy);
        }
        if (b.Parent == null) { b.LX = b.WX; b.LY = b.WY; b.LRot = b.WRot; }
        else
        {
            double pr = -b.Parent.WRot * Math.PI / 180, ddx = b.WX - b.Parent.WX, ddy = b.WY - b.Parent.WY;
            b.LX = ddx * Math.Cos(pr) - ddy * Math.Sin(pr);
            b.LY = ddx * Math.Sin(pr) + ddy * Math.Cos(pr);
            b.LRot = NormAngle(b.WRot - b.Parent.WRot);
        }
        Bones.Add(b); BM[name] = b;
        return b;
    }

    static double NormAngle(double a) { while (a > 180) a -= 360; while (a <= -180) a += 360; return a; }

    static void BuildBones()
    {
        AddBone("root", null, OX, OY, double.NaN, double.NaN, null);
        // chest pivots at the waist: breathing never moves the hips / skirt below
        AddBone("chest", "root", 470, 960, 480, 560, "ffd700ff");
        // the face keeps its proportions while the chest puffs
        AddBone("head", "chest", 505, 440, 510, 40, "ff4040ff").Inherit = "noScale";
        AddBone("pin", "head", 330, 182, 317, 282, "ff80c0ff");
        AddBone("earL", "head", 369, 272, 369, 358, "ffb0d0ff");
        AddBone("earR", "head", 570, 340, 570, 410, "ffb0d0ff");
        // long hair: the top follows the head, the lower part rests on the body (chest) and sways
        AddBone("hairL1", "head", 300, 330, 170, 560, "c080ffff");
        AddBone("hairL2", "chest", 170, 560, 120, 800, "c080ffff");
        AddBone("hairL3", "hairL2", 120, 800, 165, 1088, "c080ffff");
        AddBone("hairR1", "head", 632, 400, 652, 600, "c080ffff");
        AddBone("hairR2", "chest", 652, 600, 690, 832, "c080ffff");
        // bust pivots at the centre of each breast (bone points down, scaleX = vertical): stretch / squash moves the
        // upper slope as well as the underside
        AddBone("bustL", "chest", 334, 752, 330, 876, "ff9060ff");
        AddBone("bustR", "chest", 566, 736, 562, 864, "ff9060ff");
        AddBone("bustTopL", "bustL", 330, 664, 330, 716, "ffb080ff");
        AddBone("bustTopR", "bustR", 550, 610, 550, 662, "ffb080ff");
        AddBone("ribbonL", "chest", 200, 748, 200, 852, "ff3060ff");
        AddBone("ribbonR", "chest", 745, 650, 765, 784, "ff3060ff");
        // the tassel hangs from the waist ornament, which sits on the (static) skirt
        AddBone("tassel1", "root", 697, 944, 700, 1058, "ffe080ff");
        AddBone("cord", "tassel1", 700, 1058, 722, 1150, "ffe080ff");
        AddBone("tassel2", "cord", 722, 1150, 762, 1390, "ffe080ff");
    }

    static int Label(double x, double y)
    {
        if (InPoly(EarLPoly, x, y)) return BM["earL"].Index;
        if (InPoly(EarRPoly, x, y)) return BM["earR"].Index;
        if (InPoly(PinPoly, x, y)) return BM["pin"].Index;
        if (InPoly(RibbonLPoly, x, y)) return BM["ribbonL"].Index;
        if (InPoly(RibbonRPoly, x, y)) return BM["ribbonR"].Index;
        if (InPoly(Tassel1Poly, x, y) || InPoly(CordPoly, x, y) || InPoly(Tassel2Poly, x, y)) return Nearest(TasselSet, x, y);
        if (InPoly(HeadPoly, x, y)) return BM["head"].Index;
        // bust before hair: the hair locks lying on the breasts bounce with them
        if (InEllipse(BustTopL, x, y)) return BM["bustTopL"].Index;
        if (InEllipse(BustTopR, x, y)) return BM["bustTopR"].Index;
        if (InEllipse(BustL, x, y)) return BM["bustL"].Index;
        if (InEllipse(BustR, x, y)) return BM["bustR"].Index;
        if (InPoly(HairLPoly, x, y)) return Nearest(HairLSet, x, y);
        if (InPoly(HairRPoly, x, y)) return Nearest(HairRSet, x, y);
        return y < WaistY ? BM["chest"].Index : 0;
    }

    static bool InEllipse(double[] e, double x, double y)
    {
        double dx = (x - e[0]) / e[2], dy = (y - e[1]) / e[3];
        return dx * dx + dy * dy <= 1;
    }

    static int Nearest(string[] set, double x, double y)
    {
        double best = 1e9; int bi = -1;
        foreach (string n in set)
        {
            Bone b = BM[n];
            double d = SegDist(x, y, b.PSX, b.PSY, b.PEX, b.PEY);
            if (d < best) { best = d; bi = b.Index; }
        }
        return bi;
    }

    static double SegDist(double px, double py, double ax, double ay, double bx, double by)
    {
        double dx = bx - ax, dy = by - ay, l2 = dx * dx + dy * dy;
        double t = l2 > 0 ? ((px - ax) * dx + (py - ay) * dy) / l2 : 0;
        t = Math.Max(0, Math.Min(1, t));
        double cx = ax + t * dx - px, cy = ay + t * dy - py;
        return Math.Sqrt(cx * cx + cy * cy);
    }

    static bool InPoly(double[] p, double x, double y)
    {
        bool c = false;
        int n = p.Length / 2;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = p[i * 2], yi = p[i * 2 + 1], xj = p[j * 2], yj = p[j * 2 + 1];
            if (((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi)) c = !c;
        }
        return c;
    }

    // ===================== grid / mesh =====================
    static void BuildGrid()
    {
        List<int> xs = new List<int>();
        for (int x = 0; x < W; x += CELL) xs.Add(x);
        xs.Add(W);
        List<int> ys = new List<int>();
        for (int y = 0; y < H; y += CELL) ys.Add(y);
        ys.Add(H);
        XS = xs.ToArray(); YS = ys.ToArray();
        NX = XS.Length - 1; NY = YS.Length - 1; GW = NX + 1;
    }

    static int CellOf(int x, int y) { return (y / CELL) * NX + (x / CELL); }

    static int[] CellCorners(int c)
    {
        int i = c % NX, j = c / NX;
        return new int[] { j * GW + i, j * GW + i + 1, (j + 1) * GW + i + 1, (j + 1) * GW + i };
    }

    static bool[] Dilate(bool[] m, int r)
    {
        bool[] o = new bool[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                if (!m[y * W + x]) continue;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int xx = x + dx, yy = y + dy;
                        if (xx >= 0 && yy >= 0 && xx < W && yy < H) o[yy * W + xx] = true;
                    }
            }
        return o;
    }

    static bool[] CellsFromMask(bool[] mask)
    {
        bool[] dm = Dilate(mask, 2);
        bool[] cells = new bool[NX * NY];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (dm[y * W + x]) cells[CellOf(x, y)] = true;
        for (int iter = 0; iter < 20; iter++)
        {
            bool changed = FillHoles(cells);
            changed |= FixDiagonals(cells);
            KeepLargest(cells);
            if (!changed) break;
        }
        return cells;
    }

    static bool FillHoles(bool[] cells)
    {
        bool[] outside = new bool[NX * NY];
        Queue<int> q = new Queue<int>();
        for (int j = 0; j < NY; j++)
            for (int i = 0; i < NX; i++)
                if ((i == 0 || j == 0 || i == NX - 1 || j == NY - 1) && !cells[j * NX + i]) { outside[j * NX + i] = true; q.Enqueue(j * NX + i); }
        while (q.Count > 0)
        {
            int c = q.Dequeue(); int i = c % NX, j = c / NX;
            int[] nn = { i > 0 ? c - 1 : -1, i < NX - 1 ? c + 1 : -1, j > 0 ? c - NX : -1, j < NY - 1 ? c + NX : -1 };
            foreach (int n in nn) if (n >= 0 && !cells[n] && !outside[n]) { outside[n] = true; q.Enqueue(n); }
        }
        bool ch = false;
        for (int c = 0; c < NX * NY; c++) if (!cells[c] && !outside[c]) { cells[c] = true; ch = true; }
        return ch;
    }

    static bool FixDiagonals(bool[] cells)
    {
        bool ch = false;
        for (int j = 0; j < NY - 1; j++)
            for (int i = 0; i < NX - 1; i++)
            {
                bool a = cells[j * NX + i], b = cells[j * NX + i + 1], c = cells[(j + 1) * NX + i], d = cells[(j + 1) * NX + i + 1];
                if (a && d && !b && !c) { cells[j * NX + i + 1] = true; ch = true; }
                else if (b && c && !a && !d) { cells[j * NX + i] = true; ch = true; }
            }
        return ch;
    }

    static void KeepLargest(bool[] cells)
    {
        int[] comp = new int[NX * NY];
        for (int k = 0; k < comp.Length; k++) comp[k] = -1;
        List<int> sizes = new List<int>();
        for (int s = 0; s < NX * NY; s++)
        {
            if (!cells[s] || comp[s] >= 0) continue;
            int id = sizes.Count, size = 0;
            Queue<int> q = new Queue<int>(); q.Enqueue(s); comp[s] = id;
            while (q.Count > 0)
            {
                int c = q.Dequeue(); size++; int i = c % NX, j = c / NX;
                int[] nn = { i > 0 ? c - 1 : -1, i < NX - 1 ? c + 1 : -1, j > 0 ? c - NX : -1, j < NY - 1 ? c + NX : -1 };
                foreach (int n in nn) if (n >= 0 && cells[n] && comp[n] < 0) { comp[n] = id; q.Enqueue(n); }
            }
            sizes.Add(size);
        }
        if (sizes.Count <= 1) return;
        int big = sizes.IndexOf(sizes.Max());
        for (int c = 0; c < NX * NY; c++) if (cells[c] && comp[c] != big) cells[c] = false;
    }

    static void BuildPieceMesh(Piece p)
    {
        // boundary loop (directed edges around each kept cell)
        Dictionary<int, int> next = new Dictionary<int, int>();
        HashSet<int> vset = new HashSet<int>();
        for (int c = 0; c < NX * NY; c++)
        {
            if (!p.Cells[c]) continue;
            int i = c % NX, j = c / NX;
            int[] k = CellCorners(c); // TL, TR, BR, BL
            foreach (int g in k) vset.Add(g);
            if (j == 0 || !p.Cells[c - NX]) next[k[0]] = k[1];
            if (i == NX - 1 || !p.Cells[c + 1]) next[k[1]] = k[2];
            if (j == NY - 1 || !p.Cells[c + NX]) next[k[2]] = k[3];
            if (i == 0 || !p.Cells[c - 1]) next[k[3]] = k[0];
        }
        int start = next.Keys.Min();
        List<int> loop = new List<int>();
        int cur = start;
        do { loop.Add(cur); cur = next[cur]; } while (cur != start && loop.Count <= next.Count);
        if (loop.Count != next.Count) Console.WriteLine("WARN " + p.Name + ": boundary loops " + loop.Count + "/" + next.Count);
        p.Verts.AddRange(loop);
        p.Hull = loop.Count;
        HashSet<int> inLoop = new HashSet<int>(loop);
        foreach (int g in vset.OrderBy(v => v)) if (!inLoop.Contains(g)) p.Verts.Add(g);
        Dictionary<int, int> idx = new Dictionary<int, int>();
        for (int k = 0; k < p.Verts.Count; k++) idx[p.Verts[k]] = k;
        for (int c = 0; c < NX * NY; c++)
        {
            if (!p.Cells[c]) continue;
            int i = c % NX, j = c / NX;
            int[] k = CellCorners(c);
            int tl = idx[k[0]], tr = idx[k[1]], br = idx[k[2]], bl = idx[k[3]];
            if (((i + j) & 1) == 0) { p.Tris.AddRange(new int[] { tl, tr, br, tl, br, bl }); }
            else { p.Tris.AddRange(new int[] { tl, tr, bl, tr, br, bl }); }
        }
    }

    // ===================== animation helpers =====================
    static Anim A;

    static double[] WorldToParent(string bone, double wx, double wy)
    {
        Bone b = BM[bone];
        if (b.Parent == null) return new double[] { wx, wy };
        double pr = -b.Parent.WRot * Math.PI / 180;
        return new double[] { wx * Math.Cos(pr) - wy * Math.Sin(pr), wx * Math.Sin(pr) + wy * Math.Cos(pr) };
    }

    // sampled channel -> hermite keys (exact slopes)
    static void FR(string bone, Func<double, double> f, double dur, int n)
    {
        Tl tl = A.Get(bone, "rotate");
        for (int i = 0; i <= n; i++)
        {
            double t = dur * i / n;
            Key k = new Key(); k.T = t; k.V = new double[] { f(t) }; k.Herm = true; k.Slope = new double[] { Deriv(f, t) };
            tl.Keys.Add(k);
        }
    }

    // translate given as a WORLD-space offset (setup orientation), converted to the parent's local frame
    static void FT(string bone, Func<double, double> fx, Func<double, double> fy, double dur, int n)
    {
        Tl tl = A.Get(bone, "translate");
        for (int i = 0; i <= n; i++)
        {
            double t = dur * i / n;
            double[] v = WorldToParent(bone, fx(t), fy(t));
            double[] d = WorldToParent(bone, Deriv(fx, t), Deriv(fy, t));
            Key k = new Key(); k.T = t; k.V = v; k.Herm = true; k.Slope = d;
            tl.Keys.Add(k);
        }
    }

    static void FS(string bone, Func<double, double> fx, Func<double, double> fy, double dur, int n)
    {
        Tl tl = A.Get(bone, "scale");
        for (int i = 0; i <= n; i++)
        {
            double t = dur * i / n;
            Key k = new Key(); k.T = t; k.V = new double[] { fx(t), fy(t) }; k.Herm = true; k.Slope = new double[] { Deriv(fx, t), Deriv(fy, t) };
            tl.Keys.Add(k);
        }
    }

    static double Deriv(Func<double, double> f, double t) { double h = 1e-4; return (f(t + h) - f(t - h)) / (2 * h); }

    // smooth bump on [a,b]: 0 outside, sin^2 inside (zero value and slope at both ends)
    static double Bump(double t, double a, double b)
    {
        if (t <= a || t >= b) return 0;
        double u = Math.Sin(Math.PI * (t - a) / (b - a));
        return u * u;
    }

    // damped spring kicked at t0: 0 before t0, starts with zero value, rings at freq f and dies out with rate k.
    // Faded to exactly 0 at tEnd so the clip ends on the setup pose.
    static double Ring(double t, double t0, double f, double k, double tEnd)
    {
        if (t <= t0 || t >= tEnd) return 0;
        double u = t - t0;
        double fade = t > tEnd - 0.25 ? Bump(t, tEnd - 0.5, tEnd) : 1;
        return Math.Exp(-k * u) * Math.Sin(2 * Math.PI * f * u) * fade;
    }

    // ===================== animations =====================
    static List<Anim> BuildAnims()
    {
        List<Anim> list = new List<Anim>();

        // ---------------- idle (loop) ----------------
        // slow breath; the bust rides the breath a beat late and bounces softly at the top of each breath,
        // head tilts, hair / earrings / ribbons / tassel trail behind. Every channel is 0 at t=0 and t=TI.
        A = new Anim("idle"); list.Add(A);
        double TI = 3.2, w = 2 * Math.PI / TI;
        int N = 32;
        // S-shaped term: 0 at t=0, lagged by phase p (radians); s2 runs twice per loop
        Func<double, double, double, double> s = (amp, ph, t) => amp * (0.5 * Math.Cos(ph) - 0.5 * Math.Cos(w * t - ph));
        Func<double, double, double, double> s2 = (amp, ph, t) => amp * (0.5 * Math.Cos(ph) - 0.5 * Math.Cos(2 * w * t - ph));
        Func<double, double, double, double> s3 = (amp, ph, t) => amp * (0.5 * Math.Cos(ph) - 0.5 * Math.Cos(3 * w * t - ph));

        // breathing from the waist: the rib cage swells ~8px up and widens (bone points up: scaleX = vertical)
        FS("chest", t => 1 + s(0.02, 0.3, t), t => 1 + s(0.014, 0.3, t), TI, N);
        FR("chest", t => s(0.4, 0.5, t), TI, N);
        // bust swells with the breath: grows ~3.5% and lifts ~3px a beat after the chest, small after-bounce on top
        FT("bustL", t => 0, t => s(3.0, 0.6, t) + s2(-0.7, 2.0, t) + s3(0.35, 2.6, t), TI, N * 2);
        FT("bustR", t => 0, t => s(3.2, 0.7, t) + s2(-0.8, 2.1, t) + s3(0.4, 2.7, t), TI, N * 2);
        FS("bustL", t => 1 + s(0.035, 0.6, t) + s2(0.006, 2.0, t), t => 1 + s(0.03, 0.6, t), TI, N * 2);
        FS("bustR", t => 1 + s(0.035, 0.7, t) + s2(0.006, 2.1, t), t => 1 + s(0.03, 0.7, t), TI, N * 2);
        // upper slope: extra lift on the inhale, trailing the breast, then a soft wobble
        FT("bustTopL", t => 0, t => s(3.4, 1.0, t) + s2(-1.3, 2.4, t) + s3(0.7, 3.0, t), TI, N * 2);
        FT("bustTopR", t => 0, t => s(3.6, 1.1, t) + s2(-1.4, 2.5, t) + s3(0.75, 3.1, t), TI, N * 2);
        // head: gentle tilt + nod a little after the breath
        FR("head", t => s(1.1, 0.9, t), TI, N);
        FT("head", t => s(0.6, 0.9, t), t => s(-1.0, 1.1, t), TI, N);
        // dangles swing behind the head (pendulum lag, tips swing more)
        FR("earL", t => s(-5.0, 2.0, t) + s2(-1.2, 2.6, t), TI, N);
        FR("earR", t => s(-5.5, 2.1, t) + s2(-1.2, 2.7, t), TI, N);
        FR("pin", t => s(-4.0, 1.9, t) + s2(-1.0, 2.5, t), TI, N);
        // hair: roots follow the head, the long ends trail with increasing lag
        FR("hairL1", t => s(-0.8, 1.6, t), TI, N);
        FR("hairL2", t => s(-0.9, 2.0, t), TI, N);
        FR("hairL3", t => s(-1.4, 2.5, t), TI, N);
        FR("hairR1", t => s(-0.8, 1.6, t), TI, N);
        FR("hairR2", t => s(-1.3, 2.2, t), TI, N);
        FR("ribbonL", t => s(3.0, 1.8, t), TI, N);
        FR("ribbonR", t => s(-3.0, 1.9, t), TI, N);
        // waist tassel sways like a pendulum chain
        FR("tassel1", t => s(1.2, 1.4, t), TI, N);
        FR("cord", t => s(1.4, 2.0, t), TI, N);
        FR("tassel2", t => s(1.6, 2.6, t), TI, N);

        // ---------------- touch (one shot, back to setup pose) ----------------
        // tap reaction: a small happy hop of the upper body (0.10-0.45s), the bust bounces with a damped spring,
        // the head tilts towards the viewer, every dangle gets kicked and rings out. Ends exactly on the setup pose.
        A = new Anim("touch"); list.Add(A);
        double TT = 1.6;
        int NT = 64;
        // upper body: dip, pop up 7px, settle
        Func<double, double> hop = t => -2.5 * Bump(t, 0.0, 0.16) + 7.0 * Bump(t, 0.12, 0.62) - 1.2 * Bump(t, 0.55, 0.95);
        FT("chest", t => 0, hop, TT, NT);
        FS("chest", t => 1 + 0.018 * Bump(t, 0.12, 0.62) - 0.008 * Bump(t, 0.0, 0.16), t => 1 + 0.008 * Bump(t, 0.12, 0.62), TT, NT);
        // bust: lags the hop and rings (3 Hz, dies out ~1.2s); the stretch follows the bounce
        Func<double, double> bounceL = t => -12.0 * Ring(t, 0.16, 2.9, 3.2, TT) - 1.5 * Bump(t, 0.05, 0.25);
        Func<double, double> bounceR = t => -12.5 * Ring(t, 0.19, 2.8, 3.1, TT) - 1.5 * Bump(t, 0.07, 0.27);
        FT("bustL", t => 0, bounceL, TT, NT);
        FT("bustR", t => 0, bounceR, TT, NT);
        FS("bustL", t => 1 - 0.006 * bounceL(t), t => 1 + 0.0025 * bounceL(t), TT, NT);
        FS("bustR", t => 1 - 0.006 * bounceR(t), t => 1 + 0.0025 * bounceR(t), TT, NT);
        // upper slope rides the breast and rings on its own a beat later (overlap): the top visibly jiggles
        FT("bustTopL", t => 0, t => -12.0 * Ring(t, 0.24, 3.1, 3.0, TT), TT, NT);
        FT("bustTopR", t => 0, t => -14.0 * Ring(t, 0.27, 3.0, 2.9, TT), TT, NT);
        // head: tilt 4 deg and come back with a tiny overshoot
        FR("head", t => 4.0 * Bump(t, 0.1, 1.1) - 0.8 * Bump(t, 0.95, 1.5), TT, NT);
        FT("head", t => 2.0 * Bump(t, 0.1, 1.1), t => 1.5 * Bump(t, 0.12, 0.7), TT, NT);
        FR("earL", t => -12.0 * Ring(t, 0.2, 1.6, 2.4, TT), TT, NT);
        FR("earR", t => -13.0 * Ring(t, 0.22, 1.5, 2.4, TT), TT, NT);
        FR("pin", t => -9.0 * Ring(t, 0.2, 1.7, 2.6, TT), TT, NT);
        FR("hairL1", t => -1.5 * Ring(t, 0.2, 1.2, 2.5, TT), TT, NT);
        FR("hairL2", t => -1.6 * Ring(t, 0.26, 1.2, 2.5, TT), TT, NT);
        FR("hairL3", t => -2.4 * Ring(t, 0.32, 1.2, 2.4, TT), TT, NT);
        FR("hairR1", t => -1.5 * Ring(t, 0.2, 1.2, 2.5, TT), TT, NT);
        FR("hairR2", t => -2.2 * Ring(t, 0.28, 1.2, 2.4, TT), TT, NT);
        FR("ribbonL", t => 7.0 * Ring(t, 0.22, 1.8, 2.8, TT), TT, NT);
        FR("ribbonR", t => -7.0 * Ring(t, 0.24, 1.8, 2.8, TT), TT, NT);
        FR("tassel1", t => 2.0 * Ring(t, 0.25, 1.3, 2.4, TT), TT, NT);
        FR("cord", t => 2.4 * Ring(t, 0.3, 1.3, 2.4, TT), TT, NT);
        FR("tassel2", t => 2.8 * Ring(t, 0.36, 1.3, 2.3, TT), TT, NT);

        return list;
    }

    // ===================== json =====================
    static string F(double v)
    {
        double r = Math.Round(v, 4);
        if (Math.Abs(r) < 0.00005) r = 0;
        return r.ToString("0.####", IC);
    }

    static string BuildJson(Piece main)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{\n\"skeleton\":{\"hash\":\"holoGirlGenV1\",\"spine\":\"4.2.43\",\"x\":" + F(-OX) + ",\"y\":" + F(OY - H) + ",\"width\":" + W + ",\"height\":" + H + ",\"fps\":30,\"images\":\"./images/\",\"audio\":\"\"},\n");

        sb.Append("\"bones\":[\n");
        for (int i = 0; i < Bones.Count; i++)
        {
            Bone b = Bones[i];
            sb.Append("\t{\"name\":\"" + b.Name + "\"");
            if (b.Parent != null) sb.Append(",\"parent\":\"" + b.Parent.Name + "\"");
            if (b.HasEnd) sb.Append(",\"length\":" + F(b.Len));
            if (b.Parent != null)
            {
                if (Math.Abs(b.LRot) > 1e-6) sb.Append(",\"rotation\":" + F(b.LRot));
                if (Math.Abs(b.LX) > 1e-6) sb.Append(",\"x\":" + F(b.LX));
                if (Math.Abs(b.LY) > 1e-6) sb.Append(",\"y\":" + F(b.LY));
            }
            if (b.Inherit != null) sb.Append(",\"inherit\":\"" + b.Inherit + "\"");
            if (b.Color != null) sb.Append(",\"color\":\"" + b.Color + "\"");
            sb.Append("}" + (i < Bones.Count - 1 ? "," : "") + "\n");
        }
        sb.Append("],\n");

        sb.Append("\"slots\":[\n\t{\"name\":\"" + SLOT + "\",\"bone\":\"root\",\"attachment\":\"" + SLOT + "\"}\n],\n");

        sb.Append("\"skins\":[{\"name\":\"default\",\"attachments\":{\n");
        sb.Append("\t\"" + SLOT + "\":{\"" + SLOT + "\":" + MeshJson(main) + "}");
        sb.Append("\n}}],\n");

        List<Anim> anims = BuildAnims();
        sb.Append("\"animations\":{\n");
        for (int a = 0; a < anims.Count; a++)
        {
            Anim an = anims[a];
            sb.Append("\"" + an.Name + "\":{\n");
            sb.Append("\t\"bones\":{\n" + TimelinesJson(an.Bones) + "\n\t}");
            sb.Append("\n}" + (a < anims.Count - 1 ? "," : "") + "\n");
        }
        sb.Append("}\n}\n");
        return sb.ToString();
    }

    // root sits at the picture centre with no offset, so world = root-local spine coords
    static string MeshJson(Piece p)
    {
        StringBuilder sb = new StringBuilder();
        List<string> uvs = new List<string>();
        List<string> verts = new List<string>();
        foreach (int g in p.Verts)
        {
            double px = XS[g % GW], py = YS[g / GW];
            // UV over the WHOLE picture (region = whole page), so the mesh UV is the picture UV
            uvs.Add(F(px / W)); uvs.Add(F(py / H));
            double wx = px - OX, wy = OY - py;
            double[] w = p.VW[g];
            List<int> bi = new List<int>();
            for (int b = 0; b < w.Length; b++) if (w[b] > 0.0001) bi.Add(b);
            bi.Sort((x, y) => w[y].CompareTo(w[x]));
            if (bi.Count > 4) bi = bi.GetRange(0, 4);
            int first = bi[0];
            bi.RemoveAll(b => w[b] < 0.02 && b != first);
            double sum = 0; foreach (int b in bi) sum += w[b];
            verts.Add(bi.Count.ToString(IC));
            foreach (int b in bi)
            {
                Bone bn = Bones[b];
                double r = -bn.WRot * Math.PI / 180, dx = wx - bn.WX, dy = wy - bn.WY;
                double lx = dx * Math.Cos(r) - dy * Math.Sin(r), ly = dx * Math.Sin(r) + dy * Math.Cos(r);
                verts.Add(b.ToString(IC)); verts.Add(F(lx)); verts.Add(F(ly)); verts.Add(F(w[b] / sum));
            }
        }
        List<string> tris = new List<string>();
        foreach (int t in p.Tris) tris.Add(t.ToString(IC));
        sb.Append("{\"type\":\"mesh\",\"uvs\":[" + string.Join(",", uvs.ToArray()) + "],");
        sb.Append("\"triangles\":[" + string.Join(",", tris.ToArray()) + "],");
        sb.Append("\"vertices\":[" + string.Join(",", verts.ToArray()) + "],");
        sb.Append("\"hull\":" + p.Hull + ",\"width\":" + W + ",\"height\":" + H + "}");
        return sb.ToString();
    }

    static string TimelinesJson(List<KeyValuePair<string, List<Tl>>> list)
    {
        List<string> outer = new List<string>();
        foreach (KeyValuePair<string, List<Tl>> kv in list)
        {
            List<string> inner = new List<string>();
            foreach (Tl tl in kv.Value)
            {
                List<Key> keys = tl.Keys.OrderBy(k => k.T).ToList();
                List<string> ks = new List<string>();
                for (int i = 0; i < keys.Count; i++)
                {
                    Key k = keys[i];
                    StringBuilder sb = new StringBuilder("{");
                    List<string> f = new List<string>();
                    if (k.T != 0) f.Add("\"time\":" + F(k.T));
                    if (tl.Kind == "rotate") f.Add("\"value\":" + F(k.V[0]));
                    else { f.Add("\"x\":" + F(k.V[0])); f.Add("\"y\":" + F(k.V[1])); }
                    if (i < keys.Count - 1 && k.Herm)
                    {
                        Key n = keys[i + 1];
                        double dt = n.T - k.T;
                        List<string> cv = new List<string>();
                        for (int c = 0; c < k.V.Length; c++)
                        {
                            double v0 = k.V[c], v1 = n.V[c];
                            double m0 = k.Slope[c], m1 = n.Herm ? n.Slope[c] : m0;
                            cv.Add(F(k.T + dt / 3)); cv.Add(F(v0 + m0 * dt / 3)); cv.Add(F(n.T - dt / 3)); cv.Add(F(v1 - m1 * dt / 3));
                        }
                        f.Add("\"curve\":[" + string.Join(",", cv.ToArray()) + "]");
                    }
                    sb.Append(string.Join(",", f.ToArray()));
                    sb.Append("}");
                    ks.Add(sb.ToString());
                }
                inner.Add("\t\t\t\"" + tl.Kind + "\":[\n\t\t\t\t" + string.Join(",\n\t\t\t\t", ks.ToArray()) + "\n\t\t\t]");
            }
            outer.Add("\t\t\"" + kv.Key + "\":{\n" + string.Join(",\n", inner.ToArray()) + "\n\t\t}");
        }
        return string.Join(",\n", outer.ToArray());
    }

    // ===================== debug =====================
    // picture dimmed, gridpoints coloured by label, mesh hull and bones
    static void DebugOverlay(int[] gLabel, Piece p, string path)
    {
        using (Bitmap src = new Bitmap(Path.Combine(OutDir, NAME + ".png")))
        using (Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(255, 40, 44, 52));
            ColorMatrix cm = new ColorMatrix(); cm.Matrix33 = 0.55f;
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(cm);
                g.DrawImage(src, new Rectangle(0, 0, W, H), 0, 0, W, H, GraphicsUnit.Pixel, ia);
            }
            Random rnd = new Random(3);
            Color[] pal = new Color[Bones.Count];
            for (int i = 0; i < pal.Length; i++) pal[i] = Color.FromArgb(255, 60 + rnd.Next(195), 60 + rnd.Next(195), 60 + rnd.Next(195));
            foreach (int gp in p.Verts)
                using (SolidBrush br = new SolidBrush(pal[gLabel[gp]]))
                    g.FillRectangle(br, XS[gp % GW] - 3, YS[gp / GW] - 3, 6, 6);
            using (Pen hp = new Pen(Color.FromArgb(255, 255, 60, 60), 2))
                for (int k = 0; k < p.Hull; k++)
                {
                    int a = p.Verts[k], b = p.Verts[(k + 1) % p.Hull];
                    g.DrawLine(hp, XS[a % GW], YS[a / GW], XS[b % GW], YS[b / GW]);
                }
            using (Pen bp = new Pen(Color.Yellow, 3))
            using (Font font = new Font("Arial", 12, FontStyle.Bold))
                foreach (Bone b in Bones)
                {
                    if (!b.HasEnd) continue;
                    g.DrawLine(bp, (float)b.PSX, (float)b.PSY, (float)b.PEX, (float)b.PEY);
                    g.FillEllipse(Brushes.Red, (float)b.PSX - 5, (float)b.PSY - 5, 10, 10);
                    g.DrawString(b.Name, font, Brushes.White, (float)(b.PSX + b.PEX) / 2 + 4, (float)(b.PSY + b.PEY) / 2);
                }
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
