[CmdletBinding()]
param([string]$CompilerPath, [string]$Commit)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
& (Join-Path $PSScriptRoot 'Version.ps1') -Action Check
$version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
if (-not $Commit) { $Commit = (git -C $root rev-parse HEAD).Trim() }
if ($Commit -notmatch '^[0-9a-f]{40}$') { throw 'Gueltiger Commit-SHA erforderlich.' }
$source = Join-Path $root 'Builds\Windows'
foreach ($required in @('PlanetOne.exe', 'UnityPlayer.dll', 'PlanetOne_Data', 'MonoBleedingEdge')) {
    if (-not (Test-Path -LiteralPath (Join-Path $source $required))) { throw "Player unvollstaendig: $required" }
}
if (-not $CompilerPath) {
    $CompilerPath = Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'
}
if (-not (Test-Path -LiteralPath $CompilerPath)) { throw 'Inno Setup 6 fehlt; -CompilerPath angeben.' }
$output = Join-Path $root 'Builds\Distribution'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$name = "PlanetOne-$version-Windows-x64"
$zip = Join-Path $output "$name.zip"
$setup = Join-Path $output "$name-Setup.exe"
if ((Test-Path $zip) -or (Test-Path $setup)) { throw 'Zielpaket existiert bereits. Fuer einen neuen Lauf die bisherigen Distributionsdateien gezielt verschieben.' }
# Only ship player runtime files, not Unity's optional development/debug folders.
$staging = Join-Path $output "$name-player"
if (Test-Path $staging) { throw 'Paket-Staging existiert bereits.' }
New-Item -ItemType Directory -Path $staging | Out-Null
Get-ChildItem -LiteralPath $source | Where-Object { $_.Name -notmatch '(DoNotShip|BackUpThisFolder|\.pdb$)' } |
    Copy-Item -Destination $staging -Recurse
$manifest = [ordered]@{ product='PlanetOne'; version=$version; commit=$Commit; platform='Windows-x64'; unity=(Get-Content (Join-Path $root 'ProjectSettings\ProjectVersion.txt') | Select-Object -First 1).Split(':')[1].Trim() }
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $staging 'build-info.json') -Encoding UTF8
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip -CompressionLevel Optimal
& $CompilerPath "/DProductVersion=$version" "/DSourceDirectory=$staging" "/DOutputDirectory=$output" (Join-Path $root 'installer\PlanetOne.iss')
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $setup)) { throw 'Installer-Kompilierung fehlgeschlagen.' }
Copy-Item -LiteralPath (Join-Path $staging 'build-info.json') -Destination $output
@($zip, $setup, (Join-Path $output 'build-info.json')) | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256
    $hash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_)
} | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Encoding ASCII
Write-Output "Pakete erstellt: $zip; $setup"
