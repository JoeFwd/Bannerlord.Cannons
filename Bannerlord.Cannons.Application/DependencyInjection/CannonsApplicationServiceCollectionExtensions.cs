using Microsoft.Extensions.DependencyInjection;

namespace Bannerlord.Cannons.Application.DependencyInjection;

public static class CannonsApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCannonsApplication(this IServiceCollection services)
    {
        services.AddSingleton<IArtilleryCrewProvider, ArtilleryCrewProvider>();
        return services;
    }
}
