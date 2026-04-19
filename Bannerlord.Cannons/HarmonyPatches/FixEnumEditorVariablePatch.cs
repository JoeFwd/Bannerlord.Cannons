using System;
using System.Linq;
using System.Reflection;

namespace Bannerlord.Cannons.HarmonyPatches
{
    public static class FixEnumEditorVariablePatch
    {
        private static bool _isApplied;

        public static void Apply()
        {
            if (_isApplied)
                return;

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyForEditorEnumVariables;
            _isApplied = true;
        }

        private static Assembly? ResolveAssemblyForEditorEnumVariables(object? _, ResolveEventArgs args)
        {
            var requested = new AssemblyName(args.Name).Name;
            if (string.IsNullOrWhiteSpace(requested))
                return null;

            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly =>
                {
                    var assemblyName = assembly.GetName().Name;
                    return assemblyName != null &&
                           assemblyName.StartsWith(requested + ".", StringComparison.Ordinal);
                });
        }
    }
}
