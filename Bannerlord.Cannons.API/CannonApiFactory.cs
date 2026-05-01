using System;
using Bannerlord.Cannons.DI;
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
        CannonsServiceContainer.SetExternalLoggerFactory(loggerFactory);
    }

    private static ICannonRegistry? ResolveRegistry()
    {
        var serviceProvider = CannonsRuntimeServices.Current;
        return serviceProvider?.GetService(typeof(ICannonRegistry)) as ICannonRegistry;
    }
}
