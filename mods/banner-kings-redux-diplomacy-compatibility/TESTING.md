# Validation and in-game test checklist

## Completed automated validation

- Release compilation against Bannerlord v1.4.7 and Harmony v2.4.2: passed
  with zero warnings and zero errors.
- Static target validation against the supplied Banner Kings Redux v1.9.33.5
  DLL: all three target methods were found with the expected signatures.
- Runtime Harmony smoke test using API-compatible Redux test doubles:
  - legacy Diplomacy detection changed from `false` to `true`;
  - the direct Redux forced-peace path was skipped;
  - the two reward adapters bound to Redux-compatible postfix methods;
  - unloading the module removed its hooks.
- Release isolation check: the compatibility assembly references no Banner
  Kings or Diplomacy DLL directly, and its output directory contains no copied
  dependency DLLs.

## Recommended in-game checks

Use a backup of an existing save or a disposable new campaign.

1. Confirm the launcher order is Banner Kings Redux, Diplomacy (Fork), then
   this compatibility module.
2. Reach the main menu and check the Bannerlord debug output for
   `[BK-Diplomacy Compatibility]`, `installed 3/3 diplomacy hooks`, and
   `2/2 reward hooks`.
3. Open the kingdom diplomacy screen and confirm Diplomacy's alliance and
   non-aggression-pact controls are present and responsive.
4. Let several kingdoms simulate for multiple weeks and verify that war and
   peace proposals do not appear twice.
5. Confirm Diplomacy's war/peace cooldowns and non-aggression-pact restrictions
   prevent prohibited declarations.
6. Confirm mercenary-clan departure and kingdom votes complete without UI
   exceptions or crashes.
7. After winning battles with the relevant Banner Kings education bonuses,
   confirm Commander Warband affects influence and Famous Sellswords or the
   Cataphract lifestyle affects renown without being applied twice.

The automated checks validate patch installation and control flow. A full
Bannerlord campaign simulation is still required to validate gameplay behavior
that depends on live save data, AI state, settings, and other enabled mods.
