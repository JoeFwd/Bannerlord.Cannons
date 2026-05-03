using Bannerlord.Cannons.BattleMechanics.Artillery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    public class SpawnableArtilleryRangedSiegeWeapon : ArtilleryRangedSiegeWeapon, ISpawnable
    {
        public SpawnableArtilleryRangedSiegeWeapon()
            : this(NullLoggerFactory.Instance)
        {
        }

        protected SpawnableArtilleryRangedSiegeWeapon(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
        }

        public void SetSpawnedFromSpawner()
        {
            _spawnedFromSpawner = true;
        }
    }
}
