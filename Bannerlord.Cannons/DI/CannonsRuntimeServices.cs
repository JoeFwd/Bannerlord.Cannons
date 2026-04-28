using System;
using Microsoft.Extensions.DependencyInjection;

namespace Bannerlord.Cannons.DI;

public static class CannonsRuntimeServices
{
    public static IServiceProvider? Current { get; private set; }

    public static void Set(IServiceProvider serviceProvider)
    {
        Current = serviceProvider;
    }

    public static T GetRequiredService<T>() where T : notnull
    {
        if (Current == null)
            throw new InvalidOperationException("Cannons runtime service provider is not initialized.");

        return Current.GetRequiredService<T>();
    }
}
