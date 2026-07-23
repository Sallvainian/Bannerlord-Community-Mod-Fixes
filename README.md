# Bannerlord Community Mod Fixes

Source-only compatibility work for Mount & Blade II: Bannerlord mods that need
small, auditable repairs on current game builds. This repository intentionally
does not track compiled mod DLLs, game assemblies, dependency DLLs, release
archives, or local game installations.

## Included projects

| Project | Version | Target combination | Repository status |
| --- | ---: | --- | --- |
| [Banner Kings Redux - Diplomacy Compatibility](mods/banner-kings-redux-diplomacy-compatibility/) | 2.1.0 | Bannerlord 1.4.7; Banner Kings Redux 1.9.33.5; Diplomacy Fork 1.4.1.0; Harmony 2.4.2 | Source plus runtime smoke test |
| [Better Troop HUD - Bannerlord 1.4.7 Compatibility Port](mods/better-troop-hud-1.4.7/) | 1.1.0 | Bannerlord 1.4.7; upstream BetterTroopHUD 1.0.1 baseline | Port source plus source UI assets; generated game resources omitted |
| [Relentless Smith - Banner Kings Redux Compatibility](mods/relentless-smith-bk-redux-compatibility/) | 1.0.1 | Bannerlord 1.4.7; Banner Kings Redux 1.9.33.5; Relentless Smith Concise 1.0.4 | Independent compatibility module plus proxy verifier |
| [Captivity Events (Updated 1.4.7)](mods/captivity-events-1.4.7-compatibility-addon/) | 1.0.1 | Bannerlord 1.4.7; internal module version 1.4.7.2; Captivity Events 1.4.5.1400 | One consolidated `zCaptivityEvents` release; independent safety-layer source preserved here |

These targets describe the preserved source snapshots. They are not a promise
of compatibility with later game or dependency releases.

## External dependencies

Dependencies are not vendored. Obtain them from their respective projects:

- [Bannerlord.Harmony](https://github.com/BUTR/Bannerlord.Harmony)
- [Banner Kings Redux](https://github.com/GIO443/bannerlord-banner-kings-redux)
- [Diplomacy Fork](https://github.com/adwitkow/Bannerlord.Diplomacy)
- [Better Troop HUD upstream](https://github.com/Haarrdy/MB-BetterTroopHUD)
- [Relentless Smith Concise on Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3637912777)
- [Captivity Events on Nexus Mods](https://www.nexusmods.com/mountandblade2bannerlord/mods/1226)

Better Troop HUD also declares ButterLib, UIExtenderEx, and Mod Configuration
Menu dependencies in its `SubModule.xml`.

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
