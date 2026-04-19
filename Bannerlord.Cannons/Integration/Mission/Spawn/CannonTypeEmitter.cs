using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    public static class CannonTypeEmitter
    {
        private static readonly ModuleBuilder _module = AssemblyBuilder
            .DefineDynamicAssembly(new AssemblyName("BannerlordCannonsDynamic"), AssemblyBuilderAccess.Run)
            .DefineDynamicModule("BannerlordCannonsDynamicModule");

        public static string GetTypeName(string cannonId) => "GenericCannon_" + cannonId;

        public static Type EmitCannonType(string cannonId)
        {
            var typeBuilder = _module.DefineType(
                GetTypeName(cannonId),
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(GenericCannon)
            );

            return typeBuilder.CreateTypeInfo()!.AsType();
        }
    }
}
