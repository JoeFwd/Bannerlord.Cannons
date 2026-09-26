using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using SandBox.View.Map.Visuals;
using TaleWorlds.Core;

namespace Bannerlord.Cannons.Integration.Campaign.Patches
{
    public class SettlementVisualCalculateDataAndDurationsForSiegeMachinePatch : IPatch
    {
        private static CannonFireSoundProvider _fireSoundProvider = null!;
        private static PendingCannonFireSound _pendingFireSound = null!;

        public SettlementVisualCalculateDataAndDurationsForSiegeMachinePatch(
            CannonFireSoundProvider fireSoundProvider,
            PendingCannonFireSound pendingFireSound)
        {
            _fireSoundProvider = fireSoundProvider;
            _pendingFireSound = pendingFireSound;
        }

        public MethodInfo TargetMethod =>
            AccessTools.Method(typeof(SettlementVisual), "CalculateDataAndDurationsForSiegeMachine");

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(SettlementVisualCalculateDataAndDurationsForSiegeMachinePatch), nameof(Prefix));

        public PatchType PatchType => PatchType.Prefix;

        // Always overwrite, so a non-cannon machine clears any sound left by an earlier cannon.
        private static void Prefix(SiegeEngineType machineType)
            => _pendingFireSound.Set(_fireSoundProvider.GetCampaignMapFireSoundEventId(machineType.StringId));
    }
}
