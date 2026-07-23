using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace RelentlessSmithConciseBKReduxFixes
{
    internal static class SafePerkInitializeProxy
    {
        private static readonly object Sync = new object();
        private static readonly HashSet<string> RewrittenPerkIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "PracticalRefiner",
            "PracticalSmelter",
            "PracticalSmith"
        };
        private const string RelentlessPerkPatchTypeName = "RelentlessSmithConcise.Patches.PerkObject_Initialize_Patch";

        private static MethodInfo _originalPrefix;
        private static ParameterInfo[] _prefixParameters;
        private static int[] _targetArgumentByPrefixParameter;
        private static PropertyInfo _instanceStringIdProperty;
        private static int _skillArgumentIndex = -1;
        private static bool _restrictToRewrittenPerks;
        private static bool _configured;

        internal static void Configure(MethodBase target, MethodInfo originalPrefix)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (originalPrefix == null)
            {
                throw new ArgumentNullException(nameof(originalPrefix));
            }

            ParameterInfo[] targetParameters = target.GetParameters();
            ParameterInfo[] prefixParameters = originalPrefix.GetParameters();
            var targetIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < targetParameters.Length; index++)
            {
                if (string.IsNullOrEmpty(targetParameters[index].Name))
                {
                    throw new InvalidOperationException("The PerkObject.Initialize target has an unnamed argument.");
                }
                targetIndexByName[targetParameters[index].Name] = index;
            }

            int[] targetArgumentByPrefixParameter = Enumerable.Repeat(-1, prefixParameters.Length).ToArray();
            for (int index = 0; index < prefixParameters.Length; index++)
            {
                string name = prefixParameters[index].Name;
                if (name == "__instance")
                {
                    continue;
                }
                if (string.IsNullOrEmpty(name) || !targetIndexByName.TryGetValue(name, out int targetIndex))
                {
                    throw new InvalidOperationException("Cannot map Relentless Smith prefix argument '" + (name ?? "<null>") + "' to the target method.");
                }
                targetArgumentByPrefixParameter[index] = targetIndex;
            }

            if (!targetIndexByName.TryGetValue("skill", out int skillArgumentIndex))
            {
                throw new InvalidOperationException("The target PerkObject.Initialize overload has no argument named 'skill'.");
            }

            bool restrictToRewrittenPerks = originalPrefix.DeclaringType != null &&
                string.Equals(originalPrefix.DeclaringType.FullName, RelentlessPerkPatchTypeName, StringComparison.Ordinal);
            PropertyInfo instanceStringIdProperty = null;
            if (restrictToRewrittenPerks)
            {
                instanceStringIdProperty = target.DeclaringType?.GetProperty(
                    "StringId",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (instanceStringIdProperty == null ||
                    instanceStringIdProperty.PropertyType != typeof(string) ||
                    instanceStringIdProperty.GetIndexParameters().Length != 0)
                {
                    throw new InvalidOperationException("The PerkObject.Initialize target does not expose the expected StringId property.");
                }
            }

            lock (Sync)
            {
                _originalPrefix = originalPrefix;
                _prefixParameters = prefixParameters;
                _targetArgumentByPrefixParameter = targetArgumentByPrefixParameter;
                _instanceStringIdProperty = instanceStringIdProperty;
                _skillArgumentIndex = skillArgumentIndex;
                _restrictToRewrittenPerks = restrictToRewrittenPerks;
                _configured = true;
            }
        }

        internal static void Reset()
        {
            lock (Sync)
            {
                _configured = false;
                _originalPrefix = null;
                _prefixParameters = null;
                _targetArgumentByPrefixParameter = null;
                _instanceStringIdProperty = null;
                _skillArgumentIndex = -1;
                _restrictToRewrittenPerks = false;
            }
        }

        public static void Prefix(object __instance, object[] __args)
        {
            MethodInfo originalPrefix;
            ParameterInfo[] prefixParameters;
            int[] targetArgumentByPrefixParameter;
            PropertyInfo instanceStringIdProperty;
            int skillArgumentIndex;
            bool restrictToRewrittenPerks;

            lock (Sync)
            {
                if (!_configured)
                {
                    return;
                }
                originalPrefix = _originalPrefix;
                prefixParameters = _prefixParameters;
                targetArgumentByPrefixParameter = _targetArgumentByPrefixParameter;
                instanceStringIdProperty = _instanceStringIdProperty;
                skillArgumentIndex = _skillArgumentIndex;
                restrictToRewrittenPerks = _restrictToRewrittenPerks;
            }

            // Relentless Smith only rewrites three vanilla crafting perks. Banner
            // Kings initializes many unrelated custom perks here, including both
            // null-skill lifestyle perks and non-null custom-skill perks. Do not run
            // Relentless Smith's DefaultSkills.Crafting lookup for either group.
            if (__args == null || skillArgumentIndex < 0 || skillArgumentIndex >= __args.Length || __args[skillArgumentIndex] == null)
            {
                return;
            }
            if (restrictToRewrittenPerks)
            {
                string perkId = (string)instanceStringIdProperty.GetValue(__instance, null);
                if (!RewrittenPerkIds.Contains(perkId))
                {
                    return;
                }
            }

            object[] invokeArguments = new object[prefixParameters.Length];
            for (int index = 0; index < prefixParameters.Length; index++)
            {
                int targetIndex = targetArgumentByPrefixParameter[index];
                invokeArguments[index] = targetIndex < 0 ? __instance : __args[targetIndex];
            }

            try
            {
                originalPrefix.Invoke(null, invokeArguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Preserve the upstream prefix's exception type and stack when it
                // is executing for one of the three perks it actually modifies.
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
            finally
            {
                for (int index = 0; index < prefixParameters.Length; index++)
                {
                    int targetIndex = targetArgumentByPrefixParameter[index];
                    if (targetIndex >= 0 && prefixParameters[index].ParameterType.IsByRef)
                    {
                        __args[targetIndex] = invokeArguments[index];
                    }
                }
            }
        }
    }
}
