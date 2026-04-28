using System;
using System.Linq;
using System.Reflection;
using Bannerlord.Cannons.DI;
using Bannerlord.Cannons.Infrastructure.Icons;
using Bannerlord.Cannons.Infrastructure.Registry;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;

namespace Bannerlord.Cannons.Integration.UI.Patches
{
    public class OrderSiegeMachineItemButtonWidgetPatch : IPatch
    {
        private static readonly Lazy<IDeploymentSiegeEngineIconRepository> _repo =
            new Lazy<IDeploymentSiegeEngineIconRepository>(() =>
                new DeploymentSiegeEngineIconRepository(
                    new CannonIconProvider(CannonsRuntimeServices.GetRequiredService<ICannonRegistry>())));

        public MethodInfo TargetMethod =>
            typeof(OrderSiegeMachineItemButtonWidget).GetMethod("UpdateMachineIcon", AccessTools.all);

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(OrderSiegeMachineItemButtonWidgetPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

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
