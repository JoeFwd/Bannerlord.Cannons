using System;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Logging;

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

            EmitParameterlessConstructor(typeBuilder);
            EmitLoggerFactoryConstructor(typeBuilder);

            return typeBuilder.CreateTypeInfo()!.AsType();
        }

        private static void EmitLoggerFactoryConstructor(TypeBuilder typeBuilder)
        {
            var baseConstructor = typeof(GenericCannon).GetConstructor(new[] { typeof(ILoggerFactory) })!;
            var constructor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(ILoggerFactory) });

            var il = constructor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, baseConstructor);
            il.Emit(OpCodes.Ret);
        }

        private static void EmitParameterlessConstructor(TypeBuilder typeBuilder)
        {
            var baseConstructor = typeof(GenericCannon).GetConstructor(Type.EmptyTypes)!;
            var constructor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes);

            var il = constructor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, baseConstructor);
            il.Emit(OpCodes.Ret);
        }
    }
}
