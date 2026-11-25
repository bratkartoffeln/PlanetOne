[CmdletBinding()]
param([switch]$RunPlayer,[string]$ReferenceSave)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
$distribution = Join-Path $root 'Builds\Distribution'
$stem = "PlanetOne-$version-Windows-x64"
$fixture = Join-Path $root ('TestResults\Installer\' + [Guid]::NewGuid().ToString('N'))
$installed = Join-Path $fixture 'Installed'
$portable = Join-Path $fixture 'Portable'
New-Item -ItemType Directory -Force -Path $fixture | Out-Null
Expand-Archive -LiteralPath (Join-Path $distribution "$stem.zip") -DestinationPath $portable
# No uninstall registration or shortcuts: this fixture must not replace an existing user installation.
$process = Start-Process -FilePath (Join-Path $distribution "$stem-Setup.exe") -ArgumentList @(
    '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/NOICONS', '/TASKS=',
    '/TESTINSTALL=1', ('/DIR="' + $installed + '"'), ('/LOG="' + (Join-Path $fixture 'install.log') + '"')
) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Installer fehlgeschlagen: $($process.ExitCode)" }
foreach ($file in Get-ChildItem -LiteralPath $portable -File -Recurse) {
    $relative = $file.FullName.Substring($portable.Length + 1)
    $target = Join-Path $installed $relative
    if (-not (Test-Path -LiteralPath $target)) { throw "Installierte Datei fehlt: $relative" }
    if ((Get-FileHash $file.FullName).Hash -ne (Get-FileHash $target).Hash) { throw "Installierte Datei weicht ab: $relative" }
}
$uninstaller = Join-Path $installed 'unins000.exe'
if($RunPlayer) {
    & (Join-Path $PSScriptRoot 'Test-Presentation.ps1') -PlayerPath (Join-Path $installed 'PlanetOne.exe') -ReferenceSave $ReferenceSave
}
if (-not (Test-Path $uninstaller)) { throw 'Deinstallationsprogramm fehlt.' }
$process = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Deinstallation fehlgeschlagen: $($process.ExitCode)" }
if (Test-Path (Join-Path $installed 'PlanetOne.exe')) { throw 'Deinstallation hat den Player nicht entfernt.' }
Write-Output 'PASS Installer und ZIP enthalten dieselben Dateien; Installation und Deinstallation erfolgreich.'
Write-Output ('Installer-Pruefverzeichnis: '+$fixture)
