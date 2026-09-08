param(
    [Parameter(Mandatory=$true)] [string]$InputExe,
    [string]$OutDir = "reference/old-trainer/out"
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$sha = (Get-FileHash $InputExe -Algorithm SHA256).Hash.ToLowerInvariant()
$meta = @()
$meta += "# Legacy trainer automated analysis"
$meta += ""
$meta += "Input: $InputExe"
$meta += "SHA-256: $sha"
$meta | Set-Content -Encoding UTF8 (Join-Path $OutDir '00-hash.md')

# Prefer a repo-local UPX binary placed by CI; fall back to PATH.
$upx = $null
$localCandidates = @(
    (Join-Path $PSScriptRoot 'tools/upx.exe'),
    (Join-Path $PSScriptRoot 'tools/upx')
)
foreach ($candidate in $localCandidates) {
    if (Test-Path $candidate) { $upx = $candidate; break }
}
if (-not $upx) {
    $cmd = Get-Command upx -ErrorAction SilentlyContinue
    if ($cmd) { $upx = $cmd.Source }
}

$unpacked = Join-Path $OutDir 'BattleRealmsTrainer.unpacked.exe'
Copy-Item $InputExe $unpacked -Force

if ($upx) {
    & $upx -t $InputExe 2>&1 | Set-Content -Encoding UTF8 (Join-Path $OutDir '01-upx-test.txt')
    & $upx -d -f $unpacked 2>&1 | Set-Content -Encoding UTF8 (Join-Path $OutDir '02-upx-unpack.txt')
} else {
    'UPX not available; unpack step skipped.' | Set-Content -Encoding UTF8 (Join-Path $OutDir '02-upx-unpack.txt')
}

$target = if (Test-Path $unpacked) { $unpacked } else { $InputExe }

# GNU binutils are present on GitHub's Ubuntu runner; on Windows CI this step is optional.
$objdump = Get-Command objdump -ErrorAction SilentlyContinue
if ($objdump) {
    & $objdump.Source -x $target 2>&1 | Set-Content -Encoding UTF8 (Join-Path $OutDir '10-pe-headers-imports.txt')
    & $objdump.Source -d -Mintel $target 2>&1 | Set-Content -Encoding UTF8 (Join-Path $OutDir '20-disassembly.txt')
}

$strings = Get-Command strings -ErrorAction SilentlyContinue
if ($strings) {
    & $strings.Source -a -n 4 $target 2>&1 | Set-Content -Encoding UTF8 (Join-Path $OutDir '11-strings-ascii.txt')
    & $strings.Source -a -el -n 4 $target 2>&1 | Set-Content -Encoding UTF8 (Join-Path $OutDir '12-strings-utf16.txt')
}

# Pull out API and trainer-ish indicators for fast review.
$needle = 'OpenProcess|ReadProcessMemory|WriteProcessMemory|VirtualAllocEx|VirtualProtectEx|CreateRemoteThread|FindWindow|GetAsyncKeyState|RegisterHotKey|SetTimer|Battle|Realm|Rice|Water|Yin|Yang|Health|Stamina|Invinc|Train|Build|Horse|Wolf|Population|Unit|Player'
Get-ChildItem $OutDir -File | Where-Object { $_.Extension -eq '.txt' } | ForEach-Object {
    Select-String -Path $_.FullName -Pattern $needle -CaseSensitive:$false -ErrorAction SilentlyContinue |
        ForEach-Object { "[$($_.Path | Split-Path -Leaf)] $($_.LineNumber): $($_.Line)" }
} | Set-Content -Encoding UTF8 (Join-Path $OutDir '30-interesting-hits.txt')

Write-Host "Analysis complete -> $OutDir"
