using Bannerlord.Cannons.Infrastructure;
using Bannerlord.Cannons.Domain;
using Bannerlord.Cannons.Infrastructure.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace Bannerlord.Cannons.Infrastructure.DependencyInjection;

public static class CannonsInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCannonsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<CannonRegistry>();
        services.AddSingleton<ICannonRegistry>(sp => sp.GetRequiredService<CannonRegistry>());

        services.AddSingleton<ICannonConfigurationPathProvider, ModuleCannonConfigurationPathProvider>();
        services.AddSingleton<ICannonConfigurationReader, XmlCannonConfigurationReader>();
        return services;
    }
}
