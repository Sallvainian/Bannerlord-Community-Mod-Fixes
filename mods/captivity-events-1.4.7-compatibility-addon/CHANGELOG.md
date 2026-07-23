# Changelog

## 1.0.1

- Removed the early Harmony wrapper around Captivity Events initialization
  because it interfered with Captivity Events' parameterless `PatchAll()` call.
- Replaced ambiguous inherited-property reflection with exact declared getter
  resolution for `Culture`, `UpgradeTargets`, and `FirstBattleEquipment`.
- Consolidated the updated main mod and safety layer into one distributable
  `zCaptivityEvents` module. Its internal Bannerlord manifest version is
  `v1.4.7.2` so it remains newer than upstream dependency declarations.

## 1.0.0

- Withdrawn because its early initialization wrapper and ambiguous
  `CharacterObject.Culture` lookup could crash during startup.
- Added an early successful-load and re-entrancy guard around Captivity Events
  initialization.
- Replaced destructive missing-character-data fallbacks with non-mutating
  fallback values and diagnostics.
- Replaced per-tick image-atlas reloads with transition-based restoration.
- Added contract checks and rollback paths that restore upstream patches if a
  replacement cannot be installed safely.
