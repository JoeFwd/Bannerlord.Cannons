using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    public class GenericCannon : SpawnableArtilleryRangedSiegeWeapon
    {
        public GenericCannon()
            : this(NullLoggerFactory.Instance)
        {
        }

        public GenericCannon(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
        }

        [EditorVisibleScriptComponentVariable(true)]
        public string SiegeEngineId = "";

        public override SiegeEngineType GetSiegeEngineType()
        {
            return MBObjectManager.Instance.GetObject<SiegeEngineType>(SiegeEngineId);
        }
    }
}
