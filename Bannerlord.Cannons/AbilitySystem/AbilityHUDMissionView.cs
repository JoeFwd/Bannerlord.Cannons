using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.AbilitySystem
{
    [DefaultView]
    class AbilityHUDMissionView : MissionView
    {
        private int _countOfAbilities;
        private bool _isInitialized;
        private AbilityHUD_VM _abilityHUD_VM;
        private AbilityRadialSelection_VM _abilityRadialSelection_VM;
        private GauntletLayer _abilityLayer;
        private GauntletLayer _radialMenuLayer;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            Mission.Current.OnMainAgentChanged += (o, s) => CheckMainAgent();

            _abilityHUD_VM = new AbilityHUD_VM();
            _abilityLayer = new GauntletLayer(100);
            _abilityLayer.LoadMovie("AbilityHUD", _abilityHUD_VM);
            MissionScreen.AddLayer(_abilityLayer);

            _abilityRadialSelection_VM = new AbilityRadialSelection_VM();
            _radialMenuLayer = new GauntletLayer(98);
            _radialMenuLayer.LoadMovie("AbilityRadialSelection", _abilityRadialSelection_VM);
            MissionScreen.AddLayer(_radialMenuLayer);

            _isInitialized = true;
        }

        private void CheckMainAgent()
        {
            if (Agent.Main != null)
            {
                var component = Agent.Main.GetComponent<AbilityComponent>();
                if (component != null)
                {
                    _countOfAbilities = component.KnownAbilitySystem.Count;
                    if (_abilityRadialSelection_VM != null) _abilityRadialSelection_VM.FillAbilities(Agent.Main);
                }
            }
        }

        public void DisplayErrorMessage(string message)
        {
            if(_abilityRadialSelection_VM != null) _abilityRadialSelection_VM.DisplayErrorMessage(message);
        }

        public override void OnMissionTick(float dt)
        {
            if (_isInitialized)
            {
                bool canHudBeVisible = Agent.Main != null &&
                                       Agent.Main.State == AgentState.Active &&
                                       (Mission.Current.Mode == MissionMode.Battle || 
                                       Mission.Current.Mode == MissionMode.Stealth) &&
                                       MissionScreen.CustomCamera == null &&
                                       !MissionScreen.IsViewingCharacter() &&
                                       !MissionScreen.IsPhotoModeEnabled &&
                                       !ScreenManager.GetMouseVisibility();
                if (canHudBeVisible && _countOfAbilities > 0)
                {
                    _abilityHUD_VM.RefreshValues();
                    _abilityRadialSelection_VM.RefreshValues();
                }
                else
                {
                    _abilityHUD_VM.IsVisible = false;
                    _abilityRadialSelection_VM.IsVisible = false;
                }
            }
        }
    }
}
