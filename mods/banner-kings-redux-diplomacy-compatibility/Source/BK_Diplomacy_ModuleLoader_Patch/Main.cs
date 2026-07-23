using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BK_Diplomacy_ModuleLoader_Patch
{
    /// <summary>
    /// Bridges Banner Kings Redux's legacy Diplomacy compatibility probe to the
    /// current Bannerlord.Diplomacy module and prevents Redux from bypassing
    /// Diplomacy's peace-proposal rules.
    /// </summary>
    public sealed class SubModule : MBSubModuleBase
    {
        internal const string HarmonyId = "com.sallvainian.bannerkingsredux.diplomacy.compatibility";
        private const string ReduxBattleRewardPatchType =
            "BannerKings.Patches.VanillaModelTweakPatches+BKBattleRewardTweakPatches";

        private Harmony _harmony;
        private int _installedHookCount;
        private int _satisfiedReduxRepairCount;
        private bool _reduxRepairsChecked;

        private delegate void BattleRewardPostfixDelegate(
            PartyBase party,
            float battleValue,
            float contributionShare,
            ref ExplainedNumber result);

        private static BattleRewardPostfixDelegate _reduxInfluencePostfix;
        private static BattleRewardPostfixDelegate _reduxRenownPostfix;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            _harmony = new Harmony(HarmonyId);
            InstallDiplomacyDetectionBridge();
            InstallDiplomacyGetterFallback();
            InstallForcedPeaceGuard();

            Log($"Loaded v2.1.0; installed {_installedHookCount}/3 diplomacy hooks.");
        }

        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);

            // Redux installs its own Harmony classes during OnGameStart. Waiting
            // until game initialization is finished lets us inspect the actual
            // partial-patch state and avoids doubling hooks that did succeed.
            if (!(game?.GameType is Campaign))
            {
                return;
            }

            if (_reduxRepairsChecked)
            {
                return;
            }

            _reduxRepairsChecked = true;
            InstallReduxBattleRewardRepairs();
            Log($"Banner Kings 1.4.7 repairs satisfied " +
                $"{_satisfiedReduxRepairCount}/2 reward hooks.");
        }

        protected override void OnSubModuleUnloaded()
        {
            _harmony?.UnpatchAll(HarmonyId);
            _reduxInfluencePostfix = null;
            _reduxRenownPostfix = null;
            base.OnSubModuleUnloaded();
        }

        /// <summary>
        /// Banner Kings Redux v1.9.33.5 applies the loot postfix successfully,
        /// then fails to bind its influence postfix because Bannerlord 1.4.7
        /// renamed the target arguments. Harmony stops processing that class at
        /// the failure, so its influence and renown bonuses are absent while the
        /// earlier loot hook remains active. These adapters restore only the two
        /// missing postfixes and call Redux's own implementations.
        /// </summary>
        private void InstallReduxBattleRewardRepairs()
        {
            Type reduxPatchType = AccessTools.TypeByName(ReduxBattleRewardPatchType);
            if (reduxPatchType == null)
            {
                Log("WARNING: Redux battle-reward patch type was not found.");
                return;
            }

            Type[] rewardParameters =
            {
                typeof(PartyBase),
                typeof(float),
                typeof(float),
                typeof(float),
                typeof(bool)
            };

            MethodInfo influenceTarget = AccessTools.Method(
                typeof(DefaultBattleRewardModel),
                nameof(DefaultBattleRewardModel.CalculateInfluenceGain),
                rewardParameters);
            MethodInfo renownTarget = AccessTools.Method(
                typeof(DefaultBattleRewardModel),
                nameof(DefaultBattleRewardModel.CalculateRenownGain),
                rewardParameters);

            InstallReduxRewardPostfix(
                influenceTarget,
                reduxPatchType,
                "CalculateInfluenceGainPostfix",
                nameof(ReduxInfluenceGainPostfix),
                value => _reduxInfluencePostfix = value);
            InstallReduxRewardPostfix(
                renownTarget,
                reduxPatchType,
                "CalculateRenownGainPostfix",
                nameof(ReduxRenownGainPostfix),
                value => _reduxRenownPostfix = value);
        }

        private void InstallReduxRewardPostfix(
            MethodInfo target,
            Type reduxPatchType,
            string reduxPatchMethodName,
            string adapterMethodName,
            Action<BattleRewardPostfixDelegate> setDelegate)
        {
            if (target == null)
            {
                Log("WARNING: Bannerlord reward target not found for " +
                    reduxPatchMethodName + ".");
                return;
            }

            // Harmony patch classes can fail after earlier jobs have already
            // succeeded. Inspect this exact target instead of assuming that the
            // class-level warning means every postfix is absent.
            Patches patchInfo = Harmony.GetPatchInfo(target);
            bool alreadyActive = patchInfo?.Postfixes.Any(patch =>
                string.Equals(patch.owner, "BannerKings", StringComparison.Ordinal) ||
                string.Equals(patch.owner, HarmonyId, StringComparison.Ordinal) ||
                (patch.PatchMethod?.DeclaringType?.FullName == ReduxBattleRewardPatchType &&
                 patch.PatchMethod.Name == reduxPatchMethodName)) == true;

            if (alreadyActive)
            {
                _satisfiedReduxRepairCount++;
                Log("Reward hook already active: " + target.Name);
                return;
            }

            MethodInfo reduxPatch = AccessTools.Method(reduxPatchType, reduxPatchMethodName);
            MethodInfo adapter = AccessTools.Method(typeof(SubModule), adapterMethodName);
            if (reduxPatch == null || adapter == null)
            {
                Log("WARNING: reward adapter endpoint not found for " + target.Name + ".");
                return;
            }

            try
            {
                var implementation = (BattleRewardPostfixDelegate)
                    reduxPatch.CreateDelegate(typeof(BattleRewardPostfixDelegate));
                setDelegate(implementation);
                _harmony.Patch(target, postfix: new HarmonyMethod(adapter));
                _satisfiedReduxRepairCount++;
                Log("Restored Redux reward hook: " + target.Name);
            }
            catch (Exception ex)
            {
                Log("WARNING: failed to restore " + target.Name + ": " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void InstallDiplomacyDetectionBridge()
        {
            Type modCompatType = AccessTools.TypeByName("BannerKings.Utils.ModCompat");
            MethodInfo target = modCompatType == null
                ? null
                : AccessTools.Method(modCompatType, "IsLoaded", new[] { typeof(string), typeof(string) });

            PatchPrefix(target, nameof(ModCompatIsLoadedPrefix),
                "BannerKings.Utils.ModCompat.IsLoaded(string, string)");
        }

        private void InstallDiplomacyGetterFallback()
        {
            Type modCompatType = AccessTools.TypeByName("BannerKings.Utils.ModCompat");
            MethodInfo target = modCompatType == null
                ? null
                : AccessTools.PropertyGetter(modCompatType, "DiplomacyMod");

            PatchPostfix(target, nameof(DiplomacyModGetterPostfix),
                "BannerKings.Utils.ModCompat.DiplomacyMod");
        }

        private void InstallForcedPeaceGuard()
        {
            Type behaviorType = AccessTools.TypeByName(
                "BannerKings.Behaviours.Diplomacy.BKDiplomacyBehavior");
            MethodInfo target = behaviorType == null
                ? null
                : AccessTools.Method(behaviorType, "ForceProposePeaceFromLosingSide");

            PatchPrefix(target, nameof(SkipReduxForcedPeacePrefix),
                "BannerKings.Behaviours.Diplomacy.BKDiplomacyBehavior." +
                "ForceProposePeaceFromLosingSide()");
        }

        private void PatchPrefix(MethodInfo target, string patchMethodName, string targetDescription)
        {
            if (target == null)
            {
                Log("WARNING: target not found: " + targetDescription);
                return;
            }

            MethodInfo patch = AccessTools.Method(typeof(SubModule), patchMethodName);
            _harmony.Patch(target, prefix: new HarmonyMethod(patch));
            _installedHookCount++;
            Log("Patched " + targetDescription);
        }

        private void PatchPostfix(MethodInfo target, string patchMethodName, string targetDescription)
        {
            if (target == null)
            {
                Log("WARNING: target not found: " + targetDescription);
                return;
            }

            MethodInfo patch = AccessTools.Method(typeof(SubModule), patchMethodName);
            _harmony.Patch(target, postfix: new HarmonyMethod(patch));
            _installedHookCount++;
            Log("Patched " + targetDescription);
        }

        /// <summary>
        /// Redux v1.9.33.5 probes the obsolete identifiers "Diplomacy" and
        /// "DiplomacyFixes". The compatibility module has hard dependencies on
        /// BannerKings.Redux and Bannerlord.Diplomacy, so this exact legacy probe
        /// can safely resolve true without changing detection for any other mod.
        /// </summary>
        [HarmonyPriority(Priority.First)]
        private static bool ModCompatIsLoadedPrefix(
            string moduleId,
            string assemblyName,
            ref bool __result)
        {
            if (!string.Equals(moduleId, "Diplomacy", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(assemblyName, "DiplomacyFixes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            __result = true;
            return false;
        }

        /// <summary>
        /// Defensive fallback for Redux builds that inline or refactor IsLoaded
        /// while retaining the public DiplomacyMod compatibility property.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        private static void DiplomacyModGetterPostfix(ref bool __result)
        {
            __result = true;
        }

        /// <summary>
        /// Diplomacy owns war/peace proposal conditions and cooldowns. Redux's
        /// direct AddDecision path bypasses those checks, so do not run it while
        /// this required compatibility module is active.
        /// </summary>
        [HarmonyPriority(Priority.First)]
        private static bool SkipReduxForcedPeacePrefix()
        {
            return false;
        }

        /// <summary>
        /// Positional __0 binding avoids repeating the renamed Bannerlord
        /// parameter. Redux's implementation does not use the two float inputs,
        /// so zero placeholders preserve its exact perk adjustment behavior.
        /// </summary>
        private static void ReduxInfluenceGainPostfix(
            PartyBase __0,
            ref ExplainedNumber __result)
        {
            _reduxInfluencePostfix?.Invoke(__0, 0f, 0f, ref __result);
        }

        private static void ReduxRenownGainPostfix(
            PartyBase __0,
            ref ExplainedNumber __result)
        {
            _reduxRenownPostfix?.Invoke(__0, 0f, 0f, ref __result);
        }

        private static void Log(string message)
        {
            try
            {
                Debug.Print("[BK-Diplomacy Compatibility] " + message);
            }
            catch
            {
                // Diagnostics must never prevent the game from loading.
            }
        }
    }
}
