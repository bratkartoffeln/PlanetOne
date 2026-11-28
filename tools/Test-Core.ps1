[CmdletBinding()]
param([string]$CompilerPath,[ValidateSet('All','Reference','Combat')][string]$Suite='All')

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$outputDirectory = Join-Path $projectRoot 'TestResults\Core'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

if (-not $CompilerPath) {
    $candidates = @(
        'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe'
    )
    $CompilerPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $CompilerPath -or -not (Test-Path -LiteralPath $CompilerPath)) {
    throw 'Roslyn csc.exe fehlt. Visual Studio Build Tools verwenden oder -CompilerPath angeben.'
}

$coreFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\PlanetOne\Core') -Filter *.cs -File -Recurse)
$platformFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\PlanetOne\Platform') -Filter *.cs -File -Recurse)
$testFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Tests\Core') -Filter *.cs -File -Recurse)
if ($coreFiles.Count -eq 0 -or $testFiles.Count -eq 0) { throw 'Simulationsquellen oder Kernpruefungen fehlen.' }
$testExecutable = Join-Path $outputDirectory 'PlanetOne.Core.Tests.exe'
$compilerArguments = @(
    '/nologo', '/langversion:7.3', '/target:exe', '/platform:anycpu', '/optimize+', '/warn:4', '/warnaserror+',
    '/reference:System.Runtime.Serialization.dll', '/reference:System.Xml.dll',
    ('/out:' + $testExecutable)
) + @($coreFiles.FullName) + @($platformFiles.FullName) + @($testFiles.FullName)

Write-Output ('Compiler: ' + $CompilerPath)
& $CompilerPath @compilerArguments
if ($LASTEXITCODE -ne 0) { throw "Core-Kompilierung fehlgeschlagen: Exitcode $LASTEXITCODE" }

$testArguments=@()
if($Suite -eq 'Reference') { $testArguments=@('--reference') }
if($Suite -eq 'Combat') { $testArguments=@('--combat') }
$resultName=if($Suite -eq 'All'){'results.txt'}else{'results-'+$Suite+'.txt'}
& $testExecutable @testArguments 2>&1 | Tee-Object -FilePath (Join-Path $outputDirectory $resultName)
$testExitCode = $LASTEXITCODE
if ($testExitCode -ne 0) { throw "Core-Pruefungen fehlgeschlagen: Exitcode $testExitCode" }
Write-Output ('Pruefprotokoll: ' + (Join-Path $outputDirectory $resultName))
