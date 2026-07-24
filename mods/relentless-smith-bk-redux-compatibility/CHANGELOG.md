# Changelog

## 1.0.2

- Restored Bannerlord's automatic next-item selection after Relentless Smith
  smelts one or more selected rows.
- Preserved Relentless Smith's bulk, Ctrl-selection, and stack-smelting
  behavior while repairing only the visually cleared current row.
- Added exact runtime-contract checks for Relentless Smith's
  `SmeltingVM.TrySmeltingSelectedItems` prefix and Bannerlord's
  `OnItemSelection` callback.
- Extended the proxy verifier to cover reselection, Harmony ordering, and clean
  unload rollback.

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
