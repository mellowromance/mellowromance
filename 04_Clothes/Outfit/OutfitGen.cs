// Strawberry-cream-cake lolita outfit for the SD character (04_Clothes guide).
// Big shapes / silhouettes are low-poly meshes; lace, piping, small folds and decorations live in the textures.
// Five separate parts (headdress, top, skirt, legwear, shoes), each with its own 0-1 UV layout + texture.
// Every mesh is authored in the local space of the character bone it rides on (game rig), so the game can
// attach it directly; the OBJ export bakes the bones' rest transforms into one character-space model.
// Built by build_outfit.ps1 (C# 5 / Add-Type).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text;

public class OMesh {
    public string Name; public string Bone; public bool DS; public bool Outline;
    public List<double[]> V = new List<double[]>();
    public List<double[]> T = new List<double[]>();
    public List<int[]> F = new List<int[]>();          // interleaved (v, t) corner pairs
    public List<int[]> Hems = new List<int[]>();       // closed vertex loops along frill hems (game draws a thin ink line)
    public OMesh(string name, string bone, bool ds, bool outline) { Name = name; Bone = bone; DS = ds; Outline = outline; }
    public int AddV(double[] p) { V.Add(new double[] { p[0], p[1], p[2] }); return V.Count - 1; }
    public int AddT(double u, double v) { T.Add(new double[] { u, v }); return T.Count - 1; }
}

public class ORect {
    public string Name; public double U0, V0, U1, V1;
    public ORect(string n, double u0, double v0, double u1, double v1) { Name = n; U0 = u0; V0 = v0; U1 = u1; V1 = v1; }
    public double U(double s) { return U0 + (U1 - U0) * s; }
    public double V(double s) { return V0 + (V1 - V0) * s; }
}

public class OPart {
    public string Name; public string Label;
    public List<OMesh> Meshes = new List<OMesh>();
    public List<ORect> Islands = new List<ORect>();
    public OPart(string n, string label) { Name = n; Label = label; }
    public ORect Isl(string n, double u0, double v0, double u1, double v1) { ORect r = new ORect(n, u0, v0, u1, v1); Islands.Add(r); return r; }
    public ORect Find(string n) { foreach (ORect r in Islands) if (r.Name == n) return r; return null; }
    public OMesh Add(OMesh m) { Meshes.Add(m); return m; }
}

public delegate void VMod(int col, int ring, double a, double[] p);
public delegate double[] XF(double[] p);

public static class OutfitGen {
    const double PI = Math.PI;
    static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    // ---------------------------------------------------------------- vector helpers
    static double[] V3(double x, double y, double z) { return new double[] { x, y, z }; }
    static double[] Add(double[] a, double[] b) { return V3(a[0] + b[0], a[1] + b[1], a[2] + b[2]); }
    static double[] Sub(double[] a, double[] b) { return V3(a[0] - b[0], a[1] - b[1], a[2] - b[2]); }
    static double[] Mul(double[] a, double s) { return V3(a[0] * s, a[1] * s, a[2] * s); }
    static double Dot(double[] a, double[] b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
    static double[] Cross(double[] a, double[] b) { return V3(a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]); }
    static double Len(double[] a) { return Math.Sqrt(Dot(a, a)); }
    static double[] Norm(double[] a) { double l = Len(a); return l > 1e-12 ? Mul(a, 1 / l) : V3(0, 1, 0); }
    static XF Basis(double[] o, double[] X, double[] Y, double[] Z) {
        return delegate(double[] p) { return V3(o[0] + X[0] * p[0] + Y[0] * p[1] + Z[0] * p[2], o[1] + X[1] * p[0] + Y[1] * p[1] + Z[1] * p[2], o[2] + X[2] * p[0] + Y[2] * p[1] + Z[2] * p[2]); };
    }
    static XF Move(double x, double y, double z) { return delegate(double[] p) { return V3(p[0] + x, p[1] + y, p[2] + z); }; }
    static XF Chain(XF first, XF then) { return delegate(double[] p) { return then(first(p)); }; }

    // ---------------------------------------------------------------- mesh builders
    /// Revolve a {r, y} profile (bottom → top) about local Y. Seam at the back, so u = 0.5 is the FRONT centre;
    /// v runs along the profile (bottom v0 → top v1). Positions are shared across the seam (UVs are not).
    public static int[] Hem(int start, int segs) { int[] h = new int[segs]; for (int c = 0; c < segs; c++) h[c] = start + c; return h; }
    public static int Lathe(OMesh m, double[][] prof, int segs, double sz, ORect R, VMod mod, XF xf) {
        int start = m.V.Count;
        int n = prof.Length; double[] L = new double[n];
        for (int i = 1; i < n; i++) { double dr = prof[i][0] - prof[i - 1][0], dy = prof[i][1] - prof[i - 1][1]; L[i] = L[i - 1] + Math.Sqrt(dr * dr + dy * dy); }
        double tot = L[n - 1] > 0 ? L[n - 1] : 1;
        int[,] P = new int[n, segs];
        for (int r = 0; r < n; r++)
            for (int c = 0; c < segs; c++) {
                double a = -PI + 2 * PI * c / segs;
                double[] p = V3(prof[r][0] * Math.Sin(a), prof[r][1], prof[r][0] * Math.Cos(a));
                if (mod != null) mod(c, r, a, p);
                p[2] *= sz;
                if (xf != null) p = xf(p);
                P[r, c] = m.AddV(p);
            }
        int[,] Tt = new int[n, segs + 1];
        for (int r = 0; r < n; r++) for (int c = 0; c <= segs; c++) Tt[r, c] = m.AddT(R.U((double)c / segs), R.V(L[r] / tot));
        for (int r = 0; r < n - 1; r++)
            for (int c = 0; c < segs; c++) {
                int c2 = (c + 1) % segs;
                m.F.Add(new int[] { P[r, c], Tt[r, c], P[r, c2], Tt[r, c + 1], P[r + 1, c2], Tt[r + 1, c + 1], P[r + 1, c], Tt[r + 1, c] });
            }
        return start;
    }

