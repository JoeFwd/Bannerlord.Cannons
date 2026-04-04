using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using Bannerlord.Cannons.BattleMechanics.Artillery.Components;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery
{
    public abstract class BaseFieldSiegeWeapon : RangedSiegeWeapon
    {
        protected IBallisticsService _ballisticsService;
        protected IFireSafetyChecker _fireSafetyChecker;
        protected IAmmoPointController _ammoPointController;

        public bool PreferHighAngle = false;
        public abstract float ProjectileVelocity { get; }
        private BattleSideEnum _side;
        public override BattleSideEnum Side => _side;
        public void SetSide(BattleSideEnum side) => _side = side;
        public Target Target { get; protected set; }
        public Team Team { get; set; }
        public void SetTarget(Target target) => Target = target;
        public void ClearTarget() => Target = null;

        protected override void OnInit()
        {
            InitialiseComponents();
            base.OnInit();
        }

        protected virtual void InitialiseComponents()
        {
            _ballisticsService = _ballisticsService ?? new BallisticsService();
            _fireSafetyChecker = _fireSafetyChecker ?? new FireSafetyChecker();
            _ammoPointController = _ammoPointController ?? new AmmoPointController();
        }

        private void EnsureComponentsInitialised()
        {
            if (_ballisticsService == null || _fireSafetyChecker == null || _ammoPointController == null)
            {
                InitialiseComponents();
            }
        }

        public bool IsTargetInRange(Vec3 position)
        {
            var startPos = ProjectileEntityCurrentGlobalPosition;
            EnsureComponentsInitialised();
            return _ballisticsService.IsTargetInRange(startPos, ShootingSpeed, position);
        }

        public bool IsSafeToFire()
        {
            EnsureComponentsInitialised();
            return _fireSafetyChecker.IsSafeToFire(Scene, MissleStartingPositionForSimulation, ShootingDirection, PilotAgent);
        }

        public float GetEstimatedCurrentFlightTime()
        {
            if (Target == null) return 0;
            EnsureComponentsInitialised();
            return _ballisticsService.GetEstimatedFlightTime(ShootingSpeed, currentReleaseAngle, MissleStartingPositionForSimulation, Target.SelectedWorldPosition);
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
            EnsureComponentsInitialised();
            launchVec = Vec3.Zero;
            float angle;
            if (!_ballisticsService.TryGetReleaseAngle(MissleStartingPositionForSimulation, ShootingSpeed, target, PreferHighAngle, out angle, out launchVec))
            {
                return float.NaN;
            }

            return angle;
        }

        public override bool IsDisabledForBattleSideAI(BattleSideEnum sideEnum)
        {
            return sideEnum != Side;
        }

        protected void ForceAmmoPointUsage()
        {
            EnsureComponentsInitialised();
            _ammoPointController.ForceAmmoPointUsage(State, LoadAmmoStandingPoint, AmmoPickUpStandingPoints);
        }

        public Vec3 GetBallisticErrorAppliedDirection(float ballisticErrorAmount)
        {
            EnsureComponentsInitialised();
            return _ballisticsService.GetBallisticErrorAppliedDirection(ShootingDirection, ballisticErrorAmount);
        }
    }
}
