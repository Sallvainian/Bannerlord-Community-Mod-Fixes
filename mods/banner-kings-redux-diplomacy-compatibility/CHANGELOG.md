# Changelog

## v2.1.0

- Restored Banner Kings Redux's Commander Warband influence bonus on
  Bannerlord v1.4.7.
- Restored its Famous Sellswords and Cataphract renown bonuses.
- Added per-target Harmony ownership checks so fixed future Redux builds do not
  receive duplicate reward factors.
- Left Redux's already-active loot scaling and roster safety patches untouched.
- Continued to ship no copied Banner Kings, Diplomacy, game, or Harmony DLLs.

## v2.0.0

- Replaced the v1.0.0 no-op scaffold with functional Harmony compatibility
  hooks.
- Corrected Banner Kings Redux v1.9.33.5's legacy Diplomacy detection.
- Activated Redux's intended compatibility guards for kingdom decisions,
  diplomacy UI, mercenary behavior, peace support, and AI proposals.
- Prevented Redux's forced-peace routine from bypassing Diplomacy's proposal
  conditions and cooldowns.
- Updated the module dependency from `BannerKings` to `BannerKings.Redux`.
- Removed copied game, mod, Harmony, and framework assemblies from the release.
- Built for Bannerlord v1.4.7 and Diplomacy Fork v1.4.1.0.