    /// Sweep a closed cross-section (offsets in the path's N/B frame, counter-clockwise N → B) along a path.
    /// u runs along the path (arc length), v around the section.
    public static void Sweep(OMesh m, List<double[]> path, List<double[]> nrm, double cn, double cb, double[][] cross, bool closedPath, ORect R, Func<int, double> scale) {
        int N = path.Count, K = cross.Length;
        int[,] P = new int[N, K];
        double[] L = new double[N + 1];
        for (int i = 1; i <= N; i++) { if (i == N && !closedPath) break; L[i] = L[i - 1] + Len(Sub(path[i % N], path[i - 1])); }
        double tot = closedPath ? L[N] : L[N - 1];
        for (int i = 0; i < N; i++) {
            double[] prev = closedPath ? path[(i - 1 + N) % N] : path[Math.Max(0, i - 1)];
            double[] next = closedPath ? path[(i + 1) % N] : path[Math.Min(N - 1, i + 1)];
            double[] Tg = Norm(Sub(next, prev));
            double[] Nn = Norm(Sub(nrm[i], Mul(Tg, Dot(nrm[i], Tg))));
            double[] Bb = Cross(Tg, Nn);
            double s = scale == null ? 1 : scale(i);
            for (int j = 0; j < K; j++) {
                double n = cn + cross[j][0] * s, b = cb + cross[j][1] * s;
                P[i, j] = m.AddV(Add(path[i], Add(Mul(Nn, n), Mul(Bb, b))));
            }
        }
        int cols = closedPath ? N + 1 : N;
        int[,] Tt = new int[cols, K + 1];
        for (int i = 0; i < cols; i++) for (int j = 0; j <= K; j++) Tt[i, j] = m.AddT(R.U(L[i] / tot), R.V((double)j / K));
        int segs = closedPath ? N : N - 1;
        for (int i = 0; i < segs; i++)
            for (int j = 0; j < K; j++) {
                int i2 = (i + 1) % N, j2 = (j + 1) % K;
                m.F.Add(new int[] { P[i, j], Tt[i, j], P[i, j2], Tt[i, j + 1], P[i2, j2], Tt[i + 1, j + 1], P[i2, j], Tt[i + 1, j] });
            }
    }

    /// Open two-row strip (frills): u along the rows, v inner (0) → outer (1).
    public static void Strip(OMesh m, List<double[]> inner, List<double[]> outer, ORect R) {
        int N = inner.Count; int[] a = new int[N], b = new int[N], ta = new int[N], tb = new int[N];
        for (int i = 0; i < N; i++) {
            a[i] = m.AddV(inner[i]); b[i] = m.AddV(outer[i]);
            ta[i] = m.AddT(R.U((double)i / (N - 1)), R.V(0)); tb[i] = m.AddT(R.U((double)i / (N - 1)), R.V(1));
        }
        for (int i = 0; i < N - 1; i++) m.F.Add(new int[] { a[i], ta[i], a[i + 1], ta[i + 1], b[i + 1], tb[i + 1], b[i], tb[i] });
    }

    /// Triangle fan (caps, leaves). uvf maps a LOCAL point to 0-1 inside the island.
    public static void Fan(OMesh m, double[] c, List<double[]> ring, ORect R, Func<double[], double[]> uvf, XF xf) {
        double[] cu = uvf(c);
        int ci = m.AddV(xf == null ? c : xf(c)), ct = m.AddT(R.U(cu[0]), R.V(cu[1]));
        int K = ring.Count; int[] ids = new int[K], tids = new int[K];
        for (int k = 0; k < K; k++) { double[] u = uvf(ring[k]); ids[k] = m.AddV(xf == null ? ring[k] : xf(ring[k])); tids[k] = m.AddT(R.U(u[0]), R.V(u[1])); }
        for (int k = 0; k < K; k++) m.F.Add(new int[] { ci, ct, ids[k], tids[k], ids[(k + 1) % K], tids[(k + 1) % K] });
    }

    /// Mirror a left-side mesh to the right (x → -x, winding reversed), same UVs (L/R share their islands).
    public static OMesh Mirror(OMesh s, string bone) {
        OMesh m = new OMesh(s.Name, bone, s.DS, s.Outline);
        foreach (double[] v in s.V) m.V.Add(V3(-v[0], v[1], v[2]));
        foreach (double[] t in s.T) m.T.Add(new double[] { t[0], t[1] });
        foreach (int[] h in s.Hems) m.Hems.Add((int[])h.Clone());
        foreach (int[] f in s.F) {
            int c = f.Length / 2; int[] g = new int[f.Length];
            for (int k = 0; k < c; k++) { g[k * 2] = f[(c - 1 - k) * 2]; g[k * 2 + 1] = f[(c - 1 - k) * 2 + 1]; }
            m.F.Add(g);
        }
        return m;
    }

    static VMod Zigzag(double outF, double drop) {   // wave the bottom ring: every other column out + down → frill
        return delegate(int c, int r, double a, double[] p) { if (r == 0 && c % 2 == 1) { p[0] *= outF; p[2] *= outF; p[1] -= drop; } };
    }

    // ---------------------------------------------------------------- headdress arc (head-group space)
    // The band sits over the crown like a bonnet band, in a plane tilted back by HT, centred on the skull.
    static readonly double[] HC = { 0, 0.17, 0.0 };
    const double HT = 0.16, HR = 0.222, HMAX = 1.01;
    static double[] RotX(double[] p) { double c = Math.Cos(HT), s = Math.Sin(HT); return V3(p[0], p[1] * c - p[2] * s, p[1] * s + p[2] * c); }
    static double[] HD(double th) { return RotX(V3(Math.Sin(th), Math.Cos(th), 0)); }          // outward (N)
    static double[] HW() { return RotX(V3(0, 0, 1)); }                                          // forward (B)
    static double[] HTan(double th) { return RotX(V3(Math.Cos(th), -Math.Sin(th), 0)); }       // along the band
    static double[] HP(double th, double n, double b) { return Add(Add(HC, Mul(HD(th), HR + n)), Mul(HW(), b)); }

    static double[][] Circle(int k, double r) {
        double[][] c = new double[k][];
        for (int j = 0; j < k; j++) c[j] = new double[] { r * Math.Cos(2 * PI * j / k), r * Math.Sin(2 * PI * j / k) };
        return c;
    }

