using System.Linq;
using System.Reflection;
using Bannerlord.Cannons.Infrastructure.Icons;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Siege;

namespace Bannerlord.Cannons.Integration.UI.Patches
{
    public class MapSiegePOIBrushWidgetManualPatch : IPatch
    {
        private static IMapSiegeEngineIconRepository _repo = null!;
        private static IBannerlordSpriteRepository _spriteRepository = null!;

        public MapSiegePOIBrushWidgetManualPatch(
            IMapSiegeEngineIconRepository repo,
            IBannerlordSpriteRepository spriteRepository)
        {
            _repo = repo;
            _spriteRepository = spriteRepository;
        }

        public MethodInfo TargetMethod =>
            typeof(MapSiegePOIBrushWidget).GetMethod("SetMachineTypeIcon", BindingFlags.NonPublic | BindingFlags.Instance);

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(MapSiegePOIBrushWidgetManualPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(MapSiegePOIBrushWidget __instance, int machineType)
        {
            var cannonIcon = _repo.MapSiegeEngineIcons
                .FirstOrDefault(icon => icon.MachineType == machineType);
            if (cannonIcon is null) return;

            __instance.MachineTypeIconWidget.Sprite = _spriteRepository.GetSprite(cannonIcon.MapSiegeMarkerSpriteId);
        }
    }
}
