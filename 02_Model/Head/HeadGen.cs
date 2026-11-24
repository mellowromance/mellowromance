// SD low-poly head generator (quad-dominant, symmetric, UV-mapped to 02_Model/Face face map).
// Units: head height 1.0 — crown y = +0.52, chin y = -0.48, face looks down +Z, X = character's left.
// Built by build_head.ps1 (Windows PowerShell Add-Type, C# 5).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class HeadMesh {
    public List<double[]> V = new List<double[]>();   // positions
    public List<double[]> VT = new List<double[]>();  // uvs
    public List<int[]> F = new List<int[]>();         // faces: vertex indices (3 or 4)
    public List<int[]> FT = new List<int[]>();        // faces: uv indices
    public List<string> G = new List<string>();       // object name per face
}

public static class HeadGen {
    // ---- face-map calibration (pixels in the 1000x1024 crop of the face-map sheet)
    const double TEX_W = 1000, TEX_H = 1024;
    const double EYE_PX_Y = 675, CENTER_PX_X = 499, PX_PER_UNIT = 820;
    public const double CROWN_Y = 0.44;                   // top of the skull (shortened forehead/cranium)
    const double EYE_Y = -0.11;                           // mesh height of the painted eye centres (raised)
    const double BROW_Y = 0.055;                          // mesh height of the brows (kept where they are)
    // Vertical texture mapping, piecewise in px-per-unit so the eyes+mouth band stays undistorted:
    //   crown..brows: rest of the sheet · brows..eye top: compressed gap · eye top..below mouth: 1:1 (820)
    //   below mouth..chin: the remaining skin of the sheet.
    const double BROW_PX_Y = 470, EYETOP_PX_Y = 610, MOUTHLOW_PX_Y = 760, CROWN_PX_Y = 40, CHIN_PX_Y = 900, CHIN_Y = -0.44;
    static double TexY(double y) {
        double eyeTopY = EYE_Y + (EYE_PX_Y - EYETOP_PX_Y) / PX_PER_UNIT;
        double mouthLowY = EYE_Y - (MOUTHLOW_PX_Y - EYE_PX_Y) / PX_PER_UNIT;
        if (y >= BROW_Y) return BROW_PX_Y - (BROW_PX_Y - CROWN_PX_Y) * (y - BROW_Y) / (CROWN_Y - BROW_Y);
        if (y >= eyeTopY) return EYETOP_PX_Y - (EYETOP_PX_Y - BROW_PX_Y) * (y - eyeTopY) / (BROW_Y - eyeTopY);
        if (y >= mouthLowY) return EYE_PX_Y - PX_PER_UNIT * (y - EYE_Y);
        return MOUTHLOW_PX_Y + (CHIN_PX_Y - MOUTHLOW_PX_Y) * (mouthLowY - y) / (mouthLowY - CHIN_Y);
    }

    static double Smooth(double a, double b, double v) {
        double t = Math.Min(1, Math.Max(0, (v - a) / (b - a))); return t * t * (3 - 2 * t);
    }
    static double SMax(double a, double b, double k) { return (a + b + Math.Sqrt((a - b) * (a - b) + k * k)) / 2; }

