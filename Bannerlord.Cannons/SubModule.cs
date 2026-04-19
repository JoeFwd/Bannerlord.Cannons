using System;
using Bannerlord.Cannons.Initialisation;
using Bannerlord.Cannons.HarmonyPatches;
using Bannerlord.Cannons.Integration.Campaign;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Module = TaleWorlds.MountAndBlade.Module;

namespace Bannerlord.Cannons
{
    public class SubModule : MBSubModuleBase
    {
        private readonly CannonRegistryBootstrapper _cannonRegistryBootstrapper = new CannonRegistryBootstrapper();
        private readonly DynamicScriptTypeRegistrar _dynamicScriptTypeRegistrar = new DynamicScriptTypeRegistrar();
        private readonly CannonIconRegistrar _cannonIconRegistrar = new CannonIconRegistrar();
        private readonly HarmonyPatchApplier _harmonyPatchApplier = new HarmonyPatchApplier();
        private readonly CampaignModelRegistrar _campaignModelRegistrar = new CampaignModelRegistrar();
        private readonly MissionLogicRegistrar _missionLogicRegistrar = new MissionLogicRegistrar();
        private readonly DadgBattleSceneLoader _dadgBattleSceneLoader = new DadgBattleSceneLoader();
        private readonly StaticScriptTypeRegistrar _staticScriptTypeRegistrar = new StaticScriptTypeRegistrar();

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            FixEnumEditorVariablePatch.Apply();
            var validCannons = _cannonRegistryBootstrapper.Bootstrap();
            _dynamicScriptTypeRegistrar.Register(validCannons);
            _cannonIconRegistrar.Register();
            _harmonyPatchApplier.Apply();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            _campaignModelRegistrar.Register(game, starterObject);
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            _missionLogicRegistrar.AddTo(mission);
        }

#if !IS_MULTIPLAYER_BUILD && !RELEASE
        public override void OnGameInitializationFinished(Game game)
        {
            if (game.GameType is Campaign)
                _dadgBattleSceneLoader.Load();
        }
#endif

        public void Inject()
        {
            Module.CurrentModule.SubModules.Add(this);
            _staticScriptTypeRegistrar.RegisterAllScriptComponentBehaviors();
            _harmonyPatchApplier.Apply();
        }
    }
}
