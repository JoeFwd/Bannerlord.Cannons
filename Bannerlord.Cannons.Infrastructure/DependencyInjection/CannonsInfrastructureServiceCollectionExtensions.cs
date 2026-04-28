using Bannerlord.Cannons.Domain;
using Bannerlord.Cannons.Infrastructure.Registry;
using Bannerlord.Cannons.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bannerlord.Cannons.Infrastructure.DependencyInjection;

public static class CannonsInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCannonsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<CannonRegistry>();
        services.AddSingleton<ICannonRegistry>(sp => sp.GetRequiredService<CannonRegistry>());
        services.AddSingleton<ILoggerFactory, ConsoleLoggerFactory>();
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ICannonConfigurationReader, XmlCannonConfigurationReader>();
        return services;
    }
}
