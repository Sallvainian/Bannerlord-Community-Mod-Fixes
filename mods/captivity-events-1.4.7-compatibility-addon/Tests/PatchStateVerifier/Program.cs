using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace PatchStateVerifier
{
    internal static class Program
    {
        private const string CaptivityOwner = "com.CE.captivityEvents";
        private const string SafetyOwner = "com.sallvainian.captivityevents.147fix";
        private static readonly List<string> SearchDirectories = new List<string>();

        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: PatchStateVerifier FIX_DLL CAPTIVITY_EVENTS_DLL GAME_ROOT");
                return 2;
            }

            string fixPath = Path.GetFullPath(args[0]);
            string captivityPath = Path.GetFullPath(args[1]);
            string gameRoot = Path.GetFullPath(args[2]);
            if (!File.Exists(fixPath) ||
                !File.Exists(captivityPath) ||
                !Directory.Exists(gameRoot))
            {
                Console.Error.WriteLine("One or more required paths do not exist.");
                return 2;
            }

            AddSearchDirectory(Path.GetDirectoryName(fixPath));
            AddSearchDirectory(Path.GetDirectoryName(captivityPath));
            AddSearchDirectory(Path.Combine(gameRoot, "bin", "Win64_Shipping_Client"));
            AddModuleSearchDirectories(Path.Combine(gameRoot, "Modules"));
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            var safetyHarmony = new Harmony(SafetyOwner);
            var captivityHarmony = new Harmony(CaptivityOwner);
            try
            {
                Assembly campaignAssembly =
                    Assembly.Load("TaleWorlds.CampaignSystem");
                Assembly captivityAssembly = Assembly.LoadFrom(captivityPath);
                Assembly fixAssembly = Assembly.LoadFrom(fixPath);

                Type characterType = RequireType(
                    campaignAssembly,
                    "TaleWorlds.CampaignSystem.CharacterObject");
                Type captivityPatchType = RequireType(
                    captivityAssembly,
                    "CaptivityEvents.Patches.CEPatchCharacterObject");
                Type safetyType = RequireType(
                    fixAssembly,
                    "CaptivityEvents_147Fix.SubModule");

                MethodInfo ensurePatches = RequireMethod(
                    safetyType,
                    "EnsureCharacterSafetyPatches",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo harmonyField = safetyType.GetField(
                    "_harmony",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Require(harmonyField != null, "Safety module Harmony field was not found.");

                object safetyModule = Activator.CreateInstance(safetyType);
                harmonyField.SetValue(safetyModule, safetyHarmony);

                PatchContract[] contracts =
                {
                    CreateContract(
                        characterType,
                        captivityPatchType,
                        safetyType,
                        "UpgradeTargets"),
                    CreateContract(
                        characterType,
                        captivityPatchType,
                        safetyType,
                        "Culture"),
                    CreateContract(
                        characterType,
                        captivityPatchType,
                        safetyType,
                        "FirstBattleEquipment")
                };

                RegisterOriginals(captivityHarmony, contracts);
                InvokeEnsure(ensurePatches, safetyModule, "verifier initial pass");
                VerifySafetyState(contracts);

                RegisterOriginals(captivityHarmony, contracts);
                Require(
                    contracts.All(contract =>
                        CountOwnedPostfix(contract.Target, CaptivityOwner) == 1),
                    "The simulated late Captivity Events registration was not present.");

                InvokeEnsure(ensurePatches, safetyModule, "verifier campaign pass");
                VerifySafetyState(contracts);

                Console.WriteLine(
                    "PASS: original CharacterObject postfixes were replaced, " +
                    "late re-registration was removed, and exactly one safety " +
                    "postfix remained on every getter.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: " + Unwrap(ex));
                return 1;
            }
            finally
            {
                safetyHarmony.UnpatchAll(SafetyOwner);
                captivityHarmony.UnpatchAll(CaptivityOwner);
            }
        }

        private static PatchContract CreateContract(
            Type characterType,
            Type captivityPatchType,
            Type safetyType,
            string propertyName)
        {
            MethodInfo[] targets = characterType
                .GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly)
                .Where(method =>
                    method.Name == "get_" + propertyName &&
                    method.GetParameters().Length == 0)
                .ToArray();
            Require(
                targets.Length == 1,
                "Expected one declared CharacterObject." + propertyName +
                " getter, found " + targets.Length + ".");

            MethodInfo original = RequireMethod(
                captivityPatchType,
                propertyName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo safety = RequireMethod(
                safetyType,
                "Safe" + propertyName + "Postfix",
                BindingFlags.Static | BindingFlags.NonPublic);
            return new PatchContract(targets[0], original, safety);
        }

        private static void RegisterOriginals(
            Harmony harmony,
            IEnumerable<PatchContract> contracts)
        {
            foreach (PatchContract contract in contracts)
            {
                harmony.Patch(
                    contract.Target,
                    postfix: new HarmonyMethod(contract.OriginalPostfix));
            }
        }

        private static void InvokeEnsure(
            MethodInfo ensurePatches,
            object safetyModule,
            string stage)
        {
            ensurePatches.Invoke(safetyModule, new object[] { stage });
        }

        private static void VerifySafetyState(IEnumerable<PatchContract> contracts)
        {
            foreach (PatchContract contract in contracts)
            {
                Require(
                    CountOwnedPostfix(contract.Target, CaptivityOwner) == 0,
                    "A Captivity Events postfix remained on " +
                    contract.Target.Name + ".");
                Require(
                    CountExactPostfix(
                        contract.Target,
                        contract.SafetyPostfix,
                        SafetyOwner) == 1,
                    "The expected single safety postfix was not active on " +
                    contract.Target.Name + ".");
            }
        }

        private static int CountOwnedPostfix(MethodBase target, string owner)
        {
            return Harmony.GetPatchInfo(target)?.Postfixes.Count(
                patch => patch.owner == owner) ?? 0;
        }

        private static int CountExactPostfix(
            MethodBase target,
            MethodInfo patchMethod,
            string owner)
        {
            return Harmony.GetPatchInfo(target)?.Postfixes.Count(
                patch =>
                    patch.owner == owner &&
                    patch.PatchMethod == patchMethod) ?? 0;
        }

        private static Type RequireType(Assembly assembly, string name)
        {
            Type type = assembly.GetType(name, false);
            if (type == null)
            {
                throw new TypeLoadException(name);
            }
            return type;
        }

        private static MethodInfo RequireMethod(
            Type type,
            string name,
            BindingFlags flags)
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

        private static Exception Unwrap(Exception exception)
        {
            return exception is TargetInvocationException invocation &&
                   invocation.InnerException != null
                ? invocation.InnerException
                : exception;
        }

        private static void AddSearchDirectory(string directory)
        {
            if (!string.IsNullOrWhiteSpace(directory) &&
                Directory.Exists(directory) &&
                !SearchDirectories.Contains(
                    directory,
                    StringComparer.OrdinalIgnoreCase))
            {
                SearchDirectories.Add(directory);
            }
        }

        private static void AddModuleSearchDirectories(string modulesRoot)
        {
            if (!Directory.Exists(modulesRoot))
            {
                return;
            }

            foreach (string directory in Directory.EnumerateDirectories(
                modulesRoot,
                "Win64_Shipping_Client",
                SearchOption.AllDirectories))
            {
                AddSearchDirectory(directory);
            }
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

        private sealed class PatchContract
        {
            internal PatchContract(
                MethodInfo target,
                MethodInfo originalPostfix,
                MethodInfo safetyPostfix)
            {
                Target = target;
                OriginalPostfix = originalPostfix;
                SafetyPostfix = safetyPostfix;
            }

            internal MethodInfo Target { get; }
            internal MethodInfo OriginalPostfix { get; }
            internal MethodInfo SafetyPostfix { get; }
        }
    }
}
