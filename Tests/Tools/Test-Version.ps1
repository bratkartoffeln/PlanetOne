[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$helper = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\tools\Version.ps1'))
$utf8 = New-Object System.Text.UTF8Encoding($false, $true)
$temporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$suiteRoot = Join-Path $temporaryRoot ('PlanetOne-Version-' + [Guid]::NewGuid().ToString('N'))
$passed = 0
$fixtures = 0

function Assert-Equal($Actual, $Expected, [string]$Reason) {
    if ($Actual -cne $Expected) { throw "$Reason`nExpected: $Expected`nActual: $Actual" }
}
function Assert-True([bool]$Condition, [string]$Reason) {
    if (-not $Condition) { throw $Reason }
}
function Read-File([string]$Path) { return [System.IO.File]::ReadAllText($Path, $utf8) }
function Write-File([string]$Path, [string]$Text) { [System.IO.File]::WriteAllText($Path, $Text, $utf8) }
function New-Fixture([string]$ProductVersion = '1.0.0', [string]$Newline = "`n") {
    $script:fixtures++
    $root = Join-Path $suiteRoot ('Fixture-' + $script:fixtures)
    [void][System.IO.Directory]::CreateDirectory((Join-Path $root 'ProjectSettings'))
    $settings = @(
        '%YAML 1.1', 'PlayerSettings:',
        ('  visionOSBundleVersion: ' + $ProductVersion),
        ('  tvOSBundleVersion: ' + $ProductVersion),
        ('  bundleVersion: ' + $ProductVersion),
        '  buildNumber:', '    Standalone: 0', '    VisionOS: 14', '    iPhone: 3', '    tvOS: 9',
        '  AndroidBundleVersionCode: 7', '  iPhoneTargetOSVersionString: 15.0',
        '  AndroidMinSdkVersion: 23', '  switchDisplayVersion: 1.0.0', '  ps4AppVersion: 01.00'
    ) -join $Newline
    Write-File (Join-Path $root 'VERSION') ($ProductVersion + "`n")
    Write-File (Join-Path $root 'ProjectSettings\ProjectSettings.asset') ($settings + $Newline)
    return $root
}
function Invoke-Test([string]$Name, [scriptblock]$Body) {
    & $Body
    $script:passed++
    Write-Output ('PASS ' + $Name)
}
function Expect-FailureWithoutWrites([string]$Root, [scriptblock]$Operation, [string]$MessagePattern) {
    $versionPath = Join-Path $Root 'VERSION'
    $settingsPath = Join-Path $Root 'ProjectSettings\ProjectSettings.asset'
    $beforeVersion = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($versionPath))
    $beforeSettings = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($settingsPath))
    $failure = $null
    try { & $Operation | Out-Null } catch { $failure = $_ }
    Assert-True ($null -ne $failure) 'Expected the version operation to fail.'
    Assert-True ($failure.ToString() -match $MessagePattern) ('Unexpected failure: ' + $failure)
    Assert-Equal ([Convert]::ToBase64String([System.IO.File]::ReadAllBytes($versionPath))) $beforeVersion 'VERSION changed after failure.'
    Assert-Equal ([Convert]::ToBase64String([System.IO.File]::ReadAllBytes($settingsPath))) $beforeSettings 'ProjectSettings changed after failure.'
}

