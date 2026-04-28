using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Bannerlord.Cannons.BattleMechanics.Artillery;
using TaleWorlds.Engine;

namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    public static class SpawnerTypeEmitter
    {
        private const string BaseMuzzleVelocityFieldName = nameof(ArtilleryRangedSiegeWeapon.BaseMuzzleVelocity);
        private const string BottomReleaseAngleRestrictionFieldName = nameof(ArtilleryRangedSiegeWeapon.BottomReleaseAngleRestriction);
        private const string TopReleaseAngleRestrictionFieldName = nameof(ArtilleryRangedSiegeWeapon.TopReleaseAngleRestriction);
        private const string DirectionRestrictionDegreesFieldName = nameof(ArtilleryRangedSiegeWeapon.DirectionRestrictionDegrees);

        private static readonly ModuleBuilder _module = AssemblyBuilder
            .DefineDynamicAssembly(new AssemblyName("BannerlordCannonsSpawnersDynamic"), AssemblyBuilderAccess.Run)
            .DefineDynamicModule("BannerlordCannonsSpawnersDynamicModule");
        private static Type? _emittedSpawnerType;

        public static Type EmitSpawnerType()
        {
            if (_emittedSpawnerType != null) return _emittedSpawnerType;

            var typeBuilder = _module.DefineType(
                "GenericCannonSpawner",
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(GenericCannonSpawnerBase)
            );

            var editorVisibleCtor = typeof(EditorVisibleScriptComponentVariable)
                .GetConstructor(new[] { typeof(bool) })!;

            var artilleryFields = typeof(ArtilleryRangedSiegeWeapon)
                .GetFields(BindingFlags.Public | BindingFlags.Instance);
            var usedFieldHashes = CollectManagedEditableFieldHashes(typeof(GenericCannonSpawnerBase));

            var fieldBuilders = new Dictionary<string, FieldBuilder>(artilleryFields.Length);
            foreach (var field in artilleryFields)
            {
                if (!IsManagedSupportedEditableFieldType(field.FieldType)) continue;

                var fieldHash = GetManagedHash(field.Name);
                if (!usedFieldHashes.Add(fieldHash)) continue;

                var fb = typeBuilder.DefineField(field.Name, field.FieldType, FieldAttributes.Public);
                fb.SetCustomAttribute(new CustomAttributeBuilder(editorVisibleCtor, new object[] { true }));
                fieldBuilders[field.Name] = fb;
            }

            var defaults = ExtractFieldDefaults(typeof(ArtilleryRangedSiegeWeapon));
            EmitConstructorWithDefaults(typeBuilder, artilleryFields, fieldBuilders, defaults);
            EmitTrajectorySourceImplementations(typeBuilder, fieldBuilders);

            _emittedSpawnerType = typeBuilder.CreateTypeInfo()!.AsType();
            return _emittedSpawnerType;
        }

        private static void EmitTrajectorySourceImplementations(TypeBuilder typeBuilder, Dictionary<string, FieldBuilder> fieldBuilders)
        {
            typeBuilder.AddInterfaceImplementation(typeof(ICannonTrajectoryPreviewSource));

            EmitFloatGetterForInterface(typeBuilder, fieldBuilders, BaseMuzzleVelocityFieldName,
                nameof(ICannonTrajectoryPreviewSource.GetTrajectoryPreviewBaseMuzzleVelocity));
            EmitFloatGetterForInterface(typeBuilder, fieldBuilders, BottomReleaseAngleRestrictionFieldName,
                nameof(ICannonTrajectoryPreviewSource.GetTrajectoryPreviewBottomReleaseAngleRestriction));
            EmitFloatGetterForInterface(typeBuilder, fieldBuilders, TopReleaseAngleRestrictionFieldName,
                nameof(ICannonTrajectoryPreviewSource.GetTrajectoryPreviewTopReleaseAngleRestriction));
            EmitFloatGetterForInterface(typeBuilder, fieldBuilders, DirectionRestrictionDegreesFieldName,
                nameof(ICannonTrajectoryPreviewSource.GetTrajectoryPreviewDirectionRestrictionDegrees));
        }

        private static void EmitFloatGetterForInterface(TypeBuilder typeBuilder, Dictionary<string, FieldBuilder> fieldBuilders, string sourceFieldName, string interfaceMethodName)
        {
            if (!fieldBuilders.TryGetValue(sourceFieldName, out var fieldBuilder)) return;

            var interfaceMethod = typeof(ICannonTrajectoryPreviewSource).GetMethod(interfaceMethodName, BindingFlags.Public | BindingFlags.Instance);
            if (interfaceMethod == null) return;

            var methodBuilder = typeBuilder.DefineMethod(
                interfaceMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeof(float),
                Type.EmptyTypes);

            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fieldBuilder);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);
        }

        private static HashSet<uint> CollectManagedEditableFieldHashes(Type type)
        {
            var hashes = new HashSet<uint>();
            while (type != null)
            {
                var fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    var editableAttr = field.GetCustomAttributes(typeof(EditorVisibleScriptComponentVariable), true)
                        .FirstOrDefault() as EditorVisibleScriptComponentVariable;
                    var visible = editableAttr?.Visible ?? (!field.IsPrivate && !field.IsFamily);
                    if (!visible) continue;
                    hashes.Add(GetManagedHash(field.Name));
                }
                type = type.BaseType;
            }
            return hashes;
        }

        private static bool IsManagedSupportedEditableFieldType(Type fieldType)
        {
            if (fieldType.IsEnum) return true;
            return fieldType == typeof(int)
                   || fieldType == typeof(float)
                   || fieldType == typeof(bool)
                   || fieldType == typeof(string);
        }

        private static uint GetManagedHash(string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            uint hash = 5381;
            foreach (var b in bytes) hash = ((hash << 5) + hash) + b;
            return hash;
        }

        private static void EmitConstructorWithDefaults(
            TypeBuilder typeBuilder,
            FieldInfo[] artilleryFields,
            Dictionary<string, FieldBuilder> fieldBuilders,
            Dictionary<string, object?> defaults)
        {
            var ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            var il = ctorBuilder.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(GenericCannonSpawnerBase).GetConstructor(Type.EmptyTypes)!);

            foreach (var field in artilleryFields)
            {
                if (!defaults.TryGetValue(field.Name, out var value) || value == null) continue;
                if (!fieldBuilders.TryGetValue(field.Name, out var fb)) continue;

                il.Emit(OpCodes.Ldarg_0);

                if (field.FieldType == typeof(float))
                    il.Emit(OpCodes.Ldc_R4, (float)value);
                else if (field.FieldType == typeof(int))
                    il.Emit(OpCodes.Ldc_I4, (int)value);
                else if (field.FieldType == typeof(bool))
                {
                    bool bVal = value is bool b ? b : Convert.ToBoolean(value);
                    il.Emit(bVal ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                }
                else if (field.FieldType == typeof(string))
                {
                    var s = (string)value;
                    if (string.IsNullOrEmpty(s))
                    {
                        il.Emit(OpCodes.Pop);
                        continue;
                    }
                    il.Emit(OpCodes.Ldstr, s);
                }
                else
                {
                    il.Emit(OpCodes.Pop);
                    continue;
                }

                il.Emit(OpCodes.Stfld, fb);
            }

            il.Emit(OpCodes.Ret);
        }

        public static Dictionary<string, object?> ExtractFieldDefaults(Type type)
        {
            var defaults = new Dictionary<string, object?>();
            var current = type;
            while (current != null
                   && current != typeof(ScriptComponentBehavior)
                   && current != typeof(object))
            {
                var ctor = current.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                if (ctor != null)
                    ParseCtorFieldDefaults(ctor, defaults);
                current = current.BaseType;
            }
            return defaults;
        }

        private static void ParseCtorFieldDefaults(ConstructorInfo ctor, Dictionary<string, object?> defaults)
        {
            var body = ctor.GetMethodBody();
            if (body == null) return;
            var il = body.GetILAsByteArray();
            if (il == null) return;
            var module = ctor.Module;

            int i = 0;
            while (i < il.Length)
            {
                if (il[i] == 0x02)
                {
                    int j = i + 1;
                    if (j < il.Length && TryReadPushValue(il, j, module, out var value, out int valueSize))
                    {
                        int k = j + valueSize;
                        if (k < il.Length && il[k] == 0x7D && k + 4 < il.Length)
                        {
                            int token = BitConverter.ToInt32(il, k + 1);
                            try
                            {
                                var field = module.ResolveField(token);
                                if (field != null && !defaults.ContainsKey(field.Name))
                                    defaults[field.Name] = value;
                            }
                            catch { /* unresolvable token */ }

                            i = k + 5;
                            continue;
                        }
                    }
                }
                i += GetILInstructionSize(il, i);
            }
        }

        private static bool TryReadPushValue(byte[] il, int i, Module module, out object? value, out int size)
        {
            value = null;
            size = 0;
            if (i >= il.Length) return false;
            byte op = il[i];

            if (op is >= 0x15 and <= 0x1E) { value = op - 0x16; size = 1; return true; }
            if (op == 0x1F && i + 1 < il.Length) { value = (int)(sbyte)il[i + 1]; size = 2; return true; }
            if (op == 0x20 && i + 4 < il.Length) { value = BitConverter.ToInt32(il, i + 1); size = 5; return true; }
            if (op == 0x22 && i + 4 < il.Length) { value = BitConverter.ToSingle(il, i + 1); size = 5; return true; }
            if (op == 0x72 && i + 4 < il.Length)
                try { value = module.ResolveString(BitConverter.ToInt32(il, i + 1)); size = 5; return true; }
                catch { return false; }

            return false;
        }

        private static int GetILInstructionSize(byte[] il, int i)
        {
            if (i >= il.Length) return 1;
            byte op = il[i];

            if (op == 0xFE)
            {
                if (i + 1 >= il.Length) return 1;
                byte op2 = il[i + 1];
                return op2 is >= 0x09 and <= 0x0E ? 4 : 2;
            }

            if (op == 0x45)
                return i + 4 < il.Length ? 5 + 4 * BitConverter.ToInt32(il, i + 1) : 1;

            return op switch
            {
                >= 0x00 and <= 0x0D => 1,
                0x0E or 0x0F or 0x10 or 0x11 or 0x12 or 0x13 or 0x1F or >= 0x2B and <= 0x37 => 2,
                0x21 or 0x23 => 9,
                0x20 or 0x22 or 0x27 or 0x28 or 0x29 or >= 0x38 and <= 0x44
                    or 0x6F or 0x70 or 0x71 or 0x72 or 0x73 or 0x74
                    or 0x75 or 0x79 or 0x7A or 0x7B or 0x7C or 0x7D
                    or 0x7E or 0x7F or 0x80 or 0x81 or 0x8C or 0x8D
                    or 0xA3 or 0xA4 => 5,
                _ => 1
            };
        }
    }
}
