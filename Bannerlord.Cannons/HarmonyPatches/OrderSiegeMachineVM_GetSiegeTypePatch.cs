using System;
using Bannerlord.Cannons.Infrastructure.Registry;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.Cannons.HarmonyPatches
{
    [HarmonyPatch(typeof(OrderSiegeMachineVM), nameof(OrderSiegeMachineVM.GetSiegeType))]
    public static class OrderSiegeMachineVM_GetSiegeTypePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Type t, BattleSideEnum side, ref SiegeEngineType __result)
        {
            var cannon = CannonRegistry.Instance.GetCannonByScript(t);
            if (cannon != null)
                __result = MBObjectManager.Instance.GetObject<SiegeEngineType>(cannon.Id);
        }
    }
}
