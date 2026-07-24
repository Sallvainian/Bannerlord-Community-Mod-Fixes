using System;
using System.Reflection;
using System.Threading;

namespace RelentlessSmithConciseBKReduxFixes
{
    internal static class SmeltingSelectionProxy
    {
        private static readonly object Sync = new object();

        private static PropertyInfo _currentSelectedItemProperty;
        private static PropertyInfo _isSelectedProperty;
        private static MethodInfo _onItemSelection;
        private static bool _configured;
        private static int _runtimeFailureLogged;

        internal static void Configure(Type smeltingVmType)
        {
            if (smeltingVmType == null)
            {
                throw new ArgumentNullException(nameof(smeltingVmType));
            }

            PropertyInfo currentSelectedItemProperty = smeltingVmType.GetProperty(
                "CurrentSelectedItem",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (currentSelectedItemProperty == null ||
                !currentSelectedItemProperty.CanRead ||
                !currentSelectedItemProperty.CanWrite ||
                currentSelectedItemProperty.GetIndexParameters().Length != 0)
            {
                throw new InvalidOperationException(
                    "SmeltingVM does not expose the expected readable and writable CurrentSelectedItem property.");
            }

            Type smeltingItemType = currentSelectedItemProperty.PropertyType;
            PropertyInfo isSelectedProperty = smeltingItemType.GetProperty(
                "IsSelected",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (isSelectedProperty == null ||
                !isSelectedProperty.CanRead ||
                isSelectedProperty.PropertyType != typeof(bool) ||
                isSelectedProperty.GetIndexParameters().Length != 0)
            {
                throw new InvalidOperationException(
                    "SmeltingItemVM does not expose the expected Boolean IsSelected property.");
            }

            MethodInfo onItemSelection = smeltingVmType.GetMethod(
                "OnItemSelection",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { smeltingItemType },
                modifiers: null);
            if (onItemSelection == null || onItemSelection.ReturnType != typeof(void))
            {
                throw new InvalidOperationException(
                    "SmeltingVM does not expose the expected OnItemSelection(SmeltingItemVM) method.");
            }

            lock (Sync)
            {
                _currentSelectedItemProperty = currentSelectedItemProperty;
                _isSelectedProperty = isSelectedProperty;
                _onItemSelection = onItemSelection;
                _runtimeFailureLogged = 0;
                _configured = true;
            }
        }

        internal static void Reset()
        {
            lock (Sync)
            {
                _configured = false;
                _currentSelectedItemProperty = null;
                _isSelectedProperty = null;
                _onItemSelection = null;
                _runtimeFailureLogged = 0;
            }
        }

        public static void Postfix(object __instance)
        {
            PropertyInfo currentSelectedItemProperty;
            PropertyInfo isSelectedProperty;
            MethodInfo onItemSelection;

            lock (Sync)
            {
                if (!_configured || __instance == null)
                {
                    return;
                }
                currentSelectedItemProperty = _currentSelectedItemProperty;
                isSelectedProperty = _isSelectedProperty;
                onItemSelection = _onItemSelection;
            }

            object currentItem = null;
            try
            {
                currentItem = currentSelectedItemProperty.GetValue(__instance, null);
                if (currentItem == null || (bool)isSelectedProperty.GetValue(currentItem, null))
                {
                    return;
                }

                // Relentless Smith refreshes the list and assigns the first row to
                // CurrentSelectedItem, then clears its own multi-selection state and
                // leaves every row visually unselected. Bannerlord's selection
                // method only marks a row selected when it differs from
                // CurrentSelectedItem, so clear the stale reference first and then
                // let Bannerlord perform its normal selection transition.
                currentSelectedItemProperty.SetValue(__instance, null, null);
                onItemSelection.Invoke(__instance, new[] { currentItem });

                object restoredItem = currentSelectedItemProperty.GetValue(__instance, null);
                if (!ReferenceEquals(restoredItem, currentItem) ||
                    !(bool)isSelectedProperty.GetValue(currentItem, null))
                {
                    throw new InvalidOperationException(
                        "Bannerlord's selection callback did not restore the current smelting row.");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    if (currentItem != null &&
                        currentSelectedItemProperty.GetValue(__instance, null) == null)
                    {
                        currentSelectedItemProperty.SetValue(__instance, currentItem, null);
                    }
                }
                catch
                {
                    // Preserve the original failure for the one-time diagnostic.
                }

                if (Interlocked.Exchange(ref _runtimeFailureLogged, 1) == 0)
                {
                    FixLog.Error(
                        "Could not restore the selected smelting row after Relentless Smith refreshed the list.",
                        ex);
                }
            }
        }
    }
}
