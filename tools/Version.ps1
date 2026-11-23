<#
.SYNOPSIS
Reads, checks or advances PlanetOne's product version without touching engine/package/save versions.
.DESCRIPTION
Get and Next print only X.Y.Z. Next is read-only and defaults to Patch.
Set requires a higher explicit version (or the unchanged version for an idempotent check).
Existing Android/iOS/tvOS/visionOS build counters advance once per changed product version,
independently of the semantic version's numeric components. Rebuilds never increment them.
ProjectRoot supports isolated fixtures; it defaults to the repository containing this script.
#>
[CmdletBinding()]
param(
    [ValidateSet('Get', 'Next', 'Set', 'Check')]
    [string]$Action = 'Get',
    [ValidateSet('Patch', 'Minor', 'Major')]
    [string]$Increment = 'Patch',
    [string]$Version,
    [string]$ProjectRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Numerics
$utf8 = New-Object System.Text.UTF8Encoding($false, $true)
$semanticPattern = '(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)'

function Read-SemanticVersion([string]$Text) {
    if ($Text -notmatch ('\A' + $semanticPattern + '\z')) {
        throw "Invalid product version '$Text'. Expected canonical X.Y.Z without leading zeroes or suffixes."
    }
    $parts = $Text.Split('.') | ForEach-Object {
        [System.Numerics.BigInteger]::Parse($_, [System.Globalization.CultureInfo]::InvariantCulture)
    }
    return [pscustomobject]@{ Text = $Text; Parts = $parts }
}

function Compare-SemanticVersion($Left, $Right) {
    for ($index = 0; $index -lt 3; $index++) {
        $comparison = $Left.Parts[$index].CompareTo($Right.Parts[$index])
        if ($comparison -ne 0) { return $comparison }
    }
    return 0
}

function Read-Scalar([string]$Text) {
    $value = $Text.Trim()
    if ($value -match '\A(?<quote>[''"])(?<value>[^''"]*)\k<quote>\z') {
        return $Matches['value']
    }
    return $value
}

function Read-SettingsReferences([string]$Text, [string]$ExpectedVersion) {
    # Exact YAML paths only. Similar words in SDK, engine or save versions are not candidates.
    $productKeys = @('bundleVersion', 'visionOSBundleVersion', 'tvOSBundleVersion')
    foreach ($key in $productKeys) {
        $pattern = '(?m)^  ' + [regex]::Escape($key) + ':[ \t]*(?<value>[^\r\n]*)\r?$'
        $found = [regex]::Matches($Text, $pattern)
        if ($found.Count -gt 1 -or ($key -eq 'bundleVersion' -and $found.Count -ne 1)) {
            throw "Expected exactly one PlayerSettings.$key reference (optional platform fields may be absent)."
        }
        foreach ($match in $found) {
            $value = Read-Scalar $match.Groups['value'].Value
            [void](Read-SemanticVersion $value)
            if ($value -cne $ExpectedVersion) {
                throw "Version drift: PlayerSettings.$key is '$value', but VERSION is '$ExpectedVersion'. Resolve the difference before changing versions."
            }
            [pscustomobject]@{ Name = $key; Kind = 'Product'; Value = $value;
                Index = $match.Groups['value'].Index; Length = $match.Groups['value'].Length }
        }
    }

    $android = [regex]::Matches($Text, '(?m)^  AndroidBundleVersionCode:[ \t]*(?<value>[^\r\n]*)\r?$')
    if ($android.Count -gt 1) { throw 'Duplicate AndroidBundleVersionCode.' }
    foreach ($match in $android) {
        $value = Read-Scalar $match.Groups['value'].Value
        $counter = 0
        if ($value -notmatch '\A(0|[1-9][0-9]*)\z' -or
            -not [int]::TryParse($value, [ref]$counter) -or $counter -lt 1) {
            throw 'AndroidBundleVersionCode must be a positive Int32 counter.'
        }
        [pscustomobject]@{ Name = 'AndroidBundleVersionCode'; Kind = 'Counter'; Value = $counter;
            Index = $match.Groups['value'].Index; Length = $match.Groups['value'].Length }
    }

    $blocks = [regex]::Matches($Text, '(?m)^  buildNumber:[ \t]*(?<inline>[^\r\n]*)\r?$')
    if ($blocks.Count -gt 1) { throw 'Duplicate PlayerSettings.buildNumber mapping.' }
    if ($blocks.Count -eq 1) {
        $block = $blocks[0]
        $inline = $block.Groups['inline'].Value.Trim()
        if ($inline -ne '' -and $inline -ne '{}') { throw 'Unsupported inline buildNumber mapping.' }
        if ($inline -eq '{}') { return }
        $tailStart = $block.Index + $block.Length
        if ($tailStart -lt $Text.Length -and $Text[$tailStart] -eq "`n") { $tailStart++ }
        $tail = $Text.Substring($tailStart)
        $body = [regex]::Match($tail, '\A(?:    [^\r\n]*(?:\r?\n|\z))*').Value
        foreach ($key in @('iPhone', 'tvOS', 'VisionOS')) {
            $found = [regex]::Matches($body, '(?m)^    ' + [regex]::Escape($key) + ':[ \t]*(?<value>[^\r\n]*)\r?$')
            if ($found.Count -gt 1) { throw "Duplicate buildNumber.$key." }
            foreach ($match in $found) {
                $value = Read-Scalar $match.Groups['value'].Value
                $counter = 0
                if ($value -notmatch '\A(0|[1-9][0-9]*)\z' -or -not [int]::TryParse($value, [ref]$counter)) {
                    throw "buildNumber.$key must be a nonnegative Int32 counter."
                }
                [pscustomobject]@{ Name = 'buildNumber.' + $key; Kind = 'Counter'; Value = $counter;
                    Index = $tailStart + $match.Groups['value'].Index; Length = $match.Groups['value'].Length }
            }
        }
    }
}

if (-not $ProjectRoot) { $ProjectRoot = Join-Path $PSScriptRoot '..' }
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$versionPath = Join-Path $ProjectRoot 'VERSION'
$settingsPath = Join-Path $ProjectRoot 'ProjectSettings\ProjectSettings.asset'
if (-not [System.IO.File]::Exists($versionPath)) { throw "VERSION is missing: $versionPath" }
$originalVersionText = [System.IO.File]::ReadAllText($versionPath, $utf8)
$fileMatch = [regex]::Match($originalVersionText, '\A(?<version>' + $semanticPattern + ')(?:\r?\n)?\z')
if (-not $fileMatch.Success) { throw 'VERSION must contain one canonical X.Y.Z line.' }
$current = Read-SemanticVersion $fileMatch.Groups['version'].Value
if ($Action -ne 'Set' -and $PSBoundParameters.ContainsKey('Version')) { throw '-Version is only valid with -Action Set.' }
if ($Action -ne 'Next' -and $PSBoundParameters.ContainsKey('Increment')) { throw '-Increment is only valid with -Action Next.' }
if ($Action -eq 'Get') { Write-Output $current.Text; return }
if (-not [System.IO.File]::Exists($settingsPath)) { throw "PlayerSettings is missing: $settingsPath" }
$originalSettings = [System.IO.File]::ReadAllText($settingsPath, $utf8)
$references = @(Read-SettingsReferences $originalSettings $current.Text)

if ($Action -eq 'Check') {
    Write-Output ("Version {0} is synchronized; {1} product references and {2} platform counters checked." -f
        $current.Text, @($references | Where-Object Kind -eq 'Product').Count, @($references | Where-Object Kind -eq 'Counter').Count)
    return
}
if ($Action -eq 'Next') {
    $parts = @($current.Parts)
    switch ($Increment) {
        'Major' { $parts[0] += [System.Numerics.BigInteger]::One; $parts[1] = 0; $parts[2] = 0 }
        'Minor' { $parts[1] += [System.Numerics.BigInteger]::One; $parts[2] = 0 }
        'Patch' { $parts[2] += [System.Numerics.BigInteger]::One }
    }
    Write-Output ($parts -join '.')
    return
}

if (-not $PSBoundParameters.ContainsKey('Version')) { throw '-Action Set requires -Version X.Y.Z.' }
$requested = Read-SemanticVersion $Version
$comparison = Compare-SemanticVersion $requested $current
if ($comparison -lt 0) { throw "Refusing to downgrade product version from $($current.Text) to $Version." }
if ($comparison -eq 0) { Write-Output $current.Text; return }

# Validate every reference and counter before staging either file.
$updatedSettings = $originalSettings
foreach ($reference in ($references | Sort-Object Index -Descending)) {
    if ($reference.Kind -eq 'Product') { $replacement = $Version }
    else {
        if ($reference.Value -eq [int]::MaxValue) { throw "Counter exhausted: $($reference.Name). No files changed." }
        $replacement = ([long]$reference.Value + 1).ToString([System.Globalization.CultureInfo]::InvariantCulture)
    }
    $updatedSettings = $updatedSettings.Remove($reference.Index, $reference.Length).Insert($reference.Index, $replacement)
}
[void]@(Read-SettingsReferences $updatedSettings $Version)
$newVersionText = $Version + "`n"
$transaction = [Guid]::NewGuid().ToString('N')
$settingsStage = $settingsPath + '.version-' + $transaction + '.tmp'
$versionStage = $versionPath + '.version-' + $transaction + '.tmp'
$settingsBackup = $settingsPath + '.version-' + $transaction + '.bak'
$versionBackup = $versionPath + '.version-' + $transaction + '.bak'
$lockPath = Join-Path $ProjectRoot '.planetone-version.lock'
$lock = $null
$settingsWritten = $false
$rollbackFailed = $false
try {
    $lock = [System.IO.File]::Open($lockPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    if ([System.IO.File]::ReadAllText($versionPath, $utf8) -cne $originalVersionText -or
        [System.IO.File]::ReadAllText($settingsPath, $utf8) -cne $originalSettings) {
        throw 'Version references changed during preparation. Retry after reviewing the current files.'
    }
    [System.IO.File]::WriteAllText($settingsStage, $updatedSettings, $utf8)
    [System.IO.File]::WriteAllText($versionStage, $newVersionText, $utf8)
    [System.IO.File]::Replace($settingsStage, $settingsPath, $settingsBackup)
    $settingsWritten = $true
    [System.IO.File]::Replace($versionStage, $versionPath, $versionBackup)
}
catch {
    if ($settingsWritten -and [System.IO.File]::Exists($settingsBackup)) {
        try { [System.IO.File]::Copy($settingsBackup, $settingsPath, $true) }
        catch { $rollbackFailed = $true; Write-Warning "Rollback failed. Preserve backup $settingsBackup and restore references before retrying." }
    }
    throw
}
finally {
    if ($lock) { $lock.Dispose(); [System.IO.File]::Delete($lockPath) }
    foreach ($temporary in @($settingsStage, $versionStage, $versionBackup)) {
        if ([System.IO.File]::Exists($temporary)) { [System.IO.File]::Delete($temporary) }
    }
    if (-not $rollbackFailed -and [System.IO.File]::Exists($settingsBackup)) { [System.IO.File]::Delete($settingsBackup) }
}
Write-Output $Version
