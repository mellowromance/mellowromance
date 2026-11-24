// Texture (mapping image) guide sheets for the outfit parts: for each part one sheet with the real UV layout
// annotated (front centre, back seam, up direction, hidden areas, landmarks) and a Korean painting brief per
// UV island, plus an overview sheet with the shared rules. Compiled together with OutfitGen.cs.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

public class IslandInfo {
    public string Key, Ko, Where, Paint; public string[] Colors;
    public string Kind;                       // lathe | lathe2 (mirrored L/R) | band | tube | strip | fan | cap | knot
    public double HiddenFrom = -1, HiddenTo = -1;          // v range that is (mostly) not visible
    public double[] MarkV; public string[] MarkLabel;      // landmarks along v
    public IslandInfo(string key, string ko, string kind, string where, string paint, params string[] colors) { Key = key; Ko = ko; Kind = kind; Where = where; Paint = paint; Colors = colors; }
    public IslandInfo Hide(double a, double b) { HiddenFrom = a; HiddenTo = b; return this; }
    public IslandInfo Marks(double[] v, string[] l) { MarkV = v; MarkLabel = l; return this; }
}

public static class OutfitGuide {
    const string FONT = "Malgun Gothic";
    public static readonly string[] Titles = { "헤도레", "상의 (보디스+소매)", "스커트", "레그웨어", "신발" };
    static Color C(string h) { return ColorTranslator.FromHtml(h); }
    static readonly Color INK = C("#4a3540"), ACCENT = C("#d8456a"), SOFT = C("#fbeef2"), LINE = C("#e9c9d4");

    public static readonly string[][] Palette = {
        new string[] { "#fffaf7", "화이트(바탕)" }, new string[] { "#fff1e0", "생크림" }, new string[] { "#f0d9c0", "크림 음영" },
        new string[] { "#f9c9d8", "핑크" }, new string[] { "#f2a3bb", "미디엄 핑크" }, new string[] { "#e7849f", "진한 핑크" },
        new string[] { "#d8344d", "딸기 레드" }, new string[] { "#a8203a", "레드 음영" }, new string[] { "#6dbb5a", "잎 그린" },
        new string[] { "#ffe28a", "딸기 씨" }, new string[] { "#bcdcf5", "하늘색 리본" }, new string[] { "#86b2de", "리본 음영" },
    };