    // Head surface point for latitude theta (0 = crown) and longitude phi (0 = front, + toward +X).
    public static double[] Shape(double theta, double phi) {
        double st = Math.Sin(theta), ct = Math.Cos(theta), sp = Math.Sin(phi), cp = Math.Cos(phi);
        double ux = st * sp, uy = ct, uz = st * cp;              // unit-sphere direction
        // shorter forehead / cranium: the dome above the centre is lower (crown at CROWN_Y instead of 0.52)
        double x = 0.44 * ux, y = 0.05 + (uy > 0 ? (CROWN_Y - 0.05) : 0.47) * uy, z = 0.45 * uz;
        if (uz < 0) z *= 1.06;                                     // full, round back of the skull
        x *= 1 - 0.09 * Smooth(0.35, -0.15, uy);                   // narrower face sides at/below the eyes (less margin beside the eyes)
        double t = Smooth(0, 1, -uy), f = Smooth(-0.3, 0.7, uz);
        double mid = 1 - Math.Min(1, Math.Abs(ux) * 1.6);
        // broad forehead → cheeks that narrow toward a small, round chin
        // full baby cheeks that keep their width low on the face; the taper only kicks in right at the chin
        x *= 1 - 0.17 * f * Math.Pow(t, 2.4) - 0.06 * (1 - f) * t;
        y -= 0.03 * f * t * t * (0.5 + 0.5 * mid);                 // short lower face: barely any chin drop
        z += 0.03 * f * t * mid;
        if (z > 0.30) z = 0.30 + (z - 0.30) * 0.65;                // gently flattened face front
        double cheek = Smooth(0.0, 0.55, uz) * Math.Exp(-Math.Pow(uy + 0.62, 2) / 0.06) * Smooth(0.1, 0.55, Math.Abs(ux));
        x *= 1 + 0.1 * cheek; z += 0.045 * cheek;                  // round, puffy cheeks
        double jawFloor = -0.29 - 0.13 * Smooth(-0.6, 0.95, uz);   // jaw underside: nape (high) → chin (low), short
        y = SMax(y, jawFloor, 0.08);                               // rounded, not a crease
        if (z > 0) z += 0.028 * Math.Exp(-(x * x) / 0.0016 - Math.Pow(y + 0.152, 2) / 0.0022);   // tiny nose (between raised eyes and mouth)
        return new double[] { x, y, z };
    }

    static int AddV(HeadMesh m, double[] p) { m.V.Add(p); return m.V.Count - 1; }
    static int AddT(HeadMesh m, double u, double v) { m.VT.Add(new double[] { u, v }); return m.VT.Count - 1; }

    static double[] FrontUV(double[] p, double phi) {
        double s = Math.Abs(phi) > 1e-4 ? p[0] * (phi / Math.Sin(phi)) : p[0];   // arc length across the face
        double px = CENTER_PX_X + PX_PER_UNIT * s, py = TexY(p[1]);
        return new double[] { px / TEX_W, 1 - py / TEX_H };
    }
    static double[] SkinPatchUV(double[] p) {           // small plain-skin patch (side-edge tone, so the ±75° seam doesn't show)
        double px = 85 + 12 * Math.Max(-1, Math.Min(1, p[0] / 0.46));
        double py = 470 + 60 * Math.Max(0, Math.Min(1, (0.52 - p[1]) / 1.3));
        return new double[] { px / TEX_W, 1 - py / TEX_H };
    }

