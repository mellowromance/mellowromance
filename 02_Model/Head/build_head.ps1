# Builds the SD low-poly head:
#   02_Model/Head/head_base.obj   — quad-based base mesh (pre-subdivision original)
#   02_Model/Head/head_subd1.obj  — the same mesh after one Catmull-Clark level (smooth version)
#   02_Model/Head/head.mtl + face_map.png — material and the face texture (cropped from the face-map sheet)
# and embeds the base mesh + face map into game/index.html.
#   powershell -ExecutionPolicy Bypass -File 02_Model\Head\build_head.ps1
param(
  [string]$FaceSheet = "$PSScriptRoot\..\Face\890853ce-4e99-418e-b0fc-e4cd77c26755.png",
  [string]$GameHtml  = "$PSScriptRoot\..\..\game\index.html",
  [int]$EyeShiftPx = 30,         # how far the painted eyes are moved up on the face map (sheet px)
  [double]$EyeScale = 1.2        # how much bigger the painted eyes are drawn
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -Language CSharp -TypeDefinition @'
using System; using System.Drawing;
public static class FaceMapEdit {
    // eye centres / radii in the 1000x1024 crop of the face-map sheet
    static readonly int[,] Eyes = { { 322, 668 }, { 677, 668 } };
    const double RX = 128, RY = 74, FEATHER = 16;
    static double Mask(double x, double y) {
        double best = 0;
        for (int e = 0; e < 2; e++) {
            double dx = (x - Eyes[e, 0]) / RX, dy = (y - Eyes[e, 1]) / RY;
            double d = Math.Sqrt(dx * dx + dy * dy);                 // 1 on the ellipse edge
            double px = (d - 1) * Math.Min(RX, RY);                  // ~px outside the edge
            best = Math.Max(best, Math.Min(1, Math.Max(0, 1 - px / FEATHER)));
        }
        return best;
    }
    static double[] Bilinear(Bitmap b, double x, double y) {
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y); double fx = x - x0, fy = y - y0;
        double[] o = new double[3];
        for (int j = 0; j < 2; j++) for (int i = 0; i < 2; i++) {
            Color c = b.GetPixel(Math.Min(b.Width - 1, Math.Max(0, x0 + i)), Math.Min(b.Height - 1, Math.Max(0, y0 + j)));
            double w = (i == 0 ? 1 - fx : fx) * (j == 0 ? 1 - fy : fy);
            o[0] += c.R * w; o[1] += c.G * w; o[2] += c.B * w;
        }
        return o;
    }
    // Move each painted eye up by `shift` px and enlarge it by `scale` about its centre (feathered),
    // refilling the vacated skin. Mouth, brows and blush are left where they are.
    public static void ShiftEyesUp(Bitmap bmp, int shift, double scale) {
        if (shift <= 0 && Math.Abs(scale - 1) < 1e-6) return;
        Bitmap src = (Bitmap)bmp.Clone();
        for (int y = 470; y < 820; y++)
            for (int x = 130; x < 870; x++) {
                double mOld = Mask(x, y);
                // nearest eye → where this destination pixel samples the original eye from
                int e = x < 500 ? 0 : 1;
                double dir = e == 0 ? -1 : 1, outShift = RX * (scale - 1);   // move centres outward so the gap between the eyes is kept
                double sx = Eyes[e, 0] + (x - (Eyes[e, 0] + dir * outShift)) / scale, sy = Eyes[e, 1] + (y - (Eyes[e, 1] - shift)) / scale;
                double mNew = Mask(sx, sy);
                if (mOld <= 0 && mNew <= 0) continue;
                Color o = src.GetPixel(x, y);
                Color sk = src.GetPixel(x, 545);                     // clean skin above the eye band, same column
                double r = o.R + (sk.R - o.R) * mOld, g = o.G + (sk.G - o.G) * mOld, b = o.B + (sk.B - o.B) * mOld;
                if (mNew > 0) {
                    double[] n = Bilinear(src, sx, sy);
                    r += (n[0] - r) * mNew; g += (n[1] - g) * mNew; b += (n[2] - b) * mNew;
                }
                bmp.SetPixel(x, y, Color.FromArgb(255, (int)Math.Round(r), (int)Math.Round(g), (int)Math.Round(b)));
            }
        src.Dispose();
    }
}
'@
Add-Type -TypeDefinition ([IO.File]::ReadAllText("$PSScriptRoot\HeadGen.cs")) -Language CSharp

