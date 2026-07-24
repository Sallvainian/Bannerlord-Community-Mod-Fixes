# Relentless Smith - Banner Kings Redux Compatibility

Independent compatibility module for:

- Mount & Blade II: Bannerlord 1.4.7
- Banner Kings Redux 1.9.33.5
- Relentless Smith Concise 1.0.4
- Bannerlord.Harmony 2.4.2

Banner Kings Redux initializes null-skill lifestyle perks and custom-skill perks
before Bannerlord's vanilla `DefaultSkills` state is ready. Relentless Smith's
global `PerkObject.Initialize` prefix reads `DefaultSkills.Crafting` for every
non-null perk even though it rewrites only three vanilla smithing perks. That
combination can crash during campaign initialization.

The compatibility module leaves both upstream DLLs untouched. It replaces only
the active Relentless Smith prefix with a proxy that bypasses null-skill and
unrelated perks, invokes the original prefix for `PracticalRefiner`,
`PracticalSmelter`, and `PracticalSmith`, and preserves the original by-reference
changes and exception behavior for those relevant calls.

Relentless Smith also refreshes the smelting list after an operation, assigns
the first remaining row as the current item, then clears the visual selection
state. That forces the player to click a row before every subsequent smelt.
This module now clears Relentless Smith's stale current-row reference and then
uses Bannerlord's own `OnItemSelection` transition only when the current row
was left visually unselected. This is necessary because Bannerlord deliberately
does nothing when `OnItemSelection` receives the already-current row. Bulk,
Ctrl, and stack-selection behavior remains owned by Relentless Smith.

## Installation order

Use a compiled package and enable it after Banner Kings Redux and Relentless
Smith Concise. Do not install this source tree directly into `Modules`.

The Relentless Smith Concise Workshop item is currently unavailable. This patch
requires an existing `v1.0.4` installation and does not include or redistribute
any Relentless Smith files.

The module log is written to:

```text
%LOCALAPPDATA%\Mount and Blade II Bannerlord\Logs\RelentlessSmithConcise.BKRedux.Fixes.log
```

## Source and verification

Set `BANNERLORD_GAME_DIR` to the Bannerlord v1.4.7 directory and build the
project under `Source`. `Tests/ProxyVerifier` contains the source for the
contract/proxy verifier; it covers both the perk compatibility proxy and
automatic smelting reselection, and expects paths to a built compatibility DLL,
the installed Relentless Smith DLL, and the game root when run.

## Credits and status

This is independently written interoperability code. It does not include or
claim ownership of Relentless Smith Concise or Banner Kings Redux. Relentless
Smith Concise is credited to its Steam Workshop creator,
[dirty kebab](https://steamcommunity.com/sharedfiles/filedetails/?id=3637912777).
See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
