// Dancer girl (source/dancer_source.png, 1024x1536, transparent background) Spine generator (C# 5, .NET Framework).
// Adapted from HoloGirlGen.cs. The whole picture is ONE weighted mesh. The bust area uses a finer 8px grid and soft
// distance-based weights (no hard cut) so the skin stretches smoothly: upper slope (bustTop) and underside (bust)
// bounce separately. Also: chest breathing, head, earrings, hair pins, long hair, armband dangle, waist coin chains.
// The atlas page IS the source picture (1:1, region at 0,0, no padding), so mesh UVs equal the picture UVs.
// Usage: DancerGen.exe <source.png> <outDir>   (or: build.ps1 -Gen DancerGen)
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
    const string NAME = "DancerGirl", SLOT = "dancer_main";

    static Img Src;
    static int W, H;
    static List<Bone> Bones = new List<Bone>();
    static Dictionary<string, Bone> BM = new Dictionary<string, Bone>();
    static int[] XS, YS;
    static int NX, NY; // cell counts
    static int GW;     // gridpoints per row = NX+1

    // ---------------- regions (source px) ----------------
    static double[] EarLPoly = { 345,315, 392,315, 392,412, 345,412 };
    static double[] EarRPoly = { 522,392, 560,392, 560,478, 522,478 };
    // pendants hanging from the hair flowers
    static double[] PinLPoly = { 318,150, 350,150, 352,242, 318,242 };
    static double[] PinRPoly = { 628,235, 668,235, 668,312, 628,312 };
    static double[] TasselRPoly = { 628,312, 685,312, 685,430, 628,430 };
    // dangling gems under the left armband
    static double[] DangleLPoly = { 196,760, 232,760, 232,838, 196,838 };
    // coin chains hanging from the waist belt
    static double[] ChainLPoly = { 195,1070, 300,1030, 305,1130, 255,1200, 232,1262, 205,1250 };
    static double[] ChainRPoly = { 585,1020, 615,1020, 620,1130, 590,1130 };
    // bust ellipses (centre x, y, radius x, y): the lower part covers the cups down to the gold band, the upper part
    // covers the skin slope above the cups. Weights fall off smoothly outside the ellipse (see SoftBust)
    static double[] BustL = { 390, 740, 90, 100 };
    static double[] BustR = { 605, 722, 82, 100 };
    static double[] BustTopL = { 398, 635, 95, 50 };
    static double[] BustTopR = { 590, 610, 88, 48 };
    // face + top hair + buns + the hair beside the face; bottom edge follows the chin / jaw line
    static double[] HeadPoly = { 315,0, 725,0, 730,250, 700,340, 640,430, 560,445, 480,440, 400,425, 340,395, 312,300, 305,150 };
    // long hair: outer fall left of the arm, and the strand between the neck and the left shoulder
    static double[] HairLOuterPoly = { 150,712, 190,712, 190,780, 175,900, 160,1030, 150,1120, 140,1190, 60,1150, 96,1040, 100,910, 118,790 };
    static double[] HairLInnerPoly = { 294,430, 340,420, 332,500, 306,560, 296,650, 290,705, 270,705, 280,620, 285,500 };
    static double[] HairRPoly = { 585,440, 640,430, 645,510, 618,570, 590,560 };
    const double WaistY = 975; // above: chest family (breathes), below: root (hips, skirt, legs, hands)

    static string[] HairLSet = { "hairL1", "hairL2", "hairL3" };
    static string[] TasselRSet = { "pinR", "tasselR" };
    static string[] ChainLSet = { "chainL1", "chainL2" };

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
        // soft bust override: smooth falloff instead of a hard ellipse cut. Only touches chest / bust vertices
        int iChest = BM["chest"].Index, iBL = BM["bustL"].Index, iBR = BM["bustR"].Index, iTL = BM["bustTopL"].Index, iTR = BM["bustTopR"].Index;
        for (int g = 0; g < nG; g++)
        {
            if (!used[g]) continue;
            int lb = gLabel[g];
            if (lb != iChest && lb != iBL && lb != iBR && lb != iTL && lb != iTR) continue;
            double px = XS[g % GW], py = YS[g / GW];
            double fTL = SoftBust(BustTopL, px, py), fTR = SoftBust(BustTopR, px, py);
            double fL = SoftBust(BustL, px, py), fR = SoftBust(BustR, px, py);
            if (fTL + fTR + fL + fR <= 0) continue;
            double[] w = new double[nb];
            w[iTL] = fTL; w[iTR] = fTR;
            w[iBL] = fL * (1 - fTL); w[iBR] = fR * (1 - fTR);
            double sum = w[iTL] + w[iTR] + w[iBL] + w[iBR];
            if (sum > 1) { for (int b = 0; b < nb; b++) w[b] /= sum; sum = 1; }
            w[iChest] = 1 - sum;
            Wt[g] = w;
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
        AddBone("chest", "root", 480, 980, 500, 580, "ffd700ff");
        AddBone("head", "chest", 505, 455, 505, 60, "ff4040ff").Inherit = "noScale";
        AddBone("pinL", "head", 332, 160, 335, 235, "ff80c0ff");
        AddBone("pinR", "head", 648, 235, 652, 315, "ff80c0ff");
        AddBone("tasselR", "pinR", 652, 315, 662, 425, "ff80c0ff");
        AddBone("earL", "head", 368, 320, 366, 410, "ffb0d0ff");
        AddBone("earR", "head", 538, 395, 535, 475, "ffb0d0ff");
        AddBone("hairL1", "head", 318, 340, 300, 640, "c080ffff");
        AddBone("hairL2", "chest", 150, 720, 112, 950, "c080ffff");
        AddBone("hairL3", "hairL2", 112, 950, 95, 1170, "c080ffff");
        AddBone("hairR1", "head", 610, 440, 600, 560, "c080ffff");
        // bust pivots at the centre of each breast (bone points down, scaleX = vertical)
        AddBone("bustL", "chest", 390, 738, 388, 860, "ff9060ff");
        AddBone("bustR", "chest", 605, 722, 603, 840, "ff9060ff");
        AddBone("bustTopL", "bustL", 398, 640, 398, 692, "ffb080ff");
        AddBone("bustTopR", "bustR", 590, 615, 590, 667, "ffb080ff");
        AddBone("dangleL", "chest", 213, 760, 212, 835, "ff3060ff");
        AddBone("chainL1", "root", 285, 1045, 240, 1150, "ffe080ff");
        AddBone("chainL2", "chainL1", 240, 1150, 212, 1255, "ffe080ff");
        AddBone("chainR", "root", 598, 1020, 602, 1125, "ffe080ff");
    }

    static int Label(double x, double y)
    {
        if (InPoly(EarLPoly, x, y)) return BM["earL"].Index;
        if (InPoly(EarRPoly, x, y)) return BM["earR"].Index;
        if (InPoly(PinLPoly, x, y)) return BM["pinL"].Index;
        if (InPoly(PinRPoly, x, y) || InPoly(TasselRPoly, x, y)) return Nearest(TasselRSet, x, y);
        if (InPoly(DangleLPoly, x, y)) return BM["dangleL"].Index;
        if (InPoly(ChainLPoly, x, y)) return Nearest(ChainLSet, x, y);
        if (InPoly(ChainRPoly, x, y)) return BM["chainR"].Index;
        if (InPoly(HeadPoly, x, y)) return BM["head"].Index;
        // bust before hair: the hair locks lying on the breasts bounce with them
        if (InEllipse(BustTopL, x, y)) return BM["bustTopL"].Index;
        if (InEllipse(BustTopR, x, y)) return BM["bustTopR"].Index;
        if (InEllipse(BustL, x, y)) return BM["bustL"].Index;
        if (InEllipse(BustR, x, y)) return BM["bustR"].Index;
        if (InPoly(HairLOuterPoly, x, y) || InPoly(HairLInnerPoly, x, y)) return Nearest(HairLSet, x, y);
        if (InPoly(HairRPoly, x, y)) return BM["hairR1"].Index;
        return y < WaistY ? BM["chest"].Index : 0;
    }

    static double SmoothStep(double e0, double e1, double v)
    {
        double t = Math.Max(0, Math.Min(1, (v - e0) / (e1 - e0)));
        return t * t * (3 - 2 * t);
    }

    // normalised elliptical distance -> 1 inside 0.7, smooth to 0 at 1.5 (so the skin around the cups moves a little too)
    static double SoftBust(double[] e, double x, double y)
    {
        double dx = (x - e[0]) / e[2], dy = (y - e[1]) / e[3];
        return 1 - SmoothStep(0.7, 1.5, Math.Sqrt(dx * dx + dy * dy));
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
    static int[] ColOfX, RowOfY;

    // 16px everywhere, 8px across the bust window (x 272-720, y 560-912) so the stretched skin deforms smoothly
    static void BuildGrid()
    {
        List<int> xs = new List<int>();
        for (int x = 0; x < W; x += (x >= 272 && x < 720) ? 8 : CELL) xs.Add(x);
        xs.Add(W);
        List<int> ys = new List<int>();
        for (int y = 0; y < H; y += (y >= 560 && y < 912) ? 8 : CELL) ys.Add(y);
        ys.Add(H);
        XS = xs.ToArray(); YS = ys.ToArray();
        NX = XS.Length - 1; NY = YS.Length - 1; GW = NX + 1;
        ColOfX = new int[W + 1]; RowOfY = new int[H + 1];
        for (int i = 0; i < NX; i++) for (int x = XS[i]; x < XS[i + 1]; x++) ColOfX[x] = i;
        for (int j = 0; j < NY; j++) for (int y = YS[j]; y < YS[j + 1]; y++) RowOfY[y] = j;
        ColOfX[W] = NX - 1; RowOfY[H] = NY - 1;
    }

    static int CellOf(int x, int y) { return RowOfY[y] * NX + ColOfX[x]; }

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
        // slow breath; the bust rides the breath a beat late and bounces softly at the top of each breath
        // (underside = bustL/R, upper slope = bustTopL/R, a beat after), head tilts, dangles trail behind.
        // Every channel is 0 at t=0 and t=TI.
        A = new Anim("idle"); list.Add(A);
        double TI = 3.2, w = 2 * Math.PI / TI;
        int N = 32;
        Func<double, double, double, double> s = (amp, ph, t) => amp * (0.5 * Math.Cos(ph) - 0.5 * Math.Cos(w * t - ph));
        Func<double, double, double, double> s2 = (amp, ph, t) => amp * (0.5 * Math.Cos(ph) - 0.5 * Math.Cos(2 * w * t - ph));
        Func<double, double, double, double> s3 = (amp, ph, t) => amp * (0.5 * Math.Cos(ph) - 0.5 * Math.Cos(3 * w * t - ph));

        FS("chest", t => 1 + s(0.02, 0.3, t), t => 1 + s(0.014, 0.3, t), TI, N);
        FR("chest", t => s(0.4, 0.5, t), TI, N);
        // bust: lifts ~4px, grows ~4.5% and swings a little sideways, small after-bounce on top
        FT("bustL", t => s(-0.8, 0.8, t), t => s(4.0, 0.6, t) + s2(-1.2, 2.0, t) + s3(0.5, 2.6, t), TI, N * 2);
        FT("bustR", t => s(0.8, 0.9, t), t => s(4.2, 0.7, t) + s2(-1.3, 2.1, t) + s3(0.55, 2.7, t), TI, N * 2);
        FS("bustL", t => 1 + s(0.045, 0.6, t) + s2(0.008, 2.0, t), t => 1 + s(0.03, 0.6, t), TI, N * 2);
        FS("bustR", t => 1 + s(0.045, 0.7, t) + s2(0.008, 2.1, t), t => 1 + s(0.03, 0.7, t), TI, N * 2);
        FR("bustL", t => s(1.2, 1.0, t), TI, N);
        FR("bustR", t => s(-1.2, 1.1, t), TI, N);
        // upper slope: extra lift on the inhale, trailing the breast, then a soft wobble
        FT("bustTopL", t => 0, t => s(4.6, 1.0, t) + s2(-1.8, 2.4, t) + s3(0.9, 3.0, t), TI, N * 2);
        FT("bustTopR", t => 0, t => s(4.8, 1.1, t) + s2(-1.9, 2.5, t) + s3(0.95, 3.1, t), TI, N * 2);
        FS("bustTopL", t => 1 + s(0.02, 1.2, t), t => 1 + s(0.015, 1.2, t), TI, N);
        FS("bustTopR", t => 1 + s(0.02, 1.3, t), t => 1 + s(0.015, 1.3, t), TI, N);
        FR("head", t => s(1.1, 0.9, t), TI, N);
        FT("head", t => s(0.6, 0.9, t), t => s(-1.0, 1.1, t), TI, N);
        FR("earL", t => s(-5.0, 2.0, t) + s2(-1.2, 2.6, t), TI, N);
        FR("earR", t => s(-5.5, 2.1, t) + s2(-1.2, 2.7, t), TI, N);
        FR("pinL", t => s(-4.0, 1.9, t) + s2(-1.0, 2.5, t), TI, N);
        FR("pinR", t => s(-4.0, 1.9, t) + s2(-1.0, 2.5, t), TI, N);
        FR("tasselR", t => s(-3.5, 2.5, t) + s2(-0.9, 3.0, t), TI, N);
        FR("hairL1", t => s(-0.8, 1.6, t), TI, N);
        FR("hairL2", t => s(-0.9, 2.0, t), TI, N);
        FR("hairL3", t => s(-1.4, 2.5, t), TI, N);
        FR("hairR1", t => s(-0.9, 1.7, t), TI, N);
        FR("dangleL", t => s(3.0, 1.8, t), TI, N);
        FR("chainL1", t => s(1.4, 1.5, t), TI, N);
        FR("chainL2", t => s(1.8, 2.2, t), TI, N);
        FR("chainR", t => s(-1.2, 1.7, t), TI, N);

        // ---------------- bounce (loop, 1.1s) ----------------
        // lively bounce: the body hops, the breasts lag behind and flop (underside first, upper slope a beat later,
        // with an overshoot), the dangles and hair fly. Every channel is 0 at t=0 and t=TB.
        A = new Anim("bounce"); list.Add(A);
        double TB = 1.1, wb = 2 * Math.PI / TB;
        int NB = 44;
        Func<double, double, double, double> b1 = (amp, ph, t) => amp * (0.5 * Math.Cos(ph) - 0.5 * Math.Cos(wb * t - ph));
        Func<double, double, double, double> b2 = (amp, ph, t) => amp * (0.5 * Math.Cos(ph) - 0.5 * Math.Cos(2 * wb * t - ph));
        Func<double, double, double, double> b3 = (amp, ph, t) => amp * (0.5 * Math.Cos(ph) - 0.5 * Math.Cos(3 * wb * t - ph));
        FT("chest", t => 0, t => b1(7.0, 0.0, t) - b2(1.5, 0.4, t), TB, NB);
        FS("chest", t => 1 + b1(0.012, 0.2, t), t => 1 - b1(0.006, 0.2, t), TB, NB);
        FR("chest", t => b1(0.8, 0.4, t), TB, NB);
        // underside: lags the hop and drops, then springs back above rest and settles (pendulum feel)
        FT("bustL", t => b1(-2.0, 1.0, t), t => b1(-9.0, 1.0, t) + b2(5.0, 2.2, t) + b3(1.6, 3.0, t), TB, NB * 2);
        FT("bustR", t => b1(2.0, 1.1, t), t => b1(-9.5, 1.1, t) + b2(5.2, 2.3, t) + b3(1.7, 3.1, t), TB, NB * 2);
        FS("bustL", t => 1 - b1(0.05, 1.0, t) + b2(0.02, 2.2, t), t => 1 + b1(0.035, 1.0, t), TB, NB * 2);
        FS("bustR", t => 1 - b1(0.05, 1.1, t) + b2(0.02, 2.3, t), t => 1 + b1(0.035, 1.1, t), TB, NB * 2);
        FR("bustL", t => b1(2.4, 1.2, t) + b2(-1.0, 2.4, t), TB, NB);
        FR("bustR", t => b1(-2.4, 1.3, t) + b2(1.0, 2.5, t), TB, NB);
        // upper slope: trails the underside by another beat and overshoots more (the top flops)
        FT("bustTopL", t => 0, t => b1(-12.0, 1.6, t) + b2(6.5, 2.8, t) + b3(2.2, 3.6, t), TB, NB * 2);
        FT("bustTopR", t => 0, t => b1(-12.5, 1.7, t) + b2(6.8, 2.9, t) + b3(2.3, 3.7, t), TB, NB * 2);
        FS("bustTopL", t => 1 - b1(0.03, 1.6, t), t => 1 + b1(0.025, 1.6, t), TB, NB);
        FS("bustTopR", t => 1 - b1(0.03, 1.7, t), t => 1 + b1(0.025, 1.7, t), TB, NB);
        FR("head", t => b1(1.8, 1.0, t), TB, NB);
        FT("head", t => 0, t => b1(-2.5, 1.2, t), TB, NB);
        FR("earL", t => b1(-9.0, 2.0, t) + b2(-3.0, 2.8, t), TB, NB);
        FR("earR", t => b1(-9.5, 2.1, t) + b2(-3.0, 2.9, t), TB, NB);
        FR("pinL", t => b1(-7.0, 1.9, t) + b2(-2.0, 2.7, t), TB, NB);
        FR("pinR", t => b1(-7.0, 1.9, t) + b2(-2.0, 2.7, t), TB, NB);
        FR("tasselR", t => b1(-6.0, 2.6, t) + b2(-2.0, 3.2, t), TB, NB);
        FR("hairL1", t => b1(-1.4, 1.7, t), TB, NB);
        FR("hairL2", t => b1(-1.6, 2.1, t), TB, NB);
        FR("hairL3", t => b1(-2.6, 2.7, t), TB, NB);
        FR("hairR1", t => b1(-1.6, 1.8, t), TB, NB);
        FR("dangleL", t => b1(5.0, 1.9, t) + b2(1.5, 2.8, t), TB, NB);
        FR("chainL1", t => b1(2.4, 1.6, t), TB, NB);
        FR("chainL2", t => b1(3.2, 2.3, t), TB, NB);
        FR("chainR", t => b1(-2.2, 1.8, t), TB, NB);

        // ---------------- touch (one shot, back to setup pose) ----------------
        // tap reaction: a small happy hop of the upper body (0.10-0.45s), both bust layers bounce with a damped
        // spring (underside first, upper slope a beat later), the head tilts, every dangle gets kicked and rings out.
        A = new Anim("touch"); list.Add(A);
        double TT = 1.6;
        int NT = 64;
        Func<double, double> hop = t => -2.5 * Bump(t, 0.0, 0.16) + 7.0 * Bump(t, 0.12, 0.62) - 1.2 * Bump(t, 0.55, 0.95);
        FT("chest", t => 0, hop, TT, NT);
        FS("chest", t => 1 + 0.018 * Bump(t, 0.12, 0.62) - 0.008 * Bump(t, 0.0, 0.16), t => 1 + 0.008 * Bump(t, 0.12, 0.62), TT, NT);
        Func<double, double> bounceL = t => -16.0 * Ring(t, 0.16, 2.9, 3.0, TT) - 2.0 * Bump(t, 0.05, 0.25);
        Func<double, double> bounceR = t => -16.5 * Ring(t, 0.19, 2.8, 2.9, TT) - 2.0 * Bump(t, 0.07, 0.27);
        FT("bustL", t => 2.5 * Ring(t, 0.16, 2.9, 3.0, TT), bounceL, TT, NT);
        FT("bustR", t => -2.5 * Ring(t, 0.19, 2.8, 2.9, TT), bounceR, TT, NT);
        FS("bustL", t => 1 - 0.006 * bounceL(t), t => 1 + 0.003 * bounceL(t), TT, NT);
        FS("bustR", t => 1 - 0.006 * bounceR(t), t => 1 + 0.003 * bounceR(t), TT, NT);
        FR("bustL", t => 3.5 * Ring(t, 0.18, 2.7, 2.9, TT), TT, NT);
        FR("bustR", t => -3.5 * Ring(t, 0.21, 2.6, 2.8, TT), TT, NT);
        Func<double, double> topL = t => -19.0 * Ring(t, 0.24, 3.1, 2.8, TT);
        Func<double, double> topR = t => -20.0 * Ring(t, 0.27, 3.0, 2.7, TT);
        FT("bustTopL", t => 0, topL, TT, NT);
        FT("bustTopR", t => 0, topR, TT, NT);
        FS("bustTopL", t => 1 - 0.004 * topL(t), t => 1 + 0.003 * topL(t), TT, NT);
        FS("bustTopR", t => 1 - 0.004 * topR(t), t => 1 + 0.003 * topR(t), TT, NT);
        FR("head", t => 4.0 * Bump(t, 0.1, 1.1) - 0.8 * Bump(t, 0.95, 1.5), TT, NT);
        FT("head", t => 2.0 * Bump(t, 0.1, 1.1), t => 1.5 * Bump(t, 0.12, 0.7), TT, NT);
        FR("earL", t => -12.0 * Ring(t, 0.2, 1.6, 2.4, TT), TT, NT);
        FR("earR", t => -13.0 * Ring(t, 0.22, 1.5, 2.4, TT), TT, NT);
        FR("pinL", t => -9.0 * Ring(t, 0.2, 1.7, 2.6, TT), TT, NT);
        FR("pinR", t => -9.0 * Ring(t, 0.22, 1.7, 2.6, TT), TT, NT);
        FR("tasselR", t => -8.0 * Ring(t, 0.28, 1.5, 2.5, TT), TT, NT);
        FR("hairL1", t => -1.5 * Ring(t, 0.2, 1.2, 2.5, TT), TT, NT);
        FR("hairL2", t => -1.6 * Ring(t, 0.26, 1.2, 2.5, TT), TT, NT);
        FR("hairL3", t => -2.4 * Ring(t, 0.32, 1.2, 2.4, TT), TT, NT);
        FR("hairR1", t => -1.8 * Ring(t, 0.22, 1.2, 2.5, TT), TT, NT);
        FR("dangleL", t => 7.0 * Ring(t, 0.22, 1.8, 2.8, TT), TT, NT);
        FR("chainL1", t => 3.0 * Ring(t, 0.26, 1.3, 2.4, TT), TT, NT);
        FR("chainL2", t => 4.0 * Ring(t, 0.32, 1.3, 2.3, TT), TT, NT);
        FR("chainR", t => -3.0 * Ring(t, 0.28, 1.3, 2.4, TT), TT, NT);

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
        sb.Append("{\n\"skeleton\":{\"hash\":\"dancerGenV1\",\"spine\":\"4.2.43\",\"x\":" + F(-OX) + ",\"y\":" + F(OY - H) + ",\"width\":" + W + ",\"height\":" + H + ",\"fps\":30,\"images\":\"./images/\",\"audio\":\"\"},\n");

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
