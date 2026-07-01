using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bannerlord.Cannons.BattleMechanics.AI.ArtilleryAI;
using Bannerlord.Cannons.BattleMechanics.Artillery;
using Bannerlord.Cannons.Domain.Ammo;
using Bannerlord.Cannons.Integration.Mission.Spawn;
using TaleWorlds.MountAndBlade;
using Xunit;

namespace Bannerlord.Cannons.Tests.Integration.Mission.Spawn;

public class SpawnerTypeEmitterTests
{
    // Reach ArtilleryRangedSiegeWeapon without a direct assembly reference to Bannerlord.Cannons.
    // GenericCannon → SpawnableArtilleryRangedSiegeWeapon → ArtilleryRangedSiegeWeapon
    private static readonly Type ArtilleryType =
        typeof(GenericCannon).BaseType!.BaseType!;

    // SpawnerTypeEmitter.EmitSpawnerType() is idempotent — safe to call at class init.
    // Type metadata inspection (GetFields, IsSubclassOf, etc.) does not trigger
    // ScriptComponentBehavior's native static constructor.
    private static readonly Type SpawnerType = SpawnerTypeEmitter.EmitSpawnerType();
    private static readonly Dictionary<string, object?> Defaults =
        SpawnerTypeEmitter.ExtractFieldDefaults(ArtilleryType);

    // ── MemberData sources ───────────────────────────────────────────────────

