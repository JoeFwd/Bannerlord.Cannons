using Bannerlord.Cannons.Infrastructure.Icons;
using Bannerlord.Cannons.Infrastructure.Registry;
using HarmonyLib;
using SandBox;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.HarmonyPatches
{
    [HarmonyPatch(typeof(CampaignMapSiegePrefabEntityCache), "OnInit")]
    public static class CampaignMapSiegePrefabEntityCacheOnInitPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CampaignMapSiegePrefabEntityCache __instance)
        {
            CampaignMapSiegePrefabEntityCacheState.SiegeLaunchFrames.Clear();
            CampaignMapSiegePrefabEntityCacheState.SiegeProjectileScales.Clear();

            var repo = new PrefabSiegeEngineRepository(CannonRegistry.Instance);
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
