# Captivity Events - Bannerlord 1.4.7 Safety Fix

Compatibility module for Captivity Events `v1.4.5.1400` on Bannerlord `v1.4.7`.

It replaces three destructive `CharacterObject` null fallbacks with diagnostics
and non-mutating fallback values. A missing equipment, culture, or upgrade
target will no longer randomize the player, disable a hero, pause campaign
time, or destroy every `CustomPartyCE_` party.

The live Harmony table is checked again during campaign initialization. Any
original Captivity Events postfixes that were registered again are removed by
owner, and the module verifies that exactly one safety postfix remains on each
getter before it enumerates campaign characters.

Recovered object identities are written to `rgl_log` without displaying a
separate in-game message for every affected character.

It also replaces Captivity Events' image-atlas reload postfix, which otherwise
runs on every mission tick, with a reload that occurs only when the mission's
UI categories transition into the loaded state. This preserves the intended
texture restoration without continuously clearing and rechecking the atlas.

The module does not edit Captivity Events or save files. Load it immediately
after `zCaptivityEvents`.
