using System;
using Bannerlord.Cannons.DI;
using Bannerlord.Cannons.Initialisation;
using Bannerlord.Cannons.Integration.Campaign;
using Harmony.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons
{
    public class SubModule : MBSubModuleBase
    {
        private IServiceProvider _serviceProvider = null!;
        private bool _isInitialised;

        private void EnsureInitialised()
        {
            if (_isInitialised)
                return;

            FixEnumEditorVariablePatch.Apply();
            var validCannons = _serviceProvider.GetRequiredService<CannonRegistryBootstrapper>().Bootstrap();
            _serviceProvider.GetRequiredService<DynamicScriptTypeRegistrar>().Register(validCannons);
            _serviceProvider.GetRequiredService<CannonIconRegistrar>().Register();
            _serviceProvider.GetRequiredService<IHarmonyPatcher>().ApplyPatches();

            _isInitialised = true;
        }

        protected override void OnSubModuleLoad()
        {
            _serviceProvider = new CannonsServiceContainer().Build();
            CannonsRuntimeServices.Set(_serviceProvider);
            base.OnSubModuleLoad();
            EnsureInitialised();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            _serviceProvider.GetRequiredService<CampaignModelRegistrar>().Register(game, starterObject);
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            _serviceProvider.GetRequiredService<MissionLogicRegistrar>().AddTo(mission);
        }

    }
}
