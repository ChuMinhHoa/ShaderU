# Static server cho trang preview/capture (http://localhost:8765/index.html).
# POST /save?name=x.png lưu ảnh contact-sheet do capture.html gửi về vào $SaveDir.
param(
  [string]$Root = "$PSScriptRoot\out",
  [string]$SaveDir = "$PSScriptRoot\out\capture",
  [int]$Port = 8765
)
New-Item -ItemType Directory -Force $SaveDir | Out-Null
$l = New-Object System.Net.HttpListener
$l.Prefixes.Add("http://localhost:$Port/")
$l.Start()
Write-Output "serving $Root on $Port"
$types = @{ '.html' = 'text/html'; '.json' = 'application/json'; '.txt' = 'text/plain'; '.png' = 'image/png'; '.js' = 'text/javascript'; '.css' = 'text/css' }
while ($l.IsListening) {
  $ctx = $l.GetContext()
  try {
    $p = [Uri]::UnescapeDataString($ctx.Request.Url.LocalPath.TrimStart('/'))
    if ($ctx.Request.HttpMethod -eq 'POST' -and $p -eq 'save') {
      $name = [IO.Path]::GetFileName($ctx.Request.QueryString['name'])
      $reader = New-Object IO.StreamReader($ctx.Request.InputStream)
      $body = $reader.ReadToEnd()
      $b64 = $body.Substring($body.IndexOf(',') + 1)
      [IO.File]::WriteAllBytes((Join-Path $SaveDir $name), [Convert]::FromBase64String($b64))
      $ok = [Text.Encoding]::UTF8.GetBytes('ok')
      $ctx.Response.OutputStream.Write($ok, 0, $ok.Length)
      continue
    }
    if ($p -eq '') { $p = 'index.html' }
    $f = Join-Path $Root $p
    if (Test-Path $f -PathType Leaf) {
      $bytes = [IO.File]::ReadAllBytes($f)
      $ext = [IO.Path]::GetExtension($f).ToLower()
      if ($types.ContainsKey($ext)) { $ctx.Response.ContentType = $types[$ext] }
      $ctx.Response.Headers.Add('Cache-Control', 'no-store')
      $ctx.Response.ContentLength64 = $bytes.Length
      $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
    } else { $ctx.Response.StatusCode = 404 }
  } catch { } finally { $ctx.Response.Close() }
}
