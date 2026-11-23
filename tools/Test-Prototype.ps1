[CmdletBinding()]
param([string]$PlayerPath, [switch]$Visible)

$ErrorActionPreference = 'Stop'
if (-not $Visible) {
    throw 'Der Grafiktest benoetigt ein sichtbares Renderfenster. Bitte mit -Visible starten; ein versteckter Player liefert keine verlaesslichen Screenshots.'
}
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $PlayerPath) { $PlayerPath = Join-Path $projectRoot 'Builds\Windows\PlanetOne.exe' }
if (-not (Test-Path -LiteralPath $PlayerPath)) { throw 'Player fehlt. Zuerst tools/Unity.ps1 -Action Build ausfuehren.' }
$outputDirectory = Join-Path $projectRoot ('TestResults\Smoke\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$arguments = @(
    '-planetOneSmoke', '-smokeOutput', ('"' + $outputDirectory + '"'),
    '-screen-width', '1440', '-screen-height', '900', '-screen-fullscreen', '0',
    '-logFile', ('"' + (Join-Path $outputDirectory 'player.log') + '"')
)
$windowStyle = if ($Visible) { 'Normal' } else { 'Hidden' }
$smokeProcess = Start-Process -FilePath $PlayerPath -ArgumentList $arguments -WorkingDirectory $projectRoot -PassThru -WindowStyle $windowStyle
$finished = $false
for ($attempt = 0; $attempt -lt 3; $attempt++) {
    if ($smokeProcess.WaitForExit(30000)) { $finished = $true; break }
    Write-Output 'Grafischer Integrationstest laeuft ...'
}
if (-not $finished) {
    Stop-Process -Id $smokeProcess.Id -ErrorAction SilentlyContinue
    throw "Grafischer Integrationstest nach 90 Sekunden abgebrochen. Log: $outputDirectory"
}
$smokeProcess.Refresh()
$reportPath = Join-Path $outputDirectory 'smoke-result.txt'
if (Test-Path -LiteralPath $reportPath) { Get-Content -LiteralPath $reportPath }
if ($smokeProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $reportPath)) {
    throw "Grafischer Integrationstest fehlgeschlagen. Exitcode: $($smokeProcess.ExitCode); Log: $outputDirectory"
}
foreach ($imageName in @('space.png', 'surface.png', 'surface-details.png', 'surface-restored.png', 'military-defense.png', 'military-combat.png', 'military-restored.png', 'space-fleet.png', 'space-colony.png', 'space-logistics.png', 'space-research.png')) {
    $imagePath = Join-Path $outputDirectory $imageName
    if (-not (Test-Path -LiteralPath $imagePath) -or (Get-Item -LiteralPath $imagePath).Length -le 256) {
        throw "Screenshot fehlt oder ist leer: $imagePath"
    }
}
Write-Output ('Renderbilder und Pruefprotokoll: ' + $outputDirectory)
