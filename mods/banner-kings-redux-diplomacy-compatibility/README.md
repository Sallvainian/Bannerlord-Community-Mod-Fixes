# Banner Kings Redux - Diplomacy Compatibility

> This directory is the source project, not a ready-to-install module. Use a
> packaged release or build the project with `BANNERLORD_GAME_DIR` set to a
> Bannerlord v1.4.7 installation. No dependency binaries are stored here.

Version 2.1.0 targets:

- Mount & Blade II: Bannerlord v1.4.7
- Banner Kings Redux v1.9.33.5 (`BannerKings.Redux`)
- Diplomacy Fork v1.4.1.0 (`Bannerlord.Diplomacy`)
- Bannerlord.Harmony v2.4.2

## What the patch changes

Banner Kings Redux includes compatibility guards intended to yield overlapping
kingdom-decision, diplomacy UI, mercenary, and AI-proposal behavior to
Diplomacy. In v1.9.33.5, its detector still probes the legacy identifiers
`Diplomacy` and `DiplomacyFixes`. The current Diplomacy module uses
`Bannerlord.Diplomacy` and versioned `Bannerlord.Diplomacy.*` assemblies, so
those guards otherwise remain disabled.

This module makes Redux's legacy Diplomacy probe resolve successfully. It also
suppresses Redux's direct forced-peace proposal routine because that path queues
a decision without passing through Diplomacy's peace conditions and cooldowns.

Redux v1.9.33.5 also has stale Harmony argument names on Bannerlord v1.4.7.
Its loot-scaling patch and roster safety finalizers still install, but its
Commander Warband influence bonus and Famous Sellswords/Cataphract renown
bonuses do not. Version 2.1.0 restores exactly those two missing reward hooks by
adapting the current Bannerlord signatures to Redux's existing implementations.
It checks the live Harmony state first, so a future Redux build that repairs
them itself will not receive duplicate bonuses.

The patch does not replace either mod's DLLs and does not bundle Bannerlord,
Harmony, Banner Kings, or Diplomacy assemblies.

## Installation and load order

For a compiled release, copy the complete
`BK_Diplomacy_ModuleLoader_Patch` module folder into Bannerlord's `Modules`
directory. Enable it after both supported mods:

1. Banner Kings Redux
2. Diplomacy (Fork)
3. Banner Kings Redux - Diplomacy Compatibility

The manifest declares all three dependencies and asks the launcher to place the
compatibility module after them.

## Upgrade from v1.0.0

Replace the old `BK_Diplomacy_ModuleLoader_Patch` folder completely. Do not keep
the dependency and game DLLs bundled by v1.0.0; v2.1.0 intentionally ships only
its own compatibility DLL.

## Source and diagnostics

At startup, successful hooks are written through Bannerlord's debug output with
the prefix `[BK-Diplomacy Compatibility]`. The expected result is `3/3`
diplomacy hooks followed by `2/2` Banner Kings reward hooks during campaign
startup.
