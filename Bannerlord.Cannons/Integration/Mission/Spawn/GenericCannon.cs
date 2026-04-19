using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    public class GenericCannon : SpawnableArtilleryRangedSiegeWeapon
    {
        [EditorVisibleScriptComponentVariable(true)]
        public string SiegeEngineId = "";

        public override SiegeEngineType GetSiegeEngineType()
        {
            return MBObjectManager.Instance.GetObject<SiegeEngineType>(SiegeEngineId);
        }
    }
}
