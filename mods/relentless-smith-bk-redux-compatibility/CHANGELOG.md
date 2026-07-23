# Changelog

## 1.0.1

- Added a contract-checked proxy for Relentless Smith Concise's active
  `PerkObject.Initialize` prefix.
- Limited the original prefix to the three vanilla smithing perks it rewrites.
- Bypassed startup-unsafe `DefaultSkills` access for null-skill and unrelated
  Banner Kings Redux perks.
- Preserved relevant by-reference argument changes and upstream exception
  behavior.
- Added safe rollback if proxy installation fails and a source verifier for the
  expected runtime contract.
