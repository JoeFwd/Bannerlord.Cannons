using System;
using Bannerlord.Cannons.Infrastructure.Registry;
using Microsoft.Extensions.Logging;

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
    public static ICannonApi Create(ILoggerFactory? loggerFactory = null)
    {
        if (loggerFactory != null)
            SetExternalLoggerFactory(loggerFactory);

        return new CannonApi(ResolveRegistry);
    }

    private static void SetExternalLoggerFactory(ILoggerFactory loggerFactory)
    {
        var containerType = Type.GetType("Bannerlord.Cannons.DI.CannonsServiceContainer, Bannerlord.Cannons");
        var setMethod = containerType?.GetMethod("SetExternalLoggerFactory");
        setMethod?.Invoke(null, new object[] { loggerFactory });
    }

    private static ICannonRegistry? ResolveRegistry()
    {
        var runtimeServicesType = Type.GetType("Bannerlord.Cannons.DI.CannonsRuntimeServices, Bannerlord.Cannons");
        var currentProperty = runtimeServicesType?.GetProperty("Current");
        var serviceProvider = currentProperty?.GetValue(null) as IServiceProvider;
        return serviceProvider?.GetService(typeof(ICannonRegistry)) as ICannonRegistry;
    }
}
