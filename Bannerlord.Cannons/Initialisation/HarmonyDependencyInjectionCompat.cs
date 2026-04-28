using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Bannerlord.Cannons.Initialisation;

public static class HarmonyDependencyInjectionCompat
{
    public static void AddHarmonyPatching(IServiceCollection services)
    {
        var registrationType =
            Type.GetType("Harmony.DependencyInjection.HarmonyPatcherRegistration, Harmony.DependencyInjection")
            ?? Type.GetType("Harmony.DependencyInjection.InternalHarmonyPatcherRegistration, Harmony.DependencyInjection");

        var method = registrationType?
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m =>
                string.Equals(m.Name, "AddHarmonyPatching", StringComparison.Ordinal) &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(IServiceCollection));

        method?.Invoke(null, new object[] { services });
    }

    public static void ApplyPatches(IServiceProvider serviceProvider)
    {
        // 0.2.x path
        var interfaceType = Type.GetType("Harmony.DependencyInjection.IHarmonyPatcher, Harmony.DependencyInjection");
        if (interfaceType != null)
        {
            var patcher = serviceProvider.GetService(interfaceType);
            var apply = patcher?.GetType().GetMethod("ApplyPatches", BindingFlags.Public | BindingFlags.Instance);
            if (apply != null)
            {
                apply.Invoke(patcher, Array.Empty<object>());
                return;
            }
        }

        // 0.1.x path
        var concreteType = Type.GetType("Harmony.DependencyInjection.Services.HarmonyPatcher, Harmony.DependencyInjection");
        if (concreteType != null)
        {
            var patcher = serviceProvider.GetService(concreteType);
            var startAsync = patcher?.GetType().GetMethod("StartAsync", BindingFlags.Public | BindingFlags.Instance);
            if (startAsync != null)
                startAsync.Invoke(patcher, new object[] { CancellationToken.None });
        }
    }
}
