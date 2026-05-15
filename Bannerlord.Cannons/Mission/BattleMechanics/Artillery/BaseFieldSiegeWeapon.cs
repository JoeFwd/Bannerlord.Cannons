using NetworkMessages.FromServer;
using Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions;
using Bannerlord.Cannons.BattleMechanics.Artillery.Components;
using Bannerlord.Cannons.Domain.Ammo;
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
        protected AmmoLimit _ammoLimitEnforcer;
        protected StandingPoint? ActiveAmmoPickupPoint { get; private set; }
        private Vec3 MissleStartingPositionForSimulation => MissileStartingPositionEntityForSimulation?.GlobalPosition ?? Vec3.Zero;
        private readonly ResolveActivePickupPointUseCase _resolveUseCase = new();

        public bool PreferHighAngle = false;
        public abstract float ProjectileVelocity { get; }
        private BattleSideEnum _side;
        public override BattleSideEnum Side => _side;
        public void SetSide(BattleSideEnum side) => _side = side;
        public Target Target { get; protected set; }
        private Team _team;
        public Team Team
        {
            get => _team ??= Side == BattleSideEnum.Attacker
                ? Mission.Current?.Teams.Attacker
                : Mission.Current?.Teams.Defender;
            set => _team = value;
        }
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
            _ammoLimitEnforcer = _ammoLimitEnforcer ?? new AmmoLimit(OnAmmoConsumed);
        }

        private void EnsureComponentsInitialised()
        {
            if (_ballisticsService == null
                || _fireSafetyChecker == null
                || _ammoLimitEnforcer == null)
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
            return _ballisticsService.GetEstimatedFlightTime(ShootingSpeed, CurrentReleaseAngle, MissleStartingPositionForSimulation, Target.SelectedWorldPosition);
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

        public void ApplyConfiguredStartingAmmo()
        {
            EnsureComponentsInitialised();
            _ammoLimitEnforcer.SyncFromWeapon(AmmoCount);

            if (!IsAmmoMeshReady)
            {
                _ammoLimitEnforcer.TrySetAmmo(System.Math.Max(0, AmmoCount));
                AmmoCount = _ammoLimitEnforcer.AmmoCount;
                return;
            }

            if (_ammoLimitEnforcer.TrySetAmmo(System.Math.Max(0, AmmoCount)))
                ApplyAmmoStateToWeapon(updateAmmoMesh: true, runAmmoCheck: true, broadcastAmmoCount: false);
            else
                CheckAmmo();
        }

        protected void ForceAmmoPointUsage()
        {
            EnsureComponentsInitialised();
            if (AmmoPickUpPoints == null || AmmoPickUpPoints.Count == 0)
            {
                ActiveAmmoPickupPoint = null;
                return;
            }

            var port = new BannerlordAmmoPickupPointPort(
                LoadAmmoStandingPoint,
                AmmoPickUpPoints,
                ReloaderAgent);

            var request = port.CreateResolveRequest(ToAmmoWeaponState(State), _ammoLimitEnforcer.HasAmmo);
            var result = _resolveUseCase.Execute(request);
            port.ApplyAvailability(result.ActivationCommands);
            ActiveAmmoPickupPoint = port.ResolveStandingPoint(result.ActivePointId);
        }

        protected override bool HasAmmo
        {
            get => _ammoLimitEnforcer?.HasAmmo ?? base.HasAmmo;
            set
            {
                if (_ammoLimitEnforcer == null)
                {
                    base.HasAmmo = value;
                    return;
                }

                _ammoLimitEnforcer.SetHasAmmo(value);
            }
        }

        public override void SetAmmo(int ammoLeft)
        {
            EnsureComponentsInitialised();
            if (!_ammoLimitEnforcer.TrySetAmmo(ammoLeft))
                return;

            ApplyAmmoStateToWeapon(updateAmmoMesh: true, runAmmoCheck: true, broadcastAmmoCount: false);
        }

        protected override void ConsumeAmmo()
        {
            EnsureComponentsInitialised();
            _ammoLimitEnforcer.TryConsumeAmmo();
        }

        protected override void CheckAmmo()
        {
            EnsureComponentsInitialised();
            _ammoLimitEnforcer.SyncFromWeapon(AmmoCount);

            if (!_ammoLimitEnforcer.CheckAmmo())
                return;

            SetForcedUse(value: false);

            if (AmmoPickUpPoints == null)
                return;

            foreach (var ammoPickUpStandingPoint in AmmoPickUpPoints)
            {
                ammoPickUpStandingPoint.IsDeactivated = true;
            }
        }

        private bool IsAmmoMeshReady => AmmoPickUpPoints != null && AmmoPickUpPoints.Count > 0;

        private static AmmoWeaponState ToAmmoWeaponState(WeaponState state)
            => state == WeaponState.LoadingAmmo
                ? AmmoWeaponState.LoadingAmmo
                : AmmoWeaponState.Other;

        private void OnAmmoConsumed()
        {
            ApplyAmmoStateToWeapon(updateAmmoMesh: true, runAmmoCheck: true, broadcastAmmoCount: true);
        }

        private void ApplyAmmoStateToWeapon(bool updateAmmoMesh, bool runAmmoCheck, bool broadcastAmmoCount)
        {
            AmmoCount = _ammoLimitEnforcer.AmmoCount;

            if (broadcastAmmoCount && GameNetwork.IsServerOrRecorder)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SetRangedSiegeWeaponAmmo(Id, AmmoCount));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
            }

            if (updateAmmoMesh && IsAmmoMeshReady)
                UpdateAmmoMesh();

            if (runAmmoCheck)
                CheckAmmo();
        }

        /// <summary>
        /// Casts a ray from the muzzle to <paramref name="targetPos"/> (at ~chest height) and
        /// reports whether a shot can reach the target.
        ///
        /// Returns <c>true</c> when the path is clear or when only a destructible entity is in
        /// the way (the cannon can break it open). In the latter case <paramref name="blockingEntity"/>
        /// is set to the obstructing entity so callers can decide to aim at it instead.
        /// Returns <c>false</c> when an indestructible obstacle blocks the path.
        /// </summary>
        public bool TryGetLineOfSightObstacle(Vec3 targetPos, out GameEntity? blockingEntity)
        {
            blockingEntity = null;
            Vec3 muzzlePos = MissleStartingPositionForSimulation;
            Vec3 aimPoint = targetPos + new Vec3(0f, 0f, 1f); // approximate chest height
            float targetDistance = (aimPoint - muzzlePos).Length;
            if (targetDistance < 0.001f)
                return true;

            float collisionDistance;
            WeakGameEntity hitEntity;
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                Scene.RayCastForClosestEntityOrTerrain(
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

            if (hitEntity.IsValid && hitEntity.GetFirstScriptOfTypeInFamily<DestructableComponent>() != null)
            {
                blockingEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(hitEntity);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> when there is a shootable path to <paramref name="targetPos"/>:
        /// either clear line-of-sight, or only a destructible entity in the way.
        /// </summary>
        public bool HasLineOfSightToTarget(Vec3 targetPos)
            => TryGetLineOfSightObstacle(targetPos, out _);

        public Vec3 GetBallisticErrorAppliedDirection(float ballisticErrorAmount)
        {
            EnsureComponentsInitialised();
            return _ballisticsService.GetBallisticErrorAppliedDirection(ShootingDirection, ballisticErrorAmount);
        }

        public bool IsTargetWithinDirectionRestriction(Vec3 target) => CanShootAtPoint(target);

        /// <summary>
        /// Aims at <paramref name="target"/> using world-up axis angle calculation — equivalent
        /// to v1.2's <c>AimAtThreat</c> — so the computed direction is consistent with
        /// <c>ApplyCurrentDirectionToEntity</c> which also rotates around world up.
        /// </summary>
        public bool AimAtTargetWorldUp(Vec3 target)
        {
            float releaseAngle = GetTargetReleaseAngle(target);
            if (float.IsNaN(releaseAngle) || float.IsInfinity(releaseAngle) || releaseAngle > MathF.PI / 2f)
                return false;

            MatrixFrame globalFrame = GameEntity.GetGlobalFrame();
            globalFrame.rotation.RotateAboutUp(MathF.PI);
            float targetDirection = globalFrame.TransformToLocal(target).AsVec2.RotationInRadians;

            targetDirection = MBMath.ClampAngle(targetDirection, 0f, DirectionRestriction);
            releaseAngle    = MBMath.ClampAngle(releaseAngle, ReleaseAngleRestrictionCenter, ReleaseAngleRestrictionAngle);

            GiveExactInput(targetDirection, releaseAngle);
            return CheckIsTargetReached(target);
        }
    }
}
