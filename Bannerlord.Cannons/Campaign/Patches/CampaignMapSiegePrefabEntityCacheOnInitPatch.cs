using Bannerlord.Cannons.Infrastructure.Icons;
using Bannerlord.Cannons.DI;
using Bannerlord.Cannons.Infrastructure.Registry;
using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using SandBox;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.Integration.Campaign.Patches
{
    public class CampaignMapSiegePrefabEntityCacheOnInitPatch : IPatch
    {
        public MethodInfo TargetMethod =>
            AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCache), "OnInit");

        public MethodInfo PatchMethod =>
            AccessTools.Method(typeof(CampaignMapSiegePrefabEntityCacheOnInitPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(CampaignMapSiegePrefabEntityCache __instance)
        {
            CampaignMapSiegePrefabEntityCacheState.SiegeLaunchFrames.Clear();
            CampaignMapSiegePrefabEntityCacheState.SiegeProjectileScales.Clear();

            var repo = new PrefabSiegeEngineRepository(CannonsRuntimeServices.GetRequiredService<ICannonRegistry>());
            foreach (var prefabSiegeEngine in repo.GetPrefabSiegeEngines())
            {
                var gameEntity = GameEntity.Instantiate(
                    ((MapScene)TaleWorlds.CampaignSystem.Campaign.Current.MapSceneWrapper).Scene,
                    prefabSiegeEngine.SiegeEngineMapPrefabName, true);
                var launchFrame = gameEntity.GetChild(0)
                    .GetFirstChildEntityWithTag("projectile_position")
                    .GetGlobalFrame();
                var launchScale = gameEntity.GetChild(0).GetFrame().rotation.GetScaleVector();

                CampaignMapSiegePrefabEntityCacheState.SiegeLaunchFrames[prefabSiegeEngine.SiegeEngineId] = launchFrame;
                CampaignMapSiegePrefabEntityCacheState.SiegeProjectileScales[prefabSiegeEngine.SiegeEngineId] = launchScale;
            }
        }
    }
}
