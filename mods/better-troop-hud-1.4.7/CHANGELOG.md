# Changelog

This changelog records the Bannerlord 1.4.7 compatibility port, not the full
upstream BetterTroopHUD history.

## 1.1.0

- Skipped BetterTroopHUD movie creation in friendly civilian missions that do
  not provide a battle HUD.
- Prevented the resulting null-reference crash when entering affected cities.
- Retained the Bannerlord 1.4.7 mission-view, Gauntlet-layer, suspend/resume,
  photo-mode, and hidden-UI compatibility changes in the port.
- Kept the separate `BetterTroopHUD.147` module ID and original MCM settings ID.
