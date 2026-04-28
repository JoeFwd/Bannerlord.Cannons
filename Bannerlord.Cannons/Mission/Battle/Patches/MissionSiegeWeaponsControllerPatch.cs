using System;
using System.Reflection;
using Bannerlord.Cannons.DI;
using Bannerlord.Cannons.Infrastructure.Registry;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Missions;

namespace Bannerlord.Cannons.Integration.Mission.Battle.Patches
{
    public class MissionSiegeWeaponsControllerPatch : IPatch
    {
        public MethodInfo TargetMethod =>
            AccessTools.Method(typeof(MissionSiegeWeaponsController), "GetSiegeWeaponBaseType");

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(MissionSiegeWeaponsControllerPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(SiegeEngineType siegeWeaponType, ref Type __result)
        {
            var factory = CannonsRuntimeServices.GetRequiredService<ICannonRegistry>().GetFactory(siegeWeaponType.StringId);
            if (factory != null)
                __result = factory.CannonScriptType;
        }
    }
}
