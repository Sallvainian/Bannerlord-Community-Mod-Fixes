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
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

$metadata = switch ($Target) {
    'bk-diplomacy' {
        [pscustomobject]@{
            ProjectPath = 'mods/banner-kings-redux-diplomacy-compatibility'
            TagFormat = 'bk-diplomacy-v{0}'
            Dependencies = @(
                '[Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006) `v2.4.2`'
                '[Banner Kings Redux](https://github.com/GIO443/bannerlord-banner-kings-redux/releases/tag/v1.9.33.5) `v1.9.33.5`'
                '[Diplomacy Fork](https://github.com/adwitkow/Bannerlord.Diplomacy/releases/tag/v1.4.1.0) `v1.4.1.0`'
            )
            DependencyNotes = @()
            Upstreams = @(
                '[Banner Kings Redux by GIO443](https://github.com/GIO443/bannerlord-banner-kings-redux)'
                '[Diplomacy Fork](https://github.com/adwitkow/Bannerlord.Diplomacy)'
                '[Original Banner Kings](https://www.nexusmods.com/mountandblade2bannerlord/mods/3826)'
                '[Original Diplomacy by the Diplomacy Team](https://www.nexusmods.com/mountandblade2bannerlord/mods/832)'
            )
        }
    }
    'better-troop-hud' {
        [pscustomobject]@{
            ProjectPath = 'mods/better-troop-hud-1.4.7'
            TagFormat = 'better-troop-hud-v{0}'
            Dependencies = @(
                '[Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006) `v2.4.2`'
                '[ButterLib](https://www.nexusmods.com/mountandblade2bannerlord/mods/2018) `v2.11.1`'
                '[UIExtenderEx](https://www.nexusmods.com/mountandblade2bannerlord/mods/2102) `v2.13.3`'
                '[Mod Configuration Menu](https://www.nexusmods.com/mountandblade2bannerlord/mods/612) `v5.12.2`'
            )
            DependencyNotes = @(
                'These are the dependency versions used to validate the Bannerlord 1.4.7 port. The module manifest preserves older upstream minimum-version declarations, but those older combinations were not the validated runtime stack.'
            )
            Upstreams = @(
                '[Original Better Troop HUD by Haarrdy](https://github.com/Haarrdy/MB-BetterTroopHUD)'
            )
        }
    }
    'relentless-smith-bk-redux' {
        [pscustomobject]@{
            ProjectPath = 'mods/relentless-smith-bk-redux-compatibility'
            TagFormat = 'relentless-smith-bk-redux-v{0}'
            Dependencies = @(
                '[Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006) `v2.4.2`'
                '[Banner Kings Redux](https://github.com/GIO443/bannerlord-banner-kings-redux/releases/tag/v1.9.33.5) `v1.9.33.5`'
                '[Relentless Smith Concise](https://steamcommunity.com/sharedfiles/filedetails/?id=3637912777) `v1.0.4`'
            )
            DependencyNotes = @(
                'The Relentless Smith Concise Workshop item is currently unavailable. This patch requires an existing v1.0.4 installation and does not include or redistribute Relentless Smith files.'
            )
            Upstreams = @(
                '[Original Relentless Smith Concise by dirty kebab](https://steamcommunity.com/sharedfiles/filedetails/?id=3637912777)'
                '[Banner Kings Redux by GIO443](https://github.com/GIO443/bannerlord-banner-kings-redux)'
                '[Original Banner Kings](https://www.nexusmods.com/mountandblade2bannerlord/mods/3826)'
            )
        }
    }
    'captivity-events-1.4.7' {
        [pscustomobject]@{
            ProjectPath = 'mods/captivity-events-1.4.7-compatibility-addon'
            TagFormat = 'captivity-events-1.4.7-v{0}'
            Dependencies = @(
                '[Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006) `v2.4.2`'
                '[Mod Configuration Menu](https://www.nexusmods.com/mountandblade2bannerlord/mods/612) `v5.11.4+` (required)'
            )
            DependencyNotes = @(
                'Mod Configuration Menu is intentionally required to preserve the original mod configuration experience.'
            )
            Upstreams = @(
                '[Original Captivity Events by BadListener, uploaded by TheBadListener](https://www.nexusmods.com/mountandblade2bannerlord/mods/1226)'
            )
        }
    }
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$changelogRelativePath = "$($metadata.ProjectPath)/CHANGELOG.md"
$changelogPath = Join-Path $repositoryRoot $changelogRelativePath

if (-not (Test-Path -LiteralPath $changelogPath -PathType Leaf)) {
    throw "Changelog not found: $changelogPath"
}

$changelog = Get-Content -LiteralPath $changelogPath -Raw
$versionPattern = [regex]::Escape($Version)
$sectionPattern = "(?ms)^##\s+v?$versionPattern\s*\r?\n(?<entries>.*?)(?=^##\s+|\z)"
$sectionMatch = [regex]::Match($changelog, $sectionPattern)

if (-not $sectionMatch.Success) {
    throw "Version $Version was not found in $changelogRelativePath."
}

$changelogEntries = $sectionMatch.Groups['entries'].Value.Trim()
$releaseTag = $metadata.TagFormat -f $Version
$repositoryUrl = 'https://github.com/Sallvainian/Bannerlord-Community-Mod-Fixes'

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('## Requirements')
$lines.Add('')
$lines.Add('- **Game version:** [Mount & Blade II: Bannerlord](https://www.taleworlds.com/en/Games/Bannerlord) `v1.4.7`')
foreach ($dependency in $metadata.Dependencies) {
    $lines.Add("- $dependency")
}
foreach ($dependencyNote in $metadata.DependencyNotes) {
    $lines.Add('')
    $lines.Add("> $dependencyNote")
}
$lines.Add('')
$lines.Add("> Only direct mod dependencies are listed. Follow each linked project for any additional requirements. Bannerlord's standard game modules ship with the game.")
$lines.Add('')
$lines.Add('## Original and upstream mods')
$lines.Add('')
foreach ($upstream in $metadata.Upstreams) {
    $lines.Add("- $upstream")
}
$lines.Add('')
$lines.Add('## Changelog')
$lines.Add('')
$lines.Add("### v$Version")
$lines.Add('')
$lines.Add($changelogEntries)
$lines.Add('')
$lines.Add('## Source')
$lines.Add('')
$lines.Add("- [Source project]($repositoryUrl/tree/$releaseTag/$($metadata.ProjectPath))")
$lines.Add("- [Complete project changelog]($repositoryUrl/blob/$releaseTag/$changelogRelativePath)")

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory -and -not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$releaseNotes = ($lines -join [Environment]::NewLine) + [Environment]::NewLine
[System.IO.File]::WriteAllText(
    $OutputPath,
    $releaseNotes,
    [System.Text.UTF8Encoding]::new($false)
)

Write-Output $OutputPath