    // ================================================================ PARTS
    public static OPart Headdress() {
        OPart P = new OPart("headdress", "Headdress (bone: head)");
        ORect rBand = P.Isl("band", 0.0, 0.82, 1.0, 1.0), rFrill = P.Isl("frill (front+back)", 0.0, 0.66, 1.0, 0.80);
        ORect rPipe = P.Isl("cream piping", 0.0, 0.52, 1.0, 0.64), rBerry = P.Isl("strawberry", 0.0, 0.22, 0.24, 0.50);
        ORect rLeaf = P.Isl("leaf", 0.26, 0.30, 0.46, 0.50), rDollop = P.Isl("cream dollop", 0.48, 0.30, 0.70, 0.50);
        ORect rLoop = P.Isl("bow loop", 0.0, 0.0, 0.40, 0.20), rKnot = P.Isl("bow knot", 0.42, 0.0, 0.56, 0.14), rTail = P.Isl("bow tail", 0.58, 0.0, 0.78, 0.20);

        // band: thick strip (rectangular section) over the crown, ends tucked into the twin-tail tops
        OMesh band = P.Add(new OMesh("band", "head", false, true));
        List<double[]> path = new List<double[]>(), nrm = new List<double[]>();
        for (int i = 0; i <= 12; i++) { double th = -HMAX + 2 * HMAX * i / 12; path.Add(HP(th, 0, 0)); nrm.Add(HD(th)); }
        Sweep(band, path, nrm, 0.003, 0, new double[][] { new double[] { 0.016, -0.042 }, new double[] { 0.016, 0.042 }, new double[] { -0.014, 0.042 }, new double[] { -0.014, -0.042 } }, false, rBand, null);

        // ruffles: front (bigger, over the bangs) and back, scalloped outer edge
        OMesh frill = P.Add(new OMesh("frill", "head", true, false));
        for (int side = 0; side < 2; side++) {
            List<double[]> inn = new List<double[]>(), outr = new List<double[]>();
            double b0 = side == 0 ? 0.04 : -0.04, n1 = side == 0 ? 0.03 : 0.028, b1 = side == 0 ? 0.088 : -0.072;
            for (int i = 0; i <= 24; i++) {
                double th = -HMAX + 2 * HMAX * i / 24, k = i % 2 == 0 ? 1.0 : 0.7;
                inn.Add(HP(th, 0.004, b0)); outr.Add(HP(th, 0.004 + (n1 - 0.004) * k, b0 + (b1 - b0) * k));
            }
            Strip(frill, inn, outr, rFrill);
        }

        // whipped-cream roll along the top of the band (bumpy → piped cream)
        OMesh pipe = P.Add(new OMesh("cream_piping", "head", false, true));
        path = new List<double[]>(); nrm = new List<double[]>();
        for (int i = 0; i <= 16; i++) { double th = -HMAX * 0.96 + 2 * HMAX * 0.96 * i / 16; path.Add(HP(th, 0, 0)); nrm.Add(HD(th)); }
        Sweep(pipe, path, nrm, 0.03, 0, Circle(5, 0.017), false, rPipe, delegate(int i) { return i % 2 == 0 ? 1.18 : 0.86; });

        // strawberries (tip sunk into the cream, leaves up) + cream dollops
        OMesh berry = P.Add(new OMesh("strawberry", "head", false, true));
        OMesh leaf = P.Add(new OMesh("strawberry_leaf", "head", true, false));
        double[][] berryProf = { new double[] { 0, 0 }, new double[] { 0.017, 0.012 }, new double[] { 0.026, 0.03 }, new double[] { 0.024, 0.043 }, new double[] { 0.012, 0.051 }, new double[] { 0, 0.052 } };
        double[,] berries = { { -0.12, 1.0 }, { 0.22, 0.78 } };
        for (int k = 0; k < 2; k++) {
            double th = berries[k, 0], sc = berries[k, 1];
            XF F = Basis(HP(th, 0.028, 0.004), Mul(HTan(th), sc), Mul(HD(th), sc), Mul(HW(), sc));
            Lathe(berry, berryProf, 6, 1, rBerry, null, F);
            List<double[]> ring = new List<double[]>();
            for (int j = 0; j < 10; j++) { double a = 2 * PI * j / 10, r = j % 2 == 0 ? 0.027 : 0.011; ring.Add(V3(r * Math.Sin(a), j % 2 == 0 ? 0.045 : 0.05, r * Math.Cos(a))); }
            Fan(leaf, V3(0, 0.054, 0), ring, rLeaf, delegate(double[] p) { return new double[] { 0.5 + p[0] / 0.06, 0.5 + p[2] / 0.06 }; }, F);
        }
        OMesh dollop = P.Add(new OMesh("cream_dollop", "head", false, true));
        double[][] dolProf = { new double[] { 0.024, 0 }, new double[] { 0.021, 0.015 }, new double[] { 0.01, 0.028 }, new double[] { 0, 0.035 } };
        foreach (double th in new double[] { -0.62, -0.36, 0.46, 0.66 }) {
            XF F = Basis(HP(th, 0.026, 0), HTan(th), HD(th), HW());
            Lathe(dollop, dolProf, 6, 1, rDollop, delegate(int c, int r, double a, double[] p) { if (c % 2 == 1) { p[0] *= 0.78; p[2] *= 0.78; } }, F);
        }

        // light-blue side bow over the left twin tail: loops spread front/back, tails hang down; mirrored to the right
        OMesh bow = P.Add(new OMesh("bow", "head", false, true));
        double tb = 0.84;
        XF bowF = Basis(HP(tb, 0.032, 0.012), Mul(HW(), -1), Mul(HTan(tb), -1), HD(tb));
        double[][] knotProf = { new double[] { 0, -0.014 }, new double[] { 0.011, -0.01 }, new double[] { 0.015, 0 }, new double[] { 0.011, 0.01 }, new double[] { 0, 0.014 } };
        Lathe(bow, knotProf, 5, 0.75, rKnot, null, bowF);
        double[][] loopProf = { new double[] { 0, 0 }, new double[] { 0.015, 0.008 }, new double[] { 0.024, 0.028 }, new double[] { 0.02, 0.046 }, new double[] { 0, 0.054 } };
        double[][] tailProf = { new double[] { 0, 0 }, new double[] { 0.011, 0.006 }, new double[] { 0.011, 0.062 }, new double[] { 0, 0.068 } };
        foreach (int s in new int[] { 1, -1 }) {
            double[] Z = V3(0, 0, 1);
            double[] dl = Norm(V3(s * 0.94, 0.34, 0)); Lathe(bow, loopProf, 5, 1, rLoop, null, Chain(Basis(V3(0, 0, 0), Cross(dl, Z), dl, Mul(Z, 0.5)), bowF));
            double[] dt = Norm(V3(s * 0.35, -0.94, 0)); Lathe(bow, tailProf, 4, 1, rTail, null, Chain(Basis(V3(0, 0, 0), Cross(dt, Z), dt, Mul(Z, 0.35)), bowF));
        }
        OMesh bowR = Mirror(bow, "head"); bowR.Name = "bow_R"; bow.Name = "bow_L"; P.Add(bowR);
        return P;
    }

    public static OPart Top() {
        OPart P = new OPart("top", "One-piece top: bodice + puff sleeves + cuffs (bones: chest, sh_L/R, el_L/R)");
        ORect rBod = P.Isl("bodice (front = centre)", 0.0, 0.40, 0.70, 1.0), rCol = P.Isl("neckline frill", 0.72, 0.86, 1.0, 1.0);
        ORect rPuff = P.Isl("puff sleeve (L/R shared)", 0.0, 0.0, 0.48, 0.36), rSFr = P.Isl("sleeve frill", 0.50, 0.20, 1.0, 0.36);
        ORect rCuff = P.Isl("wrist cuff", 0.50, 0.0, 1.0, 0.18);
        // bodice: the character's existing hourglass torso silhouette (waist → chest → shoulders → stand collar)
        OMesh bod = P.Add(new OMesh("bodice", "chest", false, true));
        Lathe(bod, new double[][] { new double[] { 0.062, 0.0 }, new double[] { 0.058, 0.03 }, new double[] { 0.064, 0.07 }, new double[] { 0.08, 0.115 }, new double[] { 0.086, 0.15 },
            new double[] { 0.078, 0.18 }, new double[] { 0.054, 0.203 }, new double[] { 0.04, 0.222 }, new double[] { 0.037, 0.228 }, new double[] { 0.036, 0.255 }, new double[] { 0.032, 0.258 } }, 12, 0.82, rBod, null, null);
        OMesh col = P.Add(new OMesh("neckline_frill", "chest", true, false));
        Lathe(col, new double[][] { new double[] { 0.074, 0.186 }, new double[] { 0.056, 0.21 } }, 12, 0.82, rCol, Zigzag(1.1, 0.006), null);
        // puff sleeves (shoulder) + frilled hem, wrist cuffs (forearm)
        OMesh puff = new OMesh("puff_sleeve", "sh_L", false, true);
        Lathe(puff, new double[][] { new double[] { 0.036, -0.1 }, new double[] { 0.052, -0.093 }, new double[] { 0.063, -0.07 }, new double[] { 0.065, -0.045 }, new double[] { 0.058, -0.018 },
            new double[] { 0.042, 0.008 }, new double[] { 0.02, 0.026 } }, 8, 1, rPuff, null, Move(0.004, 0, 0));
        OMesh sfr = new OMesh("sleeve_frill", "sh_L", true, false);
        sfr.Hems.Add(Hem(Lathe(sfr, new double[][] { new double[] { 0.058, -0.114 }, new double[] { 0.045, -0.09 } }, 10, 1, rSFr, Zigzag(1.12, 0.008), Move(0.004, 0, 0)), 10));
        OMesh cuff = new OMesh("wrist_cuff", "el_L", true, false);
        Lathe(cuff, new double[][] { new double[] { 0.036, -0.143 }, new double[] { 0.028, -0.127 }, new double[] { 0.025, -0.108 } }, 10, 1, rCuff, Zigzag(1.12, 0.006), null);
        foreach (OMesh m in new OMesh[] { puff, sfr, cuff }) { P.Add(m); P.Add(Mirror(m, m.Bone.Replace("_L", "_R"))); }
        return P;
    }

