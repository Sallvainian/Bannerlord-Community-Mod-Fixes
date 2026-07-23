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
| [Captivity Events (Updated 1.4.7)](mods/captivity-events-1.4.7-compatibility-addon/) | 1.0.0 | Bannerlord 1.4.7; Captivity Events 1.4.5.1400 | Independent safety add-on source; not a Captivity Events fork or replacement |

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

Licensing differs by project. Read
[Licensing and credits](docs/licensing-and-credits.md) and each project's
license/notices before redistributing a build. There is deliberately no blanket
root license that attempts to relicense upstream-owned material.
