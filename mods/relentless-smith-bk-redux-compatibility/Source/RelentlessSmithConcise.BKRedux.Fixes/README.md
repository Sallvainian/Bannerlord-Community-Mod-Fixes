# Relentless Smith - Banner Kings Redux Compatibility

For Bannerlord 1.4.7, Banner Kings Redux 1.9.33.5, and Relentless Smith Concise 1.0.4.

Banner Kings initializes both null-skill lifestyle perks and non-null custom-skill perks before Bannerlord's vanilla `DefaultSkills` state is ready. Relentless Smith's global initialization prefix reads `DefaultSkills.Crafting` for every non-null perk, even though it only changes three vanilla smithing perks, and can therefore crash before a campaign finishes initializing.

This module leaves both upstream DLLs untouched. At the initial module screen it replaces only Relentless Smith's active `PerkObject.Initialize` prefix with a proxy that:

- skips the prefix for null-skill perks and every unrelated perk ID;
- invokes the original prefix unchanged only for `PracticalRefiner`, `PracticalSmelter`, and `PracticalSmith`;
- copies its by-reference description/name changes back to the game arguments; and
- preserves the original exception and stack behavior whenever the original prefix has relevant work.

Log: `%LOCALAPPDATA%\Mount and Blade II Bannerlord\Logs\RelentlessSmithConcise.BKRedux.Fixes.log`
