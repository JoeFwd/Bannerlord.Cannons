using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bannerlord.Cannons.Integration.Mission.Spawn;
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
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
        Assert.True(Defaults.TryGetValue("WheelRotationAxis", out var value));
        Assert.Equal("X", value);
    }

    [Fact]
    public void ExtractFieldDefaults_StartingAmmoCountIsInherited()
    {
        // startingAmmoCount is from TaleWorlds.MountAndBlade.RangedSiegeWeapon (exercises
        // inheritance chain traversal and int-push opcode variants).
        Assert.True(Defaults.TryGetValue("startingAmmoCount", out var value));
        Assert.Equal(20, (int)value!);
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
}
