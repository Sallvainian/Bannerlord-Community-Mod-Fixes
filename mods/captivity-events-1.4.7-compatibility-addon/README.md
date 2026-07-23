# Captivity Events (Updated 1.4.7)

Standalone compatibility and safety add-on for Captivity Events
`v1.4.5.1400` on Mount & Blade II: Bannerlord `v1.4.7`.

**This is not a full Captivity Events fork, replacement, or content package.**
It requires the original `zCaptivityEvents` module and contains none of its
events, images, source files, or binaries.

## What it changes

- Moves Captivity Events' successful-initialization guard ahead of its
  `Harmony.PatchAll()` call so the same patches are not installed twice.
- Replaces three destructive `CharacterObject` null fallbacks with diagnostics
  and non-mutating fallback values. Missing equipment, culture, or upgrade
  targets no longer randomize the player, disable a hero, pause campaign time,
  or destroy every `CustomPartyCE_` party.
- Replaces the every-mission-tick image-atlas reload postfix with a reload that
  occurs only when mission UI categories transition into the loaded state.

The add-on uses checked runtime contracts and restores the original Captivity
Events patches if a replacement cannot be installed safely.

## Installation order

Install Captivity Events separately. Use a compiled package of this add-on and
load it immediately after `zCaptivityEvents`. Do not install this source tree
directly into Bannerlord's `Modules` directory.

## Building

Set `BANNERLORD_GAME_DIR` to a Bannerlord v1.4.7 installation and build
`Source/CaptivityEvents.147Fix/CaptivityEvents.147Fix.csproj` in Release
configuration. Captivity Events itself is discovered at runtime and is not a
compile-time or bundled repository dependency.

## License and credits

The independently written add-on source is MIT licensed. Captivity Events
remains the work of its own author and contributors under their own terms; it
is neither copied nor relicensed here. See
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
