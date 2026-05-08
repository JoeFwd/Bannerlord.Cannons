using System;
using System.Linq;
using System.Reflection;
using Bannerlord.Cannons.Infrastructure.Icons;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

namespace Bannerlord.Cannons.Integration.UI.Patches
{
    public class MissionSiegeEngineMarkerWidgetPatch : IPatch
    {
        private static IMapSiegeEngineIconRepository _repo = null!;
        private static IBannerlordSpriteRepository _spriteRepository = null!;

        public MissionSiegeEngineMarkerWidgetPatch(
            IMapSiegeEngineIconRepository repo,
            IBannerlordSpriteRepository spriteRepository)
        {
            _repo = repo;
            _spriteRepository = spriteRepository;
        }

        public MethodInfo TargetMethod =>
            typeof(MissionSiegeEngineMarkerWidget).GetMethod("SetMachineTypeIcon", BindingFlags.NonPublic | BindingFlags.Instance);

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(MissionSiegeEngineMarkerWidgetPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(MissionSiegeEngineMarkerWidget __instance, string machineType)
        {
            // The native SetMachineTypeIcon constructs "SPGeneral\MapSiege\" + machineType and
            // calls base.Context.SpriteData.GetSprite. The mission UI context may not include
            // our custom sprite category, so fall back to the shared Bannerlord sprite data.
            var cannonIcon = _repo.MapSiegeEngineIcons
                .FirstOrDefault(icon => icon.CannonId.Equals(machineType, StringComparison.InvariantCultureIgnoreCase));

            if (cannonIcon is null || __instance.MachineTypeIconWidget is null)
                return;

            var sprite = _spriteRepository.GetSprite(cannonIcon.MapSiegeMarkerSpriteId);

            if (sprite is null)
                return;

            __instance.MachineTypeIconWidget.Sprite = sprite;
        }
    }
}