    static double[] Sub(double[] a, double[] b) { return new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] }; }
    static double[] Cross(double[] a, double[] b) { return new double[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] }; }
    static double Dot(double[] a, double[] b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
    static double[] FaceNormal(HeadMesh m, int[] f) {
        double[] n = { 0, 0, 0 };
        for (int i = 0; i < f.Length; i++) {               // Newell's method (works for quads and tris)
            double[] a = m.V[f[i]], b = m.V[f[(i + 1) % f.Length]];
            n[0] += (a[1] - b[1]) * (a[2] + b[2]); n[1] += (a[2] - b[2]) * (a[0] + b[0]); n[2] += (a[0] - b[0]) * (a[1] + b[1]);
        }
        return n;
    }
    static double[] Centroid(HeadMesh m, int[] f) {
        double[] c = { 0, 0, 0 };
        foreach (int i in f) { c[0] += m.V[i][0]; c[1] += m.V[i][1]; c[2] += m.V[i][2]; }
        c[0] /= f.Length; c[1] /= f.Length; c[2] /= f.Length; return c;
    }
    // add a face, flipping winding so its normal points away from `inside`
    static void AddFace(HeadMesh m, int[] f, int[] ft, string g, double[] inside) {
        double[] n = FaceNormal(m, f), c = Centroid(m, f);
        if (Dot(n, Sub(c, inside)) < 0) { Array.Reverse(f); Array.Reverse(ft); }
        m.F.Add(f); m.FT.Add(ft); m.G.Add(g);
    }

    public static HeadMesh Build() {
        HeadMesh m = new HeadMesh();
        const int NU = 24;                                  // columns, 15° each; col 0 = front centre, col 12 = back
        // ring latitudes (deg): denser through the eye (≈118–134°) and mouth (≈139°) band → editable loops there
        double[] ringsDeg = { 12, 24, 36, 48, 60, 72, 84, 94, 103, 111, 118, 125, 132, 139, 146, 153, 160 };
        int NR = ringsDeg.Length;
        int[,] ring = new int[NR + 4, NU];
        double[] phiOf = new double[NU];
        for (int i = 0; i < NU; i++) { double ph = 2 * Math.PI * i / NU; if (ph > Math.PI) ph -= 2 * Math.PI; phiOf[i] = ph; }
        int pole = AddV(m, Shape(0, 0));
        for (int r = 0; r < NR; r++)
            for (int i = 0; i < NU; i++) ring[r, i] = AddV(m, Shape(ringsDeg[r] * Math.PI / 180, phiOf[i]));
        // jaw underside blend ring → neck top ring → neck rings (short, thin cylinder under the head, centred)
        double neckR = 0.115, neckZ = -0.05;
        double[] neckY = { -0.34, -0.46, -0.57 };                // short neck
        for (int i = 0; i < NU; i++) {
            double[] last = m.V[ring[NR - 1, i]];
            double nx = neckR * Math.Sin(phiOf[i]), nz = neckZ + neckR * Math.Cos(phiOf[i]);
            ring[NR, i] = AddV(m, new double[] { (last[0] + nx) / 2, Math.Min(last[1], neckY[0]) - 0.005, (last[2] + nz) / 2 });
            for (int k = 0; k < 3; k++) ring[NR + 1 + k, i] = AddV(m, new double[] { nx, neckY[k], nz });
        }
        double[] inside = { 0, 0.02, 0 };
        // crown fan (the only triangles on the head)
        for (int i = 0; i < NU; i++) {
            int a = ring[0, i], b = ring[0, (i + 1) % NU];
            bool front = Math.Abs(phiOf[i]) <= 1.58 && Math.Abs(phiOf[(i + 1) % NU]) <= 1.58;
            int[] ft = new int[3];
            int[] fv = { pole, a, b };
            double[] phs = { 0, phiOf[i], phiOf[(i + 1) % NU] };
            for (int k = 0; k < 3; k++) { double[] uv = front ? FrontUV(m.V[fv[k]], phs[k]) : SkinPatchUV(m.V[fv[k]]); ft[k] = AddT(m, uv[0], uv[1]); }
            AddFace(m, fv, ft, "Head", inside);
        }
        int totalRings = NR + 4;
        for (int r = 0; r < totalRings - 1; r++)
            for (int i = 0; i < NU; i++) {
                int j = (i + 1) % NU;
                int[] fv = { ring[r, i], ring[r, j], ring[r + 1, j], ring[r + 1, i] };
                double[] phs = { phiOf[i], phiOf[j], phiOf[j], phiOf[i] };
                bool headPart = r < NR - 1;
                bool frontCols = Math.Abs(phiOf[i]) <= 1.58 && Math.Abs(phiOf[j]) <= 1.58;   // within ±75°
                double[] n = FaceNormal(m, fv), c = Centroid(m, fv);
                double len = Math.Sqrt(Dot(n, n));
                bool facingDown = len > 0 && (n[1] / len) * (Dot(n, Sub(c, inside)) < 0 ? -1 : 1) < -0.55;
                // the face map covers everything on the face side down to the chin — including the lower cheeks
                // that curve downward (they carry the blush); only the jaw underside further back uses the skin patch
                bool front = headPart && frontCols && c[1] > -0.47 && (c[2] > 0.1 || !facingDown);
                int[] ft = new int[4];
                for (int k = 0; k < 4; k++) { double[] uv = front ? FrontUV(m.V[fv[k]], phs[k]) : SkinPatchUV(m.V[fv[k]]); ft[k] = AddT(m, uv[0], uv[1]); }
                AddFace(m, fv, ft, "Head", inside);
            }
        BuildEar(m, +1);
        BuildEar(m, -1);
        return m;
    }

    // Round doll ear with a curled rim and a hollow bowl: base ring sunk into the head, raised outer rim,
    // inner rim, concave bowl, centre fan. UVs go to the ear islands in the lower corners of the face map.
    static void BuildEar(HeadMesh m, int side) {
        const int N = 10;
        double[] root = { side * 0.375, -0.11, -0.06 };            // raised to eye height
        double[] n = { side * 0.84, 0.05, 0.54 };               // facing out and forward, so the ear reads from the front
        double nl = Math.Sqrt(Dot(n, n)); n[0] /= nl; n[1] /= nl; n[2] /= nl;
        double[] up = { 0, 1, 0 };
        double[] tg = Cross(up, n); double tl = Math.Sqrt(Dot(tg, tg)); tg[0] /= tl; tg[1] /= tl; tg[2] /= tl;
        double h = 0.085, w = 0.058;
        double[][] rings = { new double[] { 0.92, -0.02 }, new double[] { 1.0, 0.03 }, new double[] { 0.74, 0.042 }, new double[] { 0.44, 0.012 } };
        int[,] idx = new int[rings.Length, N];
        double[,] lu = new double[rings.Length, N], lv = new double[rings.Length, N];
        for (int r = 0; r < rings.Length; r++)
            for (int k = 0; k < N; k++) {
                double a = 2 * Math.PI * k / N;
                double ca = Math.Cos(a), sa = Math.Sin(a);
                double bulge = ca < 0 ? 1.0 : 0.85;                 // fuller towards the back edge (C shape)
                double sc = rings[r][0], off = rings[r][1];
                double[] p = new double[3];
                for (int d = 0; d < 3; d++) p[d] = root[d] + up[d] * h * sa * sc + tg[d] * w * ca * sc * bulge + n[d] * off;
                idx[r, k] = AddV(m, p);
                lu[r, k] = 0.5 + 0.5 * ca * sc * bulge * (side > 0 ? -1 : 1);
                lv[r, k] = 0.5 + 0.5 * sa * sc;
            }
        int centre = AddV(m, new double[] { root[0] + n[0] * -0.004, root[1], root[2] + n[2] * -0.004 });
        // ear islands (px in the 1000x1024 crop): left (15..190, 830..1005), right (805..980, 830..1005)
        double ix0 = side > 0 ? 805 : 15, iy0 = 830, iw = 175, ih = 175;
        Func<double, double, int> T = (u, v) => AddT(m, (ix0 + u * iw) / TEX_W, 1 - (iy0 + (1 - v) * ih) / TEX_H);
        double[] inside = { root[0] - n[0] * 0.06, root[1], root[2] - n[2] * 0.06 };
        string g = side > 0 ? "Ear_L" : "Ear_R";
        for (int r = 0; r < rings.Length - 1; r++)
            for (int k = 0; k < N; k++) {
                int j = (k + 1) % N;
                int[] fv = { idx[r, k], idx[r, j], idx[r + 1, j], idx[r + 1, k] };
                int[] ft = { T(lu[r, k], lv[r, k]), T(lu[r, j], lv[r, j]), T(lu[r + 1, j], lv[r + 1, j]), T(lu[r + 1, k], lv[r + 1, k]) };
                AddFace(m, fv, ft, g, inside);
            }
        int last = rings.Length - 1;
        for (int k = 0; k < N; k++) {
            int j = (k + 1) % N;
            int[] fv = { centre, idx[last, k], idx[last, j] };
            int[] ft = { T(0.5, 0.5), T(lu[last, k], lv[last, k]), T(lu[last, j], lv[last, j]) };
            AddFace(m, fv, ft, g, inside);
        }
    }

    // =====================================================================================================
    // SD bob hair (after 02_Model/Face/Hair.png): main back volume (round dome → straight bob → inward hem),
    // blunt segmented bangs, thin side strands L/R, underside ring. Built off the head surface so it always
    // sits outside the skull. Quad-based; the crown fan is the only triangles.
    // =====================================================================================================
    static readonly double[] C0 = { 0, 0.05, 0 };
    static double[] Scaled(double[] p, double k) { return new double[] { C0[0] + (p[0] - C0[0]) * k, C0[1] + (p[1] - C0[1]) * k, C0[2] + (p[2] - C0[2]) * k }; }
    static double ThetaAtY(double y) { return Math.Acos(Math.Max(-1, Math.Min(1, (y - 0.05) / (CROWN_Y - 0.05)))); }
    static double HemY(double phi) { return -0.27 - 0.09 * Smooth(1.0, Math.PI, Math.Abs(phi)); }   // chin level at the sides → nape at the back

    // point on the bob silhouette: dome (head surface pushed out) above the centre line, straight bob below it
    static double[] BobPoint(double y, double phi, double k) {
        if (y >= 0.05) return Scaled(Shape(ThetaAtY(y), phi), k);
        double[] eq = Scaled(Shape(Math.PI / 2, phi), k);
        double hem = HemY(phi), t = Math.Min(1, (0.05 - y) / (0.05 - hem));
        double s = 1 - 0.07 * t * t;                                     // gently tucks in toward the hem
        return new double[] { eq[0] * s, y, eq[2] * s };
    }

    public static HeadMesh BuildHair() {
        HeadMesh m = new HeadMesh();
        const int NU = 24;
        double[] phiOf = new double[NU];
        for (int i = 0; i < NU; i++) { double ph = 2 * Math.PI * i / NU; if (ph > Math.PI) ph -= 2 * Math.PI; phiOf[i] = ph; }
        double[] inside = { 0, 0.0, 0 };
        Func<double, double, int> T = (u, v) => AddT(m, u, v);
        Func<double, double> openFront = ph => Math.Abs(ph) < 1.05 ? 1 : 0;       // ±60°: face opening below the hairline

        // ---- main volume: crown fan, dome rows, straight rows to the hem, then the underside ring
        const double K = 1.10;                                           // base volume sits under the strand chunks
        double[] domeDeg = { 10, 22, 34, 46, 58, 70, 80, 90 };
        double[] straightT = { 0.25, 0.5, 0.75, 1.0 };
        int rows = domeDeg.Length + straightT.Length + 2;
        int[,] R = new int[rows, NU];
        int pole = AddV(m, Scaled(Shape(0, 0), K));
        for (int i = 0; i < NU; i++) {
            double ph = phiOf[i];
            int r = 0;
            foreach (double d in domeDeg) R[r++, i] = AddV(m, Scaled(Shape(d * Math.PI / 180, ph), K));
            double hem = HemY(ph) + 0.05;                                       // base hem stays above the chunk tips
            foreach (double t in straightT) {
                double y = 0.05 + (hem - 0.05) * t;
                double[] p = BobPoint(y, ph, K);
                R[r++, i] = AddV(m, p);
            }
            double[] last = m.V[R[r - 1, i]];
            R[r++, i] = AddV(m, new double[] { last[0] * 0.88, last[1] + 0.03, last[2] * 0.88 });   // underside: hem turns in
            R[r++, i] = AddV(m, new double[] { last[0] * 0.84, last[1] + 0.16, last[2] * 0.84 });
        }
        for (int i = 0; i < NU; i++) {
            int j = (i + 1) % NU;
            int[] fv = { pole, R[0, i], R[0, j] };
            int[] ft = { T(0.5, 1), T((phiOf[i] + Math.PI) / (2 * Math.PI), 0.95), T((phiOf[j] + Math.PI) / (2 * Math.PI), 0.95) };
            AddFace(m, fv, ft, "Hair_Back", inside);
        }
        int domeRows = domeDeg.Length;
        for (int r = 0; r < rows - 1; r++)
            for (int i = 0; i < NU; i++) {
                int j = (i + 1) % NU;
                bool belowHairline = r >= 4;                                   // rows under ~58° in front are face
                if (belowHairline && openFront(phiOf[i]) > 0 && openFront(phiOf[j]) > 0) continue;
                if (Math.Abs(phiOf[j] - phiOf[i]) > Math.PI) { }               // seam column, fine
                int[] fv = { R[r, i], R[r, j], R[r + 1, j], R[r + 1, i] };
                double u0 = (phiOf[i] + Math.PI) / (2 * Math.PI), u1 = (phiOf[j] + Math.PI) / (2 * Math.PI);
                if (u1 < u0) u1 += 1;
                double v0 = 0.95 - 0.9 * r / (rows - 1), v1 = 0.95 - 0.9 * (r + 1) / (rows - 1);
                int[] ft = { T(u0, v0), T(u1, v0), T(u1, v1), T(u0, v1) };
                string g = r >= domeRows + straightT.Length - 1 ? "Hair_Under" : "Hair_Back";
                double[] ins = g == "Hair_Under" ? new double[] { 0, 0.4, 0 } : inside;
                AddFace(m, fv, ft, g, ins);
            }

        // ---- strand CHUNKS (Hair.png: "큰 덩어리 중심"): each chunk is a convex 3-column strip — edges tucked in,
        // middle bulged out — so neighbouring chunks meet in a V-groove, ending in a pointed tip.
        // cols: phi left/right; ys: rows top→bottom; k: radial scale; bulge: middle push; tip: extra drop of the
        // middle tip; curl: tuck-in toward the face/neck at the bottom; flare: outward swell near the hem.
        Action<double, double, double[], double, double, double, double, double, string> Chunk =
            (pa, pb, ys, k, bulge, tip, curl, flare, g) => {
                double pm = (pa + pb) / 2;
                double[] cols = { pa, pm, pb };
                int n = ys.Length;
                int[,] B = new int[n, 3];
                for (int r = 0; r < n; r++)
                    for (int c = 0; c < 3; c++) {
                        double y = ys[r];
                        double tr = (double)r / (n - 1);                              // 0 top → 1 tip
                        if (r == n - 1 && c == 1) y -= tip;
                        double kk = k * (c == 1 ? 1 + bulge : 0.985) * (1 + flare * Math.Sin(Math.PI * Math.Min(1, tr * 1.15)) );
                        double[] p = BobPoint(y, cols[c], kk);
                        double cu = Smooth(0.55, 1.0, tr) * curl;                     // tip tucks inward
                        p[0] *= 1 - cu; p[2] *= 1 - cu;
                        if (r == n - 1 && c != 1) { p[0] *= 0.97; p[2] *= 0.97; }     // tip corners narrow → point
                        B[r, c] = AddV(m, p);
                    }
                for (int r = 0; r < n - 1; r++)
                    for (int c = 0; c < 2; c++) {
                        int[] fv = { B[r, c], B[r, c + 1], B[r + 1, c + 1], B[r + 1, c] };
                        double ua = c / 2.0, ub = (c + 1) / 2.0, va = 1 - (double)r / (n - 1), vb = 1 - (double)(r + 1) / (n - 1);
                        int[] ft = { T(ua, va), T(ub, va), T(ub, vb), T(ua, vb) };
                        AddFace(m, fv, ft, g, inside);
                    }
            };

        // back + side volume: 11 big chunks from the temples round the back, slightly different lengths
        double[] lenJitter = { 0.0, 0.02, -0.01, 0.025, 0.0, 0.015, -0.005, 0.02, 0.0, 0.025, 0.01 };
        for (int s = 0; s < 11; s++) {
            double pa = 1.2 + s * ((2 * Math.PI - 2.4) / 11), pb = pa + (2 * Math.PI - 2.4) / 11;
            double mid = (pa + pb) / 2, hem = HemY(mid > Math.PI ? mid - 2 * Math.PI : mid) - 0.02 - lenJitter[s];
            double[] ys = { 0.34, 0.22, 0.1, -0.02, -0.12, -0.2, hem };
            Chunk(pa > Math.PI ? pa - 2 * Math.PI : pa, pb > Math.PI ? pb - 2 * Math.PI : pb, ys,
                  1.125 + (s % 2) * 0.006, 0.03, 0.045, 0.06, 0.035, "Hair_Back");
        }
        // bangs: 7 thick blunt chunks, longer (down to just above the eyes), alternating lengths
        for (int s = 0; s < 7; s++) {
            double pa = -1.1 + s * (2.2 / 7), pb = pa + 2.2 / 7;
            double bottom = (s % 2 == 0) ? 0.0 : 0.018;
            double[] ys = { 0.38, 0.29, 0.2, 0.12, 0.06, bottom };
            Chunk(pa, pb, ys, 1.14 + (s % 2) * 0.006, 0.025, 0.028, 0.0, 0.0, "Hair_Bangs");
        }
        // side locks: two long chunks per side in front of the ears, down past the chin, curling in
        foreach (int side in new int[] { 1, -1 }) {
            Chunk(side * 1.02, side * 1.24, new double[] { 0.26, 0.14, 0.02, -0.1, -0.2, -0.29, -0.36 },
                  1.15, 0.03, 0.05, 0.1, 0.02, side > 0 ? "Hair_Side_L" : "Hair_Side_R");
            Chunk(side * 1.22, side * 1.42, new double[] { 0.26, 0.14, 0.02, -0.1, -0.2, -0.3 },
                  1.14, 0.03, 0.045, 0.08, 0.025, side > 0 ? "Hair_Side_L" : "Hair_Side_R");
        }
        return m;
    }

    // ---- one level of Catmull-Clark (positions), UVs subdivided linearly per face; groups carried over
    public static HeadMesh Subdivide(HeadMesh m) {
        HeadMesh o = new HeadMesh();
        int nv = m.V.Count;
        var edgeFaces = new Dictionary<long, List<int>>();
        Func<int, int, long> key = (a, b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        var facePts = new double[m.F.Count][];
        for (int fi = 0; fi < m.F.Count; fi++) {
            facePts[fi] = Centroid(m, m.F[fi]);
            int[] f = m.F[fi];
            for (int i = 0; i < f.Length; i++) {
                long k = key(f[i], f[(i + 1) % f.Length]);
                List<int> l; if (!edgeFaces.TryGetValue(k, out l)) { l = new List<int>(); edgeFaces[k] = l; }
                l.Add(fi);
            }
        }
        var vFaces = new List<int>[nv]; var vEdges = new List<long>[nv];
        for (int i = 0; i < nv; i++) { vFaces[i] = new List<int>(); vEdges[i] = new List<long>(); }
        for (int fi = 0; fi < m.F.Count; fi++) foreach (int v in m.F[fi]) vFaces[v].Add(fi);
        foreach (long k in edgeFaces.Keys) { vEdges[(int)(k >> 32)].Add(k); vEdges[(int)(k & 0xffffffff)].Add(k); }
        Func<long, double[]> mid = k => {
            double[] a = m.V[(int)(k >> 32)], b = m.V[(int)(k & 0xffffffff)];
            return new double[] { (a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2 };
        };
        // new vertex points
        for (int v = 0; v < nv; v++) {
            double[] p = m.V[v];
            List<long> bnd = new List<long>();
            foreach (long k in vEdges[v]) if (edgeFaces[k].Count < 2) bnd.Add(k);
            if (bnd.Count >= 2) {
                double[] a = mid(bnd[0]), b = mid(bnd[1]);
                o.V.Add(new double[] { (p[0] * 2 + a[0] + b[0]) / 4, (p[1] * 2 + a[1] + b[1]) / 4, (p[2] * 2 + a[2] + b[2]) / 4 });
                continue;
            }
            int n = vFaces[v].Count;
            if (n < 3) { o.V.Add(new double[] { p[0], p[1], p[2] }); continue; }
            double[] Q = { 0, 0, 0 }, R = { 0, 0, 0 };
            foreach (int fi in vFaces[v]) for (int d = 0; d < 3; d++) Q[d] += facePts[fi][d] / n;
            foreach (long k in vEdges[v]) { double[] e = mid(k); for (int d = 0; d < 3; d++) R[d] += e[d] / vEdges[v].Count; }
            o.V.Add(new double[] { (Q[0] + 2 * R[0] + (n - 3) * p[0]) / n, (Q[1] + 2 * R[1] + (n - 3) * p[1]) / n, (Q[2] + 2 * R[2] + (n - 3) * p[2]) / n });
        }
        // edge points
        var edgeIdx = new Dictionary<long, int>();
        foreach (var kv in edgeFaces) {
            double[] e = mid(kv.Key);
            if (kv.Value.Count == 2) {
                double[] f1 = facePts[kv.Value[0]], f2 = facePts[kv.Value[1]];
                e = new double[] { (e[0] * 2 + f1[0] + f2[0]) / 4, (e[1] * 2 + f1[1] + f2[1]) / 4, (e[2] * 2 + f1[2] + f2[2]) / 4 };
            }
            o.V.Add(e); edgeIdx[kv.Key] = o.V.Count - 1;
        }
        // face points + new quads
        for (int fi = 0; fi < m.F.Count; fi++) {
            int[] f = m.F[fi], ft = m.FT[fi];
            o.V.Add(facePts[fi]); int fp = o.V.Count - 1;
            int c = f.Length;
            double[] fuv = { 0, 0 };
            for (int i = 0; i < c; i++) { fuv[0] += m.VT[ft[i]][0] / c; fuv[1] += m.VT[ft[i]][1] / c; }
            int tf = AddT(o, fuv[0], fuv[1]);
            int[] tv = new int[c], te = new int[c];
            for (int i = 0; i < c; i++) {
                double[] a = m.VT[ft[i]], b = m.VT[ft[(i + 1) % c]];
                tv[i] = AddT(o, a[0], a[1]);
                te[i] = AddT(o, (a[0] + b[0]) / 2, (a[1] + b[1]) / 2);
            }
            for (int i = 0; i < c; i++) {
                int prev = (i + c - 1) % c;
                o.F.Add(new int[] { f[i], edgeIdx[key(f[i], f[(i + 1) % c])], fp, edgeIdx[key(f[prev], f[i])] });
                o.FT.Add(new int[] { tv[i], te[i], tf, te[prev] });
                o.G.Add(m.G[fi]);
            }
        }
        return o;
    }

    static string F(double v) { return v.ToString("0.######", CultureInfo.InvariantCulture); }

    public static double[][] Normals(HeadMesh m) {
        double[][] nn = new double[m.V.Count][];
        for (int i = 0; i < nn.Length; i++) nn[i] = new double[3];
        foreach (int[] f in m.F) { double[] n = FaceNormal(m, f); foreach (int v in f) for (int d = 0; d < 3; d++) nn[v][d] += n[d]; }
        foreach (double[] n in nn) { double l = Math.Sqrt(Dot(n, n)); if (l > 0) for (int d = 0; d < 3; d++) n[d] /= l; }
        return nn;
    }

    public static string ToObj(HeadMesh m, string mtlFile, string comment) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# " + comment);
        sb.AppendLine("# units: head height 1.0 (crown y=+0.52, chin y=-0.48), +Z = face forward, +Y up");
        sb.AppendLine("# verts " + m.V.Count + ", faces " + m.F.Count);
        sb.AppendLine("mtllib " + mtlFile);
        foreach (double[] v in m.V) sb.AppendLine("v " + F(v[0]) + " " + F(v[1]) + " " + F(v[2]));
        foreach (double[] t in m.VT) sb.AppendLine("vt " + F(t[0]) + " " + F(t[1]));
        double[][] nn = Normals(m);
        foreach (double[] n in nn) sb.AppendLine("vn " + F(n[0]) + " " + F(n[1]) + " " + F(n[2]));
        string cur = null;
        for (int fi = 0; fi < m.F.Count; fi++) {
            if (m.G[fi] != cur) { cur = m.G[fi]; sb.AppendLine("o " + cur); sb.AppendLine("usemtl SD_Head_Skin"); sb.AppendLine("s 1"); }
            sb.Append("f");
            for (int k = 0; k < m.F[fi].Length; k++) sb.Append(" " + (m.F[fi][k] + 1) + "/" + (m.FT[fi][k] + 1) + "/" + (m.F[fi][k] + 1));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string ToJson(HeadMesh m) {
        StringBuilder sb = new StringBuilder("{\"v\":[");
        for (int i = 0; i < m.V.Count; i++) sb.Append((i > 0 ? "," : "") + F(m.V[i][0]) + "," + F(m.V[i][1]) + "," + F(m.V[i][2]));
        sb.Append("],\"t\":[");
        for (int i = 0; i < m.VT.Count; i++) sb.Append((i > 0 ? "," : "") + F(m.VT[i][0]) + "," + F(m.VT[i][1]));
        sb.Append("],\"f\":[");
        for (int fi = 0; fi < m.F.Count; fi++) {
            sb.Append(fi > 0 ? ",[" : "[");
            for (int k = 0; k < m.F[fi].Length; k++) sb.Append((k > 0 ? "," : "") + m.F[fi][k] + "," + m.FT[fi][k]);
            sb.Append("]");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    public static string Stats(HeadMesh m) {
        int q = 0, t = 0; foreach (int[] f in m.F) { if (f.Length == 4) q++; else t++; }
        return "verts " + m.V.Count + ", quads " + q + ", tris " + t + " (≈" + (q * 2 + t) + " triangles)";
    }
}
