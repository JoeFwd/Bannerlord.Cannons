using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using SandBox;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Bannerlord.Cannons.HarmonyPatches
{
    [HarmonyPatch(typeof(CampaignMapSiegePrefabEntityCache), nameof(CampaignMapSiegePrefabEntityCache.GetScaleForSiegeEngine))]
    public static class CampaignMapSiegePrefabEntityCacheGetScalePatch
    {
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void Postfix(SiegeEngineType type, BattleSideEnum side, ref Vec3 __result)
        {
            if (CampaignMapSiegePrefabEntityCacheState.SiegeProjectileScales.TryGetValue(type.StringId, out var scale))
                __result = scale;
        }
    }
}
