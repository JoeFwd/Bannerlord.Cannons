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

        /// <summary>
        /// Returns true when the straight-line path from the muzzle to <paramref name="targetPos"/>
        /// (sampled at ~chest height) is unobstructed, or is only blocked by a destructible entity
        /// (e.g. a wall that can be knocked down). Non-destructible terrain and static entities
        /// cause the method to return false so the AI skips targets sheltered behind solid cover.
        /// </summary>
        public bool HasLineOfSightToTarget(Vec3 targetPos)
        {
            Vec3 muzzlePos = MissleStartingPositionForSimulation;
            Vec3 aimPoint = targetPos + new Vec3(0f, 0f, 1f); // approximate chest height
            float targetDistance = (aimPoint - muzzlePos).Length;
            if (targetDistance < 0.001f)
                return true;

            float collisionDistance;
            GameEntity hitEntity;
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                Scene.RayCastForClosestEntityOrTerrainMT(
                    muzzlePos,
                    aimPoint,
                    out collisionDistance,
                    out hitEntity,
                    0.1f,
                    BodyFlags.CommonCollisionExcludeFlagsForMissile);
            }

            // No hit, or hit is within 2 m of the target (terrain at target's feet, etc.)
            if (float.IsNaN(collisionDistance) || collisionDistance >= targetDistance - 2f)
                return true;

            // An obstacle was hit — allow only if it can be destroyed
            return hitEntity != null && hitEntity.HasScriptOfType<DestructableComponent>();
        }

        public Vec3 GetBallisticErrorAppliedDirection(float ballisticErrorAmount)
        {
            EnsureComponentsInitialised();
            return _ballisticsService.GetBallisticErrorAppliedDirection(ShootingDirection, ballisticErrorAmount);
        }

        public void CalculateLocalAnglesFromAimFrame(Vec3 globalDirection, out float localTargetDirection, out float localTargetAngle)
        {
            globalDirection.Normalize();
            Vec2 aimDirection = ShootingDirection.AsVec2;
            if (aimDirection.LengthSquared < 0.0001f)
            {
                aimDirection = (RotationObject?.GameEntity?.GetGlobalFrame().rotation.f ?? GameEntity.GetGlobalFrame().rotation.f).AsVec2;
            }

            if (aimDirection.LengthSquared < 0.0001f)
            {
                localTargetDirection = 0f;
                localTargetAngle = 0f;
                return;
            }

            aimDirection = aimDirection.Normalized();
            Vec2 targetDirection = globalDirection.AsVec2;
            if (targetDirection.LengthSquared < 0.0001f)
            {
                localTargetDirection = 0f;
                localTargetAngle = MathF.PI / 2f;
                return;
            }

            targetDirection = targetDirection.Normalized();
            float aimYaw = aimDirection.RotationInRadians;
            float targetYaw = targetDirection.RotationInRadians;
            localTargetDirection = MBMath.WrapAngle(targetYaw - aimYaw);
            localTargetAngle = MathF.Atan2(globalDirection.Z, targetDirection.Length);
        }

        public bool AimAtTargetUsingAimFrame(Vec3 target)
        {
            if (!TryCalculateLocalDirectionAndLocalAngleToShootTargetUsingAimFrame(target, out float localTargetDirection, out float localTargetAngle))
                return false;

            if (localTargetDirection >= MathF.PI)
                return false;

            GiveExactInput(localTargetDirection, localTargetAngle);
            return MathF.Abs(currentDirection - localTargetDirection) < 0.001f
                   && MathF.Abs(currentReleaseAngle - localTargetAngle) < 0.001f;
        }

        public bool CanShootAtPointUsingAimFrame(Vec3 target)
        {
            if (!TryCalculateLocalDirectionAndLocalAngleToShootTargetUsingAimFrame(target, out float localTargetDirection, out float localTargetAngle))
                return false;

            return IsAngleWithinRestrictions(localTargetDirection, localTargetAngle) && IsTargetInRange(target);
        }

        public bool IsTargetWithinDirectionRestriction(Vec3 target)
        {
            if (!TryCalculateLocalDirectionAndLocalAngleToShootTargetUsingAimFrame(target, out float localTargetDirection, out float localTargetAngle))
                return false;

            return IsAngleWithinRestrictions(localTargetDirection, localTargetAngle);
        }

        public bool TryGetAbsoluteHorizontalAngleToTarget(Vec3 target, out float absoluteLocalTargetDirection)
        {
            absoluteLocalTargetDirection = MathF.PI;
            if (!TryCalculateLocalDirectionAndLocalAngleToShootTargetUsingAimFrame(target, out float localTargetDirection, out _))
                return false;

            absoluteLocalTargetDirection = MathF.Abs(localTargetDirection);
            return true;
        }

        private bool TryCalculateLocalDirectionAndLocalAngleToShootTargetUsingAimFrame(
            Vec3 target,
            out float localTargetDirection,
            out float localTargetAngle)
        {
            float targetReleaseAngle = GetTargetReleaseAngle(target);
            if (float.IsNaN(targetReleaseAngle) || float.IsInfinity(targetReleaseAngle) || targetReleaseAngle > MathF.PI / 2f)
            {
                localTargetDirection = MathF.PI;
                localTargetAngle = MathF.PI;
                return false;
            }

            Vec2 horizontalDirection = (target - MissleStartingPositionForSimulation).AsVec2.Normalized();
            if (horizontalDirection.LengthSquared < 0.0001f)
            {
                localTargetDirection = 0f;
                localTargetAngle = targetReleaseAngle;
                return true;
            }

            Vec3 globalDirection = new Vec3(horizontalDirection.x, horizontalDirection.y, 0f);
            globalDirection += new Vec3(0f, 0f, MathF.Sin(targetReleaseAngle));
            globalDirection.Normalize();

            CalculateLocalAnglesFromAimFrame(globalDirection, out localTargetDirection, out localTargetAngle);
            return !(float.IsNaN(localTargetDirection) || float.IsNaN(localTargetAngle));
        }

        private bool IsAngleWithinRestrictions(float localTargetDirection, float localTargetAngle)
        {
            if (localTargetAngle < BottomReleaseAngleRestriction || localTargetAngle > TopReleaseAngleRestriction)
                return false;

            return DirectionRestriction / 2f - MathF.Abs(localTargetDirection) >= 0f;
        }
    }
}
