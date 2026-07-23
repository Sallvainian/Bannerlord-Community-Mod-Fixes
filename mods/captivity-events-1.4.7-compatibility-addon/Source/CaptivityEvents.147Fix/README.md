# Captivity Events - Bannerlord 1.4.7 Safety Fix

Compatibility module for Captivity Events `v1.4.5.1400` on Bannerlord `v1.4.7`.

It moves Captivity Events' successful-initialization guard ahead of its
`Harmony.PatchAll()` call so the same patches are not installed twice. It also
replaces three destructive `CharacterObject` null fallbacks with diagnostics
and non-mutating fallback values. A missing equipment, culture, or upgrade
target will no longer randomize the player, disable a hero, pause campaign
time, or destroy every `CustomPartyCE_` party.

It also replaces Captivity Events' image-atlas reload postfix, which otherwise
runs on every mission tick, with a reload that occurs only when the mission's
UI categories transition into the loaded state. This preserves the intended
texture restoration without continuously clearing and rechecking the atlas.

The module does not edit Captivity Events or save files. Load it immediately
after `zCaptivityEvents`.