    public static List<IslandInfo> Info(string part) {
        List<IslandInfo> L = new List<IslandInfo>();
        switch (part) {
            case "headdress":
                L.Add(new IslandInfo("band", "머리띠", "band", "정수리를 귀에서 귀까지 가로지르는 두꺼운 띠. 양 끝은 양갈래 속으로 들어감.",
                    "핑크 바탕 + 길이 방향 흰 줄무늬 2~3줄, 앞·뒷면 가장자리에 작은 레이스. 세로 4칸 = 단면 4면: 아래부터 윗면(가장 잘 보임) / 앞면 / 안쪽(머리에 닿음, 안 보임) / 뒷면.",
                    "#f9c9d8", "#fffaf7").Hide(0.5, 0.75).Marks(new double[] { 0.0, 0.25, 0.5, 0.75 }, new string[] { "윗면", "앞면", "안쪽", "뒷면" }));
                L.Add(new IslandInfo("frill (front+back)", "프릴 (앞·뒤 공유)", "strip", "띠 앞(앞머리 위)과 뒤로 퍼지는 러플. 앞뒤 프릴이 같은 그림을 씀. 양면으로 보임.",
                    "흰 레이스. 아래(0%) = 띠에 붙는 쪽 → 얇은 핑크 라인, 위(100%) = 바깥 가장자리 → 스캘럽/레이스 구멍 무늬.", "#fffaf7", "#f3d6df", "#f9c9d8"));
                L.Add(new IslandInfo("cream piping", "생크림 롤", "tube", "띠 위를 따라 짜낸 휘핑크림 롤 (울퉁불퉁한 튜브).",
                    "생크림색 바탕에 비스듬한 꼬임 줄무늬(짜낸 크림 결)와 흰 하이라이트. 가로 = 띠 길이, 세로 = 튜브 둘레.", "#fff1e0", "#f0d9c0"));
                L.Add(new IslandInfo("strawberry", "딸기 (2개 공유)", "lathe", "크림 위에 꽂힌 딸기 2개. 아래(0%) = 뾰족한 끝(크림에 묻힘), 위(100%) = 꼭지 쪽.",
                    "빨강 바탕, 노란 씨 점을 엇갈리게, 위쪽은 살짝 밝게, 끝은 어둡게.", "#d8344d", "#ffe28a", "#a8203a").Marks(new double[] { 0.0, 1.0 }, new string[] { "끝", "꼭지" }));
                L.Add(new IslandInfo("leaf", "딸기 꼭지 잎", "fan", "딸기 위의 별 모양 잎(위에서 본 모양 그대로).", "초록, 가운데에서 바깥으로 잎맥.", "#6dbb5a", "#3f8a3a"));
                L.Add(new IslandInfo("cream dollop", "크림 장식 (4개 공유)", "lathe", "띠 위의 짜낸 크림 덩어리 4개. 아래 = 바닥, 위 = 뾰족한 끝. 가로 8칸 중 한 칸 걸러 홈.",
                    "생크림색 + 칸마다 번갈아 세로 홈 음영(별깍지 자국).", "#fff1e0", "#f0d9c0").Marks(new double[] { 0.0, 1.0 }, new string[] { "바닥", "끝" }));
                L.Add(new IslandInfo("bow loop", "리본 고리 (2개 공유)", "lathe", "왼쪽 양갈래 위 하늘색 리본의 고리. 아래 = 매듭 쪽, 위 = 고리 끝.", "하늘색, 가장자리 진한 하늘색 라인, 가운데 접힌 주름 1~2줄.", "#bcdcf5", "#86b2de"));
                L.Add(new IslandInfo("bow knot", "리본 매듭", "knot", "리본 가운데 매듭.", "하늘색, 가운데 주름.", "#bcdcf5", "#86b2de"));
                L.Add(new IslandInfo("bow tail", "리본 꼬리 (2개 공유)", "lathe", "아래로 늘어지는 리본 꼬리. 아래 = 매듭 쪽, 위 = 꼬리 끝.", "하늘색, 끝 쪽 V컷 느낌으로 진하게.", "#bcdcf5", "#86b2de"));
                break;
            case "top":
                L.Add(new IslandInfo("bodice (front = centre)", "보디스 (몸통)", "lathe", "허리부터 목 칼라까지 몸통 전체. 가로 가운데 = 정면, 양 끝 = 등(이음선).",
                    "정면 가운데 핑크 코르셋 패널 + 빨간 레이스업(X자), 패널 양옆 레이스. 가슴 위(약 62~70%)에 장미 장식 줄, 허리 양옆 빨간 리본, 허리 맨 아래 핑크 밴드. 75~80%는 목선 프릴에 가려짐, 맨 위는 흰 레이스 칼라. 등은 흰 바탕(원하면 레이스업).",
                    "#fffaf7", "#f9c9d8", "#d8344d", "#f2a3bb").Hide(0.74, 0.8).Marks(new double[] { 0.0, 0.55, 0.66, 0.78, 0.885 }, new string[] { "허리", "가슴", "장미 줄", "어깨", "칼라" }));
                L.Add(new IslandInfo("neckline frill", "목선 프릴", "lathe", "어깨 위 목선을 두르는 짧은 프릴. 아래(0%) = 바깥 지그재그 끝, 위 = 목 쪽.", "흰 레이스 + 끝단 핑크 라인.", "#fffaf7", "#f9c9d8", "#f3d6df"));
                L.Add(new IslandInfo("puff sleeve (L/R shared)", "퍼프 소매 (좌우 공유)", "lathe2", "어깨의 둥근 퍼프 소매. 아래 = 소매 끝(팔 쪽), 위 = 어깨 위(몸통 속으로 들어가 거의 안 보임).",
                    "흰 바탕 + 세로 개더 주름 라인(가로 8등분), 아래 끝 핑크 밴드. 좌우가 같은 그림이라 글자/비대칭 무늬 금지.", "#fffaf7", "#f3d6df", "#f9c9d8").Hide(0.82, 1.0).Marks(new double[] { 0.0, 0.5, 1.0 }, new string[] { "소매끝", "가장 볼록", "어깨" }));
                L.Add(new IslandInfo("sleeve frill", "소매 프릴", "lathe2", "퍼프 소매 끝의 프릴. 아래 = 지그재그 끝, 위 = 소매 쪽.", "흰 레이스 + 끝단 미디엄 핑크 라인.", "#fffaf7", "#f2a3bb"));
                L.Add(new IslandInfo("wrist cuff", "손목 커프", "lathe2", "손목의 작은 프릴 커프. 아래 = 손 쪽 지그재그 끝, 위 = 팔 쪽.", "흰 레이스, 위쪽 핑크 밴드.", "#fffaf7", "#f9c9d8"));
                break;
            case "skirt":
                L.Add(new IslandInfo("petticoat frill", "페티코트 프릴", "lathe", "치마 밑단 아래로 살짝 보이는 흰 러플. 아래 = 지그재그 끝.", "흰 레이스 구멍 무늬 + 끝단 핑크 라인. 아래 절반만 주로 보임.", "#fffaf7", "#f3d6df", "#f9c9d8").Hide(0.55, 1.0));
                L.Add(new IslandInfo("pink pleated skirt", "핑크 주름 치마", "lathe", "오버스커트 아래 보이는 핑크 치마. 가로 16칸 중 한 칸 걸러 안으로 접힌 주름. 위쪽은 오버스커트에 덮임.",
                    "핑크 바탕 + 칸마다 번갈아 진한 핑크 주름 음영, 밑단(0~12%) 흰 레이스/스캘럽. 35% 위로는 안 보이니 단색이면 충분.", "#f9c9d8", "#f2a3bb", "#fffaf7").Hide(0.35, 1.0).Marks(new double[] { 0.0, 0.19 }, new string[] { "밑단", "오버스커트 끝" }));
                L.Add(new IslandInfo("cream overskirt (swags)", "생크림 오버스커트", "lathe", "케이크 층처럼 스캘럽진 흰 덧치마. 가로 1/8마다(굵은 점선) 밑단이 위로 들려 8개의 드레이프가 생김.",
                    "흰 바탕에 드레이프마다 늘어진 생크림 곡선 음영 2단, 들린 지점(1/8 점선)마다 딸기+크림 장미, 드레이프 위쪽에 작은 리본. 맨 위 약 10%는 허리 크림 롤에 가려짐.",
                    "#fffaf7", "#fff1e0", "#f0d9c0", "#d8344d", "#f2a3bb").Hide(0.9, 1.0).Marks(new double[] { 0.0, 0.3, 1.0 }, new string[] { "밑단", "딸기 높이", "허리" }));
                L.Add(new IslandInfo("hem piping", "밑단 생크림 롤", "tube", "오버스커트 스캘럽 밑단을 따라가는 크림 롤. 가로 = 한 바퀴(가운데 = 정면).", "생크림 + 비스듬한 꼬임 음영.", "#fff1e0", "#f0d9c0"));
                L.Add(new IslandInfo("bloomers", "블루머", "lathe", "치마 속 흰 블루머(거의 안 보임).", "흰색 + 아래 끝 레이스 라인 정도.", "#fffaf7", "#f3d6df"));
                L.Add(new IslandInfo("waist piping", "허리 생크림 롤", "tube", "허리를 두르는 크림 롤.", "생크림 + 꼬임 음영.", "#fff1e0", "#f0d9c0"));
                break;
            case "legwear":
                L.Add(new IslandInfo("sock", "양말 (좌우 공유)", "lathe2", "무릎 아래부터 발목까지 다리 전체. 아래 = 발목, 위 = 무릎. 대부분 러플에 덮이고 사이사이 보임.", "흰 바탕 + 연한 세로 골지.", "#fffaf7", "#f6ebee").Marks(new double[] { 0.0, 1.0 }, new string[] { "발목", "무릎" }));
                L.Add(new IslandInfo("top ruffle cuff (cherries)", "맨 위 러플 (체리)", "lathe2", "무릎 바로 아래 첫 번째 러플. 아래 = 지그재그 끝, 위 = 무릎 쪽 밴드. 가운데 = 정강이 정면.",
                    "흰 바탕, 정면 가운데(50%) 높이 50% 쯤에 체리 한 쌍, 위쪽 핑크 밴드, 끝단 레이스.", "#fffaf7", "#d8344d", "#6dbb5a", "#f9c9d8").Marks(new double[] { 0.0, 0.5, 1.0 }, new string[] { "끝단", "체리", "무릎" }));
                L.Add(new IslandInfo("lower ruffles", "아래 러플 2단 (공유)", "lathe2", "종아리·발목의 러플 2단이 같은 그림을 씀. 아래 = 지그재그 끝.", "흰 레이스 + 끝단 미디엄 핑크 라인.", "#fffaf7", "#f3d6df", "#f2a3bb"));
                break;
            case "shoes":
                L.Add(new IslandInfo("upper (toe = centre)", "신발 갑피 (좌우 공유)", "lathe2", "둥근 앞코 신발 윗부분. 아래 = 밑창에 닿는 가장자리, 위 = 발등 꼭대기(양말에 덮임). 가로 가운데 = 앞코.",
                    "흰 바탕, 앞코(가로 50%, 세로 35~50%)에 핑크 리본, 아래 가장자리 핑크 스티치 라인.", "#fffaf7", "#f2a3bb", "#e7849f").Hide(0.78, 1.0).Marks(new double[] { 0.0, 0.42, 1.0 }, new string[] { "밑창", "리본", "발등" }));
                L.Add(new IslandInfo("sole top/bottom", "밑창 윗면/바닥 (공유)", "cap", "밑창을 위에서 본 모양 그대로 (윗면·바닥이 같은 그림). 위쪽 = 앞코.", "핑크 단색 (바닥 무늬를 넣어도 됨).", "#f2b0c4"));
                L.Add(new IslandInfo("sole side", "밑창 옆면", "lathe2", "두꺼운 플랫폼 밑창 옆 띠. 아래 = 바닥, 위 = 윗면 테두리. 가운데 = 앞코.", "연핑크 + 위쪽 흰 라인.", "#f7c3d3", "#fffaf7"));
                break;
        }
        return L;
    }