$base = [HeadGen]::Build()
$subd = [HeadGen]::Subdivide($base)
$enc = New-Object System.Text.UTF8Encoding $false
[IO.File]::WriteAllText("$PSScriptRoot\head_base.obj", [HeadGen]::ToObj($base, 'head.mtl', 'SD low-poly head - base mesh (quads, pre-subdivision)'), $enc)
[IO.File]::WriteAllText("$PSScriptRoot\head_subd1.obj", [HeadGen]::ToObj($subd, 'head.mtl', 'SD low-poly head - Catmull-Clark level 1'), $enc)
[IO.File]::WriteAllText("$PSScriptRoot\head.mtl", "newmtl SD_Head_Skin`nKd 1 1 1`nKs 0 0 0`nmap_Kd face_map.png`n", $enc)
# (the bob hair — [HeadGen]::BuildHair() — was rolled back; the game uses its twin-tail hair again)

# face map: the left 1000x1024 of the sheet is the UV texture; save as 1024x1024
$sheet = [System.Drawing.Image]::FromFile((Resolve-Path $FaceSheet).Path)
$crop = New-Object System.Drawing.Bitmap 1000, 1024
$g = [System.Drawing.Graphics]::FromImage($crop)
$g.DrawImage($sheet, (New-Object System.Drawing.Rectangle 0, 0, 1000, 1024), (New-Object System.Drawing.Rectangle 0, 0, 1000, 1024), [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose(); $sheet.Dispose()
# raise the painted eyes (mouth/brows/blush stay): feathered move of the two eye areas, vacated skin refilled
[FaceMapEdit]::ShiftEyesUp($crop, $EyeShiftPx, $EyeScale)
$tex = New-Object System.Drawing.Bitmap 1024, 1024
$g = [System.Drawing.Graphics]::FromImage($tex)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($crop, (New-Object System.Drawing.Rectangle 0, 0, 1024, 1024), (New-Object System.Drawing.Rectangle 0, 0, 1000, 1024), [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose(); $crop.Dispose()
$tex.Save("$PSScriptRoot\face_map.png", [System.Drawing.Imaging.ImageFormat]::Png)
$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$ep = New-Object System.Drawing.Imaging.EncoderParameters 1
$ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality), 92L
$ms = New-Object System.IO.MemoryStream; $tex.Save($ms, $codec, $ep); $tex.Dispose()
$faceUri = 'data:image/jpeg;base64,' + [Convert]::ToBase64String($ms.ToArray())

# embed into the game (base mesh: low poly, smooth-shaded at runtime)
$block = "<!--HEAD_MESH_START-->`n<script>window.HEAD_MESH = $([HeadGen]::ToJson($base)); window.HEAD_FACE_MAP = '$faceUri';</script>`n<!--HEAD_MESH_END-->"
$htmlPath = (Resolve-Path $GameHtml).Path
$text = [IO.File]::ReadAllText($htmlPath)
$re = '(?s)<!--HEAD_MESH_START-->.*?<!--HEAD_MESH_END-->'
if ($text -match $re) { $text = [regex]::Replace($text, $re, [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $block }) }
else { $text = $text.Replace('</body>', "$block`n</body>") }
[IO.File]::WriteAllText($htmlPath, $text, $enc)

"base : " + [HeadGen]::Stats($base)
"subd1: " + [HeadGen]::Stats($subd)
"face map embedded: {0:N0} KB" -f ($faceUri.Length / 1024)
