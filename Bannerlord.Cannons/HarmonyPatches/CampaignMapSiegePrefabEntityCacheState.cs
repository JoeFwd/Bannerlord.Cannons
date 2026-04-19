using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Bannerlord.Cannons.HarmonyPatches
{
    internal static class CampaignMapSiegePrefabEntityCacheState
    {
        internal static readonly Dictionary<string, MatrixFrame> SiegeLaunchFrames = new Dictionary<string, MatrixFrame>();
        internal static readonly Dictionary<string, Vec3> SiegeProjectileScales = new Dictionary<string, Vec3>();
    }
}
