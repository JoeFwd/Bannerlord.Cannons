using System;
using System.Linq;
using System.Reflection;
using Bannerlord.Cannons.Infrastructure.Icons;
using Bannerlord.Cannons.Infrastructure.Registry;
using HarmonyLib;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Siege;
using TaleWorlds.TwoDimension;

namespace Bannerlord.Cannons.HarmonyPatches
{
    [HarmonyPatch]
    public static class MapSiegePOIBrushWidgetManualPatch
    {
        private static readonly Lazy<IMapSiegeEngineIconRepository> _repo =
            new Lazy<IMapSiegeEngineIconRepository>(() =>
                new MapSiegeEngineIconRepository(CannonRegistry.Instance));

        static MethodBase TargetMethod() =>
            typeof(MapSiegePOIBrushWidget).GetMethod("SetMachineTypeIcon",
                BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPostfix]
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
