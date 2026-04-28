using Bannerlord.Cannons.Domain;
using Bannerlord.Cannons.Initialisation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Bannerlord.Cannons.Application.DependencyInjection;
using Bannerlord.Cannons.Infrastructure.DependencyInjection;
using System.Reflection;

namespace Bannerlord.Cannons.DI;

public class CannonsServiceContainer
{
    public IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddCannonsInfrastructure();
        services.AddCannonsApplication();
        RegisterHarmonyPatches(services);
        HarmonyDependencyInjectionCompat.AddHarmonyPatching(services);

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

    private static void RegisterHarmonyPatches(IServiceCollection services)
    {
        var patchInterface = Type.GetType("Harmony.DependencyInjection.Patches.IPatch, Harmony.DependencyInjection");
        if (patchInterface == null)
            return;

        var patchTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && patchInterface.IsAssignableFrom(type));

        foreach (var patchType in patchTypes)
            services.AddSingleton(patchInterface, patchType);
    }
}
