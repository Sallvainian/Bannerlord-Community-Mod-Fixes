# Changelog

## 1.0.0

- Added an early successful-load and re-entrancy guard around Captivity Events
  initialization.
- Replaced destructive missing-character-data fallbacks with non-mutating
  fallback values and diagnostics.
- Replaced per-tick image-atlas reloads with transition-based restoration.
- Added contract checks and rollback paths that restore upstream patches if a
  replacement cannot be installed safely.
