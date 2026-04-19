using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using SandBox;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Bannerlord.Cannons.HarmonyPatches
{
    [HarmonyPatch(typeof(CampaignMapSiegePrefabEntityCache), nameof(CampaignMapSiegePrefabEntityCache.GetLaunchEntitialFrameForSiegeEngine))]
    public static class CampaignMapSiegePrefabEntityCacheGetLaunchFramePatch
    {
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void Postfix(SiegeEngineType type, BattleSideEnum side, ref MatrixFrame __result)
        {
            if (CampaignMapSiegePrefabEntityCacheState.SiegeLaunchFrames.TryGetValue(type.StringId, out var frame))
                __result = frame;
        }
    }
}
