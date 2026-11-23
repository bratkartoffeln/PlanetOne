[CmdletBinding()]
param(
    [ValidateSet('Prepare', 'Build', 'Open')]
    [string]$Action = 'Open',
    [string]$UnityPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$versionLine = Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt') |
    Where-Object { $_ -match '^m_EditorVersion:' } | Select-Object -First 1
$editorVersion = ($versionLine -split ':', 2)[1].Trim()
if (-not $UnityPath) { $UnityPath = $env:UNITY_EDITOR }
if (-not $UnityPath) {
    $candidates = @(
        (Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"),
        "E:\Program Files\Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
    )
    $UnityPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity $editorVersion fehlt. Im Unity Hub installieren oder -UnityPath beziehungsweise UNITY_EDITOR setzen."
}

$projectArgument = '"' + $projectRoot + '"'
if ($Action -eq 'Open') {
    Start-Process -FilePath $UnityPath -ArgumentList @('-projectPath', $projectArgument) -WorkingDirectory $projectRoot | Out-Null
    Write-Output "Unity-Projekt geoeffnet: $projectRoot"
    return
}

$logDirectory = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
$logPath = Join-Path $logDirectory ("Unity-$Action.log")
$method = if ($Action -eq 'Build') { 'PlanetOne.Editor.BuildTools.BuildWindows' } else { 'PlanetOne.Editor.BuildTools.PrepareProject' }
$arguments = @(
    '-batchmode', '-nographics', '-quit', '-projectPath', $projectArgument,
    '-buildTarget', 'StandaloneWindows64', '-executeMethod', $method,
    '-logFile', ('"' + $logPath + '"')
)
Write-Output "Unity $editorVersion $Action; Log: $logPath"
$unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WorkingDirectory $projectRoot -PassThru -Wait -WindowStyle Hidden
if ($unityProcess.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Tail 60 }
    throw "Unity $Action fehlgeschlagen: Exitcode $($unityProcess.ExitCode). Vollstaendiges Log: $logPath"
}
Write-Output "Unity $Action erfolgreich."