    public static OPart Skirt() {
        OPart P = new OPart("skirt", "Skirt: petticoat + pleated skirt + scalloped cream overskirt + bloomers (bones: skirt, hips)");
        ORect rPet = P.Isl("petticoat frill", 0.0, 0.0, 1.0, 0.14), rSk = P.Isl("pink pleated skirt", 0.0, 0.16, 1.0, 0.48);
        ORect rOv = P.Isl("cream overskirt (swags)", 0.0, 0.50, 1.0, 0.78), rHem = P.Isl("hem piping", 0.0, 0.80, 1.0, 0.90);
        ORect rBl = P.Isl("bloomers", 0.0, 0.92, 0.40, 1.0), rWp = P.Isl("waist piping", 0.42, 0.92, 1.0, 1.0);
        const double DROP = 0.02;                     // whole skirt sits this much lower than the authored profiles
        XF off = Move(0, -DROP, -0.006);
        // Raised waist (skirt top at y 0.07 instead of 0.02) and a ROUND, full bell silhouette (petticoat volume):
        // the profile swells out right below the waist and rounds over into the hem instead of a straight A-line.
        // pink skirt: soft box pleats near the hem
        OMesh sk = P.Add(new OMesh("pleated_skirt", "skirt", true, true));
        sk.Hems.Add(Hem(sk.V.Count, 16));
        Lathe(sk, new double[][] { new double[] { 0.2, -0.14 }, new double[] { 0.214, -0.095 }, new double[] { 0.206, -0.045 }, new double[] { 0.172, 0.005 }, new double[] { 0.112, 0.05 }, new double[] { 0.07, 0.072 } },
            16, 0.9, rSk, delegate(int c, int r, double a, double[] p) { if (c % 2 == 0 && r < 3) { double f = 1 - 0.08 * (1 - r / 3.0); p[0] *= f; p[2] *= f; } }, off);
        // layer 1 (bottom): white petticoat ruffle peeking under the hem
        OMesh pet = P.Add(new OMesh("petticoat", "skirt", true, false));
        pet.Hems.Add(Hem(Lathe(pet, new double[][] { new double[] { 0.218, -0.166 }, new double[] { 0.196, -0.124 } }, 16, 0.9, rPet, Zigzag(1.08, 0.008), off), 16));
        // layer 2 (middle): extra white ruffle tier around the skirt, under the overskirt (shares the petticoat UVs)
        OMesh tier = P.Add(new OMesh("ruffle_tier", "skirt", true, false));
        tier.Hems.Add(Hem(Lathe(tier, new double[][] { new double[] { 0.236, -0.116 }, new double[] { 0.222, -0.074 } }, 16, 0.9, rPet, Zigzag(1.07, 0.008), off), 16));
        // layer 3 (top): cream overskirt with 8 swags — hem lifted at every swag join (even columns) → cake-tier scallops
        OMesh ov = P.Add(new OMesh("overskirt", "skirt", true, true));
        Lathe(ov, new double[][] { new double[] { 0.228, -0.075 }, new double[] { 0.222, -0.04 }, new double[] { 0.195, 0.0 }, new double[] { 0.145, 0.038 }, new double[] { 0.097, 0.06 }, new double[] { 0.08, 0.072 } },
            16, 0.9, rOv, delegate(int c, int r, double a, double[] p) { if (r == 0 && c % 2 == 0) { p[0] = 0.224 * Math.Sin(a); p[1] = -0.05; p[2] = 0.224 * Math.Cos(a); } }, off);
        // piped cream following the scalloped hem, and a cream ring at the (raised) waist
        OMesh hem = P.Add(new OMesh("hem_piping", "skirt", false, true));
        List<double[]> path = new List<double[]>(), nrm = new List<double[]>();
        for (int k = 0; k < 24; k++) {
            double a = -PI + 2 * PI * k / 24, y = -0.05 - 0.025 * Math.Abs(Math.Sin(4 * a)), r = 0.228 + (y + 0.075) * (0.222 - 0.228) / 0.035 + 0.004;
            path.Add(V3(r * Math.Sin(a), y - DROP, r * Math.Cos(a) * 0.9 - 0.006)); nrm.Add(V3(Math.Sin(a), 0, Math.Cos(a)));
        }
        Sweep(hem, path, nrm, 0, 0, Circle(5, 0.012), true, rHem, delegate(int i) { return i % 2 == 0 ? 1.18 : 0.88; });
        OMesh wp = P.Add(new OMesh("waist_piping", "skirt", false, true));
        path = new List<double[]>(); nrm = new List<double[]>();
        for (int k = 0; k < 12; k++) { double a = -PI + 2 * PI * k / 12; path.Add(V3(0.094 * Math.Sin(a), 0.066 - DROP, 0.094 * Math.Cos(a) * 0.9 - 0.006)); nrm.Add(V3(Math.Sin(a), 0, Math.Cos(a))); }
        Sweep(wp, path, nrm, 0, 0, Circle(5, 0.009), true, rWp, delegate(int i) { return i % 2 == 0 ? 1.15 : 0.9; });
        OMesh bl = P.Add(new OMesh("bloomers", "hips", false, true));
        Lathe(bl, new double[][] { new double[] { 0.09, -0.1 }, new double[] { 0.085, -0.06 }, new double[] { 0.07, 0.0 } }, 8, 0.9, rBl, null, null);
        return P;
    }

