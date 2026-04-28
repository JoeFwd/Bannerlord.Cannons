using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bannerlord.Cannons.Api;

public static class CannonSystemInitialiser
{
    private static bool _isInitialised;

    public static void Initialise()
    {
        if (_isInitialised)
            return;

        // If the implementation assembly is not loaded yet, defer initialisation.
        if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Bannerlord.Cannons"))
            return;

        var subModuleType = Type.GetType("Bannerlord.Cannons.SubModule, Bannerlord.Cannons");
        if (subModuleType == null)
            throw new InvalidOperationException("Could not find Bannerlord.Cannons.SubModule type.");

        object? instance;
        try
        {
            instance = Activator.CreateInstance(subModuleType);
        }
        catch (FileNotFoundException)
        {
            // Dependencies are not yet resolvable in the current load phase; try again later.
            return;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is FileNotFoundException)
        {
            // Constructor failed due missing dependent assembly in current load context; try again later.
            return;
        }
        var injectMethod = subModuleType.GetMethod("Inject");
        if (instance == null || injectMethod == null)
            throw new InvalidOperationException("Could not create submodule instance or find Inject method.");

        injectMethod.Invoke(instance, null);
        _isInitialised = true;
    }
}
