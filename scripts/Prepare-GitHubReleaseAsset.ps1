[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet(
        'bk-diplomacy',
        'better-troop-hud',
        'relentless-smith-bk-redux',
        'captivity-events-1.4.7'
    )]
    [string]$Target,

    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:[.-][0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$ModdingRoot = $env:BANNERLORD_MODDING_ROOT,

    [string]$OutputDirectory = $env:RUNNER_TEMP
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ModdingRoot)) {
    $ModdingRoot = 'C:\Users\frank\Projects\Game-Modding\Bannerlord'
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $env:TEMP 'bannerlord-community-mod-releases'
}

$ModdingRoot = (Resolve-Path -LiteralPath $ModdingRoot).Path
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

switch ($Target) {
    'bk-diplomacy' {
        $sourcePattern = Join-Path $ModdingRoot (
            'Bannerlord-Community-Mod-Fixes\artifacts\release-prep-*\archives\' +
            "BK-Diplomacy-Compatibility-v$Version-Bannerlord-v1.4.7.zip"
        )
        $archiveName = "Banner-Kings-Redux-Diplomacy-Compatibility-v$Version.zip"
        $releaseTag = "bk-diplomacy-v$Version"
        $moduleRoot = 'BK_Diplomacy_ModuleLoader_Patch'
        $requiredDlls = @('BK_Diplomacy_ModuleLoader_Patch.dll')
        $displayName = 'Banner Kings Redux - Diplomacy Compatibility'
    }
    'better-troop-hud' {
        $sourcePattern = Join-Path $ModdingRoot (
            "BetterTroopHUD-1.4.7\Distribution\Better-Troop-HUD-1.4.7-v$Version.zip"
        )
        $archiveName = "Better-Troop-HUD-1.4.7-v$Version.zip"
        $releaseTag = "better-troop-hud-v$Version"
        $moduleRoot = 'BetterTroopHUD.147'
        $requiredDlls = @('BetterTroopHUD.dll')
        $displayName = 'Better Troop HUD - Bannerlord 1.4.7 Compatibility Port'
    }
    'relentless-smith-bk-redux' {
        $sourcePattern = Join-Path $ModdingRoot (
            "RelentlessSmith-BKRedux-Fix\Distribution\" +
            "RelentlessSmithConcise.BKRedux.Fixes-v$Version.zip"
        )
        $archiveName = "Relentless-Smith-BK-Redux-Compatibility-v$Version.zip"
        $releaseTag = "relentless-smith-bk-redux-v$Version"
        $moduleRoot = 'RelentlessSmithConcise.BKRedux.Fixes'
        $requiredDlls = @('RelentlessSmithConcise.BKRedux.Fixes.dll')
        $displayName = 'Relentless Smith - Banner Kings Redux Compatibility'
    }
    'captivity-events-1.4.7' {
        $sourcePattern = Join-Path $ModdingRoot (
            "CaptivityEvents-147-Fix\Distribution\" +
            "Captivity-Events-Updated-1.4.7-v$Version.zip"
        )
        $archiveName = "Captivity-Events-Updated-1.4.7-v$Version.zip"
        $releaseTag = "captivity-events-1.4.7-v$Version"
        $moduleRoot = 'zCaptivityEvents'
        $requiredDlls = @(
            'CaptivityEvents.dll',
            'CaptivityEvents.147Fix.dll'
        )
        $displayName = 'Captivity Events (Updated 1.4.7)'
    }
}

$sourceMatches = @(Get-Item -Path $sourcePattern -ErrorAction SilentlyContinue)
if ($sourceMatches.Count -ne 1) {
    throw "Expected exactly one release archive matching '$sourcePattern'; found $($sourceMatches.Count)."
}

$sourceArchive = $sourceMatches[0].FullName
$outputArchive = Join-Path $OutputDirectory $archiveName

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($sourceArchive)
try {
    $entryNames = @($archive.Entries | ForEach-Object FullName)
    $topLevelRoots = @(
        $entryNames |
            Where-Object { $_ } |
            ForEach-Object { ($_ -split '/')[0] } |
            Sort-Object -Unique
    )

    if ($topLevelRoots.Count -ne 1 -or $topLevelRoots[0] -ne $moduleRoot) {
        throw "Archive must contain only the '$moduleRoot' module root."
    }

    $manifestEntry = "$moduleRoot/SubModule.xml"
    if ($manifestEntry -notin $entryNames) {
        throw "Archive is missing $manifestEntry."
    }

    foreach ($dllName in $requiredDlls) {
        $dllEntry = "$moduleRoot/bin/Win64_Shipping_Client/$dllName"
        if ($dllEntry -notin $entryNames) {
            throw "Archive is missing $dllEntry."
        }
    }
}
finally {
    $archive.Dispose()
}

Copy-Item -LiteralPath $sourceArchive -Destination $outputArchive -Force
$sha256 = (Get-FileHash -LiteralPath $outputArchive -Algorithm SHA256).Hash

if ($env:GITHUB_OUTPUT) {
    @(
        "archive_path=$outputArchive"
        "archive_name=$archiveName"
        "release_tag=$releaseTag"
        "display_name=$displayName"
        "version=$Version"
        "sha256=$sha256"
    ) | Add-Content -LiteralPath $env:GITHUB_OUTPUT
}

[pscustomobject]@{
    Target = $Target
    Version = $Version
    ReleaseTag = $releaseTag
    DisplayName = $displayName
    Archive = $outputArchive
    SHA256 = $sha256
}