try {
    Invoke-Test 'Get and Next are read-only; patch, minor and major increments are correct' {
        $root = New-Fixture '2.4.9'
        $settings = Read-File (Join-Path $root 'ProjectSettings\ProjectSettings.asset')
        Assert-Equal (& $helper -ProjectRoot $root -Action Get) '2.4.9' 'Get result.'
        Assert-Equal (& $helper -ProjectRoot $root -Action Next) '2.4.10' 'Default patch result.'
        Assert-Equal (& $helper -ProjectRoot $root -Action Next -Increment Minor) '2.5.0' 'Minor resets patch.'
        Assert-Equal (& $helper -ProjectRoot $root -Action Next -Increment Major) '3.0.0' 'Major resets minor and patch.'
        Assert-Equal (Read-File (Join-Path $root 'VERSION')) "2.4.9`n" 'Next changed VERSION.'
        Assert-Equal (Read-File (Join-Path $root 'ProjectSettings\ProjectSettings.asset')) $settings 'Next changed settings.'
    }
    Invoke-Test 'Set synchronizes explicit product references and increments independent platform counters once' {
        $root = New-Fixture
        Assert-Equal (& $helper -ProjectRoot $root -Action Set -Version '1.0.1') '1.0.1' 'Set result.'
        $settings = Read-File (Join-Path $root 'ProjectSettings\ProjectSettings.asset')
        foreach ($line in @('  bundleVersion: 1.0.1', '  visionOSBundleVersion: 1.0.1', '  tvOSBundleVersion: 1.0.1',
            '  AndroidBundleVersionCode: 8', '    VisionOS: 15', '    iPhone: 4', '    tvOS: 10', '    Standalone: 0',
            '  iPhoneTargetOSVersionString: 15.0', '  AndroidMinSdkVersion: 23', '  switchDisplayVersion: 1.0.0', '  ps4AppVersion: 01.00')) {
            Assert-True (($settings -split "`n") -ccontains $line) ('Missing or changed line: ' + $line)
        }
        Assert-Equal (& $helper -ProjectRoot $root -Action Check) 'Version 1.0.1 is synchronized; 3 product references and 4 platform counters checked.' 'Check summary.'
        Assert-Equal (& $helper -ProjectRoot $root -Action Set -Version '1.0.1') '1.0.1' 'Same version is idempotent.'
        Assert-Equal (Read-File (Join-Path $root 'ProjectSettings\ProjectSettings.asset')) $settings 'Repeated Set changed a counter.'
        Assert-Equal (Read-File (Join-Path $root 'VERSION')) "1.0.1`n" 'VERSION must end with one real newline.'
        $bytes = [System.IO.File]::ReadAllBytes((Join-Path $root 'VERSION'))
        Assert-True ($bytes[0] -eq 49 -and $bytes[$bytes.Length - 1] -eq 10) 'VERSION has a BOM or incorrect newline.'
        Assert-Equal @(Get-ChildItem -LiteralPath $root -Recurse -Force -File | Where-Object { $_.Name -match '\.version-|\.planetone-version\.lock' }).Count 0 'Transaction files remained.'
    }
    Invoke-Test 'CRLF settings and unrelated file bytes remain unchanged' {
        $root = New-Fixture '1.0.0' "`r`n"
        $unrelated = @('ProjectSettings\ProjectVersion.txt', 'manifest.json', 'save.json')
        foreach ($path in $unrelated) { Write-File (Join-Path $root $path) '{"version":"1.0.0","schemaVersion":1}' }
        & $helper -ProjectRoot $root -Action Set -Version '1.1.0' | Out-Null
        $settings = Read-File (Join-Path $root 'ProjectSettings\ProjectSettings.asset')
        Assert-True (-not [regex]::IsMatch($settings, '(?<!\r)\n')) 'Set changed CRLF line endings.'
        foreach ($path in $unrelated) { Assert-Equal (Read-File (Join-Path $root $path)) '{"version":"1.0.0","schemaVersion":1}' ('Unrelated file changed: ' + $path) }
    }
    Invoke-Test 'Invalid target versions and downgrades fail before writing' {
        $root = New-Fixture
        foreach ($target in @('01.0.1', '1.01.1', '1.0', '1.0.1-preview', '1.0.1+build', "1.0.1`n")) {
            Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Set -Version $target } 'Invalid product version'
        }
        Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Set -Version '0.9.9' } 'Refusing to downgrade'
        Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Set } 'requires -Version'
    }
    Invoke-Test 'Every product reference is validated before even a no-op Set or Next' {
        $root = New-Fixture
        $path = Join-Path $root 'ProjectSettings\ProjectSettings.asset'
        Write-File $path ((Read-File $path).Replace('  tvOSBundleVersion: 1.0.0', '  tvOSBundleVersion: 9.9.9'))
        foreach ($action in @('Check', 'Next')) {
            Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action $action } 'Version drift'
        }
        Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Set -Version '1.0.0' } 'Version drift'
        Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Set -Version '1.0.1' } 'Version drift'
    }
    Invoke-Test 'Duplicate references, malformed counters and unsupported mappings fail without writes' {
        foreach ($mutation in @(
            { param($text) $text + "  bundleVersion: 1.0.0`n" },
            { param($text) $text.Replace('    iPhone: 3', "    iPhone: 3`n    iPhone: 3") },
            { param($text) $text.Replace('  AndroidBundleVersionCode: 7', '  AndroidBundleVersionCode: -1') },
            { param($text) $text.Replace('    VisionOS: 14', '    VisionOS: 1.2.3') },
            { param($text) $text.Replace('  buildNumber:', '  buildNumber: {iPhone: 3}') }
        )) {
            $root = New-Fixture
            $path = Join-Path $root 'ProjectSettings\ProjectSettings.asset'
            Write-File $path (& $mutation (Read-File $path))
            Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Set -Version '1.0.1' } 'Expected exactly|Duplicate|counter|Unsupported'
        }
    }
    Invoke-Test 'Counter overflow fails atomically before staging either file' {
        foreach ($counterLine in @('  AndroidBundleVersionCode: 7', '    VisionOS: 14', '    iPhone: 3', '    tvOS: 9')) {
            $root = New-Fixture
            $path = Join-Path $root 'ProjectSettings\ProjectSettings.asset'
            $replacement = ($counterLine -replace ': [0-9]+$', ': 2147483647')
            Write-File $path ((Read-File $path).Replace($counterLine, $replacement))
            Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Set -Version '1.0.1' } 'Counter exhausted'
        }
    }
    Invoke-Test 'SemVer increments use arbitrary precision rather than overflowing a build counter' {
        $root = New-Fixture '1.0.9223372036854775807'
        Assert-Equal (& $helper -ProjectRoot $root -Action Next) '1.0.9223372036854775808' 'Large patch overflowed.'
    }
    Invoke-Test 'Optional platform fields may be absent and quoted versions are supported' {
        $root = New-Fixture
        $path = Join-Path $root 'ProjectSettings\ProjectSettings.asset'
        Write-File $path "PlayerSettings:`n  bundleVersion: '1.0.0'`n  buildNumber: {}`n"
        Assert-Equal (& $helper -ProjectRoot $root -Action Set -Version '2.0.0') '2.0.0' 'Minimal settings Set.'
        Assert-Equal (& $helper -ProjectRoot $root -Action Check) 'Version 2.0.0 is synchronized; 1 product references and 0 platform counters checked.' 'Optional fields Check.'
    }
    Invoke-Test 'A competing version lock prevents changes and is never removed by a failed caller' {
        $root = New-Fixture
        $lockPath = Join-Path $root '.planetone-version.lock'
        Write-File $lockPath 'owned by a different version process'
        Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Set -Version '1.0.1' } 'already exists|bereits vorhanden'
        Assert-Equal (Read-File $lockPath) 'owned by a different version process' 'A foreign transaction lock was removed.'
    }
    Invoke-Test 'A failed VERSION replacement rolls back already-written settings and counters' {
        $root = New-Fixture
        $versionPath = Join-Path $root 'VERSION'
        $readLock = [System.IO.File]::Open($versionPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
        try {
            Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Set -Version '1.0.1' } '\S'
        }
        finally { $readLock.Dispose() }
        Assert-Equal (& $helper -ProjectRoot $root -Action Check) 'Version 1.0.0 is synchronized; 3 product references and 4 platform counters checked.' 'Rollback left inconsistent references.'
        Assert-Equal @(Get-ChildItem -LiteralPath $root -Recurse -Force -File | Where-Object { $_.Name -match '\.version-|\.planetone-version\.lock' }).Count 0 'Failed transaction left temporary files.'
    }
    Invoke-Test 'Malformed central VERSION and command parameter misuse are rejected' {
        $root = New-Fixture
        Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Get -Version '1.0.0' } 'only valid'
        Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Set -Version '1.0.1' -Increment Minor } 'only valid'
        Write-File (Join-Path $root 'VERSION') "1.0.0`n1.0.1`n"
        Expect-FailureWithoutWrites $root { & $helper -ProjectRoot $root -Action Set -Version '1.0.1' } 'one canonical'
    }
    Write-Output ("Version helper: {0} tests passed using {1} isolated fixtures; repository version was not modified." -f $passed, $fixtures)
}
finally {
    $resolvedSuite = [System.IO.Path]::GetFullPath($suiteRoot)
    $expectedPrefix = $temporaryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar + 'PlanetOne-Version-'
    if (-not $resolvedSuite.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing fixture cleanup outside intended temporary directory: $resolvedSuite"
    }
    if (Test-Path -LiteralPath $resolvedSuite) { Remove-Item -LiteralPath $resolvedSuite -Recurse -Force }
}
