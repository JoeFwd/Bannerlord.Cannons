using System;
using System.Linq;
using System.Reflection;
using Bannerlord.Cannons.DI;
using Bannerlord.Cannons.Infrastructure.Icons;
using Bannerlord.Cannons.Infrastructure.Registry;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Siege;
using TaleWorlds.TwoDimension;

namespace Bannerlord.Cannons.Integration.UI.Patches
{
    public class MapSiegePOIBrushWidgetManualPatch : IPatch
    {
        private static readonly Lazy<IMapSiegeEngineIconRepository> _repo =
            new Lazy<IMapSiegeEngineIconRepository>(() =>
                new MapSiegeEngineIconRepository(CannonsRuntimeServices.GetRequiredService<ICannonRegistry>()));

        public MethodInfo TargetMethod =>
            typeof(MapSiegePOIBrushWidget).GetMethod("SetMachineTypeIcon", BindingFlags.NonPublic | BindingFlags.Instance);

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(MapSiegePOIBrushWidgetManualPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(MapSiegePOIBrushWidget __instance, int machineType)
        {
            var cannonIcon = _repo.Value.MapSiegeEngineIcons
                .FirstOrDefault(icon => icon.MachineType == machineType);
            if (cannonIcon is null) return;

            // SpriteData is accessed via UIResourceManager which is static
            var spriteData = TaleWorlds.Engine.GauntletUI.UIResourceManager.SpriteData;
            __instance.MachineTypeIconWidget.Sprite = spriteData.GetSprite(cannonIcon.MapSiegeMarkerSpriteId);
        }
    }
}
