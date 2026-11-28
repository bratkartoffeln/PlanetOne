[CmdletBinding()]
param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe',
    [string]$CompilerPath = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $UnityPath)) { throw 'Unity-Referenzassemblies fehlen.' }
if (-not (Test-Path -LiteralPath $CompilerPath)) { throw 'Roslyn-Compiler fehlt; -CompilerPath angeben.' }
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$editorData = Join-Path (Split-Path $UnityPath -Parent) 'Data'
$outputDirectory = Join-Path $projectRoot 'TestResults\UnitySourceCheck'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$frameworkReferences = @(Get-ChildItem -LiteralPath (Join-Path $editorData 'NetStandard\ref\2.1.0') -Filter *.dll -File)
$engineReferences = @(Get-ChildItem -LiteralPath (Join-Path $editorData 'Managed\UnityEngine') -Filter 'UnityEngine*.dll' -File)
if ($frameworkReferences.Count -eq 0 -or $engineReferences.Count -eq 0) { throw 'Unity-Referenzprofil fehlt.' }

function Build-SourceAssembly {
    param([string]$Name, [string]$Folder, [string[]]$References, [string[]]$Defines)
    $sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot $Folder) -Filter *.cs -File -Recurse)
    if ($sourceFiles.Count -eq 0) { throw "Keine Quellen fuer $Name" }
    $assemblyPath = Join-Path $outputDirectory ($Name + '.dll')
    $responsePath = Join-Path $outputDirectory ($Name + '.rsp')
    $arguments = @('/nologo', '/nostdlib+', '/target:library', '/langversion:7.3', '/warn:4')
    $arguments += '/out:"' + $assemblyPath + '"'
    if ($Defines.Count -gt 0) { $arguments += '/define:' + ($Defines -join ';') }
    $arguments += @($References | ForEach-Object { '/reference:"' + $_ + '"' })
    $arguments += @($sourceFiles | ForEach-Object { '"' + $_.FullName + '"' })
    $arguments | Set-Content -Encoding UTF8 -LiteralPath $responsePath
    & $CompilerPath '/noconfig' ('@' + $responsePath)
    if ($LASTEXITCODE -ne 0) { throw "$Name konnte nicht gegen Unity-Referenzen kompiliert werden." }
    Write-Output "PASS $Name source compile"
}

$baseReferences = @($frameworkReferences.FullName)
Build-SourceAssembly -Name 'PlanetOne.Core' -Folder 'Assets\PlanetOne\Core' -References $baseReferences -Defines @()
$platformReferences = $baseReferences + @(Join-Path $outputDirectory 'PlanetOne.Core.dll')
Build-SourceAssembly -Name 'PlanetOne.Platform' -Folder 'Assets\PlanetOne\Platform' -References $platformReferences -Defines @()
$runtimeReferences = $platformReferences + @($engineReferences.FullName) + @(Join-Path $outputDirectory 'PlanetOne.Platform.dll')
Build-SourceAssembly -Name 'PlanetOne.Runtime' -Folder 'Assets\PlanetOne\Runtime' -References $runtimeReferences -Defines @('UNITY_STANDALONE_WIN')
$editorReferences = $runtimeReferences + @(Join-Path $outputDirectory 'PlanetOne.Runtime.dll')
$editorReferences += @(Get-ChildItem -LiteralPath (Join-Path $editorData 'Managed\UnityEngine') -Filter 'UnityEditor*.dll' -File | ForEach-Object { $_.FullName })
Build-SourceAssembly -Name 'PlanetOne.Editor' -Folder 'Assets\PlanetOne\Editor' -References $editorReferences -Defines @('UNITY_EDITOR', 'UNITY_STANDALONE_WIN')
Write-Output 'Quellkompilierung bestanden. Unity-Import, Shader, Build und Laufzeit sind separate Pruefungen.'
