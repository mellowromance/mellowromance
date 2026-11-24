# Fits the hand-made / AI-made mapping images in 04_Clothes\Mapping onto the real UV layout.
# The source images don't follow the UV islands exactly (proportions changed, pieces moved), so each island
# is cut out of the source and stretched back into its true UV rectangle -> tex\<part>.png (used by build_outfit.ps1).
# Table rows: source rect (sx0,sy0,sx1,sy1) -> destination rect (dx0,dy0,dx1,dy1), 1024px coordinates.
# sx0 > sx1 means "mirrored horizontally" (used for the back halves of the cylinders so the seams line up).
#   powershell -ExecutionPolicy Bypass -File 04_Clothes\Outfit\fit_mapping.ps1
param([string]$Src = "$PSScriptRoot\..\Mapping", [string]$Out = "$PSScriptRoot\tex", [int]$Bleed = 6)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$maps = [ordered]@{
  headdress = @(
    @(0,4,1024,182,     0,0,1024,184),       # band
    @(0,206,1024,348,   0,205,1024,348),     # frill
    @(0,366,1024,492,   0,369,1024,492),     # cream piping
    @(8,516,244,794,    0,512,246,799),      # strawberry
    @(280,518,466,706,  266,512,471,717),    # leaf
    @(498,518,708,708,  492,512,717,717),    # cream dollop
    @(8,826,402,1016,   0,819,410,1024),     # bow loop
    @(440,906,572,1016, 430,881,573,1024),   # bow knot
    @(616,826,794,1016, 594,819,799,1024)    # bow tail
  )
  top = @(
    # bodice: front half (u .25-.75) from the front illustration, rows re-spaced so the rosettes sit below the
    # neckline frill (collar ruffle -> collar/shoulder, rosettes -> t .58-.76, corset -> waist)
    @(748,6,1020,138,   0,0,717,147),        # collar/shoulder band all round <- the drawn white neckline ruffle
    @(240,65,480,140,   215,147,502,258),    # rosette row (front)
    @(150,140,570,600,  215,258,502,614),    # corset front incl. both red bows (u .3-.7 → bows sit on the front, not the sides)
    # sides + back (u .7 → 1 → .3): lacing column only, no bows, mirrored so the lacing is centred on the back seam
    @(440,140,360,600,  502,147,717,614),
    @(360,140,280,600,  0,147,215,614),
    @(748,6,1020,138,   737,0,1024,143),     # neckline frill
    @(18,128,202,300,   0,655,246,1024),     # puff sleeve: the drawn puff balloon, front ...
    @(202,128,18,300,   246,655,492,1024),   # ... and mirrored for the back
    @(522,662,1020,814, 512,655,1024,819),   # sleeve frill
    @(522,850,1020,1018,512,840,1024,1024)   # wrist cuff
  )
  skirt = @(
    @(4,4,408,78,       0,0,410,82),         # bloomers
    @(430,4,1020,80,    430,0,1024,82),      # waist piping
    @(0,98,1024,210,    0,102,1024,205),     # hem piping
    # overskirt: bow-to-bow repeat (bow | strawberries | bow) twice around -> decorations land on the lifted swag joins
    @(180,226,840,512,  0,225,512,512),
    @(180,226,840,512,  512,225,1024,512),
    # the tiles meet at the back seam (u 0/1) in a red bow -> cover it with plain swag from beside the bow
    @(300,290,240,405,  -2,290,48,405),
    @(240,290,300,405,  976,290,1026,405),
    @(0,524,1024,866,   0,532,1024,860),     # pink pleated skirt
    @(0,872,1024,1024,  0,881,1024,1024)     # petticoat
  )
  legwear = @(
    @(14,4,480,1020,    0,0,307,1024),       # sock (source was drawn ~1.6x too wide)
    @(512,6,1020,285,   328,0,1024,492),     # top ruffle cuff with cherries (first tier of the drawing)
    @(522,745,1020,975, 328,532,1024,1024)   # lower ruffles (lace band + ruffle)
  )
  shoes = @(
    @(4,328,368,494,    0,328,369,492),      # sole top/bottom
    @(120,925,480,1000, 0,102,1024,307),     # sole side <- the pink platform band under the drawn shoe
    @(95,720,520,900,   256,512,768,1024),   # upper front: one drawn shoe, bow on the toe (u .5)
    @(230,780,105,900,  0,512,256,1024),     # upper back halves: the shoe's plain white side, mirrored to meet the front
    @(105,780,230,900,  768,512,1024,1024)
  )
}

New-Item -ItemType Directory -Force $Out | Out-Null
$attr = New-Object System.Drawing.Imaging.ImageAttributes
$attr.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)    # no dark fringes at the crop edges
foreach ($name in $maps.Keys) {
  $srcPath = Join-Path $Src "$name.png"
  if (-not (Test-Path $srcPath)) { "skip $name (no $srcPath)"; continue }
  $img = [System.Drawing.Image]::FromFile((Resolve-Path $srcPath).Path)
  $k = $img.Width / 1024.0                                             # source may be any square size
  $dst = New-Object System.Drawing.Bitmap 1024, 1024
  $g = [System.Drawing.Graphics]::FromImage($dst)
  $g.Clear([System.Drawing.Color]::FromArgb(255, 255, 250, 247))
  $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  foreach ($m in $maps[$name]) {
    $sx0, $sy0, $sx1, $sy1, $dx0, $dy0, $dx1, $dy1 = $m
    $mirror = $sx0 -gt $sx1
    $sl = [Math]::Min($sx0, $sx1) * $k; $sw = [Math]::Abs($sx1 - $sx0) * $k
    $srcRect = New-Object System.Drawing.RectangleF ([float]$sl), ([float]($sy0 * $k)), ([float]$sw), ([float](($sy1 - $sy0) * $k))
    $x0 = $dx0 - $Bleed; $x1 = $dx1 + $Bleed; $y0 = $dy0 - $Bleed; $y1 = $dy1 + $Bleed      # bleed past the island edge
    if ($mirror) { $ul = New-Object System.Drawing.PointF $x1, $y0; $ur = New-Object System.Drawing.PointF $x0, $y0; $ll = New-Object System.Drawing.PointF $x1, $y1 }
    else         { $ul = New-Object System.Drawing.PointF $x0, $y0; $ur = New-Object System.Drawing.PointF $x1, $y0; $ll = New-Object System.Drawing.PointF $x0, $y1 }
    $g.DrawImage($img, [System.Drawing.PointF[]]@($ul, $ur, $ll), $srcRect, [System.Drawing.GraphicsUnit]::Pixel, $attr)
  }
  $g.Dispose(); $img.Dispose()
  $dst.Save((Join-Path $Out "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png); $dst.Dispose()
  "fitted $name -> tex\$name.png"
}
