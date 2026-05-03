using System;
using Bannerlord.Cannons.Integration.Mission.Spawn;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.Engine;
using Xunit;

namespace Bannerlord.Cannons.Tests.Integration.Mission.Spawn;

public class GenericCannonFactoryTests
{
    private static readonly ILoggerFactory LoggerFactory = NullLoggerFactory.Instance;

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var cannonId = "test_cannon";
        var scriptType = typeof(TestGenericCannon);

        // Act
        var factory = new GenericCannonFactory(cannonId, scriptType, LoggerFactory);

        // Assert
        Assert.Equal(scriptType, factory.CannonScriptType);
    }

    [Fact]
    public void Constructor_WithNullCannonId_ThrowsArgumentNullException()
    {
        // Arrange
        string? cannonId = null;
        var scriptType = typeof(TestGenericCannon);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GenericCannonFactory(cannonId!, scriptType, LoggerFactory));
    }

    [Fact]
    public void Constructor_WithNullScriptType_ThrowsArgumentNullException()
    {
        // Arrange
        var cannonId = "test_cannon";
        Type? scriptType = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GenericCannonFactory(cannonId, scriptType!, LoggerFactory));
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var cannonId = "test_cannon";
        var scriptType = typeof(TestGenericCannon);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GenericCannonFactory(cannonId, scriptType, null!));
    }

    [Fact]
    public void Constructor_WithNonGenericCannonType_ThrowsArgumentException()
    {
        // Arrange
        var cannonId = "test_cannon";
        var scriptType = typeof(string); // Not a GenericCannon subclass

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new GenericCannonFactory(cannonId, scriptType, LoggerFactory));
        Assert.Contains("must be a subclass of GenericCannon", exception.Message);
    }

    [Fact]
    public void CreateCannon_WithTypeWithoutLoggerFactoryConstructor_ThrowsInvalidOperationException()
    {
        // Arrange
        var cannonId = "test_cannon";
        var scriptType = typeof(CannonWithoutLoggerFactoryConstructor);
        var factory = new GenericCannonFactory(cannonId, scriptType, LoggerFactory);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateCannon());
        Assert.Contains("Failed to create cannon instance", exception.Message);
        Assert.Contains("test_cannon", exception.Message);
    }

    private class TestGenericCannon : GenericCannon
    {
        public TestGenericCannon(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
        }
    }

    private class CannonWithoutLoggerFactoryConstructor : GenericCannon
    {
        public CannonWithoutLoggerFactoryConstructor(string requiredParam)
        {
            // Constructor that requires parameters
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            return string.Empty;
        }
    }
}
