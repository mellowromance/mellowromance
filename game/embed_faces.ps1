# Embeds the painted face maps from 02_Model/Face into game/index.html (so the game still runs by
# double-clicking the HTML file — browsers block loading local image files from file:// pages).
# Re-run this after editing the face images:
#   powershell -ExecutionPolicy Bypass -File game\embed_faces.ps1
param(
  [string]$Open  = "$PSScriptRoot\..\02_Model\Face\Face02-Basic.png",
  [string]$Blink = "$PSScriptRoot\..\02_Model\Face\Face-Blink.png",
  [string]$Html  = "$PSScriptRoot\index.html",
  [int]$Size = 800
)
Add-Type -AssemblyName System.Drawing

function Get-DataUri([string]$path, [int]$size) {
  $src = [System.Drawing.Image]::FromFile((Resolve-Path $path))
  $bmp = New-Object System.Drawing.Bitmap $size, $size
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $g.DrawImage($src, 0, 0, $size, $size)
  $g.Dispose(); $src.Dispose()
  $codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
  $ep = New-Object System.Drawing.Imaging.EncoderParameters 1
  $ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality), 92L
  $ms = New-Object System.IO.MemoryStream
  $bmp.Save($ms, $codec, $ep); $bmp.Dispose()
  'data:image/jpeg;base64,' + [Convert]::ToBase64String($ms.ToArray())
}

$open = Get-DataUri $Open $Size
$blink = Get-DataUri $Blink $Size
$block = "<!--FACE_IMAGES_START-->`n<script>window.FACE_IMAGES = { open: '$open', blink: '$blink' };</script>`n<!--FACE_IMAGES_END-->"

$htmlPath = (Resolve-Path $Html).Path
$text = [IO.File]::ReadAllText($htmlPath)
$re = '(?s)<!--FACE_IMAGES_START-->.*?<!--FACE_IMAGES_END-->'
if ($text -match $re) { $text = [regex]::Replace($text, $re, [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $block }) }
else { $text = $text.Replace('</body>', "$block`n</body>") }
[IO.File]::WriteAllText($htmlPath, $text, (New-Object System.Text.UTF8Encoding $false))
"Embedded face maps: open {0:N0} KB, blink {1:N0} KB" -f ($open.Length / 1024), ($blink.Length / 1024)
