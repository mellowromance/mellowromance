# Builds the strawberry-cream-cake outfit (04_Clothes guide) as 5 separate low-poly parts with UVs:
#   obj\outfit_all.obj            all parts in one file (groups = part_mesh_bone), character rest pose
#   obj\outfit_<part>.obj         one file per part (headdress / top / skirt / legwear / shoes) + obj\outfit.mtl
#   uv\uv_<part>.png              UV layout over white   (reference)
#   uv\uv_<part>_overlay.png      UV layout, transparent (put on top of your paint layer)
#   tex\<part>_placeholder.png    simple placeholder texture painted on the real UV layout
#   tex\<part>.png                YOUR finished texture — if this file exists it is used instead of the placeholder
# and embeds meshes + textures into game/index.html.
#   powershell -ExecutionPolicy Bypass -File 04_Clothes\Outfit\build_outfit.ps1
param(
  [string]$GameHtml = "$PSScriptRoot\..\..\game\index.html",
  [int]$TexSize = 1024
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -Path "$PSScriptRoot\OutfitGen.cs", "$PSScriptRoot\OutfitGuide.cs"

foreach ($d in 'obj', 'uv', 'tex', 'guide') { New-Item -ItemType Directory -Force "$PSScriptRoot\$d" | Out-Null }
$enc = New-Object System.Text.UTF8Encoding $false
$parts = [OutfitGen]::BuildAll()

$mtl = New-Object System.Text.StringBuilder
$tex = New-Object 'System.Collections.Generic.Dictionary[string,string]'
foreach ($p in $parts) {
  $n = $p.Name
  # UV layouts + placeholder
  $b = [OutfitGen]::UvLayout($p, $TexSize, $true);  $b.Save("$PSScriptRoot\uv\uv_$n.png", [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
  $b = [OutfitGen]::UvLayout($p, $TexSize, $false); $b.Save("$PSScriptRoot\uv\uv_${n}_overlay.png", [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
  $b = [OutfitGen]::Placeholder($p, $TexSize);      $b.Save("$PSScriptRoot\tex\${n}_placeholder.png", [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
  # texture used: your tex\<part>.png if present, else the placeholder
  $final = "$PSScriptRoot\tex\$n.png"
  $use = if (Test-Path $final) { $final } else { "$PSScriptRoot\tex\${n}_placeholder.png" }
  # embedded as JPEG (q92): the outfit textures are opaque, and PNG made the game file ~10 MB
  $src = [System.Drawing.Image]::FromFile($use)
  $flat = New-Object System.Drawing.Bitmap $src.Width, $src.Height
  $fg = [System.Drawing.Graphics]::FromImage($flat); $fg.Clear([System.Drawing.Color]::White); $fg.DrawImage($src, 0, 0, $src.Width, $src.Height); $fg.Dispose(); $src.Dispose()
  $jpgCodec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
  $ep = New-Object System.Drawing.Imaging.EncoderParameters 1
  $ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality), ([long]92)
  $ms = New-Object System.IO.MemoryStream; $flat.Save($ms, $jpgCodec, $ep); $flat.Dispose()
  $tex[$n] = 'data:image/jpeg;base64,' + [Convert]::ToBase64String($ms.ToArray())
  [void]$mtl.Append("newmtl Outfit_$n`nKd 1 1 1`nKs 0 0 0`nmap_Kd ../tex/$([IO.Path]::GetFileName($use))`n`n")
  [IO.File]::WriteAllText("$PSScriptRoot\obj\outfit_$n.obj", [OutfitGen]::ToObj(@($p), 'outfit.mtl', "SD outfit - $($p.Label)"), $enc)
  "{0,-10} {1,5} tris   texture: {2}" -f $n, [OutfitGen]::Tris($p), [IO.Path]::GetFileName($use)
}
[IO.File]::WriteAllText("$PSScriptRoot\obj\outfit.mtl", $mtl.ToString(), $enc)
[IO.File]::WriteAllText("$PSScriptRoot\obj\outfit_all.obj", [OutfitGen]::ToObj($parts, 'outfit.mtl', 'SD outfit - strawberry cream cake lolita (all parts)'), $enc)
$total = 0; foreach ($p in $parts) { $total += [OutfitGen]::Tris($p) }
"total      $total tris"

# mapping-image guide sheets (what to paint where, on the real UV layout)
$titles = [OutfitGuide]::Titles
$b = [OutfitGuide]::Overview($parts, $titles); $b.Save("$PSScriptRoot\guide\guide_00_overview.png", [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
for ($i = 0; $i -lt $parts.Count; $i++) {
  $b = [OutfitGuide]::Sheet($parts[$i], $titles[$i], $TexSize)
  $b.Save("$PSScriptRoot\guide\guide_$($i + 1)_$($parts[$i].Name).png", [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
}
"guide sheets written to $PSScriptRoot\guide"

# embed into the game
$block = "<!--OUTFIT_START-->`n<script>window.OUTFIT = $([OutfitGen]::ToJson($parts, $tex));</script>`n<!--OUTFIT_END-->"
$htmlPath = (Resolve-Path $GameHtml).Path
$text = [IO.File]::ReadAllText($htmlPath)
$re = '(?s)<!--OUTFIT_START-->.*?<!--OUTFIT_END-->'
if ($text -match $re) { $text = [regex]::Replace($text, $re, [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $block }) }
else { $text = $text.Replace('<!--HEAD_MESH_START-->', "$block`n<!--HEAD_MESH_START-->") }
[IO.File]::WriteAllText($htmlPath, $text, $enc)
"embedded into $htmlPath"
