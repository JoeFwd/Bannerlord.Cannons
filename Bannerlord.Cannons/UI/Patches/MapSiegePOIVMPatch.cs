using System;
using System.Linq;
using System.Reflection;
using Bannerlord.Cannons.Infrastructure.Icons;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using SandBox.ViewModelCollection.MapSiege;

namespace Bannerlord.Cannons.Integration.UI.Patches
{
    public class MapSiegePOIVMPatch : IPatch
    {
        private static IMapSiegeEngineIconRepository _repo = null!;

        public MapSiegePOIVMPatch(IMapSiegeEngineIconRepository repo)
        {
            _repo = repo;
        }

        public MethodInfo TargetMethod =>
            typeof(MapSiegePOIVM).GetMethod("RefreshMachineType", BindingFlags.Instance | BindingFlags.NonPublic);

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(MapSiegePOIVMPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(MapSiegePOIVM __instance)
        {
            var machine = __instance.Machine;
            if (machine is null) return;

            var machineType = _repo.MapSiegeEngineIcons
                .FirstOrDefault(icon => icon.CannonId.Equals(machine.SiegeEngine.StringId, StringComparison.InvariantCultureIgnoreCase))
                ?.MachineType;

            if (machineType is null) return;

            AccessTools.Field(typeof(MapSiegePOIVM), "_bindMachineType")
                .SetValue(__instance, machineType);
        }
    }
}
