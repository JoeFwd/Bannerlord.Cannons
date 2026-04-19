using Bannerlord.Cannons.BattleMechanics.Artillery;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    public class SpawnableArtilleryRangedSiegeWeapon : ArtilleryRangedSiegeWeapon, ISpawnable
    {
        public void SetSpawnedFromSpawner()
        {
            _spawnedFromSpawner = true;
        }
    }
}
