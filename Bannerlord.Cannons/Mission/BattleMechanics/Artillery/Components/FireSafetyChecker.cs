using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Performs the forward ray-cast safety check before the cannon fires.
    /// This is a stateless, pure-function class that replicates the logic from
    /// <c>BaseFieldSiegeWeapon.IsSafeToFire()</c> exactly.
    /// </summary>
    public class FireSafetyChecker : IFireSafetyChecker
    {
        /// <inheritdoc/>
        public bool IsSafeToFire(Scene scene, Vec3 muzzlePos, Vec3 shootingDirection, Agent pilotAgent)
        {
            float distanceA, distanceE;
            Agent agent;

            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                agent = Mission.Current.RayCastForClosestAgent(
                    muzzlePos,
                    muzzlePos + shootingDirection.NormalizedCopy() * 60,
                    out distanceA,
                    -1,
                    0.05f);

                Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(
                    muzzlePos,
                    muzzlePos + shootingDirection.NormalizedCopy() * 25,
                    out distanceE,
                    out GameEntity _,
                    0.05f);
            }

            return !(distanceA < 50 && agent != null && !agent.IsEnemyOf(pilotAgent) || distanceE < 15);
        }
    }
}