    static RectangleF Px(ORect r, float S, float ox, float oy) { return new RectangleF(ox + (float)(r.U0 * S), oy + (float)((1 - r.V1) * S), (float)((r.U1 - r.U0) * S), (float)((r.V1 - r.V0) * S)); }
    static PointF At(ORect r, float S, float ox, float oy, double s, double t) { return new PointF(ox + (float)(r.U(s) * S), oy + (float)((1 - r.V(t)) * S)); }

    static void Wrap(Graphics g, string text, Font f, Brush b, float x, ref float y, float w) {
        SizeF sz = g.MeasureString(text, f, (int)w);
        g.DrawString(text, f, b, new RectangleF(x, y, w, sz.Height + 4));
        y += sz.Height + 2;
    }

    static void Rules(Graphics g, float x, ref float y, float w, Font fb, Font f) {
        string[] rules = {
            "이미지: 1024×1024 PNG (2048×2048도 가능, 정사각형 유지). 이 시트 왼쪽 UV 칸과 1:1로 같은 위치에 그리기.",
            "점선 박스(UV 영역) 안만 쓰임. 박스 밖은 비워도 됨. 경계는 6~8px 바깥까지 색을 번지게(블리드) 칠하기.",
            "원통형 영역: 가로 가운데 = 정면, 양 끝 = 등 쪽 이음선 → 왼쪽 끝과 오른쪽 끝 색·무늬가 이어지게.",
            "세로: 아래 = 옷의 아래쪽(밑단·소매 끝), 위 = 위쪽(허리·어깨). 별도 표시는 각 영역 설명 참고.",
            "'좌우 공유' 영역은 한 장으로 양쪽(L/R)에 쓰임 → 글자·비대칭 무늬는 반대쪽에서 뒤집혀 보임.",
            "회색 빗금 = 거의 안 보이는 곳 (단색이면 충분).",
            "게임은 2px 픽셀 렌더 → 6px보다 가는 선은 뭉개짐. 레이스·크림 결은 굵고 또렷하게.",
            "완성 파일: 04_Clothes/Outfit/tex/<파츠>.png 로 저장 후 build_outfit.ps1 실행.",
        };
        using (SolidBrush ink = new SolidBrush(INK)) using (SolidBrush ac = new SolidBrush(ACCENT)) {
            g.DrawString("공통 규칙", fb, ac, x, y); y += fb.GetHeight(g) + 6;
            for (int i = 0; i < rules.Length; i++) Wrap(g, (i + 1) + ". " + rules[i], f, ink, x, ref y, w);
        }
    }

