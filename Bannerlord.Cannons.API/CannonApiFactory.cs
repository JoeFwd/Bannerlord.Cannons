using System;
using Bannerlord.Cannons.Infrastructure.Logging;
using Bannerlord.Cannons.Infrastructure.Registry;
using Bannerlord.Cannons.Logging;
using MelLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace Bannerlord.Cannons.Api;

public static class CannonApiFactory
{
    /// <summary>
    /// Creates the cannon API.
    /// <para>
    /// Pass a <paramref name="loggerFactory"/> to redirect the mod's internal logging to
    /// your own logger. Must be called before Bannerlord.Cannons' SubModule finishes loading
    /// (e.g. from your own SubModule constructor).
    /// </para>
    /// </summary>
    public static ICannonApi Create(MelLoggerFactory? loggerFactory = null)
    {
        if (loggerFactory != null)
            LoggerFactoryProvider.Set(new MicrosoftLoggerFactoryAdapter(loggerFactory));
        else
            LoggerFactoryProvider.Set(new ConsoleLoggerFactory());
        

        return new CannonApi(ResolveRegistry);
    }

    private static ICannonRegistry? ResolveRegistry()
    {
        var runtimeServicesType = Type.GetType("Bannerlord.Cannons.DI.CannonsRuntimeServices, Bannerlord.Cannons");
        var currentProperty = runtimeServicesType?.GetProperty("Current");
        var serviceProvider = currentProperty?.GetValue(null) as IServiceProvider;
        return serviceProvider?.GetService(typeof(ICannonRegistry)) as ICannonRegistry;
    }
}
