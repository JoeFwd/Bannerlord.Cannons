using System;
using Bannerlord.Cannons.Infrastructure.Registry;

namespace Bannerlord.Cannons.Api;

public static class CannonApiFactory
{
    public static ICannonApi Create()
    {
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
