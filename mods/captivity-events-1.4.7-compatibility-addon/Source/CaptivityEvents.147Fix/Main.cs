using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CaptivityEvents_147Fix
{
    public sealed class SubModule : MBSubModuleBase
    {
        internal const string HarmonyId = "com.sallvainian.captivityevents.147fix";
        private const string CaptivityEventsHarmonyId = "com.CE.captivityEvents";
        private const string CaptivityEventsSubModuleType = "CaptivityEvents.CESubModule";
        private const string CaptivityEventsCharacterPatchType =
            "CaptivityEvents.Patches.CEPatchCharacterObject";
        private const string CaptivityEventsMissionCategoryPatchType =
            "CaptivityEvents.Patches.CEPatchMissionGauntletCategoryLoadManager";
        private const string MissionCategoryLoadManagerType =
            "TaleWorlds.MountAndBlade.GauntletUI.Mission.MissionGauntletCategoryLoadManager";
        private const string ObservedCharacterId = "1073743824";

        private static readonly object WarningLock = new object();
        private static readonly object MissionImageReloadLock = new object();
        private static readonly HashSet<string> ReportedCharacterProblems =
            new HashSet<string>(StringComparer.Ordinal);

        private static MethodInfo _captivityEventsInstanceGetter;
        private static MethodInfo _captivityEventsReloadImagesMethod;
        private static Mission _lastMissionWithLoadedImageCategories;

        private Harmony _harmony;
        private bool _characterSafetyInstalled;
        private bool _missionImageSafetyInstalled;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            _harmony = new Harmony(HarmonyId);
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InstallCharacterSafetyPatches();
            InstallMissionImageSafetyPatch();
        }

        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            if (!(game?.GameType is Campaign))
            {
                return;
            }

            try
            {
                CharacterObject observed = CharacterObject.All.FirstOrDefault(
                    character => string.Equals(
                        character?.Id.ToString(),
                        ObservedCharacterId,
                        StringComparison.Ordinal));

                Log(observed == null
                    ? "Observed CharacterObject " + ObservedCharacterId +
                      " was not present in this campaign."
                    : "Observed CharacterObject resolved: " + DescribeCharacter(observed));
            }
            catch (Exception ex)
            {
                Log("WARNING: failed to resolve observed CharacterObject: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            _harmony?.UnpatchAll(HarmonyId);
            base.OnSubModuleUnloaded();
        }

        private void InstallCharacterSafetyPatches()
        {
            if (_characterSafetyInstalled)
            {
                return;
            }

            Type cePatchType = AccessTools.TypeByName(CaptivityEventsCharacterPatchType);
            if (cePatchType == null)
            {
                Log("WARNING: Captivity Events CharacterObject patch type was not found.");
                return;
            }

            var contracts = new[]
            {
                new CharacterPatchContract(
                    "UpgradeTargets",
                    ResolveDeclaredGetter(
                        typeof(CharacterObject),
                        "UpgradeTargets",
                        typeof(CharacterObject[])),
                    AccessTools.Method(cePatchType, "UpgradeTargets"),
                    AccessTools.Method(typeof(SubModule), nameof(SafeUpgradeTargetsPostfix))),
                new CharacterPatchContract(
                    "Culture",
                    ResolveDeclaredGetter(
                        typeof(CharacterObject),
                        "Culture",
                        typeof(CultureObject)),
                    AccessTools.Method(cePatchType, "Culture"),
                    AccessTools.Method(typeof(SubModule), nameof(SafeCulturePostfix))),
                new CharacterPatchContract(
                    "FirstBattleEquipment",
                    ResolveDeclaredGetter(
                        typeof(CharacterObject),
                        "FirstBattleEquipment",
                        typeof(Equipment)),
                    AccessTools.Method(cePatchType, "FirstBattleEquipment"),
                    AccessTools.Method(typeof(SubModule), nameof(SafeFirstBattleEquipmentPostfix)))
            };

            if (contracts.Any(contract => !contract.IsValid ||
                !HasExactCaptivityEventsPostfix(contract.Target, contract.CaptivityEventsPostfix)))
            {
                Log("WARNING: one or more Captivity Events CharacterObject contracts did not " +
                    "match the live Harmony table; no destructive fallback was changed.");
                return;
            }

            try
            {
                foreach (CharacterPatchContract contract in contracts)
                {
                    _harmony.Unpatch(contract.Target, contract.CaptivityEventsPostfix);
                }

                foreach (CharacterPatchContract contract in contracts)
                {
                    _harmony.Patch(
                        contract.Target,
                        postfix: new HarmonyMethod(contract.SafePostfix)
                        {
                            priority = Priority.Last
                        });
                }

                _characterSafetyInstalled = true;
                Log("Replaced 3/3 destructive CharacterObject fallbacks with safe diagnostics.");
            }
            catch (Exception ex)
            {
                foreach (CharacterPatchContract contract in contracts)
                {
                    try
                    {
                        _harmony.Unpatch(contract.Target, contract.SafePostfix);
                        new Harmony(CaptivityEventsHarmonyId).Patch(
                            contract.Target,
                            postfix: new HarmonyMethod(contract.CaptivityEventsPostfix));
                    }
                    catch
                    {
                        // Keep trying to restore the remaining original CE patches.
                    }
                }

                Log("ERROR: safe CharacterObject patch transaction failed; attempted to " +
                    "restore Captivity Events originals. " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void InstallMissionImageSafetyPatch()
        {
            if (_missionImageSafetyInstalled)
            {
                return;
            }

            Type cePatchType = AccessTools.TypeByName(CaptivityEventsMissionCategoryPatchType);
            Type targetType = AccessTools.TypeByName(MissionCategoryLoadManagerType);
            Type ceSubModuleType = AccessTools.TypeByName(CaptivityEventsSubModuleType);

            MethodInfo target = targetType == null
                ? null
                : AccessTools.Method(targetType, "LoadUnloadAllCategories", new[] { typeof(bool) });
            MethodInfo cePostfix = cePatchType == null
                ? null
                : AccessTools.Method(cePatchType, "LoadUnloadAllCategoriesPostfix", new[] { typeof(bool) });
            MethodInfo safePostfix = AccessTools.Method(
                typeof(SubModule), nameof(SafeLoadUnloadAllCategoriesPostfix));
            MethodInfo instanceGetter = ceSubModuleType == null
                ? null
                : ResolveDeclaredGetter(
                    ceSubModuleType,
                    "Instance",
                    ceSubModuleType);
            MethodInfo reloadImages = ceSubModuleType == null
                ? null
                : AccessTools.Method(ceSubModuleType, "ReloadImagesAgain", Type.EmptyTypes);

            bool contractMatches = target != null && target.ReturnType == typeof(void) &&
                                   cePostfix != null && cePostfix.ReturnType == typeof(void) &&
                                   safePostfix != null &&
                                   instanceGetter != null && instanceGetter.IsStatic &&
                                   ceSubModuleType.IsAssignableFrom(instanceGetter.ReturnType) &&
                                   reloadImages != null && !reloadImages.IsStatic &&
                                   reloadImages.ReturnType == typeof(void) &&
                                   HasExactCaptivityEventsPostfix(target, cePostfix);

            if (!contractMatches)
            {
                Log("WARNING: Captivity Events mission-image patch contract did not match; " +
                    "its original behavior was left unchanged.");
                return;
            }

            try
            {
                _captivityEventsInstanceGetter = instanceGetter;
                _captivityEventsReloadImagesMethod = reloadImages;
                _harmony.Unpatch(target, cePostfix);
                _harmony.Patch(
                    target,
                    postfix: new HarmonyMethod(safePostfix)
                    {
                        priority = Priority.Last
                    });
                _missionImageSafetyInstalled = true;
                Log("Replaced per-tick Captivity Events image reload with transition-based reload.");
            }
            catch (Exception ex)
            {
                try
                {
                    _harmony.Unpatch(target, safePostfix);
                    new Harmony(CaptivityEventsHarmonyId).Patch(
                        target,
                        postfix: new HarmonyMethod(cePostfix));
                }
                catch
                {
                    // Preserve the original exception while making a best effort to restore CE.
                }

                _captivityEventsInstanceGetter = null;
                _captivityEventsReloadImagesMethod = null;
                Log("ERROR: mission-image patch transaction failed; attempted to restore " +
                    "Captivity Events original. " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static MethodInfo ResolveDeclaredGetter(
            Type declaringType,
            string propertyName,
            Type returnType)
        {
            MethodInfo[] matches = declaringType
                .GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly)
                .Where(method =>
                    string.Equals(
                        method.Name,
                        "get_" + propertyName,
                        StringComparison.Ordinal) &&
                    method.ReturnType == returnType &&
                    method.GetParameters().Length == 0)
                .ToArray();

            return matches.Length == 1 ? matches[0] : null;
        }

        private static bool HasExactCaptivityEventsPostfix(MethodInfo target, MethodInfo patch)
        {
            Patches patchInfo = Harmony.GetPatchInfo(target);
            return patchInfo?.Postfixes.Any(entry =>
                string.Equals(entry.owner, CaptivityEventsHarmonyId, StringComparison.Ordinal) &&
                entry.PatchMethod == patch) == true;
        }

        [HarmonyPriority(Priority.Last)]
        private static void SafeUpgradeTargetsPostfix(
            CharacterObject __instance,
            ref CharacterObject[] __result)
        {
            if (__result != null)
            {
                return;
            }

            ReportCharacterProblem("UpgradeTargets", __instance);
            __result = Array.Empty<CharacterObject>();
        }

        [HarmonyPriority(Priority.Last)]
        private static void SafeCulturePostfix(
            CharacterObject __instance,
            ref CultureObject __result)
        {
            if (__result != null)
            {
                return;
            }

            ReportCharacterProblem("Culture", __instance);
            try
            {
                __result = MBObjectManager.Instance?.GetObject<CultureObject>("empire");
            }
            catch
            {
                __result = null;
            }

            if (__result == null)
            {
                __result = new CultureObject();
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static void SafeFirstBattleEquipmentPostfix(
            CharacterObject __instance,
            ref Equipment __result)
        {
            if (__result != null)
            {
                return;
            }

            ReportCharacterProblem("FirstBattleEquipment", __instance);
            __result = new Equipment(Equipment.EquipmentType.Battle);
        }

        [HarmonyPriority(Priority.Last)]
        private static void SafeLoadUnloadAllCategoriesPostfix(bool load)
        {
            try
            {
                Mission mission = Mission.Current;
                bool shouldReload = false;

                lock (MissionImageReloadLock)
                {
                    if (!load || mission == null)
                    {
                        _lastMissionWithLoadedImageCategories = null;
                        return;
                    }

                    if (!ReferenceEquals(_lastMissionWithLoadedImageCategories, mission))
                    {
                        _lastMissionWithLoadedImageCategories = mission;
                        shouldReload = true;
                    }
                }

                if (!shouldReload)
                {
                    return;
                }

                object ceInstance = _captivityEventsInstanceGetter?.Invoke(null, null);
                if (ceInstance == null || _captivityEventsReloadImagesMethod == null)
                {
                    Log("WARNING: skipped Captivity Events image restoration because its " +
                        "submodule instance was unavailable.");
                    return;
                }

                _captivityEventsReloadImagesMethod.Invoke(ceInstance, null);
                Log("Restored Captivity Events images once for a mission/category-load transition.");
            }
            catch (Exception ex)
            {
                lock (MissionImageReloadLock)
                {
                    _lastMissionWithLoadedImageCategories = null;
                }

                Exception cause = ex is TargetInvocationException invocation &&
                                  invocation.InnerException != null
                    ? invocation.InnerException
                    : ex;
                Log("WARNING: Captivity Events image restoration failed safely: " +
                    cause.GetType().Name + ": " + cause.Message);
            }
        }

        private static void ReportCharacterProblem(string propertyName, CharacterObject character)
        {
            string identity = DescribeCharacter(character);
            string key = propertyName + ":" + SafeValue(() => character?.Id.ToString());

            lock (WarningLock)
            {
                if (!ReportedCharacterProblems.Add(key))
                {
                    return;
                }
            }

            Log("SAFE FALLBACK: null CharacterObject." + propertyName + "; " + identity);
            try
            {
                string label = SafeValue(() => character?.Name?.ToString());
                string stringId = SafeValue(() => character?.StringId);
                InformationManager.DisplayMessage(new InformationMessage(
                    "Captivity Events safe fallback: " + propertyName +
                    " was null for " + label + " [" + stringId + "]. See rgl_log.",
                    Colors.Yellow));
            }
            catch
            {
                // A diagnostic message must never interrupt mission creation.
            }
        }

        private static string DescribeCharacter(CharacterObject character)
        {
            if (character == null)
            {
                return "character=<null>";
            }

            return "id=" + SafeValue(() => character.Id.ToString()) +
                   ", stringId=" + SafeValue(() => character.StringId) +
                   ", name=" + SafeValue(() => character.Name?.ToString()) +
                   ", isHero=" + SafeValue(() => character.IsHero.ToString()) +
                   ", hero=" + SafeValue(() => character.HeroObject?.Name?.ToString()) +
                   ", isPlayer=" + SafeValue(() => character.IsPlayerCharacter.ToString());
        }

        private static string SafeValue(Func<string> valueFactory)
        {
            try
            {
                string value = valueFactory();
                return string.IsNullOrWhiteSpace(value) ? "<none>" : value;
            }
            catch (Exception ex)
            {
                return "<error:" + ex.GetType().Name + ">";
            }
        }

        private static void Log(string message)
        {
            try
            {
                Debug.Print("[CaptivityEvents.147Fix] " + message);
            }
            catch
            {
                // Diagnostics must never prevent the game from loading.
            }
        }

        private sealed class CharacterPatchContract
        {
            internal CharacterPatchContract(
                string name,
                MethodInfo target,
                MethodInfo captivityEventsPostfix,
                MethodInfo safePostfix)
            {
                Name = name;
                Target = target;
                CaptivityEventsPostfix = captivityEventsPostfix;
                SafePostfix = safePostfix;
            }

            internal string Name { get; }
            internal MethodInfo Target { get; }
            internal MethodInfo CaptivityEventsPostfix { get; }
            internal MethodInfo SafePostfix { get; }

            internal bool IsValid => Target != null &&
                                     CaptivityEventsPostfix != null &&
                                     SafePostfix != null;
        }
    }
}