    public static OPart Legwear() {
        OPart P = new OPart("legwear", "Legwear: socks + ruffled leg warmers (bones: knee_L/R, L/R shared UVs)");
        ORect rSock = P.Isl("sock", 0.0, 0.0, 0.30, 1.0), rT1 = P.Isl("top ruffle cuff (cherries)", 0.32, 0.52, 1.0, 1.0), rT = P.Isl("lower ruffles", 0.32, 0.0, 1.0, 0.48);
        OMesh sock = new OMesh("sock", "knee_L", false, true);
        // sock + lowest ruffle reach down into the shoe so no ankle gap shows
        Lathe(sock, new double[][] { new double[] { 0.044, -0.318 }, new double[] { 0.042, -0.272 }, new double[] { 0.039, -0.2 }, new double[] { 0.04, -0.12 }, new double[] { 0.043, -0.05 }, new double[] { 0.047, 0.0 }, new double[] { 0.046, 0.03 } }, 8, 0.95, rSock, null, null);
        OMesh rf = new OMesh("ruffles", "knee_L", true, false);
        rf.Hems.Add(Hem(rf.V.Count, 10)); Lathe(rf, new double[][] { new double[] { 0.066, -0.065 }, new double[] { 0.051, -0.025 }, new double[] { 0.05, 0.02 }, new double[] { 0.054, 0.038 } }, 10, 0.95, rT1, Zigzag(1.1, 0.008), null);
        rf.Hems.Add(Hem(rf.V.Count, 10)); Lathe(rf, new double[][] { new double[] { 0.066, -0.158 }, new double[] { 0.047, -0.095 } }, 10, 0.95, rT, Zigzag(1.1, 0.008), null);
        rf.Hems.Add(Hem(rf.V.Count, 10)); Lathe(rf, new double[][] { new double[] { 0.08, -0.296 }, new double[] { 0.05, -0.19 } }, 10, 0.95, rT, Zigzag(1.1, 0.008), null);
        foreach (OMesh m in new OMesh[] { sock, rf }) { P.Add(m); P.Add(Mirror(m, "knee_R")); }
        return P;
    }

    public static OPart Shoes() {
        OPart P = new OPart("shoes", "Shoes: chunky platform (bones: ankle_L/R, L/R shared UVs)");
        ORect rUp = P.Isl("upper (toe = centre)", 0.0, 0.0, 1.0, 0.50), rCap = P.Isl("sole top/bottom", 0.0, 0.52, 0.36, 0.68), rSide = P.Isl("sole side", 0.0, 0.70, 1.0, 0.90);
        OMesh up = new OMesh("upper", "ankle_L", false, true);
        Lathe(up, new double[][] { new double[] { 0.064, 0 }, new double[] { 0.061, 0.015 }, new double[] { 0.05, 0.0285 }, new double[] { 0.03, 0.0405 }, new double[] { 0, 0.0465 } }, 10, 1.42, rUp, null, Move(0, -0.052, 0.02));
        OMesh sole = new OMesh("sole", "ankle_L", false, true);
        Lathe(sole, new double[][] { new double[] { 0.068, -0.078 }, new double[] { 0.071, -0.06 }, new double[] { 0.068, -0.044 } }, 10, 1.45, rSide, null, Move(0, 0, 0.02));
        foreach (double y in new double[] { -0.044, -0.078 }) {
            List<double[]> ring = new List<double[]>();
            for (int k = 0; k < 10; k++) { double a = -PI + 2 * PI * k / 10; ring.Add(V3(0.068 * Math.Sin(a), y, 0.068 * Math.Cos(a) * 1.45 + 0.02)); }
            if (y < -0.05) ring.Reverse();                                  // bottom cap faces down
            Fan(sole, V3(0, y, 0.02), ring, rCap, delegate(double[] p) { return new double[] { 0.5 + p[0] / 0.15, 0.5 + (p[2] - 0.02) / 0.21 }; }, null);
        }
        foreach (OMesh m in new OMesh[] { up, sole }) { P.Add(m); P.Add(Mirror(m, "ankle_R")); }
        return P;
    }

    public static OPart[] BuildAll() { return new OPart[] { Headdress(), Top(), Skirt(), Legwear(), Shoes() }; }

    // ================================================================ export
    // Rest transforms of the game rig's bones in character space (feet on y = 0, facing +z, s = +1 is the left side).
    static double[] BoneRest(string bone) {           // {tx, ty, tz, scale}
        double s = bone.EndsWith("_R") ? -1 : 1;
        switch (bone.Replace("_L", "").Replace("_R", "")) {
            case "hips": case "skirt": case "chest": return new double[] { 0, 0.73, 0, 1 };
            case "head": return new double[] { 0, 0.978, 0, 0.86 };
            case "sh": return new double[] { 0.08 * s, 0.91, 0, 1 };
            case "el": return new double[] { 0.08 * s, 0.73, 0, 1 };
            case "knee": return new double[] { 0.05 * s, 0.36, 0, 1 };
            case "ankle": return new double[] { 0.05 * s, 0.07, 0, 1 };
        }
        return new double[] { 0, 0, 0, 1 };
    }
    static string F4(double d) { return Math.Round(d, 5).ToString("0.#####", IC); }

