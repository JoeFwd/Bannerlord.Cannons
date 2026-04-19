using System;
using System.Linq;
using System.Reflection;
using Bannerlord.Cannons.Infrastructure.Icons;
using Bannerlord.Cannons.Infrastructure.Registry;
using HarmonyLib;
using SandBox.ViewModelCollection.MapSiege;

namespace Bannerlord.Cannons.HarmonyPatches
{
    [HarmonyPatch]
    public static class MapSiegePOIVMPatch
    {
        private static readonly Lazy<IMapSiegeEngineIconRepository> _repo =
            new Lazy<IMapSiegeEngineIconRepository>(() =>
                new MapSiegeEngineIconRepository(CannonRegistry.Instance));

        static MethodBase TargetMethod() =>
            typeof(MapSiegePOIVM).GetMethod("RefreshMachineType",
                BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPostfix]
        private static void Postfix(MapSiegePOIVM __instance)
        {
            var machine = __instance.Machine;
            if (machine is null) return;

            var machineType = _repo.Value.MapSiegeEngineIcons
                .FirstOrDefault(icon => icon.CannonId.Equals(machine.SiegeEngine.StringId, StringComparison.InvariantCultureIgnoreCase))
                ?.MachineType;

            if (machineType is null) return;

            AccessTools.Field(typeof(MapSiegePOIVM), "_bindMachineType")
                .SetValue(__instance, machineType);
        }
    }
}
