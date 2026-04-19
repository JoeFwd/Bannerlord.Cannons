using System;
using System.Linq;
using System.Reflection;
using Bannerlord.Cannons.Infrastructure.Icons;
using Bannerlord.Cannons.Infrastructure.Registry;
using HarmonyLib;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;

namespace Bannerlord.Cannons.HarmonyPatches
{
    [HarmonyPatch]
    public static class OrderSiegeMachineItemButtonWidgetPatch
    {
        private static readonly Lazy<IDeploymentSiegeEngineIconRepository> _repo =
            new Lazy<IDeploymentSiegeEngineIconRepository>(() =>
                new DeploymentSiegeEngineIconRepository(new CannonIconProvider(CannonRegistry.Instance)));

        static MethodBase TargetMethod() =>
            typeof(OrderSiegeMachineItemButtonWidget).GetMethod("UpdateMachineIcon", AccessTools.all);

        [HarmonyPostfix]
        private static void Postfix(OrderSiegeMachineItemButtonWidget __instance)
        {
            var machineClass =
                AccessTools.Property(typeof(OrderSiegeMachineItemButtonWidget), "MachineClass")
                    .GetValue(__instance) as string;
            var machineIconWidget =
                AccessTools.Property(typeof(OrderSiegeMachineItemButtonWidget), "MachineIconWidget")
                    .GetValue(__instance) as Widget;

            if (machineClass == null || machineIconWidget == null) return;

            var icon = _repo.Value.SiegeEngineIcons
                .FirstOrDefault(i => i.Name.Equals(machineClass, StringComparison.InvariantCultureIgnoreCase));
            if (icon != null)
            {
                var isRemainingCountVisibleField =
                    AccessTools.Field(typeof(OrderSiegeMachineItemButtonWidget), "_isRemainingCountVisible");
                isRemainingCountVisibleField.SetValue(__instance, true);
                machineIconWidget.SetState(icon.Name);
            }
        }
    }
}
