# Captivity Events (Updated 1.4.7)

Source for the compatibility and safety layer embedded in the consolidated
Captivity Events `v1.4.7.2` release for Mount & Blade II: Bannerlord `v1.4.7`.

End users install one `zCaptivityEvents` module from the release ZIP. They do
not install or enable a separate `CaptivityEvents.147Fix` module. The standalone
manifest in this source directory exists only for development and testing.

The repository does not duplicate Captivity Events' events, images, or
binaries. Those upstream files remain under their original terms and are
included only in the consolidated release package.

## What it changes

- Replaces three destructive `CharacterObject` null fallbacks with diagnostics
  and non-mutating fallback values. Missing equipment, culture, or upgrade
  targets no longer randomize the player, disable a hero, pause campaign time,
  or destroy every `CustomPartyCE_` party.
- Replaces the every-mission-tick image-atlas reload postfix with a reload that
  occurs only when mission UI categories transition into the loaded state.
- Resolves the exact Bannerlord `CharacterObject` getters instead of using an
  ambiguous inherited-property lookup.

The add-on uses checked runtime contracts and restores the original Captivity
Events patches if a replacement cannot be installed safely.

## Installation

Extract the consolidated release into Bannerlord's `Modules` directory and
enable the single `zCaptivityEvents` module. Do not install this source tree
directly and do not combine the consolidated release with the old standalone
safety-fix ZIP.

Bannerlord.Harmony `v2.4.2` and Mod Configuration Menu `v5.11.4` or newer are
required. The MCM dependency is intentionally retained to provide the original
mod's configuration experience.

## Building

Set `BANNERLORD_GAME_DIR` to a Bannerlord v1.4.7 installation and build
`Source/CaptivityEvents.147Fix/CaptivityEvents.147Fix.csproj` in Release
configuration. Embed the resulting DLL as the second submodule inside the
updated `zCaptivityEvents` package. Captivity Events itself is discovered at
runtime and is not a compile-time repository dependency.

## License and credits

The independently written add-on source is MIT licensed. Captivity Events was
created by BadListener and uploaded to Nexus Mods by TheBadListener; it remains
their work under its original terms and is neither copied nor relicensed here.
See
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
