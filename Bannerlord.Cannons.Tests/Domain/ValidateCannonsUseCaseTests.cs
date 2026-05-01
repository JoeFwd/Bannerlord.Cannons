using System.Collections.Generic;
using System.Linq;
using System;
using Bannerlord.Cannons.Domain;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bannerlord.Cannons.Tests.Domain
{
    public class ValidateCannonsUseCaseTests
    {
        private sealed class FakeLogger : ILogger
        {
            public List<string> WarnMessages { get; } = new();

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter)
            {
                if (logLevel == LogLevel.Warning)
                    WarnMessages.Add(formatter(state, exception));
            }
        }

        private sealed class FakeLoggerFactory : ILoggerFactory
        {
            public FakeLogger Logger { get; } = new();
            public void AddProvider(ILoggerProvider provider) { }
            public ILogger CreateLogger(string categoryName) => Logger;
            public void Dispose() { }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }

        private static Cannon ValidCannon(string id = "cannon_a") => new(
            id,
            "Cannon A",
            "Order\\SiegeIcons\\cannon_a",
            "SPGeneral\\MapSiege\\cannon_a",
            "SPGeneral\\Siege\\cannon_a",
            "cannon_a_mapicon",
            "cannonball_mapicon_projectile",
            "ballista_a_mapicon_reload",
            "ballista_a_mapicon_fire",
            8,
            0,
            true,
            true
        );

        [Fact]
        public void GetValidCannons_AllValid_ReturnsAllCannons()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannons = new[] { ValidCannon("cannon_a"), ValidCannon("cannon_b") };

            var result = useCase.GetValidCannons(cannons).ToList();

            Assert.Equal(2, result.Count);
            Assert.Empty(loggerFactory.Logger.WarnMessages);
        }

        [Fact]
        public void GetValidCannons_EmptyId_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { Id = "" };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Single(loggerFactory.Logger.WarnMessages);
            Assert.Equal("Cannon is invalid: Id is null or empty. Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_WhitespaceDisplayName_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { DisplayName = "  " };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Single(loggerFactory.Logger.WarnMessages);
            Assert.Equal("Cannon 'cannon_a' is invalid: DisplayName is null or empty. Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_EmptySiegeDeploymentSelectionIconSpriteId_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { SiegeDeploymentSelectionIconSpriteId = "" };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Equal("Cannon 'cannon_a' is invalid: SiegeDeploymentSelectionIconSpriteId is null or empty. Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_EmptyMapSiegeMarkerSpriteId_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { MapSiegeMarkerSpriteId = "" };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Equal("Cannon 'cannon_a' is invalid: MapSiegeMarkerSpriteId is null or empty. Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_EmptyCampaignMapSelectionIconSpriteId_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { CampaignMapSelectionIconSpriteId = "" };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Equal("Cannon 'cannon_a' is invalid: CampaignMapSelectionIconSpriteId is null or empty. Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_EmptyCampaignMapPrefabName_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { CampaignMapPrefabName = "" };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Equal("Cannon 'cannon_a' is invalid: CampaignMapPrefabName is null or empty. Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_EmptyCampaignMapProjectilePrefabName_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { CampaignMapProjectilePrefabName = "" };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Equal("Cannon 'cannon_a' is invalid: CampaignMapProjectilePrefabName is null or empty. Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_EmptyCampaignMapReloadAnimationName_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { CampaignMapReloadAnimationName = "" };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Equal("Cannon 'cannon_a' is invalid: CampaignMapReloadAnimationName is null or empty. Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_EmptyCampaignMapFireAnimationName_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { CampaignMapFireAnimationName = "" };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Equal("Cannon 'cannon_a' is invalid: CampaignMapFireAnimationName is null or empty. Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_ZeroMachineType_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { MachineType = 0 };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Equal("Cannon 'cannon_a' is invalid: MachineType must be greater than 0 (got 0). Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_NegativeMachineType_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { MachineType = -1 };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Equal("Cannon 'cannon_a' is invalid: MachineType must be greater than 0 (got -1). Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_NegativeBoneIndex_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { CampaignMapProjectileBoneIndex = -1 };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Equal("Cannon 'cannon_a' is invalid: CampaignMapProjectileBoneIndex must be >= 0 (got -1). Cannon will be skipped.", loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_InvalidIdFormat_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { Id = "my-cannon" };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Equal(
                "Cannon 'my-cannon' is invalid: Id must start with a letter and contain only letters, digits, or underscores. Cannon will be skipped.",
                loggerFactory.Logger.WarnMessages[0]);
        }

        [Fact]
        public void GetValidCannons_IdStartingWithDigit_FiltersOutAndLogs()
        {
            var loggerFactory = new FakeLoggerFactory();
            var useCase = new ValidateCannonsUseCase(loggerFactory);
            var cannon = ValidCannon() with { Id = "1st_cannon" };

            var result = useCase.GetValidCannons(new[] { cannon }).ToList();

            Assert.Empty(result);
            Assert.Single(loggerFactory.Logger.WarnMessages);
        }
    }
}
