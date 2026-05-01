using Bannerlord.Cannons.Domain;
using Bannerlord.Cannons.Initialisation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Bannerlord.Cannons.Application.DependencyInjection;
using Bannerlord.Cannons.Infrastructure.DependencyInjection;
using System.Reflection;
using Harmony.DependencyInjection;
using Harmony.DependencyInjection.Patches;
using Bannerlord.Cannons.Integration.Campaign.Patches;
using Bannerlord.Cannons.Integration.Mission.Battle.Patches;
using Bannerlord.Cannons.Integration.UI.Patches;

namespace Bannerlord.Cannons.DI;

public class CannonsServiceContainer
{
    public IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddCannonsInfrastructure();
        services.AddCannonsApplication();
        services.AddHarmonyPatching();

        services.AddSingleton<IPatch, CampaignMapSiegePrefabEntityCacheGetScalePatch>();
        services.AddSingleton<IPatch, CampaignMapSiegePrefabEntityCacheGetLaunchFramePatch>();
        services.AddSingleton<IPatch, CampaignMapSiegePrefabEntityCacheOnInitPatch>();
        services.AddSingleton<IPatch, ArtilleryShootProjectileAuxPatch>();
        services.AddSingleton<IPatch, ArtilleryCanShootAtPointPatch>();
        services.AddSingleton<IPatch, ArtilleryGetAirFrictionConstantPatch>();
        services.AddSingleton<IPatch, MissionSiegeWeaponsControllerPatch>();
        services.AddSingleton<IPatch, MapSiegePOIBrushWidgetManualPatch>();
        services.AddSingleton<IPatch, MapSiegePOIVMPatch>();
        services.AddSingleton<IPatch, OrderSiegeMachineItemButtonWidgetPatch>();
        services.AddSingleton<IPatch, OrderSiegeMachineVM_GetSiegeTypePatch>();

        services.AddSingleton<ValidateCannonsUseCase>();
        services.AddSingleton<CannonRegistryBootstrapper>();
        services.AddSingleton<DynamicScriptTypeRegistrar>();
        services.AddSingleton<CannonIconRegistrar>();
        services.AddSingleton<CampaignModelRegistrar>();
        services.AddSingleton<MissionLogicRegistrar>();
        services.AddSingleton<DadgBattleSceneLoader>();
        services.AddSingleton<StaticScriptTypeRegistrar>();

        return services.BuildServiceProvider();
    }
}