    static void Hatch(Graphics g, RectangleF rc) {
        using (HatchBrush hb = new HatchBrush(HatchStyle.WideUpwardDiagonal, Color.FromArgb(150, 150, 150, 160), Color.FromArgb(70, 200, 200, 210))) g.FillRectangle(hb, rc);
    }

    /// Annotated UV map at (ox, oy) with size S.
    static void DrawUv(Graphics g, OPart P, List<IslandInfo> info, float ox, float oy, float S, bool annotate) {
        using (SolidBrush bg = new SolidBrush(Color.White)) g.FillRectangle(bg, ox, oy, S, S);
        using (Pen fr = new Pen(LINE, 2)) g.DrawRectangle(fr, ox, oy, S, S);
        Font fs = new Font(FONT, Math.Max(9, S / 70f), FontStyle.Bold, GraphicsUnit.Pixel);
        Font fn = new Font(FONT, Math.Max(10, S / 48f), FontStyle.Bold, GraphicsUnit.Pixel);
        for (int i = 0; i < info.Count; i++) {
            IslandInfo I = info[i]; ORect r = P.Find(I.Key); if (r == null) continue;
            RectangleF rc = Px(r, S, ox, oy);
            Color c0 = C(I.Colors[0]);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, (c0.R + 255 * 2) / 3 - 6, (c0.G + 255 * 2) / 3 - 6, (c0.B + 255 * 2) / 3 - 6))) g.FillRectangle(b, rc);
            if (I.HiddenFrom >= 0) { PointF a = At(r, S, ox, oy, 0, I.HiddenTo), b2 = At(r, S, ox, oy, 1, I.HiddenFrom); Hatch(g, new RectangleF(a.X, a.Y, b2.X - a.X, b2.Y - a.Y)); }
        }
        // wireframe
        using (Pen wire = new Pen(Color.FromArgb(120, 74, 53, 64), 1f))
            foreach (OMesh m in P.Meshes)
                foreach (int[] f in m.F) {
                    int c = f.Length / 2; PointF[] pts = new PointF[c];
                    for (int k = 0; k < c; k++) { double[] t = m.T[f[k * 2 + 1]]; pts[k] = new PointF(ox + (float)(t[0] * S), oy + (float)((1 - t[1]) * S)); }
                    g.DrawPolygon(wire, pts);
                }
        for (int i = 0; i < info.Count; i++) {
            IslandInfo I = info[i]; ORect r = P.Find(I.Key); if (r == null) continue;
            RectangleF rc = Px(r, S, ox, oy);
            using (Pen box = new Pen(ACCENT, 2f)) { box.DashStyle = DashStyle.Dash; g.DrawRectangle(box, rc.X, rc.Y, rc.Width, rc.Height); }
            if (annotate) {
                bool cyl = I.Kind == "lathe" || I.Kind == "lathe2" || I.Kind == "tube";
                using (SolidBrush tb = new SolidBrush(ACCENT)) using (SolidBrush ib = new SolidBrush(INK)) {
                    if (I.Kind == "lathe" || I.Kind == "lathe2") {
                        using (Pen cp = new Pen(Color.FromArgb(220, 216, 69, 106), 2.5f)) { cp.DashStyle = DashStyle.DashDot; PointF a = At(r, S, ox, oy, 0.5, 0), b = At(r, S, ox, oy, 0.5, 1); g.DrawLine(cp, a, b); }
                        using (Pen sp = new Pen(Color.FromArgb(140, 74, 53, 64), 1.5f)) { sp.DashStyle = DashStyle.Dot; foreach (double s in new double[] { 0.25, 0.75 }) g.DrawLine(sp, At(r, S, ox, oy, s, 0), At(r, S, ox, oy, s, 1)); }
                        if (rc.Width > 120) {
                            PointF q = At(r, S, ox, oy, 0.5, 0.02); g.DrawString(I.Key.StartsWith("upper") ? "앞코" : "정면", fs, tb, q.X + 3, q.Y - fs.GetHeight(g) - 1);
                            if (rc.Width > 420) {
                                q = At(r, S, ox, oy, 0.25, 0.02); g.DrawString(I.Kind == "lathe2" ? "안쪽(몸 쪽)" : "캐릭터 R 옆", fs, ib, q.X + 3, q.Y - fs.GetHeight(g) - 1);
                                q = At(r, S, ox, oy, 0.75, 0.02); g.DrawString(I.Kind == "lathe2" ? "바깥쪽" : "캐릭터 L 옆", fs, ib, q.X + 3, q.Y - fs.GetHeight(g) - 1);
                            }
                            q = At(r, S, ox, oy, 0.0, 0.02); g.DrawString("뒤", fs, ib, q.X + 3, q.Y - fs.GetHeight(g) - 1);
                            q = At(r, S, ox, oy, 1.0, 0.02); SizeF w = g.MeasureString("뒤", fs); g.DrawString("뒤", fs, ib, q.X - w.Width - 3, q.Y - fs.GetHeight(g) - 1);
                        }
                    }
                    if (I.Key.StartsWith("cream overskirt"))
                        using (Pen jp = new Pen(Color.FromArgb(200, 216, 69, 106), 2f)) { jp.DashStyle = DashStyle.Dash; for (int k = 1; k < 8; k++) if (k != 4) g.DrawLine(jp, At(r, S, ox, oy, k / 8.0, 0), At(r, S, ox, oy, k / 8.0, 0.35)); }
                    if (I.Kind == "band" || I.Kind == "tube") {
                        PointF q = At(r, S, ox, oy, 0.5, 0.0);
                        string t = I.Kind == "band" || I.Key == "cream piping" ? "← 캐릭터 오른쪽 끝 · · · 왼쪽 끝 →" : "← 뒤 · · · 정면(가운데) · · · 뒤 →";
                        SizeF w = g.MeasureString(t, fs); g.DrawString(t, fs, ib, q.X - w.Width / 2, q.Y - fs.GetHeight(g) - 2);
                    }
                    if (I.Kind == "strip" && rc.Height > 40) {
                        PointF q = At(r, S, ox, oy, 0.6, 0.06); g.DrawString("아래 = 띠에 붙는 쪽", fs, ib, q.X, q.Y - fs.GetHeight(g));
                        q = At(r, S, ox, oy, 0.6, 0.97); g.DrawString("위 = 바깥 가장자리(스캘럽)", fs, ib, q.X, q.Y);
                    }
                    if (I.Kind == "cap") { PointF q = At(r, S, ox, oy, 0.5, 0.97); SizeF w = g.MeasureString("▲ 앞코", fs); g.DrawString("▲ 앞코", fs, tb, q.X - w.Width / 2, q.Y); }
                    if (I.MarkV != null && rc.Height > 50)
                        using (Pen mp = new Pen(Color.FromArgb(200, 74, 53, 64), 1.5f))
                            for (int k = 0; k < I.MarkV.Length; k++) {
                                PointF q = At(r, S, ox, oy, 0, I.MarkV[k]);
                                g.DrawLine(mp, q.X, q.Y, q.X + 10, q.Y);
                                float yy = Math.Min(Math.Max(q.Y - fs.GetHeight(g) / 2, rc.Top), rc.Bottom - fs.GetHeight(g));
                                SizeF w = g.MeasureString(I.MarkLabel[k], fs);
                                using (SolidBrush wb = new SolidBrush(Color.FromArgb(210, 255, 255, 255))) g.FillRectangle(wb, q.X + 12, yy, w.Width, w.Height);
                                g.DrawString(I.MarkLabel[k], fs, ib, q.X + 12, yy);
                            }
                }
            }
            // number badge
            float br = Math.Max(10, S / 52f);
            PointF bc = new PointF(rc.Right - br - 4, rc.Top + br + 4);
            if (rc.Height < br * 2 + 8) bc = new PointF(rc.Right - br - 4, rc.Top + rc.Height / 2);
            using (SolidBrush b = new SolidBrush(ACCENT)) g.FillEllipse(b, bc.X - br, bc.Y - br, br * 2, br * 2);
            string num = (i + 1).ToString(); SizeF ns = g.MeasureString(num, fn);
            using (SolidBrush w = new SolidBrush(Color.White)) g.DrawString(num, fn, w, bc.X - ns.Width / 2, bc.Y - ns.Height / 2);
        }
        fs.Dispose(); fn.Dispose();
    }

    static void Swatch(Graphics g, string hex, float x, float y, float s) {
        using (SolidBrush b = new SolidBrush(C(hex))) g.FillRectangle(b, x, y, s, s);
        using (Pen p = new Pen(LINE, 1.5f)) g.DrawRectangle(p, x, y, s, s);
    }

    public static Bitmap Sheet(OPart P, string titleKo, int fileTex) {
        List<IslandInfo> info = Info(P.Name);
        const int W = 2200, H = 1400, S = 1200, OX = 40, OY = 140;
        Bitmap bmp = new Bitmap(W, H);
        using (Graphics g = Graphics.FromImage(bmp)) {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (SolidBrush bg = new SolidBrush(C("#fff8fa"))) g.FillRectangle(bg, 0, 0, W, H);
            Font ft = new Font(FONT, 44, FontStyle.Bold, GraphicsUnit.Pixel), fh = new Font(FONT, 24, FontStyle.Bold, GraphicsUnit.Pixel);
            Font fb = new Font(FONT, 20, FontStyle.Bold, GraphicsUnit.Pixel), f = new Font(FONT, 17, FontStyle.Regular, GraphicsUnit.Pixel);
            using (SolidBrush ink = new SolidBrush(INK)) using (SolidBrush ac = new SolidBrush(ACCENT)) {
                g.DrawString("맵핑 이미지 가이드 — " + titleKo, ft, ink, OX, 26);
                g.DrawString("파일: tex/" + P.Name + ".png   ·   " + fileTex + "×" + fileTex + " PNG   ·   왼쪽 UV 칸 = 실제 이미지 영역 (1:1)", fh, ac, OX, 88);
                DrawUv(g, P, info, OX, OY, S, true);
                // right panel
                float x = OX + S + 40, w = W - x - 40, y = OY;
                using (SolidBrush pb = new SolidBrush(Color.White)) g.FillRectangle(pb, x - 16, y - 12, w + 32, H - y - 28);
                using (Pen pp = new Pen(LINE, 2)) g.DrawRectangle(pp, x - 16, y - 12, w + 32, H - y - 28);
                g.DrawString("영역별로 그릴 내용", fb, ac, x, y); y += fb.GetHeight(g) + 8;
                for (int i = 0; i < info.Count; i++) {
                    IslandInfo I = info[i];
                    using (SolidBrush b = new SolidBrush(ACCENT)) g.FillEllipse(b, x, y + 1, 26, 26);
                    string num = (i + 1).ToString(); SizeF ns = g.MeasureString(num, fb);
                    using (SolidBrush wb = new SolidBrush(Color.White)) g.DrawString(num, fb, wb, x + 13 - ns.Width / 2, y + 14 - ns.Height / 2);
                    g.DrawString(I.Ko, fb, ink, x + 36, y); float sx = x + 36 + g.MeasureString(I.Ko, fb).Width + 8;
                    foreach (string hx in I.Colors) { Swatch(g, hx, sx, y + 4, 20); sx += 26; }
                    y += fb.GetHeight(g) + 2;
                    Wrap(g, "위치: " + I.Where, f, ink, x + 36, ref y, w - 36);
                    Wrap(g, "그릴 것: " + I.Paint, f, ac, x + 36, ref y, w - 36);
                    y += 8;
                }
                y += 6;
                using (Pen sep = new Pen(LINE, 2)) g.DrawLine(sep, x, y, x + w, y); y += 12;
                Rules(g, x, ref y, w, fb, f);
            }
            ft.Dispose(); fh.Dispose(); fb.Dispose(); f.Dispose();
        }
        return bmp;
    }

    public static Bitmap Overview(OPart[] parts, string[] titles) {
        const int W = 2200, H = 1400;
        Bitmap bmp = new Bitmap(W, H);
        using (Graphics g = Graphics.FromImage(bmp)) {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (SolidBrush bg = new SolidBrush(C("#fff8fa"))) g.FillRectangle(bg, 0, 0, W, H);
            Font ft = new Font(FONT, 44, FontStyle.Bold, GraphicsUnit.Pixel), fh = new Font(FONT, 22, FontStyle.Bold, GraphicsUnit.Pixel);
            Font fb = new Font(FONT, 20, FontStyle.Bold, GraphicsUnit.Pixel), f = new Font(FONT, 17, FontStyle.Regular, GraphicsUnit.Pixel);
            using (SolidBrush ink = new SolidBrush(INK)) using (SolidBrush ac = new SolidBrush(ACCENT)) {
                g.DrawString("맵핑 이미지 가이드 — 전체 (딸기 생크림 케이크 로리타)", ft, ink, 40, 26);
                g.DrawString("텍스처 5장 · 파츠마다 UV 독립 · 자세한 내용은 guide_<파츠>.png", fh, ac, 40, 88);
                float tw = 400, gap = 22, y0 = 150;
                for (int i = 0; i < parts.Length; i++) {
                    float x = 40 + i * (tw + gap);
                    DrawUv(g, parts[i], Info(parts[i].Name), x, y0, tw, false);
                    g.DrawString((i + 1) + ". " + titles[i], fb, ink, x, y0 + tw + 8);
                    g.DrawString("tex/" + parts[i].Name + ".png", f, ac, x, y0 + tw + 36);
                }
                float y = y0 + tw + 90, colW = 1000;
                Rules(g, 40, ref y, colW, fb, f);
                // palette
                float px = 40 + colW + 80, py = y0 + tw + 90;
                g.DrawString("색상 팔레트 (가이드 기준, 자유롭게 조정 가능)", fb, ac, px, py); py += 36;
                for (int i = 0; i < Palette.Length; i++) {
                    float x = px + (i % 3) * 330, yy = py + (i / 3) * 60;
                    Swatch(g, Palette[i][0], x, yy, 44);
                    g.DrawString(Palette[i][1], fb, ink, x + 56, yy + 2); g.DrawString(Palette[i][0], f, ink, x + 56, yy + 26);
                }
            }
            ft.Dispose(); fh.Dispose(); fb.Dispose(); f.Dispose();
        }
        return bmp;
    }
}
