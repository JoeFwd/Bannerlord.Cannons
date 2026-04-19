using Bannerlord.Cannons.Domain;
using Xunit;

namespace Bannerlord.Cannons.Tests.Domain
{
    public class CannonTests
    {
        [Fact]
        public void TestCannonsCreation()
        {
            // Arrange & Act
            var properties = new Cannon(
                "test_cannon",
                "Test Cannon",
                "Order\\SiegeIcons\\test_sprite",
                "SPGeneral\\MapSiege\\test_sprite",
                "SPGeneral\\Siege\\test_sprite",
                "test_prefab",
                "test_projectile",
                "test_reload",
                "test_fire",
                1,
                0,
                true
            );

            // Assert
            Assert.Equal("test_cannon", properties.Id);
            Assert.Equal("Test Cannon", properties.DisplayName);
            Assert.Equal("Order\\SiegeIcons\\test_sprite", properties.SiegeDeploymentSelectionIconSpriteId);
            Assert.Equal("SPGeneral\\MapSiege\\test_sprite", properties.MapSiegeMarkerSpriteId);
            Assert.Equal("SPGeneral\\Siege\\test_sprite", properties.CampaignMapSelectionIconSpriteId);
            Assert.Equal("test_prefab", properties.CampaignMapPrefabName);
            Assert.Equal("test_projectile", properties.CampaignMapProjectilePrefabName);
            Assert.Equal("test_reload", properties.CampaignMapReloadAnimationName);
            Assert.Equal("test_fire", properties.CampaignMapFireAnimationName);
            Assert.Equal(1, properties.MachineType);
            Assert.Equal(0, properties.CampaignMapProjectileBoneIndex);
            Assert.True(properties.IsDefensiveSiegeWeapon);
        }

        [Fact]
        public void TestCannonsEquality()
        {
            // Arrange
            var properties1 = new Cannon(
                "test_cannon", "Test",
                "Order\\SiegeIcons\\sprite", "SPGeneral\\MapSiege\\sprite", "SPGeneral\\Siege\\sprite",
                "prefab", "proj", "reload", "fire", 1, 0, true
            );
            var properties2 = new Cannon(
                "test_cannon", "Test",
                "Order\\SiegeIcons\\sprite", "SPGeneral\\MapSiege\\sprite", "SPGeneral\\Siege\\sprite",
                "prefab", "proj", "reload", "fire", 1, 0, true
            );
            var properties3 = new Cannon(
                "other_cannon", "Test",
                "Order\\SiegeIcons\\sprite", "SPGeneral\\MapSiege\\sprite", "SPGeneral\\Siege\\sprite",
                "prefab", "proj", "reload", "fire", 1, 0, true
            );

            // Act & Assert
            Assert.Equal(properties1, properties2);
            Assert.NotEqual(properties1, properties3);
        }

        [Fact]
        public void TestCannonsDeconstruction()
        {
            // Arrange
            var properties = new Cannon(
                "test_cannon",
                "Test Cannon",
                "Order\\SiegeIcons\\test_sprite",
                "SPGeneral\\MapSiege\\test_sprite",
                "SPGeneral\\Siege\\test_sprite",
                "test_prefab",
                "test_projectile",
                "test_reload",
                "test_fire",
                1,
                0,
                true
            );

            // Act
            var (id, displayName, siegeDeploymentSelectionIconSpriteId, mapSiegeMarkerSpriteId, campaignMapSelectionIconSpriteId,
                campaignMapPrefabName, campaignMapProjectilePrefabName, campaignMapReloadAnimationName,
                campaignMapFireAnimationName, machineType, campaignMapProjectileBoneIndex, IsDefensiveSiegeWeapon) = properties;

            // Assert
            Assert.Equal("test_cannon", id);
            Assert.Equal("Test Cannon", displayName);
            Assert.Equal("Order\\SiegeIcons\\test_sprite", siegeDeploymentSelectionIconSpriteId);
            Assert.Equal("SPGeneral\\MapSiege\\test_sprite", mapSiegeMarkerSpriteId);
            Assert.Equal("SPGeneral\\Siege\\test_sprite", campaignMapSelectionIconSpriteId);
            Assert.Equal("test_prefab", campaignMapPrefabName);
            Assert.Equal("test_projectile", campaignMapProjectilePrefabName);
            Assert.Equal("test_reload", campaignMapReloadAnimationName);
            Assert.Equal("test_fire", campaignMapFireAnimationName);
            Assert.Equal(1, machineType);
            Assert.Equal(0, campaignMapProjectileBoneIndex);
            Assert.True(IsDefensiveSiegeWeapon);
        }

        [Fact]
        public void TestCannonsToString()
        {
            // Arrange
            var properties = new Cannon(
                "test_cannon",
                "Test Cannon",
                "Order\\SiegeIcons\\test_sprite",
                "SPGeneral\\MapSiege\\test_sprite",
                "SPGeneral\\Siege\\test_sprite",
                "test_prefab",
                "test_projectile",
                "test_reload",
                "test_fire",
                1,
                0,
                true
            );

            // Act
            var toStringResult = properties.ToString();

            // Assert
            Assert.Contains("test_cannon", toStringResult);
            Assert.Contains("Test Cannon", toStringResult);
            Assert.Contains("Order\\SiegeIcons\\test_sprite", toStringResult);
        }
    }
}
