# Changelog

## 1.0.2

- Re-checks the live Harmony patch table during campaign initialization before
  any diagnostic `CharacterObject` enumeration occurs.
- Removes re-applied Captivity Events `Culture`, `UpgradeTargets`, and
  `FirstBattleEquipment` postfixes by Harmony owner and verifies that none
  remain.
- Verifies exactly one safety postfix is active on each getter and reports
  whether a late original patch had to be removed.
- Prevents Captivity Events' original red warning spam and destructive
  character/party recovery path from running alongside the safety layer.
- Records each recovered object identity in `rgl_log` without replacing the
  red spam with another set of per-character chat messages.

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