    public static IEnumerable<object[]> SupportedPublicArtilleryFields =>
        ArtilleryType
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => IsSupportedType(f.FieldType))
            .Select(f => new object[] { f.Name });

    public static IEnumerable<object[]> ExtractedDefaultsForPublicArtilleryFields =>
        Defaults.Keys
            .Where(k => ArtilleryType.GetField(k, BindingFlags.Public | BindingFlags.Instance) != null)
            .Select(k => new object[] { k });

    public static IEnumerable<object[]> ExtractedDefaultsForNonPublicArtilleryFields =>
        Defaults.Keys
            .Where(k => ArtilleryType.GetField(k, BindingFlags.Public | BindingFlags.Instance) == null)
            .Select(k => new object[] { k });

    // ── Sanity: emitted type ─────────────────────────────────────────────────

    [Fact]
    public void EmitSpawnerType_ReturnsTypeNamedGenericCannonSpawner()
    {
        Assert.Equal("GenericCannonSpawner", SpawnerType.Name);
    }

    [Fact]
    public void EmitSpawnerType_ReturnedTypeIsSubclassOfGenericCannonSpawnerBase()
    {
        Assert.True(SpawnerType.IsSubclassOf(typeof(GenericCannonSpawnerBase)));
    }

    // ── Field presence ───────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(SupportedPublicArtilleryFields))]
    public void EmitSpawnerType_SupportedPublicArtilleryFieldExistsInSpawner(string fieldName)
    {
        var field = SpawnerType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(field);
    }

    [Theory]
    [MemberData(nameof(ExtractedDefaultsForPublicArtilleryFields))]
    public void EmitSpawnerType_HasPublicFieldForEveryPublicExtractedDefault(string fieldName)
    {
        var field = SpawnerType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(field);
    }

    [Theory]
    [MemberData(nameof(ExtractedDefaultsForNonPublicArtilleryFields))]
    public void EmitSpawnerType_NonPublicDefaultDoesNotLeakIntoSpawner(string fieldName)
    {
        var field = SpawnerType.GetField(fieldName,
            BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Null(field);
    }

    // ── IL parser: known values ──────────────────────────────────────────────

    [Fact]
    public void ExtractFieldDefaults_FireSoundIDIsSetToMortarShot1()
    {
        Assert.True(Defaults.TryGetValue("FireSoundID", out var value));
        Assert.Equal("mortar_shot_1", value);
    }

    [Fact]
    public void ExtractFieldDefaults_FireSoundID2IsSetToMortarShot2()
    {
        Assert.True(Defaults.TryGetValue("FireSoundID2", out var value));
        Assert.Equal("mortar_shot_2", value);
    }

    [Fact]
    public void ExtractFieldDefaults_RecoilDurationIsSet()
    {
        Assert.True(Defaults.TryGetValue("RecoilDuration", out var value));
        Assert.Equal(0.8f, (float)value!);
    }

    [Fact]
    public void ExtractFieldDefaults_WheelRotationAxisIsX()
    {
        Assert.Equal(0, (int)WheelRotationAxis.X);
        if (Defaults.TryGetValue("WheelRotationAxis", out var value))
            Assert.Equal("X", value);
    }

    [Fact]
    public void ExtractFieldDefaults_StartingAmmoCountUsesExplicitCannonDefault()
    {
        Assert.True(Defaults.TryGetValue("StartingAmmoCount", out var value));
        Assert.Equal(20, (int)value!);
    }

    // ── Ammo contract ───────────────────────────────────────────────────────

    [Fact]
    public void ApplyConfiguredStartingAmmo_SyncsAmmoLimitFromStartingAmmoReserve()
    {
        var method = typeof(BaseFieldSiegeWeapon).GetMethod(nameof(BaseFieldSiegeWeapon.ApplyConfiguredStartingAmmo))!;
        var startingAmmoCount = typeof(BaseFieldSiegeWeapon).BaseType!
            .GetField("StartingAmmoCount", BindingFlags.Public | BindingFlags.Instance)!;
        var syncFromWeapon = typeof(AmmoLimit).GetMethod(nameof(AmmoLimit.SyncFromWeapon))!;
        var mathMax = typeof(Math).GetMethod(nameof(Math.Max), new[] { typeof(int), typeof(int) })!;

        Assert.True(MethodBodyReferencesMember(method, startingAmmoCount));
        Assert.True(MethodBodyReferencesMember(method, syncFromWeapon));
        Assert.True(MethodBodyReferencesMember(method, mathMax));
        Assert.Contains((byte)0x59, method.GetMethodBody()!.GetILAsByteArray()!); // sub
    }

    [Fact]
    public void ApplyConfiguredStartingAmmo_DoesNotReadCurrentAmmoCountWhenSeedingAmmoLimit()
    {
        var method = typeof(BaseFieldSiegeWeapon).GetMethod(nameof(BaseFieldSiegeWeapon.ApplyConfiguredStartingAmmo))!;
        var ammoCountGetter = typeof(BaseFieldSiegeWeapon).BaseType!
            .GetProperty("AmmoCount", BindingFlags.Public | BindingFlags.Instance)!
            .GetMethod!;

        Assert.False(MethodBodyReferencesMember(method, ammoCountGetter));
    }

    [Fact]
    public void OnMissionReset_ReappliesConfiguredStartingAmmoAfterNativeReset()
    {
        var method = typeof(BaseFieldSiegeWeapon).GetMethod("OnMissionReset", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var applyConfiguredStartingAmmo = typeof(BaseFieldSiegeWeapon)
            .GetMethod(nameof(BaseFieldSiegeWeapon.ApplyConfiguredStartingAmmo))!;
        var nativeReset = typeof(BaseFieldSiegeWeapon).BaseType!
            .GetMethod("OnMissionReset", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.Equal(typeof(BaseFieldSiegeWeapon), method.DeclaringType);
        Assert.True(MethodBodyReferencesMember(method, nativeReset));
        Assert.True(MethodBodyReferencesMember(method, applyConfiguredStartingAmmo));
    }

    // ── AI formation assignment contract ───────────────────────────────────

    [Fact]
    public void ArtilleryOnTick_UsesCustomFormationAssignmentGate()
    {
        var method = typeof(ArtilleryRangedSiegeWeapon)
            .GetMethod("OnTick", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var shouldManageAiFormationUsage = typeof(ArtilleryRangedSiegeWeapon)
            .GetMethod("ShouldManageAiFormationUsage", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var handleAiTeamUsage = typeof(ArtilleryRangedSiegeWeapon)
            .GetMethod("HandleAITeamUsage", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.True(MethodBodyReferencesMember(method, shouldManageAiFormationUsage));
        Assert.True(MethodBodyReferencesMember(method, handleAiTeamUsage));
    }

    [Fact]
    public void FieldBattleWeaponAI_DoesNotGateFiringOnFormationFiringOrder()
    {
        var method = typeof(FieldBattleWeaponAI)
            .GetMethod("TickWithTarget", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var firingOrderGetter = typeof(Formation).GetProperty(nameof(Formation.FiringOrder))!.GetMethod!;

        Assert.False(MethodBodyReferencesMember(method, firingOrderGetter));
    }

    [Fact]
    public void ExtractFieldDefaults_TopReleaseAngleRestrictionIsInherited()
    {
        // TopReleaseAngleRestriction = (float)Math.PI / 2f ≈ 1.5707964f (ldc.r4 from base class)
        Assert.True(Defaults.TryGetValue("TopReleaseAngleRestriction", out var value));
        Assert.Equal((float)(Math.PI / 2.0), (float)value!, 5);
    }

    // ── Skipped: requires native game engine ─────────────────────────────────

    [Fact(Skip = "Requires TaleWorlds.Engine native runtime — run in-game or with a real engine context")]
    public void EmitSpawnerType_EmittedConstructorSetsExpectedDefaults()
    {
        var spawner = Activator.CreateInstance(SpawnerType)!;
        foreach (var (fieldName, expectedValue) in Defaults)
        {
            if (expectedValue == null) continue;
            var field = SpawnerType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) continue;
            var actual = field.GetValue(spawner);
            Assert.Equal(expectedValue, actual);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsSupportedType(Type t) =>
        t == typeof(int) || t == typeof(float) || t == typeof(bool) || t == typeof(string) || t.IsEnum;

    private static bool MethodBodyReferencesMember(MethodInfo method, MemberInfo member)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il == null) return false;

        for (var i = 0; i < il.Length; i += GetILInstructionSize(il, i))
        {
            if (!OpcodeHasMetadataToken(il, i)) continue;

            var tokenOffset = il[i] == 0xFE ? i + 2 : i + 1;
            if (tokenOffset + 4 > il.Length) continue;

            var token = BitConverter.ToInt32(il, tokenOffset);
            try
            {
                if (method.Module.ResolveMember(token) == member)
                    return true;
            }
            catch
            {
                // Ignore tokens that cannot be resolved in this reflection-only check.
            }
        }

        return false;
    }

    private static bool OpcodeHasMetadataToken(byte[] il, int i)
    {
        if (il[i] == 0xFE)
            return false;

        return il[i] is 0x28 or 0x6F or 0x73 or 0x7B or 0x7C or 0x7D or 0x7E or 0x7F or 0x80;
    }

    private static int GetILInstructionSize(byte[] il, int i)
    {
        if (i >= il.Length) return 1;
        var op = il[i];

        if (op == 0xFE)
        {
            if (i + 1 >= il.Length) return 1;
            var op2 = il[i + 1];
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