    public static string ToObj(OPart[] parts, string mtl, string header) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# " + header);
        sb.AppendLine("# character space, metres-ish game units, feet at y=0, facing +Z. Groups = part_mesh_bone.");
        sb.AppendLine("mtllib " + mtl);
        int vo = 1, to = 1;
        foreach (OPart P in parts) {
            sb.AppendLine("o " + P.Name);
            sb.AppendLine("usemtl Outfit_" + P.Name);
            foreach (OMesh m in P.Meshes) {
                double[] B = BoneRest(m.Bone);
                sb.AppendLine("g " + P.Name + "_" + m.Name + "_" + m.Bone);
                foreach (double[] v in m.V) sb.AppendLine("v " + F4(B[0] + v[0] * B[3]) + " " + F4(B[1] + v[1] * B[3]) + " " + F4(B[2] + v[2] * B[3]));
                foreach (double[] t in m.T) sb.AppendLine("vt " + F4(t[0]) + " " + F4(t[1]));
                foreach (int[] f in m.F) {
                    sb.Append("f");
                    for (int k = 0; k < f.Length; k += 2) sb.Append(" " + (f[k] + vo) + "/" + (f[k + 1] + to));
                    sb.AppendLine();
                }
                vo += m.V.Count; to += m.T.Count;
            }
        }
        return sb.ToString();
    }

    public static int Tris(OPart P) { int n = 0; foreach (OMesh m in P.Meshes) foreach (int[] f in m.F) n += f.Length / 2 - 2; return n; }

    /// Game embed: per part the texture (filled in by the build script) and the bone-local meshes.
    public static string ToJson(OPart[] parts, Dictionary<string, string> tex) {
        StringBuilder sb = new StringBuilder("{\"parts\":[");
        for (int p = 0; p < parts.Length; p++) {
            OPart P = parts[p];
            if (p > 0) sb.Append(",");
            sb.Append("{\"name\":\"" + P.Name + "\",\"tex\":\"" + tex[P.Name] + "\",\"meshes\":[");
            for (int i = 0; i < P.Meshes.Count; i++) {
                OMesh m = P.Meshes[i];
                if (i > 0) sb.Append(",");
                sb.Append("{\"name\":\"" + m.Name + "\",\"bone\":\"" + m.Bone + "\",\"ds\":" + (m.DS ? "true" : "false") + ",\"ol\":" + (m.Outline ? "true" : "false") + ",\"v\":[");
                for (int k = 0; k < m.V.Count; k++) { if (k > 0) sb.Append(","); sb.Append(F4(m.V[k][0]) + "," + F4(m.V[k][1]) + "," + F4(m.V[k][2])); }
                sb.Append("],\"t\":[");
                for (int k = 0; k < m.T.Count; k++) { if (k > 0) sb.Append(","); sb.Append(F4(m.T[k][0]) + "," + F4(m.T[k][1])); }
                sb.Append("],\"f\":[");
                for (int k = 0; k < m.F.Count; k++) { if (k > 0) sb.Append(","); sb.Append("[" + string.Join(",", Array.ConvertAll(m.F[k], delegate(int x) { return x.ToString(IC); })) + "]"); }
                sb.Append("]");
                if (m.Hems.Count > 0) { sb.Append(",\"h\":["); for (int k = 0; k < m.Hems.Count; k++) { if (k > 0) sb.Append(","); sb.Append("[" + string.Join(",", Array.ConvertAll(m.Hems[k], delegate(int x) { return x.ToString(IC); })) + "]"); } sb.Append("]"); }
                sb.Append("}");
            }
            sb.Append("]}");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    // ================================================================ UV layout sheet (texture template)
    static PointF UvPx(double u, double v, int S) { return new PointF((float)(u * S), (float)((1 - v) * S)); }
    static RectangleF Px(ORect r, int S) { return new RectangleF((float)(r.U0 * S), (float)((1 - r.V1) * S), (float)((r.U1 - r.U0) * S), (float)((r.V1 - r.V0) * S)); }

    /// UV wireframe on a transparent background (drop it on top of a paint layer), island boxes + names.
    public static Bitmap UvLayout(OPart P, int S, bool background) {
        Bitmap bmp = new Bitmap(S, S);
        using (Graphics g = Graphics.FromImage(bmp)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(background ? Color.White : Color.Transparent);
            using (Pen box = new Pen(Color.FromArgb(200, 230, 120, 150), 1.5f)) {
                box.DashStyle = DashStyle.Dash;
                foreach (ORect r in P.Islands) g.DrawRectangle(box, Px(r, S).X, Px(r, S).Y, Px(r, S).Width, Px(r, S).Height);
            }
            using (Pen wire = new Pen(Color.FromArgb(230, 40, 40, 60), 1f)) {
                foreach (OMesh m in P.Meshes)
                    foreach (int[] f in m.F) {
                        int c = f.Length / 2; PointF[] pts = new PointF[c];
                        for (int k = 0; k < c; k++) { double[] t = m.T[f[k * 2 + 1]]; pts[k] = UvPx(t[0], t[1], S); }
                        g.DrawPolygon(wire, pts);
                    }
            }
            using (Font fnt = new Font("Segoe UI", S / 64f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(255, 200, 60, 100)))
                foreach (ORect r in P.Islands) { RectangleF rc = Px(r, S); g.DrawString(r.Name, fnt, tb, rc.X + 3, rc.Y + 2); }
        }
        return bmp;
    }

    // ================================================================ placeholder textures (painted on the real UV layout)
    static Color C(string h) { return ColorTranslator.FromHtml(h); }
    static readonly Color WHITE = C("#fffaf7"), CREAM = C("#fff1e0"), CREAM_SH = C("#f0d9c0"), PINK = C("#f9c9d8"), PINK_MID = C("#f2a3bb"),
        PINK_DEEP = C("#e7849f"), RED = C("#d8344d"), RED_DK = C("#a8203a"), GREEN = C("#6dbb5a"), GREEN_DK = C("#3f8a3a"),
        SEED = C("#ffe28a"), BLUE = C("#bcdcf5"), BLUE_DK = C("#86b2de"), LACE = C("#f3d6df");

    static PointF At(ORect r, int S, double s, double t) { return UvPx(r.U(s), r.V(t), S); }
    static void Fill(Graphics g, ORect r, int S, Color c) { using (SolidBrush b = new SolidBrush(c)) { RectangleF rc = Px(r, S); rc.Inflate(4, 4); g.FillRectangle(b, rc); } }
    static void HLine(Graphics g, ORect r, int S, double t, Color c, float w) { using (Pen p = new Pen(c, w)) g.DrawLine(p, At(r, S, 0, t), At(r, S, 1, t)); }
    static void VLines(Graphics g, ORect r, int S, int n, Color c, float w, double t0, double t1) {
        using (Pen p = new Pen(c, w)) for (int k = 0; k < n; k++) { double s = (k + 0.5) / n; g.DrawLine(p, At(r, S, s, t0), At(r, S, s, t1)); }
    }
    static void Dots(Graphics g, ORect r, int S, double t, int n, float rad, Color c) {
        using (SolidBrush b = new SolidBrush(c)) for (int k = 0; k < n; k++) { PointF q = At(r, S, (k + 0.5) / n, t); g.FillEllipse(b, q.X - rad, q.Y - rad, rad * 2, rad * 2); }
    }
    static void Scallops(Graphics g, ORect r, int S, double t, int n, float rad, Color c) {   // row of half-discs (lace edge)
        using (SolidBrush b = new SolidBrush(c)) for (int k = 0; k <= n; k++) { PointF q = At(r, S, (double)k / n, t); g.FillEllipse(b, q.X - rad, q.Y - rad, rad * 2, rad * 2); }
    }
    static void Twist(Graphics g, ORect r, int S, float step) {                               // piped-cream twist shading
        Fill(g, r, S, CREAM); RectangleF rc = Px(r, S);
        using (Pen p = new Pen(CREAM_SH, step * 0.28f)) for (float x = rc.Left - rc.Height; x < rc.Right; x += step) g.DrawLine(p, x, rc.Bottom, x + rc.Height * 0.8f, rc.Top);
        using (Pen p = new Pen(Color.White, step * 0.12f)) for (float x = rc.Left - rc.Height + step * 0.5f; x < rc.Right; x += step) g.DrawLine(p, x, rc.Bottom, x + rc.Height * 0.8f, rc.Top);
    }
    static void Strawberry(Graphics g, PointF c, float s) {
        using (GraphicsPath gp = new GraphicsPath()) {
            gp.AddBezier(c.X, c.Y + s * 0.9f, c.X - s * 0.9f, c.Y + s * 0.2f, c.X - s * 0.7f, c.Y - s * 0.6f, c.X, c.Y - s * 0.45f);
            gp.AddBezier(c.X, c.Y - s * 0.45f, c.X + s * 0.7f, c.Y - s * 0.6f, c.X + s * 0.9f, c.Y + s * 0.2f, c.X, c.Y + s * 0.9f);
            using (SolidBrush b = new SolidBrush(RED)) g.FillPath(b, gp);
        }
        using (SolidBrush b = new SolidBrush(SEED))
            for (int i = 0; i < 3; i++) for (int j = -1; j <= 1; j++) { float x = c.X + j * s * 0.3f + (i % 2) * s * 0.15f, y = c.Y - s * 0.15f + i * s * 0.28f; g.FillEllipse(b, x - s * 0.05f, y - s * 0.07f, s * 0.1f, s * 0.14f); }
        using (SolidBrush b = new SolidBrush(GREEN))
            for (int k = 0; k < 5; k++) { double a = -PI / 2 + (k - 2) * 0.55; g.FillEllipse(b, (float)(c.X + Math.Cos(a) * s * 0.28 - s * 0.14), (float)(c.Y - s * 0.5 + Math.Sin(a) * s * 0.12 - s * 0.08), s * 0.28f, s * 0.16f); }
    }
    static void Cherry(Graphics g, PointF c, float s) {
        using (Pen p = new Pen(GREEN_DK, Math.Max(2, s * 0.08f))) { g.DrawBezier(p, c.X - s * 0.35f, c.Y, c.X - s * 0.3f, c.Y - s * 0.5f, c.X, c.Y - s * 0.8f, c.X + s * 0.05f, c.Y - s * 0.9f); g.DrawBezier(p, c.X + s * 0.35f, c.Y, c.X + s * 0.3f, c.Y - s * 0.5f, c.X + s * 0.1f, c.Y - s * 0.8f, c.X + s * 0.05f, c.Y - s * 0.9f); }
        using (SolidBrush b = new SolidBrush(GREEN)) g.FillEllipse(b, c.X + s * 0.05f, c.Y - s * 1.0f, s * 0.4f, s * 0.2f);
        using (SolidBrush b = new SolidBrush(RED)) foreach (float dx in new float[] { -0.35f, 0.35f }) g.FillEllipse(b, c.X + dx * s - s * 0.3f, c.Y - s * 0.1f, s * 0.6f, s * 0.6f);
        using (SolidBrush b = new SolidBrush(Color.FromArgb(200, 255, 255, 255))) foreach (float dx in new float[] { -0.35f, 0.35f }) g.FillEllipse(b, c.X + dx * s - s * 0.15f, c.Y, s * 0.12f, s * 0.12f);
    }
    static void Rose(Graphics g, PointF c, float r, Color fill, Color dark) {
        using (SolidBrush b = new SolidBrush(fill)) g.FillEllipse(b, c.X - r, c.Y - r, r * 2, r * 2);
        using (Pen p = new Pen(dark, Math.Max(1.5f, r * 0.16f))) { g.DrawArc(p, c.X - r * 0.7f, c.Y - r * 0.7f, r * 1.4f, r * 1.4f, 200, 230); g.DrawArc(p, c.X - r * 0.35f, c.Y - r * 0.35f, r * 0.7f, r * 0.7f, 20, 250); }
    }
    static void Bow(Graphics g, PointF c, float w, Color col, Color dark) {
        PointF[] l = { c, new PointF(c.X - w, c.Y - w * 0.55f), new PointF(c.X - w, c.Y + w * 0.55f) };
        PointF[] r = { c, new PointF(c.X + w, c.Y - w * 0.55f), new PointF(c.X + w, c.Y + w * 0.55f) };
        PointF[] t1 = { c, new PointF(c.X - w * 0.6f, c.Y + w * 1.1f), new PointF(c.X - w * 0.25f, c.Y + w * 1.15f) };
        PointF[] t2 = { c, new PointF(c.X + w * 0.6f, c.Y + w * 1.1f), new PointF(c.X + w * 0.25f, c.Y + w * 1.15f) };
        using (SolidBrush b = new SolidBrush(col)) { g.FillPolygon(b, t1); g.FillPolygon(b, t2); g.FillPolygon(b, l); g.FillPolygon(b, r); }
        using (Pen p = new Pen(dark, Math.Max(1.5f, w * 0.1f))) { g.DrawPolygon(p, l); g.DrawPolygon(p, r); }
        using (SolidBrush b = new SolidBrush(dark)) g.FillEllipse(b, c.X - w * 0.22f, c.Y - w * 0.22f, w * 0.44f, w * 0.44f);
    }
    static void Lacing(Graphics g, ORect r, int S, double s0, double s1, double t0, double t1, int n) {
        using (Pen p = new Pen(RED, Px(r, S).Width * (float)(s1 - s0) * 0.12f)) {
            p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
            for (int k = 0; k < n; k++) {
                double a = t0 + (t1 - t0) * k / n, b = t0 + (t1 - t0) * (k + 1) / n;
                g.DrawLine(p, At(r, S, s0, a), At(r, S, s1, b)); g.DrawLine(p, At(r, S, s1, a), At(r, S, s0, b));
            }
        }
        using (SolidBrush eb = new SolidBrush(RED_DK)) for (int k = 0; k <= n; k++) foreach (double s in new double[] { s0, s1 }) { PointF q = At(r, S, s, t0 + (t1 - t0) * k / n); g.FillEllipse(eb, q.X - 3, q.Y - 3, 6, 6); }
    }

    public static Bitmap Placeholder(OPart P, int S) {
        Bitmap bmp = new Bitmap(S, S);
        using (Graphics g = Graphics.FromImage(bmp)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(WHITE);
            float u = S / 1024f;
            switch (P.Name) {
                case "headdress": {
                    ORect r = P.Find("band"); Fill(g, r, S, PINK); HLine(g, r, S, 0.12, WHITE, 6 * u); HLine(g, r, S, 0.38, WHITE, 6 * u); HLine(g, r, S, 0.62, PINK_MID, 4 * u); HLine(g, r, S, 0.88, WHITE, 6 * u);
                    r = P.Find("frill (front+back)"); Fill(g, r, S, WHITE); Dots(g, r, S, 0.55, 48, 5 * u, LACE); Scallops(g, r, S, 0.92, 48, 9 * u, LACE); HLine(g, r, S, 0.08, PINK, 4 * u);
                    Twist(g, P.Find("cream piping"), S, 26 * u);
                    r = P.Find("strawberry"); Fill(g, r, S, RED); Dots(g, r, S, 0.3, 6, 4 * u, SEED); Dots(g, r, S, 0.5, 6, 4 * u, SEED); Dots(g, r, S, 0.7, 6, 4 * u, SEED); HLine(g, r, S, 0.03, RED_DK, 14 * u);
                    r = P.Find("leaf"); Fill(g, r, S, GREEN); VLines(g, r, S, 5, GREEN_DK, 3 * u, 0.1, 0.9);
                    r = P.Find("cream dollop"); Fill(g, r, S, CREAM); VLines(g, r, S, 8, CREAM_SH, 8 * u, 0.0, 0.85);
                    foreach (string n in new string[] { "bow loop", "bow knot", "bow tail" }) { r = P.Find(n); Fill(g, r, S, BLUE); HLine(g, r, S, 0.1, BLUE_DK, 5 * u); HLine(g, r, S, 0.9, BLUE_DK, 5 * u); }
                    break;
                }
                case "top": {
                    ORect r = P.Find("bodice (front = centre)"); Fill(g, r, S, WHITE);
                    // front corset panel: pink, lace edged, red lacing; rosette row on the neckline; red bows at the waist
                    using (SolidBrush b = new SolidBrush(PINK)) { PointF a = At(r, S, 0.38, 0.72), c = At(r, S, 0.62, 0.0); g.FillRectangle(b, a.X, a.Y, c.X - a.X, c.Y - a.Y); }
                    using (SolidBrush b = new SolidBrush(LACE)) for (int k = 0; k < 14; k++) foreach (double s in new double[] { 0.38, 0.62 }) { PointF q = At(r, S, s, 0.03 + 0.05 * k); g.FillEllipse(b, q.X - 7 * u, q.Y - 7 * u, 14 * u, 14 * u); }
                    Lacing(g, r, S, 0.465, 0.535, 0.08, 0.62, 6);
                    double[] rs = { 0.40, 0.445, 0.5, 0.555, 0.60 }; Color[] rc = { WHITE, PINK_MID, RED, PINK_MID, WHITE };
                    for (int k = 0; k < 5; k++) Rose(g, At(r, S, rs[k], 0.66), (k == 2 ? 22 : 17) * u, rc[k], k == 2 ? RED_DK : PINK_DEEP);
                    using (SolidBrush b = new SolidBrush(PINK_MID)) { PointF a = At(r, S, 0, 0.05), c = At(r, S, 1, 0.0); g.FillRectangle(b, a.X, a.Y, c.X - a.X, c.Y - a.Y + 4); }
                    Bow(g, At(r, S, 0.36, 0.16), 20 * u, RED, RED_DK); Bow(g, At(r, S, 0.64, 0.16), 20 * u, RED, RED_DK);
                    using (SolidBrush b = new SolidBrush(PINK)) { PointF a = At(r, S, 0.0, 0.62), c = At(r, S, 0.06, 0.05); g.FillRectangle(b, a.X, a.Y, c.X - a.X, c.Y - a.Y); a = At(r, S, 0.94, 0.62); c = At(r, S, 1.0, 0.05); g.FillRectangle(b, a.X, a.Y, c.X - a.X, c.Y - a.Y); }
                    HLine(g, r, S, 0.9, LACE, 5 * u); Dots(g, r, S, 0.95, 40, 4 * u, LACE);
                    r = P.Find("neckline frill"); Fill(g, r, S, WHITE); Dots(g, r, S, 0.5, 24, 4 * u, LACE); HLine(g, r, S, 0.05, PINK, 5 * u);
                    r = P.Find("puff sleeve (L/R shared)"); Fill(g, r, S, WHITE); VLines(g, r, S, 8, LACE, 5 * u, 0.1, 0.9); HLine(g, r, S, 0.04, PINK, 12 * u);
                    r = P.Find("sleeve frill"); Fill(g, r, S, WHITE); Dots(g, r, S, 0.5, 30, 5 * u, LACE); HLine(g, r, S, 0.06, PINK_MID, 6 * u);
                    r = P.Find("wrist cuff"); Fill(g, r, S, WHITE); Dots(g, r, S, 0.5, 30, 5 * u, LACE); HLine(g, r, S, 0.06, PINK_MID, 6 * u); HLine(g, r, S, 0.9, PINK, 8 * u);
                    break;
                }
                case "skirt": {
                    ORect r = P.Find("pink pleated skirt"); Fill(g, r, S, PINK); VLines(g, r, S, 8, PINK_MID, 10 * u, 0.0, 0.8);
                    using (SolidBrush b = new SolidBrush(WHITE)) { PointF a = At(r, S, 0, 0.12), c = At(r, S, 1, 0); g.FillRectangle(b, a.X, a.Y, c.X - a.X, c.Y - a.Y + 4); }
                    Scallops(g, r, S, 0.12, 48, 9 * u, WHITE); Dots(g, r, S, 0.05, 48, 4 * u, LACE);
                    r = P.Find("petticoat frill"); Fill(g, r, S, WHITE); Dots(g, r, S, 0.5, 48, 5 * u, LACE); HLine(g, r, S, 0.08, PINK, 5 * u);
                    r = P.Find("cream overskirt (swags)"); Fill(g, r, S, WHITE);
                    using (Pen p = new Pen(CREAM_SH, 6 * u)) using (Pen p2 = new Pen(CREAM, 14 * u))
                        for (int k = 0; k < 8; k++) foreach (double t in new double[] { 0.22, 0.42 }) {
                            PointF a = At(r, S, k / 8.0, t + 0.1), m = At(r, S, (k + 0.5) / 8.0, t - 0.06), b = At(r, S, (k + 1) / 8.0, t + 0.1);
                            g.DrawBezier(p2, a, new PointF(a.X, m.Y), new PointF(b.X, m.Y), b); g.DrawBezier(p, a, new PointF(a.X, m.Y + 6 * u), new PointF(b.X, m.Y + 6 * u), b);
                        }
                    for (int k = 0; k <= 8; k++) { Strawberry(g, At(r, S, k / 8.0, 0.3), 30 * u); Rose(g, At(r, S, k / 8.0, 0.55), 12 * u, WHITE, LACE); }
                    for (int k = 0; k < 8; k++) Bow(g, At(r, S, (k + 0.5) / 8.0, 0.72), 14 * u, PINK_MID, PINK_DEEP);
                    HLine(g, r, S, 0.96, PINK, 6 * u);
                    Twist(g, P.Find("hem piping"), S, 22 * u); Twist(g, P.Find("waist piping"), S, 18 * u);
                    r = P.Find("bloomers"); Fill(g, r, S, WHITE); HLine(g, r, S, 0.1, LACE, 5 * u);
                    break;
                }
                case "legwear": {
                    ORect r = P.Find("sock"); Fill(g, r, S, WHITE); VLines(g, r, S, 8, C("#f6ebee"), 4 * u, 0.0, 1.0);
                    r = P.Find("top ruffle cuff (cherries)"); Fill(g, r, S, WHITE); HLine(g, r, S, 0.9, PINK, 10 * u); Dots(g, r, S, 0.12, 36, 4 * u, LACE); HLine(g, r, S, 0.03, PINK_MID, 5 * u);
                    Cherry(g, At(r, S, 0.5, 0.52), 40 * u); Cherry(g, At(r, S, 0.0, 0.52), 34 * u); Cherry(g, At(r, S, 1.0, 0.52), 34 * u);
                    r = P.Find("lower ruffles"); Fill(g, r, S, WHITE); Dots(g, r, S, 0.3, 36, 5 * u, LACE); HLine(g, r, S, 0.05, PINK_MID, 6 * u);
                    break;
                }
                case "shoes": {
                    ORect r = P.Find("upper (toe = centre)"); Fill(g, r, S, WHITE); HLine(g, r, S, 0.08, PINK, 6 * u); Bow(g, At(r, S, 0.5, 0.42), 40 * u, PINK_MID, PINK_DEEP);
                    r = P.Find("sole side"); Fill(g, r, S, C("#f7c3d3")); HLine(g, r, S, 0.85, WHITE, 8 * u);
                    r = P.Find("sole top/bottom"); Fill(g, r, S, C("#f2b0c4"));
                    break;
                }
            }
        }
        return bmp;
    }
}
