using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace RelentlessSmithConciseBKReduxFixes
{
    internal static class PatchCoordinator
    {
        internal const string HarmonyId = "community.bannerlord.relentless-smith-concise.bk-redux.fixes";
        private const string RelentlessHarmonyId = "relentless.smith.concise";
        private const string UnsafePatchTypeName = "RelentlessSmithConcise.Patches.PerkObject_Initialize_Patch";

        private static Harmony _harmony;
        private static MethodBase _target;
        private static MethodInfo _unsafePrefix;
        private static MethodInfo _safePrefix;
        private static Patch _unsafePatchMetadata;
        private static bool _replaced;
        private static bool _applied;

        internal static void Apply()
        {
            if (_applied)
            {
                return;
            }

            _harmony = new Harmony(HarmonyId);
            try
            {
                Type unsafePatchType = AccessTools.TypeByName(UnsafePatchTypeName);
                _unsafePrefix = unsafePatchType == null
                    ? null
                    : AccessTools.DeclaredMethod(unsafePatchType, "Prefix");

                if (_unsafePrefix == null)
                {
                    FixLog.Warn("Relentless Smith's PerkObject.Initialize prefix was not found. The supplied build may already be fixed; no replacement was installed.");
                    _applied = true;
                    return;
                }
                if (!_unsafePrefix.IsStatic || _unsafePrefix.ReturnType != typeof(void))
                {
                    throw new InvalidOperationException("The Relentless Smith prefix has an unexpected method contract.");
                }

                PatchBinding[] matchingBindings = Harmony.GetAllPatchedMethods()
                    .SelectMany(GetUnsafeBindings)
                    .ToArray();
                if (matchingBindings.Length == 0)
                {
                    FixLog.Warn("Relentless Smith's unsafe prefix is not active. No replacement was necessary.");
                    _applied = true;
                    return;
                }
                if (matchingBindings.Length != 1)
                {
                    throw new InvalidOperationException(
                        "Expected one active binding for the Relentless Smith prefix, but found " + matchingBindings.Length + ".");
                }
                if (!string.Equals(matchingBindings[0].Patch.owner, RelentlessHarmonyId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The matching Relentless Smith prefix is owned by '" + matchingBindings[0].Patch.owner +
                        "' instead of the expected Harmony owner '" + RelentlessHarmonyId + "'.");
                }

                _target = matchingBindings[0].Target;
                _unsafePatchMetadata = matchingBindings[0].Patch;
                ValidateTargetContract(_target);
                _safePrefix = AccessTools.DeclaredMethod(typeof(SafePerkInitializeProxy), nameof(SafePerkInitializeProxy.Prefix));
                if (_safePrefix == null)
                {
                    throw new MissingMethodException(typeof(SafePerkInitializeProxy).FullName, nameof(SafePerkInitializeProxy.Prefix));
                }

                SafePerkInitializeProxy.Configure(_target, _unsafePrefix);
                _replaced = true;
                _harmony.Unpatch(_target, _unsafePrefix);
                if (HasUnsafePrefix(_target))
                {
                    throw new InvalidOperationException("Harmony reported that the unsafe prefix remained active after the targeted unpatch.");
                }

                // Keep the upstream owner as well as its ordering metadata so any
                // third-party before/after constraints continue to address this slot.
                new Harmony(RelentlessHarmonyId).Patch(
                    _target,
                    prefix: CopyPatchMetadata(_safePrefix, _unsafePatchMetadata));
                if (!HasPatch(_target, _safePrefix, RelentlessHarmonyId))
                {
                    throw new InvalidOperationException("The null-safe replacement prefix was not present after Harmony patching.");
                }

                _applied = true;
                FixLog.Info(
                    "Replaced only " + UnsafePatchTypeName + ".Prefix on " + Describe(_target) +
                    ". Only Relentless Smith's three rewritten vanilla perk IDs invoke the original prefix; unrelated Banner Kings perks bypass its startup-unsafe DefaultSkills lookup.");
            }
            catch (Exception ex)
            {
                RollBackPartialApply();
                FixLog.Error("Could not install the Relentless Smith / Banner Kings Redux compatibility guard.", ex);
            }
        }

        internal static void Unapply()
        {
            try
            {
                if (_target != null && _safePrefix != null && _harmony != null)
                {
                    _harmony.Unpatch(_target, _safePrefix);
                }
                RestoreOriginalPrefixIfNeeded();
                FixLog.Info("Compatibility guard removed during module unload.");
            }
            catch (Exception ex)
            {
                FixLog.Error("Could not remove the compatibility guard cleanly during module unload.", ex);
            }
            finally
            {
                SafePerkInitializeProxy.Reset();
                _harmony = null;
                _target = null;
                _unsafePrefix = null;
                _safePrefix = null;
                _unsafePatchMetadata = null;
                _replaced = false;
                _applied = false;
            }
        }

        private static void RollBackPartialApply()
        {
            try
            {
                if (_target != null && _safePrefix != null && _harmony != null)
                {
                    _harmony.Unpatch(_target, _safePrefix);
                }
            }
            catch (Exception ex)
            {
                FixLog.Error("Failed to remove a partially installed safe prefix.", ex);
            }

            try
            {
                RestoreOriginalPrefixIfNeeded();
            }
            catch (Exception ex)
            {
                FixLog.Error("Failed to restore Relentless Smith's original prefix after a partial installation.", ex);
            }
            SafePerkInitializeProxy.Reset();
        }

        private static void RestoreOriginalPrefixIfNeeded()
        {
            if (!_replaced || _target == null || _unsafePrefix == null)
            {
                return;
            }
            if (HasUnsafePrefix(_target))
            {
                _replaced = false;
                return;
            }

            new Harmony(RelentlessHarmonyId).Patch(_target, prefix: CopyPatchMetadata(_unsafePrefix, _unsafePatchMetadata));
            _replaced = false;
        }

        private static IEnumerable<PatchBinding> GetUnsafeBindings(MethodBase target)
        {
            Patches patches = Harmony.GetPatchInfo(target);
            if (patches == null)
            {
                yield break;
            }
            foreach (Patch patch in patches.Prefixes.Where(patch => patch.PatchMethod == _unsafePrefix))
            {
                yield return new PatchBinding(target, patch);
            }
        }

        private static void ValidateTargetContract(MethodBase target)
        {
            ParameterInfo[] parameters = target.GetParameters();
            string[] expectedTypes =
            {
                "System.String",
                "TaleWorlds.Core.SkillObject",
                "System.Int32",
                "TaleWorlds.CampaignSystem.CharacterDevelopment.PerkObject",
                "System.String",
                "TaleWorlds.CampaignSystem.PartyRole",
                "System.Single",
                "TaleWorlds.Core.EffectIncrementType",
                "System.String",
                "TaleWorlds.CampaignSystem.PartyRole",
                "System.Single",
                "TaleWorlds.Core.EffectIncrementType",
                "TaleWorlds.Core.TroopUsageFlags",
                "TaleWorlds.Core.TroopUsageFlags"
            };
            if (target.DeclaringType == null ||
                target.DeclaringType.FullName != "TaleWorlds.CampaignSystem.CharacterDevelopment.PerkObject" ||
                target.Name != "Initialize" ||
                parameters.Length != expectedTypes.Length ||
                !parameters.Select(parameter => parameter.ParameterType.FullName).SequenceEqual(expectedTypes) ||
                parameters.Count(parameter => parameter.Name == "skill") != 1)
            {
                throw new InvalidOperationException(
                    "The active Relentless Smith patch target is not the expected 14-argument PerkObject.Initialize overload: " +
                    Describe(target) + ".");
            }
        }

        private static HarmonyMethod CopyPatchMetadata(MethodInfo method, Patch source)
        {
            var result = new HarmonyMethod(method);
            if (source == null)
            {
                return result;
            }
            result.priority = source.priority;
            result.before = source.before == null ? null : (string[])source.before.Clone();
            result.after = source.after == null ? null : (string[])source.after.Clone();
            result.debug = source.debug;
            return result;
        }

        private static bool HasUnsafePrefix(MethodBase method)
        {
            Patches patches = Harmony.GetPatchInfo(method);
            return patches != null && patches.Prefixes.Any(patch => patch.PatchMethod == _unsafePrefix);
        }

        private static bool HasPatch(MethodBase method, MethodInfo patchMethod, string owner)
        {
            Patches patches = Harmony.GetPatchInfo(method);
            return patches != null && patches.Prefixes.Any(
                patch => patch.PatchMethod == patchMethod && string.Equals(patch.owner, owner, StringComparison.Ordinal));
        }

        private static string Describe(MethodBase method)
        {
            return (method.DeclaringType == null ? "<unknown>" : method.DeclaringType.FullName) + "." + method.Name;
        }

        private sealed class PatchBinding
        {
            internal PatchBinding(MethodBase target, Patch patch)
            {
                Target = target;
                Patch = patch;
            }

            internal MethodBase Target { get; }
            internal Patch Patch { get; }
        }
    }
}
