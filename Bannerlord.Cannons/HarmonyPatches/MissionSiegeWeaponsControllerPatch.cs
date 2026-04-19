using System;
using Bannerlord.Cannons.Infrastructure.Registry;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Missions;

namespace Bannerlord.Cannons.HarmonyPatches
{
    [HarmonyPatch(typeof(MissionSiegeWeaponsController), "GetSiegeWeaponBaseType")]
    public static class MissionSiegeWeaponsControllerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SiegeEngineType siegeWeaponType, ref Type __result)
        {
            var factory = CannonRegistry.Instance.GetFactory(siegeWeaponType.StringId);
            if (factory != null)
                __result = factory.CannonScriptType;
        }
    }
}
