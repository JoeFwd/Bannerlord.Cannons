using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI;
using Bannerlord.Cannons.BattleMechanics.Artillery.Components;
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
        private ActionIndexCache _pushAnimationActionIndex;
        private static readonly ActionIndexCache act_pickup_boulder_begin = ActionIndexCache.Create("act_pickup_boulder_begin");
        private static readonly ActionIndexCache act_pickup_boulder_end = ActionIndexCache.Create("act_pickup_boulder_end");

        public string IdleActionName;
        public string ShootActionName;
        public string Reload1ActionName;
        public string Reload2ActionName;
        public string RotateLeftActionName;
        public string RotateRightActionName;
        public string LoadAmmoBeginActionName;
        public string LoadAmmoEndActionName;
        public string Reload2IdleActionName;
        public string PushActionName;
        #endregion

        private readonly string _barrelTag = "Barrel";
        private readonly string _baseTag = "Battery_Base";
        private readonly string _leftWheelTag = "Wheel_L";
        private readonly string _rightWheelTag = "Wheel_R";
        public string FireSoundID = "mortar_shot_1";
        public string FireSoundID2 = "mortar_shot_2";
        public float RecoilDuration = 0.1f;
        public float Recoil2Duration = 0.8f;
        public string DisplayName = "Artillery";
        public float BaseMuzzleVelocity = 40f;
        public float SlideBackFrameFactor = 0.6f;
        public float WheelRadius = 0.3f;
        public string WheelRotationAxis = nameof(Bannerlord.Cannons.BattleMechanics.Artillery.WheelRotationAxis.X);
        private WheelRotationAxis _wheelRotationAxis = Bannerlord.Cannons.BattleMechanics.Artillery.WheelRotationAxis.X;
        public string CannonShotExplosionEffect;
        private CannonEntities _cannonEntities = null!;
        private IRecoilEffect _recoilEffect = null!;
        private IWheelAnimator _wheelAnimator = null!;
        private IFireEffectsPlayer _fireEffectsPlayer = null!;
        private IAmmoPickupHandler _ammoPickupHandler = null!;
        private IAmmoLoadHandler _ammoLoadHandler = null!;
        private IAIFormationManager _aiFormationManager = null!;
        private float _verticalOffsetAngle;
        private MatrixFrame _barrelInitialLocalFrame;
        private Agent? _lastLoaderAgent;
        private StandingPoint? _waitStandingPoint;
        private StandingPoint? _pushStandingPoint;
        private float _lastCurrentDirection;
        private float _waitTimerStart;
        private bool _waitTimerRunning;
        private bool _isPushingBack;
        private bool _isRecoilReturning;
        private bool _pushAnimationStarted;
        private Agent? _pushAgent;
        public string PushStandingPointTag = "push_cannon";

        public override float DirectionRestriction => 100f;
        protected override float ShootingSpeed => BaseMuzzleVelocity;
        public override float ProjectileVelocity => ShootingSpeed;
        protected override Vec3 ShootingDirection => Projectile.GameEntity.GetGlobalFrame().rotation.f;
        protected override float MaximumBallisticError => 0.2f;

        protected virtual ITargetingPolicy CreateTargetingPolicy() => new TargetingPolicy();

        public override UsableMachineAIBase CreateAIBehaviorObject()
        {
            if (Mission.Current?.IsSiegeBattle ?? false)
                return new FieldSiegeWeaponAI(this);
            return new FieldBattleWeaponAI(this);
        }

        // --- Init ---

        protected override void OnInit()
        {
            if (!Enum.TryParse(WheelRotationAxis, out _wheelRotationAxis))
                _wheelRotationAxis = Bannerlord.Cannons.BattleMechanics.Artillery.WheelRotationAxis.X;

            _artilleryCrewProvider = ArtilleryCrewProviderFactory.CreateArtilleryCrewProvider();
            _targetingPolicy = CreateTargetingPolicy();

            BuildInitContext();
            InitialiseOrchestratorComponents();
            base.OnInit();
            InitialiseMissileStartingPositionEntityForSimulation();
            InitialisePostBaseInitContext();
            _fireEffectsPlayer.Initialise(FireSoundID, FireSoundID2, CannonShotExplosionEffect, Scene);

            Projectile.SetVisibleSynched(false);
            timeGapBetweenShootActionAndProjectileLeaving = 0f;
            timeGapBetweenShootingEndAndReloadingStart = 0f;
            EnemyRangeToStopUsing = 5f;
            PilotStandingPoint.AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
            _lastCurrentDirection = currentDirection;
            ApplyAimChange();

            Mission.Current.OnBeforeAgentRemoved += OnBeforeAgentRemoved;
        }

        protected override void OnRemoved(int removeReason)
        {
            base.OnRemoved(removeReason);
            if (Mission.Current != null)
                Mission.Current.OnBeforeAgentRemoved -= OnBeforeAgentRemoved;
        }

        private void OnBeforeAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            if (_lastLoaderAgent == affectedAgent)
                _lastLoaderAgent = null;

            if (_pushAgent == affectedAgent && _isPushingBack)
                CompletePushAnimation();
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

            var childEntities = new List<GameEntity>();
            GameEntity.GetChildrenRecursive(ref childEntities);
            MissileStartingPositionEntityForSimulation = childEntities.FirstOrDefault(x => x.Name == "projectile_leaving_position");
        }

        private void InitialisePostBaseInitContext()
        {
            _waitStandingPoint = StandingPoints.FirstOrDefault(sp => sp.GameEntity.HasTag(WaitStandingPointTag));
            _pushStandingPoint = StandingPoints.FirstOrDefault(sp => sp.GameEntity.HasTag(PushStandingPointTag));
            _pushStandingPoint?.SetIsDeactivatedSynched(true);
            _barrelInitialLocalFrame = _cannonEntities.Barrel.GameEntity.GetFrame();

            GameEntity? projectileEntity = Projectile?.GameEntity;
            if (projectileEntity != null)
            {
                Vec3 shootingDirection = projectileEntity.GetGlobalFrame().rotation.f;
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
            _wheelAnimator = new WheelAnimator(_cannonEntities.WheelL, _cannonEntities.WheelR, _wheelRotationAxis);
            _recoilEffect = new RecoilEffect(_cannonEntities.Body, _wheelAnimator, RecoilDuration, Recoil2Duration, SlideBackFrameFactor, WheelRadius);
            _fireEffectsPlayer = new FireEffectsPlayer();
            _ammoPickupHandler = new AmmoPickupHandler();
            _ammoLoadHandler = new AmmoLoadHandler();
            _aiFormationManager = new AIFormationManager(_artilleryCrewProvider);
        }

        protected override void ApplyAimChange()
        {
            base.ApplyAimChange();
            MatrixFrame barrelFrame = _barrelInitialLocalFrame;
            barrelFrame.rotation.RotateAboutSide(-currentReleaseAngle + _verticalOffsetAngle);
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
            _pushAnimationActionIndex = ActionIndexCache.Create(PushActionName);
        }

        // --- Tick ---

        protected override void OnTick(float dt)
        {
            CheckNullReloaderOriginalPoint();
            base.OnTick(dt);
            HandleAmmoPickup();
            HandleAmmoLoad();
            ForceAmmoPointUsage();
            HandleWaitingTimer();
            UpdateRecoilEffect(dt);
            HandlePushBack();
            HandleRecoilReturn(dt);
            // UpdateWheelRotation(dt);
            HandleAITeamUsage();
        }

        private void HandleAITeamUsage()
        {
            if (Team != null)
                _aiFormationManager.Update(Team, UserFormations, this);
        }

        private void CheckNullReloaderOriginalPoint()
        {
            if (ReloaderAgentOriginalPoint == null && ReloaderAgent != null)
            {
                ReloaderAgent.StopUsingGameObject(true);
                ReloaderAgent = null;
            }
        }

        private void HandleWaitingTimer()
        {
            if (State != WeaponState.WaitingBeforeIdle || Mission.Current == null)
                return;

            if (_waitTimerRunning && !_isPushingBack && !_isRecoilReturning && Mission.Current.CurrentTime >= _waitTimerStart + 2f)
            {
                _waitTimerRunning = false;
                State = WeaponState.Idle;
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
                State = WeaponState.WaitingBeforeIdle;
            }
        }

        private void HandleAmmoPickup()
        {
            Agent reloaderAgent = ReloaderAgent;
            _ammoPickupHandler.Update(
                AmmoPickUpPoints,
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
                    StartPushBackPhase();
                    break;
                case WeaponState.LoadingAmmo:
                    SetActivationWaitingPoint(false);
                    break;
            }
        }

        private void SendLoaderAgentToWaitingPoint()
        {
            if (_waitStandingPoint != null && (_lastLoaderAgent?.IsAIControlled ?? false) && _lastLoaderAgent.IsActive())
            {
                SetActivationWaitingPoint(true);
                _lastLoaderAgent.AIMoveToGameObjectEnable(_waitStandingPoint, this, Agent.AIScriptedFrameFlags.NoAttack);
            }
        }

        protected override void ApplyCurrentDirectionToEntity()
        {
            base.ApplyCurrentDirectionToEntity();
            _lastCurrentDirection = currentDirection;
        }

        private void SetActivationWaitingPoint(bool activate)
        {
            _waitStandingPoint?.SetIsDeactivatedSynched(!activate);
        }

        private void SetWaitingTimer()
        {
            if (Mission.Current == null)
                return;

            _waitTimerStart = Mission.Current.CurrentTime;
            _waitTimerRunning = true;
        }

        private void StartPushBackPhase()
        {
            _pushAnimationStarted = false;
            if (_pushStandingPoint == null)
            {
                // No push point in scene — skip phase and go straight to return
                _isRecoilReturning = true;
                _recoilEffect.BeginReturn();
                return;
            }
            _isPushingBack = true;
            SetActivationPushPoint(true);
            if ((_lastLoaderAgent?.IsAIControlled ?? false) && _lastLoaderAgent.IsActive())
                _lastLoaderAgent.AIMoveToGameObjectEnable(_pushStandingPoint, this, Agent.AIScriptedFrameFlags.NoAttack);
        }

        private void HandlePushBack()
        {
            if (!_isPushingBack || _pushStandingPoint == null) return;

            if (_pushStandingPoint.HasUser)
            {
                var user = _pushStandingPoint.UserAgent;
                _pushAgent = user;

                if (!_pushAnimationStarted)
                {
                    if (user.SetActionChannel(1, _pushAnimationActionIndex))
                        _pushAnimationStarted = true;
                    else
                        user.StopUsingGameObject(true); // can't play — release agent immediately
                }
                else if (user.GetCurrentAction(1) != _pushAnimationActionIndex)
                {
                    // Animation finished (or interrupted) — release agent
                    user.StopUsingGameObject(true);
                }
                // else: animation still in progress — wait
            }
            else if (_pushAgent != null)
            {
                // Agent left the point — push complete
                _pushAgent = null;
                _pushAnimationStarted = false;
                CompletePushAnimation();
            }
        }

        private void CompletePushAnimation()
        {
            _isPushingBack = false;
            _pushAnimationStarted = false;
            SetActivationPushPoint(false);
            _isRecoilReturning = true;
            _recoilEffect.BeginReturn();
        }

        private void HandleRecoilReturn(float dt)
        {
            if (!_isRecoilReturning) return;

            if (_recoilEffect.UpdateReturn(dt))
            {
                _isRecoilReturning = false;
                SendLoaderAgentToWaitingPoint();
                SetWaitingTimer();
            }
        }

        private void SetActivationPushPoint(bool activate)
            => _pushStandingPoint?.SetIsDeactivatedSynched(!activate);

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
            else if (usableGameObject.GameEntity.HasTag(PushStandingPointTag))
                textObject = new TextObject("{=cannon_push}{KEY} Push Cannon");
            else
                textObject = new TextObject("{=fEQAPJ2e}{KEY} Use");

            textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            return textObject;
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            return new TextObject(DisplayName, null).ToString();
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
