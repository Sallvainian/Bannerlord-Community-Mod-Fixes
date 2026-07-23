using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace BannerKings.Utils
{
    // Test double matching the Redux v1.9.33.5 API used by the compatibility
    // module. The unpatched legacy probe deliberately returns false.
    public static class ModCompat
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsLoaded(string moduleId, string assemblyName = null)
        {
            return false;
        }

        public static bool DiplomacyMod => IsLoaded("Diplomacy", "DiplomacyFixes");
    }
}

namespace BannerKings.Behaviours.Diplomacy
{
    // Test double matching the private Redux method suppressed by the patch.
    public sealed class BKDiplomacyBehavior
    {
        public static int ForcedPeaceCalls { get; private set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ForceProposePeaceFromLosingSide()
        {
            ForcedPeaceCalls++;
        }
    }
}

namespace BannerKings.Patches
{
    // Test doubles matching the two private Redux postfixes that v2.1 adapts
    // to Bannerlord 1.4.7's renamed reward-model arguments.
    internal static class VanillaModelTweakPatches
    {
        internal static class BKBattleRewardTweakPatches
        {
            internal static int InfluenceCalls { get; private set; }
            internal static int RenownCalls { get; private set; }

            private static void CalculateInfluenceGainPostfix(
                PartyBase party,
                float influenceValueOfBattle,
                float contributionShare,
                ref ExplainedNumber result)
            {
                InfluenceCalls++;
            }

            private static void CalculateRenownGainPostfix(
                PartyBase party,
                float renownValueOfBattle,
                float contributionShare,
                ref ExplainedNumber result)
            {
                RenownCalls++;
            }
        }
    }
}

internal static class Program
{
    private static string[] _probingDirectories;

    private static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: RuntimeHookSmokeTest <patch-dll> <game-bin> <harmony-bin>");
            return 2;
        }

        string patchDll = Path.GetFullPath(args[0]);
        _probingDirectories = new[]
        {
            Path.GetDirectoryName(patchDll),
            Path.GetFullPath(args[1]),
            Path.GetFullPath(args[2]),
        };

        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        // Keep game-assembly types out of Main's JIT pass so the resolver above
        // is active before TaleWorlds.CampaignSystem is requested.
        return Run(patchDll);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string patchDll)
    {
        Type behaviorType = typeof(BannerKings.Behaviours.Diplomacy.BKDiplomacyBehavior);
        object behavior = Activator.CreateInstance(behaviorType);
        MethodInfo forcedPeace = behaviorType.GetMethod(
            "ForceProposePeaceFromLosingSide",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert(!BannerKings.Utils.ModCompat.DiplomacyMod,
            "Legacy detector should be false before the compatibility module loads.");

        forcedPeace.Invoke(behavior, null);
        Assert(BannerKings.Behaviours.Diplomacy.BKDiplomacyBehavior.ForcedPeaceCalls == 1,
            "Forced-peace test double should execute before patching.");

        Assembly patchAssembly = Assembly.LoadFrom(patchDll);
        Type subModuleType = patchAssembly.GetType(
            "BK_Diplomacy_ModuleLoader_Patch.SubModule",
            throwOnError: true);
        object subModule = Activator.CreateInstance(subModuleType);
        MethodInfo load = subModuleType.GetMethod(
            "OnSubModuleLoad",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo unload = subModuleType.GetMethod(
            "OnSubModuleUnloaded",
            BindingFlags.Instance | BindingFlags.NonPublic);

        load.Invoke(subModule, null);

        Assert(BannerKings.Utils.ModCompat.DiplomacyMod,
            "Compatibility module should make Redux's Diplomacy probe true.");

        forcedPeace.Invoke(behavior, null);
        Assert(BannerKings.Behaviours.Diplomacy.BKDiplomacyBehavior.ForcedPeaceCalls == 1,
            "Compatibility module should suppress Redux's direct forced-peace path.");

        MethodInfo installRewardRepairs = subModuleType.GetMethod(
            "InstallReduxBattleRewardRepairs",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo influenceAdapter = subModuleType.GetMethod(
            "ReduxInfluenceGainPostfix",
            BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo renownAdapter = subModuleType.GetMethod(
            "ReduxRenownGainPostfix",
            BindingFlags.Static | BindingFlags.NonPublic);

        installRewardRepairs.Invoke(subModule, null);
        influenceAdapter.Invoke(null, new object[] { null, new ExplainedNumber(1f) });
        renownAdapter.Invoke(null, new object[] { null, new ExplainedNumber(1f) });

        Assert(BannerKings.Patches.VanillaModelTweakPatches
                .BKBattleRewardTweakPatches.InfluenceCalls == 1,
            "Influence adapter should invoke Redux's existing postfix.");
        Assert(BannerKings.Patches.VanillaModelTweakPatches
                .BKBattleRewardTweakPatches.RenownCalls == 1,
            "Renown adapter should invoke Redux's existing postfix.");

        unload.Invoke(subModule, null);

        Assert(!BannerKings.Utils.ModCompat.DiplomacyMod,
            "Unloading should remove the Diplomacy detection hooks.");

        forcedPeace.Invoke(behavior, null);
        Assert(BannerKings.Behaviours.Diplomacy.BKDiplomacyBehavior.ForcedPeaceCalls == 2,
            "Unloading should remove the forced-peace suppression hook.");

        influenceAdapter.Invoke(null, new object[] { null, new ExplainedNumber(1f) });
        renownAdapter.Invoke(null, new object[] { null, new ExplainedNumber(1f) });
        Assert(BannerKings.Patches.VanillaModelTweakPatches
                .BKBattleRewardTweakPatches.InfluenceCalls == 1,
            "Unloading should detach the Redux influence delegate.");
        Assert(BannerKings.Patches.VanillaModelTweakPatches
                .BKBattleRewardTweakPatches.RenownCalls == 1,
            "Unloading should detach the Redux renown delegate.");

        Console.WriteLine("PASS: diplomacy hooks, reward adapters, and teardown all behaved as expected.");
        return 0;
    }

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        string fileName = new AssemblyName(args.Name).Name + ".dll";
        foreach (string directory in _probingDirectories)
        {
            string candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate))
            {
                return Assembly.LoadFrom(candidate);
            }
        }

        return null;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
