using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.CrossHairs;
using TOR_Core.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.Battle.CrosshairMissionBehavior
{
    [OverrideView(typeof(MissionCrosshair))]
    public class CustomCrosshairMissionBehavior : MissionView
    {
        private bool _areCrosshairsInitialized;
        private ICrosshair _currentCrosshair;
        private Crosshair _weaponCrosshair;
        private AbilityCrosshair _abilityCrosshair;
        private AbilityComponent _abilityComponent;
        private AbilityManagerMissionLogic _missionLogic;

        public override void OnMissionScreenTick(float dt)
        {
            if (!_areCrosshairsInitialized)
            {
                if (Agent.Main != null && MissionScreen != null)
                    InitializeCrosshairs();
                else
                    return;
            }
            if (CanUseCrosshair())
            {
                if (CanUseAbilityCrosshair())
                {
                    if (_currentCrosshair == _weaponCrosshair)
                        _weaponCrosshair.DisableTargetGadgetOpacities();

                    if (_currentCrosshair != _abilityCrosshair)
                        ChangeCrosshair(_abilityCrosshair);
                }
                else if (!Agent.Main.WieldedWeapon.IsEmpty)
                {
                    if (_currentCrosshair != _weaponCrosshair)
                        ChangeCrosshair(_weaponCrosshair);
                }
                else
                {
                    ChangeCrosshair(null);
                }
                if (_currentCrosshair != null) _currentCrosshair.Tick();

            }
            else if (_currentCrosshair != null)
                ChangeCrosshair(null);
        }

        private void ChangeCrosshair(ICrosshair crosshair)
        {
            _currentCrosshair?.Hide();
            _currentCrosshair = crosshair;
            if (_currentCrosshair != null) _currentCrosshair.Show();
        }

        private bool CanUseCrosshair()
        {
            return Agent.Main != null &&
                   Agent.Main.State == AgentState.Active &&
                     Mission.Mode != MissionMode.Conversation &&
                     Mission.Mode != MissionMode.Deployment &&
                     Mission.Mode != MissionMode.CutScene &&
                     MissionScreen != null &&
                     MissionScreen.CustomCamera == null &&
                     (MissionScreen.OrderFlag == null || !MissionScreen.OrderFlag.IsVisible) &&
                     !MissionScreen.IsViewingCharacter() &&
                     !MissionScreen.IsPhotoModeEnabled &&
                     !MBEditor.EditModeEnabled &&
                     BannerlordConfig.DisplayTargetingReticule &&
                     !ScreenManager.GetMouseVisibility();
        }

        private bool CanUseAbilityCrosshair()
        {
            return !Mission.IsFriendlyMission &&
                   _missionLogic != null &&
                   _missionLogic.CurrentState == AbilityModeState.Targeting &&
                   !_abilityComponent.CurrentAbility.IsDisabled(Agent.Main, out _);
        }

        private void InitializeCrosshairs()
        {
            _weaponCrosshair = new Crosshair();
            _weaponCrosshair.InitializeCrosshair();

            if (Agent.Main.IsAbilityUser() && (_abilityComponent = Agent.Main.GetComponent<AbilityComponent>()) != null)
            {
                _missionLogic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
                _abilityComponent.InitializeCrosshairs();
                _abilityCrosshair = _abilityComponent.CurrentAbility?.Crosshair;
            }
            _areCrosshairsInitialized = true;

        }

        public override void OnMissionScreenFinalize()
        {
            if (!_areCrosshairsInitialized)
            {
                return;
            }
            _weaponCrosshair.FinalizeCrosshair();
            _abilityComponent = null;
            _abilityCrosshair = null;
            _areCrosshairsInitialized = false;
        }
    }
}