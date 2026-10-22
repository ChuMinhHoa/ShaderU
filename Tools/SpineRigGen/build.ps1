# Build một generator (mặc định TigerGen.cs) và sinh Spine (json + atlas + png) từ ảnh nguồn.
# Ví dụ:  powershell -ExecutionPolicy Bypass -File Tools\SpineRigGen\build.ps1
#         powershell -ExecutionPolicy Bypass -File Tools\SpineRigGen\build.ps1 -Gen FootmanGen
#         powershell -ExecutionPolicy Bypass -File Tools\SpineRigGen\build.ps1 -Source D:\art\hero.png -SharedR 3.5
param(
    [string]$Gen = "TigerGen",
    [string]$Source = "",
    [string]$Out = "",
    [double]$SharedR = 3.5
)

# ảnh nguồn và thư mục ra mặc định theo generator: TigerGen -> source\tiger_source.png, out\
if ($Source -eq "") { $Source = "$PSScriptRoot\source\" + ($Gen -replace 'Gen$', '').ToLower() + "_source.png" }
if ($Out -eq "") { $Out = if ($Gen -eq "TigerGen") { "$PSScriptRoot\out" } else { "$PSScriptRoot\out\" + ($Gen -replace 'Gen$', '') } }

$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$exe = "$PSScriptRoot\out\$Gen.exe"
New-Item -ItemType Directory -Force "$PSScriptRoot\out" | Out-Null

& $csc -nologo -optimize -nowarn:649 -r:System.Drawing.dll "-out:$exe" "$PSScriptRoot\$Gen.cs"
if ($LASTEXITCODE -ne 0) { Write-Error "Compile failed"; exit 1 }

& $exe $Source $Out $SharedR.ToString([Globalization.CultureInfo]::InvariantCulture)
if ($LASTEXITCODE -ne 0) { Write-Error "Generate failed"; exit 1 }

# trang preview đọc file skeleton cùng thư mục (?skel=<Tên> để chọn, mặc định Tiger)
Copy-Item "$PSScriptRoot\preview\*.html" $Out -Force
Write-Output "Done -> $Out"
