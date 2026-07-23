# Better Troop HUD - Bannerlord 1.4.7 compatibility port

This local build is based on Haarrdy's MIT-licensed BetterTroopHUD source at:

https://github.com/Haarrdy/MB-BetterTroopHUD

Local module Id: `BetterTroopHUD.147`  
Assembly/module version: `1.1.0`

The port replaces the removed `MissionGauntletBattleUIBase` API with the current `MissionBattleUIBaseView` lifecycle, updates the `GauntletLayer` constructor and UI context access, recompiles collection signatures against Bannerlord 1.4.7, and adds current suspend/resume and hidden-battle-UI handling.

Version 1.1.0 prevents the battle HUD movie from loading in friendly civilian missions. Bannerlord 1.4.7 can add `OrderTroopPlacer` to a town-center mission even though no battle HUD is available, which previously caused a null-reference crash while entering a city.

Keep the original Workshop module `BetterTroopHUD` disabled while this module is enabled. The different module Id prevents Steam Workshop updates from overwriting the local build. MCM settings keep the original `BetterTroopHUD` settings Id so existing preferences remain usable.
