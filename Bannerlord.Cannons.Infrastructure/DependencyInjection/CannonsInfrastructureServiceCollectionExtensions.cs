using Bannerlord.Cannons.Domain;
using Bannerlord.Cannons.Infrastructure.Logging;
using Bannerlord.Cannons.Infrastructure.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using DomainILoggerFactory = Bannerlord.Cannons.Logging.ILoggerFactory;
using MelILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using MelILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bannerlord.Cannons.Infrastructure.DependencyInjection;

public static class CannonsInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCannonsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<CannonRegistry>();
        services.AddSingleton<ICannonRegistry>(sp => sp.GetRequiredService<CannonRegistry>());

        var adapter = Bannerlord.Cannons.Logging.LoggerFactoryProvider.Get() as MicrosoftLoggerFactoryAdapter
                      ?? new MicrosoftLoggerFactoryAdapter(NullLoggerFactory.Instance);
        services.AddSingleton<DomainILoggerFactory>(adapter);
        services.AddSingleton<MelILoggerFactory>(adapter);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(LoggerWrapper<>));

        services.AddSingleton<ICannonConfigurationReader, XmlCannonConfigurationReader>();
        return services;
    }
}
