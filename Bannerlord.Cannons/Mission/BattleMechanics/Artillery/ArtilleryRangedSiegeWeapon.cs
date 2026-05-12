using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI;
using Bannerlord.Cannons.BattleMechanics.Artillery.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery
{
    public enum WheelRotationAxis
    {
        X,
        Y
    }

    public class ArtilleryRangedSiegeWeapon : BaseFieldSiegeWeapon
    {
        private enum CannonCycleState
        {
            None,
            Push
        }

        private IArtilleryCrewProvider _artilleryCrewProvider = null!;
        private ITargetingPolicy _targetingPolicy = null!;

        #region animations
        private ActionIndexCache _idleAnimationActionIndex;
        private ActionIndexCache _shootAnimationActionIndex;
        private ActionIndexCache _reload1AnimationActionIndex;
        private ActionIndexCache _reload2AnimationActionIndex;
        private ActionIndexCache _rotateLeftAnimationActionIndex;
        private ActionIndexCache _rotateRightAnimationActionIndex;
        private ActionIndexCache _loadAmmoBeginAnimationActionIndex;
        private ActionIndexCache _loadAmmoEndAnimationActionIndex;
        private ActionIndexCache _reload2IdleActionIndex;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public string IdleActionName;
        public string ShootActionName;
        public string Reload1ActionName;
        public string Reload2ActionName;
        public string RotateLeftActionName;
        public string RotateRightActionName;
        public string LoadAmmoBeginActionName;
        public string LoadAmmoEndActionName;
        public string Reload2IdleActionName;
        #endregion

        private readonly string _barrelTag = "Barrel";
        private readonly string _baseTag = "Battery_Base";
        private readonly string _leftWheelTag = "Wheel_L";
        private readonly string _rightWheelTag = "Wheel_R";
        public string FireSoundID = "mortar_shot_1";
        public string FireSoundID2 = "mortar_shot_2";
        public float RecoilDuration = 0.8f;
        public float PushDuration = 0.8f;
        public string DisplayName = "Artillery";
        public float BaseMuzzleVelocity = 40f;
        public float RecoilDistance = 0.6f;
        public WheelRotationAxis WheelRotationAxis = WheelRotationAxis.X;
        public string CannonShotExplosionEffect;
        private CannonEntities _cannonEntities = null!;
        private IRecoilEffect _recoilEffect = null!;
        private IWheelAnimator _wheelAnimator = null!;
        private IFireEffectsPlayer _fireEffectsPlayer = null!;
        private IAmmoPickupHandler _ammoPickupHandler = null!;
        private IAmmoLoadHandler _ammoLoadHandler = null!;
        private IAIFormationManager _aiFormationManager = null!;
        private IPostReloadReadinessPolicy _postReloadReadinessPolicy = null!;
        private float _verticalOffsetAngle;
        private MatrixFrame _barrelInitialLocalFrame;
        private Agent? _lastLoaderAgent;
        private StandingPoint? _waitStandingPoint;
        private float _lastCurrentDirection;
        private CannonCycleState _cycleState;
        public float DirectionRestrictionDegrees = 100f;

        public override float DirectionRestriction => DirectionRestrictionDegrees * (MathF.PI / 180f);
        protected override float ShootingSpeed => BaseMuzzleVelocity;
        public override float ProjectileVelocity => ShootingSpeed;
        protected override Vec3 ShootingDirection => Projectile.GameEntity.GetGlobalFrame().rotation.f;
        protected override float MaximumBallisticError => 0.2f;

        public ArtilleryRangedSiegeWeapon()
        {
            _loggerFactory = NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger<ArtilleryRangedSiegeWeapon>();
        }

        protected ArtilleryRangedSiegeWeapon(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger<ArtilleryRangedSiegeWeapon>();
        }

        protected virtual ITargetingPolicy CreateTargetingPolicy() => new TargetingPolicy();

        public override UsableMachineAIBase CreateAIBehaviorObject()
        {
            _logger.LogInformation(
                "Creating logged cannon AI for {CannonName}. Entity={CannonEntity}, Side={CannonSide}, IsSiegeBattle={IsSiegeBattle}.",
                DisplayName,
                (GameEntity.IsValid ? GameEntity.Name : null) ?? string.Empty,
                Side,
                Mission.Current?.IsSiegeBattle ?? false);

            return Mission.Current.IsSiegeBattle && Side.Equals(BattleSideEnum.Attacker)
                ? (UsableMachineAIBase) new FieldSiegeWeaponAI(this)
                : new FieldBattleWeaponAI(this, _loggerFactory);
        }

        // --- Init ---

        protected override void OnInit()
        {

            _artilleryCrewProvider = ArtilleryCrewProviderFactory.CreateArtilleryCrewProvider();
            _targetingPolicy = CreateTargetingPolicy();

            BuildInitContext();
            InitialiseOrchestratorComponents();
            base.OnInit();
            InitialiseMissileStartingPositionEntityForSimulation();
            InitialisePostBaseInitContext();
            _fireEffectsPlayer.Initialise(FireSoundID, FireSoundID2, CannonShotExplosionEffect, Scene);

            Projectile.SetVisibleSynched(false);
            TimeGapBetweenShootActionAndProjectileLeaving = 0f;
            TimeGapBetweenShootingEndAndReloadingStart = 0f;
            EnemyRangeToStopUsing = 5f;
            PilotStandingPoint.AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
            _lastCurrentDirection = CurrentDirection;
            ApplyAimChange();
            ApplyConfiguredStartingAmmo();

            Mission.Current.OnBeforeAgentRemoved += OnBeforeAgentRemoved;
        }

        protected override void OnRemoved(int removeReason)
        {
            base.OnRemoved(removeReason);
            if (Mission.Current != null)
                Mission.Current.OnBeforeAgentRemoved -= OnBeforeAgentRemoved;

            foreach (var formation in UserFormations?.ToList() ?? new List<Formation>())
                formation.StopUsingMachine(this);
        }

        private void OnBeforeAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            if (_lastLoaderAgent == affectedAgent)
                _lastLoaderAgent = null;
        }

        private void BuildInitContext()
        {
            _cannonEntities = CannonEntities.Collect(GameEntity, _baseTag, _barrelTag, _leftWheelTag, _rightWheelTag);

            RotationObject = _cannonEntities.Body;
        }

        private void InitialiseMissileStartingPositionEntityForSimulation()
        {
            if (MissileStartingPositionEntityForSimulation != null)
                return;

            var childEntities = new List<WeakGameEntity>();
            GameEntity.GetChildrenRecursive(ref childEntities);
            var match = childEntities.FirstOrDefault(x => x.IsValid && x.Name == "projectile_leaving_position");
            if (match.IsValid)
                MissileStartingPositionEntityForSimulation = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(match);
        }

        private void InitialisePostBaseInitContext()
        {
            _waitStandingPoint = StandingPoints.FirstOrDefault(sp => sp.GameEntity.HasTag(WaitStandingPointTag));
            _barrelInitialLocalFrame = _cannonEntities.Barrel.GameEntity.GetFrame();

            WeakGameEntity? projectileEntity = Projectile?.GameEntity;
            if (projectileEntity.HasValue && projectileEntity.Value.IsValid)
            {
                Vec3 shootingDirection = projectileEntity.Value.GetGlobalFrame().rotation.f;
                Vec3 v = new Vec3(0f, shootingDirection.AsVec2.Length, shootingDirection.Z);
                _verticalOffsetAngle = Vec3.AngleBetweenTwoVectors(v, Vec3.Forward);
            }
            else
            {
                _verticalOffsetAngle = 0f;
            }
        }

        private void InitialiseOrchestratorComponents()
        {
            InitialiseComponents();
            _wheelAnimator = new WheelAnimator(_cannonEntities.WheelL, _cannonEntities.WheelR, () => WheelRotationAxis);
            _recoilEffect = new RecoilEffect(_cannonEntities.Body, _wheelAnimator,
                () => RecoilDuration,
                () => PushDuration,
                () => RecoilDistance);
            _fireEffectsPlayer = new FireEffectsPlayer();
            _ammoPickupHandler = new AmmoPickupHandler(_ammoLimitEnforcer);
            _ammoLoadHandler = new AmmoLoadHandler();
            _aiFormationManager = new AIFormationManager(_artilleryCrewProvider);
            _postReloadReadinessPolicy = new FixedDelayPostReloadReadinessPolicy();
        }

        protected override void HandleUserAiming(float dt)
        {
            float prevTargetDirection = TargetDirection;
            base.HandleUserAiming(dt);

            // Fix for 360° rotation: the engine's ApproachToAngle uses raw subtraction
            // (not angle-aware arithmetic). When TargetDirection crosses the ±π boundary
            // and WrapAngle flips its sign, ApproachToAngle sees a huge raw difference and
            // rotates in the wrong direction (the "bounce"). Fix: when a wrap-around is
            // detected, shift CurrentDirection by ±2π so it stays on the same side as
            // TargetDirection. RotateAboutAnArbitraryVector uses cos/sin which are periodic,
            // so the entity rotation is visually identical.
            float targetJump = TargetDirection - prevTargetDirection;
            if (targetJump < -MathF.PI)
                CurrentDirection -= 2f * MathF.PI;
            else if (targetJump > MathF.PI)
                CurrentDirection += 2f * MathF.PI;
        }

        protected override void ApplyAimChange()
        {
            base.ApplyAimChange();
            MatrixFrame barrelFrame = _barrelInitialLocalFrame;
            barrelFrame.rotation.RotateAboutSide(CurrentReleaseAngle + _verticalOffsetAngle);
            _cannonEntities.Barrel.GameEntity.SetFrame(ref barrelFrame);
        }

        protected override void RegisterAnimationParameters()
        {
            SkeletonOwnerObjects = new SynchedMissionObject[0];
            Skeletons = new Skeleton[0];
            _idleAnimationActionIndex = ActionIndexCache.Create(IdleActionName);
            _shootAnimationActionIndex = ActionIndexCache.Create(ShootActionName);
            _reload1AnimationActionIndex = ActionIndexCache.Create(Reload1ActionName);
            _reload2AnimationActionIndex = ActionIndexCache.Create(Reload2ActionName);
            _rotateLeftAnimationActionIndex = ActionIndexCache.Create(RotateLeftActionName);
            _rotateRightAnimationActionIndex = ActionIndexCache.Create(RotateRightActionName);
            _loadAmmoBeginAnimationActionIndex = ActionIndexCache.Create(LoadAmmoBeginActionName);
            _loadAmmoEndAnimationActionIndex = ActionIndexCache.Create(LoadAmmoEndActionName);
            _reload2IdleActionIndex = ActionIndexCache.Create(Reload2IdleActionName);
        }

        // --- Tick ---

        protected override void OnTick(float dt)
        {
            CheckNullReloaderOriginalPoint();
            base.OnTick(dt);
            ForceAmmoPointUsage();
            HandleAmmoPickup();
            HandleAmmoLoad();
            HandleWaitingTimer(dt);
            UpdateRecoilEffect(dt);
            HandleRecoilReturn(dt);
            // TODO: use that for field battles because there is no native team siege engine behaviour 
            // HandleAITeamUsage(dt);
        }

        private bool IsPushInProgress() => _cycleState == CannonCycleState.Push;

        private void HandleAITeamUsage(float dt)
        {
            if (Team == null) return;
            _aiFormationManager.Update(Team, UserFormations, this);
        }

        private void CheckNullReloaderOriginalPoint()
        {
            if (ReloaderAgent == null)
                return;

            bool isInvalidForDetachment = !ReloaderAgent.IsActive()
                                          || ReloaderAgent.Team == null
                                          || ReloaderAgent.Detachment != this;

            if (ReloaderAgentOriginalPoint == null || isInvalidForDetachment)
            {
                ReloaderAgent.StopUsingGameObject(true);
                ReloaderAgent = null;
            }
        }

        private void HandleWaitingTimer(float dt)
        {
            if (State != WeaponState.WaitingBeforeIdle)
                return;

            _postReloadReadinessPolicy.Update(dt);

            if (!IsPushInProgress() && _postReloadReadinessPolicy.IsDelayElapsed)
            {
                _postReloadReadinessPolicy.Reset();
                EnterPushState();
            }
        }

        private void HandleAmmoLoad()
        {
            if (_ammoLoadHandler.Update(
                    LoadAmmoStandingPoint,
                    ref _lastLoaderAgent,
                    _loadAmmoBeginAnimationActionIndex,
                    _loadAmmoEndAnimationActionIndex,
                    OriginalMissileItem))
            {
                _postReloadReadinessPolicy.MarkReloadCompleted();

                State = WeaponState.WaitingBeforeIdle;
            }
        }

        private void HandleAmmoPickup()
        {
            Agent reloaderAgent = ReloaderAgent;
            _ammoPickupHandler.Update(
                ActiveAmmoPickupPoint,
                LoadAmmoStandingPoint,
                ReloaderAgentOriginalPoint,
                ref reloaderAgent,
                OriginalMissileItem,
                LoadedMissileItem,
                _loadAmmoEndAnimationActionIndex,
                this);
            ReloaderAgent = reloaderAgent;
        }

        // --- State transitions ---

        protected override void OnRangedSiegeWeaponStateChange()
        {
            base.OnRangedSiegeWeaponStateChange();
            switch (State)
            {
                case WeaponState.Shooting:
                    PlayFireProjectileEffects();
                    State = WeaponState.WaitingAfterShooting;
                    break;
                case WeaponState.WaitingAfterShooting:
                    DoSlideBack();
                    break;
                case WeaponState.WaitingBeforeIdle:
                    SendLoaderAgentToWaitingPoint();
                    break;
                case WeaponState.LoadingAmmo:
                    SetActivationWaitingPoint(false);
                    break;
            }
        }

        private void EnterPushState()
        {
            _cycleState = CannonCycleState.Push;
            _recoilEffect.BeginReturn();
        }

        private void ExitPushState()
        {
            _cycleState = CannonCycleState.None;
        }

        private void SendLoaderAgentToWaitingPoint()
        {
            if (_waitStandingPoint != null && CanUseAsMachineMover(_lastLoaderAgent))
            {
                SetActivationWaitingPoint(true);
                _lastLoaderAgent.AIMoveToGameObjectEnable(_waitStandingPoint, this, Agent.AIScriptedFrameFlags.NoAttack);
            }
        }

        protected override void ApplyCurrentDirectionToEntity()
        {
            base.ApplyCurrentDirectionToEntity();
            _lastCurrentDirection = CurrentDirection;
        }

        private void SetActivationWaitingPoint(bool activate)
        {
            _waitStandingPoint?.SetIsDeactivatedSynched(!activate);
        }

        private bool CanUseAsMachineMover(Agent? agent)
            => agent is { IsAIControlled: true }
               && agent.IsActive()
               && agent.Team != null
               && agent.Detachment == this;

        private void HandleRecoilReturn(float dt)
        {
            if (!IsPushInProgress()) return;

            if (_recoilEffect.UpdateReturn(dt))
            {
                ExitPushState();
                State = WeaponState.Idle;
            }
        }

        private void PlayFireProjectileEffects()
        {
            MatrixFrame frame = MissileStartingPositionEntityForSimulation.GetGlobalFrame();
            _fireEffectsPlayer.Play(frame, GameEntity.GlobalPosition);
        }

        private void DoSlideBack()
        {
            _recoilEffect.Begin(_cannonEntities.Body.GameEntity.GetFrame());
        }

        private void UpdateRecoilEffect(float dt)
        {
            if (State != WeaponState.WaitingAfterShooting) return;

            if (_recoilEffect.Update(dt))
            {
                State = WeaponState.LoadingAmmo;
                _fireEffectsPlayer.Stop();
            }
        }

        // --- Targeting and UI ---

        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            TextObject textObject;
            if (usableGameObject.GameEntity.HasTag(AmmoLoadTag))
                textObject = new TextObject("{=Na81xuXn}{KEY} Reload");
            else if (usableGameObject.GameEntity.HasTag(AmmoPickUpTag))
                textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up");
            else
                textObject = new TextObject("{=fEQAPJ2e}{KEY} Use");

            textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            return textObject;
        }

        public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
        {
            return new TextObject(DisplayName, null);
        }

        public override SiegeEngineType GetSiegeEngineType() => Side != BattleSideEnum.Attacker ? DefaultSiegeEngineTypes.Catapult : DefaultSiegeEngineTypes.Onager;

        public override TargetFlags GetTargetFlags()
        {
            return _targetingPolicy.BuildFlags(IsDestroyed, IsDeactivated, Side);
        }

        public override float GetTargetValue(List<Vec3> weaponPos)
        {
            return _targetingPolicy.ComputeBaseTargetValue(
                GetUserMultiplierOfWeapon(),
                GetDistanceMultiplierOfWeapon(weaponPos[0]),
                GetHitPointMultiplierOfWeapon());
        }

        public override float ProcessTargetValue(float baseValue, TargetFlags flags)
        {
            return _targetingPolicy.ProcessTargetValue(baseValue, flags);
        }

        protected override void GetSoundEventIndices()
        {
            MoveSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/mangonel/move");
        }

        protected override void SetActivationLoadAmmoPoint(bool activate)
        {
            LoadAmmoStandingPoint.SetIsDeactivatedSynched(!activate);
        }

    }
}
