# Bannerlord Community Mod Fixes

Source-only compatibility work for Mount & Blade II: Bannerlord mods that need
small, auditable repairs on current game builds. This repository intentionally
does not track compiled mod DLLs, game assemblies, dependency DLLs, release
archives, or local game installations.

## Mod List

| Mod | Current version | Direct download |
| --- | :--- | --- |
| Banner Kings Redux - Diplomacy Compatibility | 2.1.0 | [Download ZIP](https://github.com/Sallvainian/Bannerlord-Community-Mod-Fixes/releases/download/bk-diplomacy-v2.1.0/Banner-Kings-Redux-Diplomacy-Compatibility-v2.1.0.zip) |
| Better Troop HUD - Bannerlord 1.4.7 Compatibility Port | 1.1.0 | [Download ZIP](https://github.com/Sallvainian/Bannerlord-Community-Mod-Fixes/releases/download/better-troop-hud-v1.1.0/Better-Troop-HUD-1.4.7-v1.1.0.zip) |
| Relentless Smith - Banner Kings Redux Compatibility | 1.0.1 | [Download ZIP](https://github.com/Sallvainian/Bannerlord-Community-Mod-Fixes/releases/download/relentless-smith-bk-redux-v1.0.1/Relentless-Smith-BK-Redux-Compatibility-v1.0.1.zip) |
| Captivity Events (Updated 1.4.7) | 1.0.1 | [Download ZIP](https://github.com/Sallvainian/Bannerlord-Community-Mod-Fixes/releases/download/captivity-events-1.4.7-v1.0.1/Captivity-Events-Updated-1.4.7-v1.0.1.zip) |

### Requirements and original mods

All four releases target Mount & Blade II: Bannerlord `v1.4.7`. The table lists
the direct mod dependencies declared or targeted by each release; Bannerlord's
standard game modules are omitted. Follow the linked projects for any
additional requirements they declare.

| Mod | Direct mod dependencies | Original and upstream mods |
| --- | --- | --- |
| Banner Kings Redux - Diplomacy Compatibility | [Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006) `v2.4.2`<br>[Banner Kings Redux](https://github.com/GIO443/bannerlord-banner-kings-redux/releases/tag/v1.9.33.5) `v1.9.33.5`<br>[Diplomacy Fork](https://github.com/adwitkow/Bannerlord.Diplomacy/releases/tag/v1.4.1.0) `v1.4.1.0` | [Banner Kings Redux by GIO443](https://github.com/GIO443/bannerlord-banner-kings-redux)<br>[Diplomacy Fork](https://github.com/adwitkow/Bannerlord.Diplomacy)<br>[Original Banner Kings](https://www.nexusmods.com/mountandblade2bannerlord/mods/3826)<br>[Original Diplomacy by the Diplomacy Team](https://www.nexusmods.com/mountandblade2bannerlord/mods/832) |
| Better Troop HUD - Bannerlord 1.4.7 Compatibility Port | [Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006) `v2.2.2+`<br>[ButterLib](https://www.nexusmods.com/mountandblade2bannerlord/mods/2018) `v2.8.0+`<br>[UIExtenderEx](https://www.nexusmods.com/mountandblade2bannerlord/mods/2102) `v2.8.0+`<br>[Mod Configuration Menu](https://www.nexusmods.com/mountandblade2bannerlord/mods/612) `v5.7.1+` | [Original Better Troop HUD by Haarrdy](https://github.com/Haarrdy/MB-BetterTroopHUD) |
| Relentless Smith - Banner Kings Redux Compatibility | [Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006) `v2.4.2`<br>[Banner Kings Redux](https://github.com/GIO443/bannerlord-banner-kings-redux/releases/tag/v1.9.33.5) `v1.9.33.5`<br>[Relentless Smith Concise](https://steamcommunity.com/sharedfiles/filedetails/?id=3637912777) `v1.0.4` | [Original Relentless Smith Concise by dirty kebab](https://steamcommunity.com/sharedfiles/filedetails/?id=3637912777)<br>[Banner Kings Redux by GIO443](https://github.com/GIO443/bannerlord-banner-kings-redux)<br>[Original Banner Kings](https://www.nexusmods.com/mountandblade2bannerlord/mods/3826) |
| Captivity Events (Updated 1.4.7) | [Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006) `v2.4.2`<br>Optional: [Mod Configuration Menu](https://www.nexusmods.com/mountandblade2bannerlord/mods/612) `v5.11.4+` | [Original Captivity Events by TheBadListener](https://www.nexusmods.com/mountandblade2bannerlord/mods/1226) |

## Included projects

| Project | Version | Target combination | Repository status |
| --- | ---: | --- | --- |
| [Banner Kings Redux - Diplomacy Compatibility](mods/banner-kings-redux-diplomacy-compatibility/) | 2.1.0 | Bannerlord 1.4.7; Banner Kings Redux 1.9.33.5; Diplomacy Fork 1.4.1.0; Harmony 2.4.2 | Source plus runtime smoke test |
| [Better Troop HUD - Bannerlord 1.4.7 Compatibility Port](mods/better-troop-hud-1.4.7/) | 1.1.0 | Bannerlord 1.4.7; upstream BetterTroopHUD 1.0.1 baseline | Port source plus source UI assets; generated game resources omitted |
| [Relentless Smith - Banner Kings Redux Compatibility](mods/relentless-smith-bk-redux-compatibility/) | 1.0.1 | Bannerlord 1.4.7; Banner Kings Redux 1.9.33.5; Relentless Smith Concise 1.0.4 | Independent compatibility module plus proxy verifier |
| [Captivity Events (Updated 1.4.7)](mods/captivity-events-1.4.7-compatibility-addon/) | 1.0.1 | Bannerlord 1.4.7; internal module version 1.4.7.2; Captivity Events 1.4.5.1400 | One consolidated `zCaptivityEvents` release; independent safety-layer source preserved here |

These targets describe the preserved source snapshots. They are not a promise
of compatibility with later game or dependency releases.

## Dependency policy

Dependencies are not vendored in this source repository. Obtain them from the
linked projects in the requirements table above and respect their authors'
licenses and distribution terms.

## Building

Install the required Bannerlord version and dependencies, then set
`BANNERLORD_GAME_DIR` to the game directory before building. For example in
PowerShell:

```powershell
$env:BANNERLORD_GAME_DIR = 'C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord'
dotnet build .\mods\banner-kings-redux-diplomacy-compatibility\Source\BK_Diplomacy_ModuleLoader_Patch\BK_Diplomacy_ModuleLoader_Patch.csproj -c Release
```

Each project targets .NET Framework and resolves TaleWorlds and Harmony
assemblies from that external installation. Build output is ignored by Git.

Better Troop HUD's upstream build target can copy a normal build directly into
the configured game's `Modules` directory. For compile-only validation, pass
`-p:ModuleId=`. Its generated `.tpac` and `.rdc` UI resources are not produced
by `Bannerlord.BuildResources`; a complete release must regenerate them with
the appropriate TaleWorlds asset pipeline from the retained source assets.

## Releases and installation

The main branch is for reviewable source. A release package must be built and
should contain only the module's own output plus its manifest and documentation.
Do not install this repository tree directly into Bannerlord's `Modules`
directory.

### Nexus Mods publishing

Installable GitHub Release ZIPs are prepared by
`.github/workflows/publish-github-release.yml`. The workflow runs on the
`bannerlord` self-hosted Windows runner because the verified distribution
artifacts depend on the local Bannerlord modding workspace and generated game
assets that are not stored in this repository. It can be triggered manually or
by pushing one of the release tags listed below.

Set the repository Actions variable `BANNERLORD_MODDING_ROOT` to the root of the
local modding workspace. On the maintained Windows runner this is:

`C:\Users\frank\Projects\Game-Modding\Bannerlord`

The workflow validates that each ZIP has exactly one expected Bannerlord module
root, its `SubModule.xml`, and its required runtime DLLs before creating or
updating the GitHub Release.

The repository uses Nexus Mods' official
[`Nexus-Mods/upload-action`](https://github.com/Nexus-Mods/upload-action) to
publish a GitHub release ZIP as a new version of an existing Nexus file. The
workflow is triggered by publishing a non-prerelease GitHub release, or it can
be run manually for an existing release tag.

Release tags and ZIP names must follow these pairs:

| Release tag | Required release asset |
| --- | --- |
| `bk-diplomacy-v2.1.0` | `Banner-Kings-Redux-Diplomacy-Compatibility-v2.1.0.zip` |
| `better-troop-hud-v1.1.0` | `Better-Troop-HUD-1.4.7-v1.1.0.zip` |
| `relentless-smith-bk-redux-v1.0.1` | `Relentless-Smith-BK-Redux-Compatibility-v1.0.1.zip` |
| `captivity-events-1.4.7-v1.0.1` | `Captivity-Events-Updated-1.4.7-v1.0.1.zip` |

Configure the repository Actions secret `NEXUSMODS_API_KEY` and these Actions
variables before running the workflow:

- `NEXUSMODS_FILE_ID_BK_DIPLOMACY`
- `NEXUSMODS_FILE_ID_BETTER_TROOP_HUD`
- `NEXUSMODS_FILE_ID_RELENTLESS_SMITH`
- `NEXUSMODS_FILE_ID_CAPTIVITY_EVENTS`

The Nexus Upload API beta updates existing mod files. Each Nexus mod page must
therefore exist before this workflow can run, and each variable above must be
set to the persistent file ID that receives future versions.

Licensing differs by project. Read
[Licensing and credits](docs/licensing-and-credits.md) and each project's
license/notices before redistributing a build. There is deliberately no blanket
root license that attempts to relicense upstream-owned material.
