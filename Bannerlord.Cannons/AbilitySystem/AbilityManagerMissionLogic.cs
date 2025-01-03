using System;
using System.Collections.Generic;
using TOR_Core.GameManagers;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.Extensions;
using TOR_Core.BattleMechanics.AI.CastingAI.Components;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.Localization;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem
{
    public class AbilityManagerMissionLogic : MissionLogic
    {
        private const int MaxPlaceableCannons = 10;
        
        private bool _shouldSheathWeapon;
        private bool _shouldWieldWeapon;
        private bool _shouldPlayIdleCastStanceAnim;
        private bool _hasInitializedForMainAgent;
        private AbilityModeState _currentState = AbilityModeState.Off;
        private EquipmentIndex _mainHand;
        private EquipmentIndex _offHand;
        private AbilityComponent _abilityComponent;
        private GameKeyContext _keyContext = HotKeyManager.GetCategory("CombatHotKeyCategory");
        private static ActionIndexCache _idleAnimation = ActionIndexCache.Create("act_spellcasting_idle");
        private ParticleSystem[] _psys = null;
        private readonly string _castingStanceParticleName = "psys_spellcasting_stance";
        private readonly float DamagePortionForChargingCareerAbility = 1f;
        private Dictionary<Team, int> _artillerySlots = [];
        private GameKey _quickCastMenuKey;
        private GameKey _quickCast;
        private GameKey _specialMoveKey;
        private AbilityHUDMissionView _abilityView;
        private int _timeRequestID = 1338;
        private float _lastActivationDeltaTime;
        private float _disableCombatActionsDuration = 0.3f;
        private bool _disableCombatActionsAfterCast;
        private float _elapsedTimeSinceLastActivation;
        private bool _wieldOffHandStaff;
        public delegate void OnHideOutBossFightInit();
        public event OnHideOutBossFightInit OnInitHideOutBossFight;

        public AbilityModeState CurrentState => _currentState;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            Mission.OnItemPickUp += OnItemPickup;
        }

        public override void EarlyStart()
        {
            base.EarlyStart();
            OnInitHideOutBossFight = null;
            _abilityView = Mission.Current.GetMissionBehavior<AbilityHUDMissionView>();
            Game.Current.EventManager.RegisterEvent(new Action<MissionPlayerToggledOrderViewEvent>(OnPlayerToggleOrder));
            _quickCastMenuKey = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey("QuickCastSelectionMenu");
            _quickCast = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey("QuickCast");
            _specialMoveKey = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey("CareerAbilityCast");
        }

        public override void OnPreMissionTick(float dt)
        {
            _elapsedTimeSinceLastActivation += dt;
            if(_disableCombatActionsAfterCast && _elapsedTimeSinceLastActivation > (_lastActivationDeltaTime + _disableCombatActionsDuration))
            {
                _disableCombatActionsAfterCast = false;
            }

            if (!_hasInitializedForMainAgent)
            {
                if (Agent.Main != null)
                {
                    _abilityComponent = Agent.Main.GetComponent<AbilityComponent>();
                    SetUpCastStanceParticles();
                    _hasInitializedForMainAgent = true;
                }
            }
            else if (IsAbilityModeAvailableForMainAgent())
            {
                CheckIfMainAgentHasPendingActivation();

                HandleInput(dt);

                UpdateWieldedItems();

                HandleAnimations();
            }
        }

        private void EnableTargetingMode()
        {
            _mainHand = Agent.Main.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            _offHand = Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand);
            _currentState = AbilityModeState.Targeting;
            _abilityView.MissionScreen?.SetRadialMenuActiveState(false);

            ChangeKeyBindings();
            SlowDownTime(true);

            _shouldSheathWeapon = false;
            _shouldPlayIdleCastStanceAnim = false;
        }

        private void EnableQuickSelectionMenuMode()
        {
            _currentState = AbilityModeState.QuickMenuSelection;
            _abilityView.MissionScreen?.SetRadialMenuActiveState(true);
            _mainHand = Agent.Main.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            _offHand = Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand);
            ChangeKeyBindings();
            SlowDownTime(true);
        }

        private void SlowDownTime(bool enable)
        {
            bool isSlowTimeActive = Mission.Current.GetRequestedTimeSpeed(_timeRequestID, out _);
            if(isSlowTimeActive && !enable)
            {
                Mission.Current.RemoveTimeSpeedRequest(_timeRequestID);
                return;
            }
            else if(!isSlowTimeActive && enable)
            {
                Mission.TimeSpeedRequest timeRequest = new(0.3f, _timeRequestID);
                _timeRequestID = timeRequest.RequestID;
                Mission.Current.AddTimeSpeedRequest(timeRequest);
            }
        }

        private void DisableAbilityMode(bool isTakingNewWeapon, TextObject errorMessage)
        {
            if (isTakingNewWeapon)
            {
                _mainHand = EquipmentIndex.None;
                _offHand = EquipmentIndex.None;
            }
            else
            {
                _shouldWieldWeapon = true;
            }

            _currentState = AbilityModeState.Off;
            if (_abilityComponent != null) _abilityComponent.LastCastWasQuickCast = false;

            ChangeKeyBindings();
            SlowDownTime(false);
            _abilityView.MissionScreen?.SetRadialMenuActiveState(false);
            // TODO: completely remove ItemTraitAgentComponents ?
            // var traitcomp = Agent.Main.GetComponent<ItemTraitAgentComponent>();
            // traitcomp?.EnableAllParticles(true);

            EnableCastStanceParticles(false);
            if(errorMessage != null)
            {
                _abilityView.DisplayErrorMessage(errorMessage.ToString());
            }
        }

        internal void OnCastStart(Ability ability, Agent agent)
        {
            if (agent == Agent.Main)
            {
                _currentState = AbilityModeState.Casting;
            }
        }

        internal void OnCastComplete(Ability ability, Agent agent)
        {
            if (ability is ItemBoundAbility && ability.Template.AbilityEffectType == AbilityEffectType.ArtilleryPlacement)
            {
                if (_artillerySlots.ContainsKey(agent.Team))
                {
                    _artillerySlots[agent.Team]--;
                }
            }

            if (agent == Agent.Main)
            {
                if (CurrentState == AbilityModeState.Casting) DisableAbilityMode(false, null);
            }
        }

        private void HandleInput(float dt)
        {
            if (Input.IsKeyDown(InputKey.Tab))
                return;

            if(_currentState == AbilityModeState.QuickMenuSelection || _currentState == AbilityModeState.Targeting)
            {
                if (Input.IsKeyPressed(InputKey.RightMouseButton))
                {
                    DisableAbilityMode(false, null);
                    return;
                }
            }

            switch (_currentState)
            {
                case AbilityModeState.Off:
                    {
                        if (Input.IsKeyPressed(_quickCastMenuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(_quickCastMenuKey.ControllerKey.InputKey))
                        {
                            EnableQuickSelectionMenuMode();
                        }
                        else if ((Input.IsKeyPressed(_quickCast.KeyboardKey.InputKey) || Input.IsKeyPressed(_quickCast.ControllerKey.InputKey)) && _abilityComponent.CurrentAbility != null && !_abilityComponent.CurrentAbility.IsDisabled(Agent.Main, out _))
                        {
                            _abilityComponent.LastCastWasQuickCast = true;
                            if (!Agent.Main.TryCastCurrentAbility(out TextObject failureReason))
                            {
                                DisableAbilityMode(false, failureReason);
                            }
                        }
                    }
                    break;
                case AbilityModeState.QuickMenuSelection:
                    {
                        if (!Input.IsKeyDown(_quickCastMenuKey.KeyboardKey.InputKey) && !Input.IsKeyDown(_quickCastMenuKey.ControllerKey.InputKey))
                        {
                            if (_abilityComponent.CurrentAbility.IsDisabled(Agent.Main, out TextObject failureReason))
                            {
                                DisableAbilityMode(false, failureReason);
                                return;
                            }

                            if (_abilityComponent.CurrentAbility.RequiresTargeting)
                            {
                                EnableTargetingMode();
                            }
                            else
                            {
                                if (!Agent.Main.TryCastCurrentAbility(out failureReason))
                                {
                                    DisableAbilityMode(false, failureReason);
                                }
                                else
                                {
                                    _mainHand = Agent.Main.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                                    _offHand = Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand);
                                    _lastActivationDeltaTime = dt;
                                    _elapsedTimeSinceLastActivation = 0;
                                    _disableCombatActionsAfterCast = true;
                                }
                            }
                        }
                    }
                    break;
                case AbilityModeState.Targeting:
                    {
                        if (Input.IsKeyPressed(InputKey.LeftMouseButton))
                        {
                            bool flag = _abilityComponent.CurrentAbility.Crosshair == null ||
                                        !_abilityComponent.CurrentAbility.Crosshair.IsVisible;
                            if (!flag)
                            {
                                if (!Agent.Main.TryCastCurrentAbility(out TextObject failureReason))
                                {
                                    DisableAbilityMode(false, failureReason);
                                }
                                else
                                {
                                    _lastActivationDeltaTime = dt;
                                    _elapsedTimeSinceLastActivation = 0;
                                    _disableCombatActionsAfterCast = true;
                                }
                            }
                        }
                        else if (Input.IsKeyPressed(_quickCastMenuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(_quickCastMenuKey.ControllerKey.InputKey))
                        {
                            EnableQuickSelectionMenuMode();
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void CheckIfMainAgentHasPendingActivation()
        {
            if (_abilityComponent.CurrentAbility.IsActivationPending) _abilityComponent.CurrentAbility.ActivateAbility(Agent.Main);
        }

        private void HandleAnimations()
        {
            if (CurrentState != AbilityModeState.Off)
            {
                var action = Agent.Main.GetCurrentAction(1);
                if (CurrentState == AbilityModeState.Targeting && _shouldPlayIdleCastStanceAnim && action != _idleAnimation)
                {
                    Agent.Main.SetActionChannel(1, _idleAnimation);
                }
            }
        }

        private void UpdateWieldedItems()
        {
            if (_currentState == AbilityModeState.Targeting && _shouldSheathWeapon)
            {
                if (Agent.Main.GetWieldedItemIndex(Agent.HandIndex.MainHand) != EquipmentIndex.None)
                {
                    Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
                }
                
                _shouldSheathWeapon = false;
            }

            if (_currentState == AbilityModeState.Off && _shouldWieldWeapon)
            {
                if (Agent.Main.GetWieldedItemIndex(Agent.HandIndex.MainHand) != _mainHand)
                {
                    Agent.Main.TryToWieldWeaponInSlot(_mainHand, Agent.WeaponWieldActionType.WithAnimation, false);
                }
                else if (Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand) != _offHand)
                {
                    Agent.Main.TryToWieldWeaponInSlot(_offHand, Agent.WeaponWieldActionType.WithAnimation, false);
                }
                _shouldWieldWeapon = false;
            }
        }

        public int GetArtillerySlotsLeftForTeam(Team team)
        {
            _artillerySlots.TryGetValue(team, out int slotsLeft);
            return slotsLeft;
        }

        public override void OnTeamDeployed(Team team)
        {
            InitTeam(team);
        }

        private void InitTeam(Team team)
        {
            if (team is null || team.TeamAgents.IsEmpty())
                return;

            RefreshMaxArtilleryCountForTeam(team);
        }

        private void RefreshMaxArtilleryCountForTeam(Team team)
        {
            if (_artillerySlots.ContainsKey(team))
            {
                _artillerySlots[team] = 0;
                foreach (var agent in team.TeamAgents)
                {
                    if (agent.CanPlaceArtillery() || agent.IsHero && agent.IsEngineerCompanion())
                    {
                        _artillerySlots[team] += MaxPlaceableCannons;
                    }
                }
            }
            else
            {
                _artillerySlots.Add(team, 0);
                RefreshMaxArtilleryCountForTeam(team);
            }
        }

        public override void OnMissionResultReady(MissionResult missionResult)
        {
            if (missionResult.PlayerDefeated || missionResult.PlayerVictory)
            {
                var agents = Mission.Current.Agents;
                foreach (var agent in agents)
                {
                    if (agent.IsMainAgent && agent.IsActive())
                    {
                        DisableAbilityMode(true, null);
                    }

                    var abilityComponent = agent.GetComponent<AbilityComponent>();
                    if (abilityComponent != null)
                    {
                        var abilities = abilityComponent.KnownAbilitySystem;
                        foreach (var ability in abilities)
                        {
                            ability.DeactivateAbility();
                        }
                    }

                    // TODO: completely remove status effects? 
                    // var comp = agent.GetComponent<StatusEffectComponent>();
                    // comp?.Dispose();
                }
            }
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            BindWeaponKeys();
            Mission.OnItemPickUp -= OnItemPickup;
        }

        public override void OnAgentCreated(Agent agent)
        {
            if (IsCastingMission())
            {
                if (agent.IsAbilityUser())
                {
                    agent.AddComponent(new AbilityComponent(agent));
                    if (agent.IsAIControlled)
                    {
                        agent.AddComponent(new WizardAIComponent(agent));
                    }
                }
            }
        }

        protected override void OnAgentControllerChanged(Agent agent, Agent.ControllerType oldController)
        {
            if (agent.Controller == Agent.ControllerType.Player)
            {
                _hasInitializedForMainAgent = false;
            }
        }

        public bool IsCastingMission()
        {
            return !Mission.IsFriendlyMission &&
                   Mission.CombatType != Mission.MissionCombatType.ArenaCombat &&
                   Mission.CombatType != Mission.MissionCombatType.NoCombat;
            ;
        }

        private bool IsAbilityModeAvailableForMainAgent()
        {
            return Agent.Main != null &&
                   Agent.Main.IsActive() &&
                   !ScreenManager.GetMouseVisibility() &&
                   IsCastingMission() &&
                   !Mission.IsInPhotoMode &&
                   !Mission.IsOrderMenuOpen &&
                   (Mission.Mode == MissionMode.Battle ||
                    Mission.Mode == MissionMode.Stealth) &&
                   _abilityComponent != null &&
                   _abilityComponent.CurrentAbility != null;
        }

        private void EnableCastStanceParticles(bool enable)
        {
            if (_psys != null)
            {
                if (_wieldOffHandStaff)
                {
                    _psys[0].SetEnable(enable);
                    return;
                }
                foreach (var psys in _psys)
                {
                    psys?.SetEnable(enable);
                }
            }
        }

        private void ChangeKeyBindings()
        {
            if (_abilityComponent != null && _currentState != AbilityModeState.Off)
            {
                UnbindWeaponKeys();
            }
            else
            {
                BindWeaponKeys();
            }
        }

        private void BindWeaponKeys()
        {
            _keyContext.GetGameKey(11).KeyboardKey.ChangeKey(InputKey.MouseScrollUp);
            _keyContext.GetGameKey(12).KeyboardKey.ChangeKey(InputKey.MouseScrollDown);
            _keyContext.GetGameKey(18).KeyboardKey.ChangeKey(InputKey.Numpad1);
            _keyContext.GetGameKey(19).KeyboardKey.ChangeKey(InputKey.Numpad2);
            _keyContext.GetGameKey(20).KeyboardKey.ChangeKey(InputKey.Numpad3);
            _keyContext.GetGameKey(21).KeyboardKey.ChangeKey(InputKey.Numpad4);
        }

        private void UnbindWeaponKeys()
        {
            _keyContext.GetGameKey(11).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(12).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(18).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(19).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(20).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(21).KeyboardKey.ChangeKey(InputKey.Invalid);
        }

        private void OnItemPickup(Agent agent, SpawnedItemEntity item)
        {
            if (agent == Agent.Main) DisableAbilityMode(true, null);
        }

        private void SetUpCastStanceParticles()
        {
            if (_abilityComponent != null)
            {
                _psys = new ParticleSystem[2];
                _psys[0] = TORParticleSystem.ApplyParticleToAgentBone(Agent.Main, _castingStanceParticleName, Game.Current.DefaultMonster.MainHandItemBoneIndex, out GameEntity entity);
                _psys[1] = TORParticleSystem.ApplyParticleToAgentBone(Agent.Main, _castingStanceParticleName, Game.Current.DefaultMonster.OffHandItemBoneIndex, out entity);
                EnableCastStanceParticles(false);
            }
        }

        private void OnPlayerToggleOrder(MissionPlayerToggledOrderViewEvent @event)
        {
            if (@event.IsOrderEnabled)
            {
                if (_currentState == AbilityModeState.Targeting || _currentState == AbilityModeState.QuickMenuSelection)
                {
                    DisableAbilityMode(false, null);
                }
            }
        }

        public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent == Agent.Main) SlowDownTime(false);
        }
    }

    public enum AbilityModeState
    {
        Off,
        QuickMenuSelection,
        Targeting,
        Casting
    }
}