using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery
{
    public abstract class BaseFieldSiegeWeapon : RangedSiegeWeapon
    {
        public bool PreferHighAngle = false;
        public abstract float ProjectileVelocity { get; }
        private BattleSideEnum _side;
        public override BattleSideEnum Side => _side;
        public void SetSide(BattleSideEnum side) => _side = side;
        public Target Target { get; protected set; }
        public Team Team { get; set; }
        public void SetTarget(Target target) => Target = target;
        public void ClearTarget() => Target = null;
        public bool IsTargetInRange(Vec3 position)
        {
            var startPos = ProjectileEntityCurrentGlobalPosition;
            var diff = position - startPos;
            var maxrange = Ballistics.GetMaximumRange(ShootingSpeed, diff.z);
            diff.z = 0;
            return diff.Length < maxrange;
        }

        public bool IsSafeToFire()
        {
            float distanceA, distanceE;
            Agent agent;
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                agent = Mission.Current.RayCastForClosestAgent(MissleStartingPositionForSimulation, MissleStartingPositionForSimulation + ShootingDirection.NormalizedCopy() * 60, out distanceA, -1, 0.05f);
                Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(MissleStartingPositionForSimulation, MissleStartingPositionForSimulation + ShootingDirection.NormalizedCopy() * 25, out distanceE, out GameEntity _, 0.05f);
            }
            return !(distanceA < 50 && agent != null && !agent.IsEnemyOf(PilotAgent) || distanceE < 15);
        }

        public float GetEstimatedCurrentFlightTime()
        {
            if (Target == null) return 0;
            var diff = Target.SelectedWorldPosition - MissleStartingPositionForSimulation;
            return Ballistics.GetTimeOfProjectileFlight(ShootingSpeed, currentReleaseAngle, diff.Length);
        }

        /// <summary>
        /// Populated by GetTargetReleaseAngle as a side effect whenever the native AI calls AimAtThreat.
        /// Used by the Harmony patch in ShootProjectileAux to apply proper ballistic trajectory.
        /// </summary>
        public Vec3 LastAiLaunchVector { get; private set; }

        public override float GetTargetReleaseAngle(Vec3 target)
        {
            float angle = GetTargetReleaseAngle(target, out Vec3 launchVec);
            if (!float.IsNaN(angle))
            {
                LastAiLaunchVector = launchVec;
                return angle;
            }
            return base.GetTargetReleaseAngle(target);
        }

        public float GetTargetReleaseAngle(Vec3 target, out Vec3 launchVec)
        {
            Vec3 low = Vec3.Zero;
            Vec3 high = Vec3.Zero;
            launchVec = Vec3.Zero;
            float angle = 0;
            int numSolutions = Ballistics.GetLaunchVectorForProjectileToHitTarget(MissleStartingPositionForSimulation, ShootingSpeed, target, out low, out high);
            if (numSolutions <= 0) return float.NaN;

            if (numSolutions == 2)
            {
                if (PreferHighAngle) launchVec = high;
                else launchVec = low;
            }
            else
            {
                if (low != Vec3.Zero) launchVec = low;
                else launchVec = high;
            }

            Vec3 forward = launchVec.NormalizedCopy();
            forward.z = 0;
            Vec3 dir = launchVec.NormalizedCopy();
            Vec3 diff = dir - forward;
            float zDiff = diff.z;
            angle = Vec3.AngleBetweenTwoVectors(forward, dir);
            if (zDiff < 0) angle = -angle;
            return angle;
        }

        public override bool IsDisabledForBattleSideAI(BattleSideEnum sideEnum)
        {
            return sideEnum != Side;
        }

        protected void ForceAmmoPointUsage()
        {
            if (State == WeaponState.LoadingAmmo && !LoadAmmoStandingPoint.HasUser && !LoadAmmoStandingPoint.HasAIMovingTo)
            {
                foreach (var sp in AmmoPickUpStandingPoints)
                {
                    if (sp.IsDeactivated) sp.SetIsDeactivatedSynched(false);
                }
            }
            else
            {
                foreach (var sp in AmmoPickUpStandingPoints)
                {
                    if (!sp.IsDeactivated) sp.SetIsDeactivatedSynched(true);
                }
            }
        }

        public Vec3 GetBallisticErrorAppliedDirection(float ballisticErrorAmount)
        {
            // TODO: refactor it just for our needs -> Copied from RangedSiegeWeapon
            Mat3 mat3 = new Mat3()
            {
                f = this.ShootingDirection,
                u = Vec3.Up
            };
            mat3.Orthonormalize();
            float a = MBRandom.RandomFloat * 6.28318548f;
            mat3.RotateAboutForward(a);
            float f = ballisticErrorAmount * MBRandom.RandomFloat;
            mat3.RotateAboutSide(f.ToRadians());
            return mat3.f;
        }
    }
}
