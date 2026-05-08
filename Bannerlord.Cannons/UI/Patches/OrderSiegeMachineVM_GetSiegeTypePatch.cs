using System;
using System.Reflection;
using Bannerlord.Cannons.Infrastructure.Registry;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.Cannons.Integration.UI.Patches
{
    public class OrderSiegeMachineVM_GetSiegeTypePatch : IPatch
    {
        private static ICannonRegistry _registry = null!;

        public OrderSiegeMachineVM_GetSiegeTypePatch(ICannonRegistry registry)
        {
            _registry = registry;
        }

        public MethodInfo TargetMethod =>
            AccessTools.Method(typeof(OrderSiegeMachineVM), nameof(OrderSiegeMachineVM.GetSiegeType));

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(OrderSiegeMachineVM_GetSiegeTypePatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(Type t, BattleSideEnum side, ref SiegeEngineType __result)
        {
            var cannon = _registry.GetCannonByScript(t);
            if (cannon != null)
                __result = MBObjectManager.Instance.GetObject<SiegeEngineType>(cannon.Id);
        }
    }
}
