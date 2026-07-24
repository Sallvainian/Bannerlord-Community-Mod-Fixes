# Better Troop HUD - Bannerlord 1.4.7 Compatibility Port

Source-only compatibility port of Haarrdy's BetterTroopHUD for Mount & Blade
II: Bannerlord v1.4.7. The current port version is `1.1.0`, based on upstream
version `1.0.1` at commit
`56015be090317f92783da19a8ced5e6441715813`.

This is a community port, not an official upstream release. Keep the original
`BetterTroopHUD` module disabled when using a compiled build of this port; its
module ID is `BetterTroopHUD.147` so Workshop updates cannot overwrite it.

## Port changes

- Replaced the removed `MissionGauntletBattleUIBase` API with the current
  `MissionBattleUIBaseView` lifecycle.
- Updated `GauntletLayer` construction, layer ownership, UI-context access,
  suspend/resume handling, and hidden battle-UI updates for Bannerlord 1.4.7.
- Prevented the HUD movie from loading in friendly civilian settlement
  missions, which can contain `OrderTroopPlacer` without a battle HUD and
  previously caused a null-reference crash while entering a city.
- Retained the original MCM settings ID so existing preferences remain usable.

## Dependencies

The manifest declares Harmony, ButterLib, UIExtenderEx, Mod Configuration Menu,
and the standard Bannerlord single-player modules. Obtain those dependencies
from their own authors; none are bundled here.

The Bannerlord 1.4.7 port was validated with:

- Bannerlord.Harmony `v2.4.2`
- ButterLib `v2.11.1`
- UIExtenderEx `v2.13.3`
- Mod Configuration Menu `v5.12.2`

The manifest preserves the older upstream minimum-version declarations for
launcher compatibility. Those older dependency combinations were not the
validated runtime stack for this port.

## Build and packaging

Set `BANNERLORD_GAME_DIR` to a Bannerlord v1.4.7 installation. For a compile-only
check that does not deploy into the game directory, run:

```powershell
dotnet build .\Source\BetterTroopHUD\BetterTroopHUD.csproj -c Release -p:ModuleId=
```

The upstream `Bannerlord.BuildResources` targets copy a normal build with a
non-empty `ModuleId` directly into the configured game's `Modules` directory.
Use that behavior deliberately, not as a repository validation step.

The repository retains the UI XML and source images needed to recreate the UI.
It does not retain generated `.tpac` or RuntimeDataCache `.rdc` files. The
`Bannerlord.BuildResources` package does not compile those resources; a complete
release must regenerate them with the appropriate TaleWorlds asset pipeline.
They belong in release artifacts, not in source control.

## Nexus artwork

The two Better Troop HUD title images under `assets/nexus` deliberately omit
invented formation markers. The separate
`better-troop-hud-upstream-in-game-reference-1920x1080.png` is the original
author's `Assets/Mod preview - Image 5.png` from the upstream commit cited above.
It is retained as an authentic reference for the widget's in-game appearance,
not represented as a fresh screenshot captured from this Bannerlord 1.4.7 port.

## Credits and license

Original BetterTroopHUD code and design are by
[Haarrdy](https://github.com/Haarrdy) and are distributed under the preserved
[MIT license](LICENSE). The original project documentation is retained as
[UPSTREAM_README.md](UPSTREAM_README.md).

The upstream README expressly excludes its TaleWorlds-derived icons from the
MIT license. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) before
redistributing a build.
