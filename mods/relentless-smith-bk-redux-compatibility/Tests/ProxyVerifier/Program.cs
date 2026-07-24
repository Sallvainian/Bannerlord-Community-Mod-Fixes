using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ProxyVerifier
{
    internal static class Program
    {
        private static readonly List<string> SearchDirectories = new List<string>();

        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: ProxyVerifier FIX_DLL RELENTLESS_DLL GAME_ROOT");
                return 2;
            }

            string fixPath = Path.GetFullPath(args[0]);
            string relentlessPath = Path.GetFullPath(args[1]);
            string gameRoot = Path.GetFullPath(args[2]);
            if (!File.Exists(fixPath) || !File.Exists(relentlessPath) || !Directory.Exists(gameRoot))
            {
                Console.Error.WriteLine("One or more required paths do not exist.");
                return 2;
            }

            AddSearchDirectory(Path.GetDirectoryName(fixPath));
            AddSearchDirectory(Path.GetDirectoryName(relentlessPath));
            AddSearchDirectory(Path.Combine(gameRoot, "bin", "Win64_Shipping_Client"));
            AddModuleSearchDirectories(Path.Combine(gameRoot, "Modules"));

            string steamApps = Directory.GetParent(Directory.GetParent(gameRoot).FullName).FullName;
            AddModuleSearchDirectories(Path.Combine(steamApps, "workshop", "content", "261550"));
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            try
            {
                Assembly fixAssembly = Assembly.LoadFrom(fixPath);
                Type proxyType = RequireType(fixAssembly, "RelentlessSmithConciseBKReduxFixes.SafePerkInitializeProxy");
                MethodInfo configure = RequireMethod(proxyType, "Configure", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo reset = RequireMethod(proxyType, "Reset", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo safePrefix = RequireMethod(proxyType, "Prefix", BindingFlags.Static | BindingFlags.Public);
                Type selectionProxyType = RequireType(
                    fixAssembly,
                    "RelentlessSmithConciseBKReduxFixes.SmeltingSelectionProxy");
                MethodInfo configureSelection = RequireMethod(
                    selectionProxyType,
                    "Configure",
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo resetSelection = RequireMethod(
                    selectionProxyType,
                    "Reset",
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo selectionPostfix = RequireMethod(
                    selectionProxyType,
                    "Postfix",
                    BindingFlags.Static | BindingFlags.Public);

                VerifyActualContracts(relentlessPath, configure, safePrefix, reset);
                VerifyProxyBehavior(configure, safePrefix, reset);
                VerifyActualSmeltingContract(configureSelection, resetSelection);
                VerifySmeltingSelectionBehavior(configureSelection, selectionPostfix, resetSelection);
                VerifyCoordinatorReplacement(
                    fixAssembly,
                    relentlessPath,
                    safePrefix,
                    selectionPostfix);

                Console.WriteLine("PASS: perk contract binding and replacement, smelting contract binding, automatic next-item reselection, metadata preservation, and rollback verified.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: " + ex);
                return 1;
            }
        }

        private static void VerifyActualContracts(
            string relentlessPath,
            MethodInfo configure,
            MethodInfo safePrefix,
            MethodInfo reset)
        {
            Assembly relentlessAssembly = Assembly.LoadFrom(relentlessPath);
            Type unsafeType = RequireType(relentlessAssembly, "RelentlessSmithConcise.Patches.PerkObject_Initialize_Patch");
            MethodInfo unsafePrefix = RequireMethod(unsafeType, "Prefix", BindingFlags.Static | BindingFlags.NonPublic);
            Type perkType = RequireType(
                Assembly.Load("TaleWorlds.CampaignSystem"),
                "TaleWorlds.CampaignSystem.CharacterDevelopment.PerkObject");

            MethodInfo[] candidates = perkType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.Name == "Initialize")
                .Where(method => method.GetParameters().Any(parameter => parameter.Name == "skill"))
                .Where(method => unsafePrefix.GetParameters().Count(parameter => parameter.Name != "__instance") == method.GetParameters().Length)
                .ToArray();
            if (candidates.Length != 1)
            {
                throw new InvalidOperationException("Expected one actual PerkObject.Initialize contract but found " + candidates.Length + ".");
            }

            MethodInfo target = candidates[0];
            configure.Invoke(null, new object[] { target, unsafePrefix });
            try
            {
                object[] targetArguments = new object[target.GetParameters().Length];
                int skillIndex = Array.FindIndex(target.GetParameters(), parameter => parameter.Name == "skill");
                targetArguments[skillIndex] = null;
                safePrefix.Invoke(null, new object[] { null, targetArguments });

                Type skillType = RequireType(Assembly.Load("TaleWorlds.Core"), "TaleWorlds.Core.SkillObject");
                object nonVanillaSkill = Activator.CreateInstance(skillType, new object[] { "VerifierCustomSkill" });
                object unrelatedPerk = Activator.CreateInstance(perkType, new object[] { "LifestyleVerifierPerk" });
                targetArguments[skillIndex] = nonVanillaSkill;
                safePrefix.Invoke(null, new[] { unrelatedPerk, targetArguments });
            }
            finally
            {
                reset.Invoke(null, null);
            }
        }

        private static void VerifyActualSmeltingContract(
            MethodInfo configureSelection,
            MethodInfo resetSelection)
        {
            Type smeltingVmType = RequireType(
                Assembly.Load("TaleWorlds.CampaignSystem.ViewModelCollection"),
                "TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting.SmeltingVM");
            configureSelection.Invoke(null, new object[] { smeltingVmType });
            resetSelection.Invoke(null, null);
        }

        private static void VerifySmeltingSelectionBehavior(
            MethodInfo configureSelection,
            MethodInfo selectionPostfix,
            MethodInfo resetSelection)
        {
            configureSelection.Invoke(null, new object[] { typeof(FakeSmeltingVm) });
            try
            {
                var vm = new FakeSmeltingVm
                {
                    CurrentSelectedItem = new FakeSmeltingItem()
                };

                selectionPostfix.Invoke(null, new object[] { vm });
                Require(vm.CurrentSelectedItem.IsSelected, "The visually cleared next item was not reselected.");
                Require(vm.SelectionCallCount == 1, "Bannerlord's selection callback was not invoked exactly once.");

                selectionPostfix.Invoke(null, new object[] { vm });
                Require(vm.SelectionCallCount == 1, "An already selected item was selected a second time.");

                vm.CurrentSelectedItem = null;
                selectionPostfix.Invoke(null, new object[] { vm });
                Require(vm.SelectionCallCount == 1, "A null current item invoked the selection callback.");
            }
            finally
            {
                resetSelection.Invoke(null, null);
            }
        }

        private static void VerifyProxyBehavior(
            MethodInfo configure,
            MethodInfo safePrefix,
            MethodInfo reset)
        {
            MethodInfo target = typeof(FakePerk).GetMethod(nameof(FakePerk.Initialize));
            MethodInfo originalPrefix = typeof(FakeRelentlessPatch).GetMethod(
                "Prefix",
                BindingFlags.Static | BindingFlags.NonPublic);
            configure.Invoke(null, new object[] { target, originalPrefix });
            var harmony = new Harmony("community.bannerlord.relentless-smith.proxy-verifier");
            try
            {
                var instance = new FakePerk();
                harmony.Patch(target, prefix: new HarmonyMethod(safePrefix));

                instance.Initialize("old", null, "primary", "secondary");
                Require(FakeRelentlessPatch.CallCount == 0, "Null skill did not bypass the original prefix.");
                Require(instance.ReceivedName == "old", "Null-skill call unexpectedly changed a target argument.");

                instance.Initialize("old", new object(), "primary", "secondary");
                Require(FakeRelentlessPatch.CallCount == 1, "Non-null call did not invoke the original prefix exactly once.");
                Require(instance.ReceivedName == "renamed", "Name by-ref update was not propagated through Harmony's __args array.");
                Require(instance.ReceivedPrimaryDescription == "primary-updated", "Primary description by-ref update was not propagated through Harmony's __args array.");
                Require(instance.ReceivedSecondaryDescription == "secondary-updated", "Secondary description by-ref update was not propagated through Harmony's __args array.");

                bool sawOriginalException = false;
                try
                {
                    instance.Initialize("throw", new object(), "primary", "secondary");
                }
                catch (InvalidOperationException ex) when (ex.Message == "intentional verifier exception")
                {
                    sawOriginalException = true;
                }
                Require(sawOriginalException, "A non-null original exception was swallowed or changed to a reflection wrapper.");
            }
            finally
            {
                harmony.UnpatchAll(harmony.Id);
                reset.Invoke(null, null);
            }
        }

        private static void VerifyCoordinatorReplacement(
            Assembly fixAssembly,
            string relentlessPath,
            MethodInfo safePrefix,
            MethodInfo selectionPostfix)
        {
            Assembly relentlessAssembly = Assembly.LoadFrom(relentlessPath);
            Type unsafeType = RequireType(relentlessAssembly, "RelentlessSmithConcise.Patches.PerkObject_Initialize_Patch");
            MethodInfo unsafePrefix = RequireMethod(unsafeType, "Prefix", BindingFlags.Static | BindingFlags.NonPublic);
            Type unsafeSmeltingType = RequireType(
                relentlessAssembly,
                "RelentlessSmithConcise.Patches.SmeltingVM_TrySmeltingSelectedItems_BulkSmelt_Patch");
            MethodInfo unsafeSmeltingPrefix = RequireMethod(
                unsafeSmeltingType,
                "Prefix",
                BindingFlags.Static | BindingFlags.NonPublic);
            Type perkType = RequireType(
                Assembly.Load("TaleWorlds.CampaignSystem"),
                "TaleWorlds.CampaignSystem.CharacterDevelopment.PerkObject");
            MethodInfo target = perkType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(method => method.Name == "Initialize" && method.GetParameters().Length == 14 && method.GetParameters().Any(parameter => parameter.Name == "skill"));
            Type smeltingVmType = RequireType(
                Assembly.Load("TaleWorlds.CampaignSystem.ViewModelCollection"),
                "TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting.SmeltingVM");
            MethodInfo smeltingTarget = smeltingVmType.GetMethod(
                "TrySmeltingSelectedItems",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Require(smeltingTarget != null, "Could not resolve SmeltingVM.TrySmeltingSelectedItems.");

            const string upstreamOwner = "relentless.smith.concise";
            const string fixOwner = "community.bannerlord.relentless-smith-concise.bk-redux.fixes";
            var upstreamHarmony = new Harmony(upstreamOwner);
            var originalMetadata = new HarmonyMethod(unsafePrefix)
            {
                priority = Priority.High,
                before = new[] { "verifier.before" },
                after = new[] { "verifier.after" },
                debug = false
            };
            Type coordinatorType = RequireType(fixAssembly, "RelentlessSmithConciseBKReduxFixes.PatchCoordinator");
            MethodInfo apply = RequireMethod(coordinatorType, "Apply", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo unapply = RequireMethod(coordinatorType, "Unapply", BindingFlags.Static | BindingFlags.NonPublic);
            bool coordinatorApplied = false;

            try
            {
                upstreamHarmony.Patch(target, prefix: originalMetadata);
                upstreamHarmony.Patch(
                    smeltingTarget,
                    prefix: new HarmonyMethod(unsafeSmeltingPrefix));
                Require(FindPrefix(target, unsafePrefix, upstreamOwner) != null, "Verifier could not install the original upstream prefix.");
                Require(
                    FindPrefix(smeltingTarget, unsafeSmeltingPrefix, upstreamOwner) != null,
                    "Verifier could not install the original bulk-smelting prefix.");

                apply.Invoke(null, null);
                coordinatorApplied = true;
                Require(FindPrefix(target, unsafePrefix, upstreamOwner) == null, "Coordinator left the unsafe upstream prefix active.");
                Patch replacement = FindPrefix(target, safePrefix, upstreamOwner);
                Require(replacement != null, "Coordinator did not install the safe proxy under the upstream owner.");
                Require(replacement.priority == originalMetadata.priority, "Coordinator changed Harmony priority.");
                Require(replacement.before.SequenceEqual(originalMetadata.before), "Coordinator changed Harmony before metadata.");
                Require(replacement.after.SequenceEqual(originalMetadata.after), "Coordinator changed Harmony after metadata.");
                Patch selection = FindPostfix(smeltingTarget, selectionPostfix, fixOwner);
                Require(selection != null, "Coordinator did not install the smelting-selection postfix.");
                Require(selection.priority == Priority.Last, "Smelting-selection postfix does not run at the final priority.");
                Require(
                    selection.after.Contains(upstreamOwner),
                    "Smelting-selection postfix lost its explicit ordering after Relentless Smith.");

                unapply.Invoke(null, null);
                coordinatorApplied = false;
                Require(FindPrefix(target, safePrefix, upstreamOwner) == null, "Coordinator left the safe proxy active after unload.");
                Patch restored = FindPrefix(target, unsafePrefix, upstreamOwner);
                Require(restored != null, "Coordinator did not restore the original prefix on unload.");
                Require(restored.priority == originalMetadata.priority, "Rollback changed Harmony priority.");
                Require(restored.before.SequenceEqual(originalMetadata.before), "Rollback changed Harmony before metadata.");
                Require(restored.after.SequenceEqual(originalMetadata.after), "Rollback changed Harmony after metadata.");
                Require(
                    FindPostfix(smeltingTarget, selectionPostfix, fixOwner) == null,
                    "Coordinator left the smelting-selection postfix active after unload.");
            }
            finally
            {
                if (coordinatorApplied)
                {
                    try
                    {
                        unapply.Invoke(null, null);
                    }
                    catch
                    {
                    }
                }
                upstreamHarmony.Unpatch(target, unsafePrefix);
                upstreamHarmony.Unpatch(target, safePrefix);
                upstreamHarmony.Unpatch(smeltingTarget, unsafeSmeltingPrefix);
                new Harmony(fixOwner).Unpatch(smeltingTarget, selectionPostfix);
            }
        }

        private static Patch FindPrefix(MethodBase target, MethodInfo patchMethod, string owner)
        {
            Patches patches = Harmony.GetPatchInfo(target);
            return patches == null
                ? null
                : patches.Prefixes.SingleOrDefault(patch => patch.PatchMethod == patchMethod && patch.owner == owner);
        }

        private static Patch FindPostfix(MethodBase target, MethodInfo patchMethod, string owner)
        {
            Patches patches = Harmony.GetPatchInfo(target);
            return patches == null
                ? null
                : patches.Postfixes.SingleOrDefault(
                    patch => patch.PatchMethod == patchMethod && patch.owner == owner);
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            string fileName = new AssemblyName(args.Name).Name + ".dll";
            foreach (string directory in SearchDirectories)
            {
                string candidate = Path.Combine(directory, fileName);
                if (File.Exists(candidate))
                {
                    return Assembly.LoadFrom(candidate);
                }
            }
            return null;
        }

        private static void AddModuleSearchDirectories(string root)
        {
            if (!Directory.Exists(root))
            {
                return;
            }
            foreach (string module in Directory.GetDirectories(root))
            {
                AddSearchDirectory(Path.Combine(module, "bin", "Win64_Shipping_Client"));
            }
        }

        private static void AddSearchDirectory(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && !SearchDirectories.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                SearchDirectories.Add(path);
            }
        }

        private static Type RequireType(Assembly assembly, string name)
        {
            return assembly.GetType(name, throwOnError: true, ignoreCase: false);
        }

        private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags)
        {
            MethodInfo method = type.GetMethod(name, flags);
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, name);
            }
            return method;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class FakePerk
        {
            internal string ReceivedName;
            internal string ReceivedPrimaryDescription;
            internal string ReceivedSecondaryDescription;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Initialize(string name, object skill, string primaryDescription, string secondaryDescription)
            {
                ReceivedName = name;
                ReceivedPrimaryDescription = primaryDescription;
                ReceivedSecondaryDescription = secondaryDescription;
            }
        }

        private static class FakeRelentlessPatch
        {
            internal static int CallCount;

            private static void Prefix(
                FakePerk __instance,
                ref string name,
                object skill,
                ref string primaryDescription,
                ref string secondaryDescription)
            {
                CallCount++;
                if (name == "throw")
                {
                    throw new InvalidOperationException("intentional verifier exception");
                }
                name = "renamed";
                primaryDescription = "primary-updated";
                secondaryDescription = "secondary-updated";
            }
        }

        private sealed class FakeSmeltingVm
        {
            public FakeSmeltingItem CurrentSelectedItem { get; set; }

            internal int SelectionCallCount { get; private set; }

            private void OnItemSelection(FakeSmeltingItem item)
            {
                CurrentSelectedItem = item;
                CurrentSelectedItem.IsSelected = true;
                SelectionCallCount++;
            }
        }

        private sealed class FakeSmeltingItem
        {
            public bool IsSelected { get; set; }
        }
    }
}
