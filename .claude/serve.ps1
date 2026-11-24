# Tiny local static server so the game can be opened inside Claude's browser pane (http://localhost:8123).
param([string]$Root, [int]$Port = 8123)
$l = New-Object System.Net.HttpListener
$l.Prefixes.Add("http://localhost:$Port/")
$l.Start()
Write-Host "serving $Root on $Port"
$types = @{ '.html' = 'text/html; charset=utf-8'; '.js' = 'text/javascript'; '.css' = 'text/css'; '.png' = 'image/png'; '.jpg' = 'image/jpeg' }
while ($l.IsListening) {
  $c = $l.GetContext()
  $p = $c.Request.Url.AbsolutePath.TrimStart('/')
  if ($p -eq '') { $p = 'index.html' }
  $f = Join-Path $Root $p
  if (Test-Path $f -PathType Leaf) {
    $b = [IO.File]::ReadAllBytes($f)
    $ext = [IO.Path]::GetExtension($f)
    $c.Response.ContentType = $(if ($types[$ext]) { $types[$ext] } else { 'application/octet-stream' })
    $c.Response.Headers.Add('Cache-Control', 'no-store')   # always the latest game/index.html on reload
    $c.Response.OutputStream.Write($b, 0, $b.Length)
  } else { $c.Response.StatusCode = 404 }
  $c.Response.Close()
}
