using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Domain;
using Bannerlord.Cannons.Infrastructure.Registry;
using Xunit;

namespace Bannerlord.Cannons.Tests.Infrastructure;

public class CannonRegistryTests
{
    [Fact]
    public void RegisterCannon_WithValidParameters_StoresCannon()
    {
        // Arrange
        var registry = new CannonRegistry();
        var cannon = CreateTestCannon("test_cannon");
        var factory = new FakeCannonFactory(typeof(string));

        // Act
        registry.RegisterCannon(cannon, factory);

        // Assert
        var retrievedCannon = registry.GetCannon("test_cannon");
        Assert.NotNull(retrievedCannon);
        Assert.Equal("test_cannon", retrievedCannon!.Id);

        var retrievedFactory = registry.GetFactory("test_cannon");
        Assert.NotNull(retrievedFactory);
        Assert.Same(factory, retrievedFactory);
    }

    [Fact]
    public void GetCannon_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var registry = new CannonRegistry();

        // Act
        var result = registry.GetCannon("non_existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCannonByScript_WithExistingScriptType_ReturnsCannon()
    {
        // Arrange
        var registry = new CannonRegistry();
        var type = typeof(string);
        var cannon = CreateTestCannon("test_cannon");
        var factory = new FakeCannonFactory(type);
        registry.RegisterCannon(cannon, factory);

        // Act
        var result = registry.GetCannonByScript(type);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test_cannon", result!.Id);
    }

    [Fact]
    public void GetCannonByScript_WithNonExistentScriptType_ReturnsNull()
    {
        // Arrange
        var registry = new CannonRegistry();

        // Act
        var result = registry.GetCannonByScript(typeof(string));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetFactory_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var registry = new CannonRegistry();

        // Act
        var result = registry.GetFactory("non_existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAllCannons_WithMultipleCannons_ReturnsAllCannons()
    {
        // Arrange
        var registry = new CannonRegistry();
        var type1 = typeof(string);
        var type2 = typeof(int);
        var cannon1 = CreateTestCannon("cannon1");
        var cannon2 = CreateTestCannon("cannon2");
        var factory1 = new FakeCannonFactory(type1);
        var factory2 = new FakeCannonFactory(type2);

        registry.RegisterCannon(cannon1, factory1);
        registry.RegisterCannon(cannon2, factory2);

        // Act
        var allCannons = registry.GetAllCannons().ToList();

        // Assert
        Assert.Equal(2, allCannons.Count);
        Assert.Contains(allCannons, c => c.Id == "cannon1");
        Assert.Contains(allCannons, c => c.Id == "cannon2");
    }

    private static Cannon CreateTestCannon(string id)
    {
        return new Cannon(
            id,
            "Test Cannon",
            "test_sprite",
            "test_marker",
            "test_selection",
            "test_prefab",
            "test_projectile",
            "test_reload",
            "test_fire",
            1,
            0,
            true,
            true
        );
    }

    private class FakeCannonFactory : ICannonFactory
    {
        public FakeCannonFactory(Type type)
        {
            CannonScriptType = type;
        }

        public Type CannonScriptType { get; }
    }
}
