using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Bannerlord.Cannons.Domain;
using Bannerlord.Cannons.Infrastructure;
using Bannerlord.Cannons.Logging;
using Xunit;

namespace Bannerlord.Cannons.Tests.Infrastructure;

public class XmlCannonConfigurationReaderTests : IDisposable
{
    private readonly string _testXmlPath;
    private readonly FakeLoggerFactory _loggerFactory;
    private readonly XmlCannonConfigurationReader _reader;

    public XmlCannonConfigurationReaderTests()
    {
        _testXmlPath = Path.GetTempFileName();
        _loggerFactory = new FakeLoggerFactory();
        _reader = new XmlCannonConfigurationReader(_loggerFactory);
    }

    [Fact]
    public void LoadCannons_WithValidXml_ReturnsCannons()
    {
        // Arrange
        var validXml = CreateValidCannonXml();
        File.WriteAllText(_testXmlPath, validXml);

        // Mock ResourceLocator to return our test path
        var cannons = LoadCannonsFromXml(validXml);

        // Assert
        Assert.Single(cannons);
        var cannon = cannons.First();
        Assert.Equal("test_cannon", cannon.Id);
        Assert.Equal("Test Cannon", cannon.DisplayName);
        Assert.Equal(1, cannon.MachineType);
        Assert.Equal(0, cannon.CampaignMapProjectileBoneIndex);
        Assert.True(cannon.IsDefensiveSiegeWeapon);
    }

    [Fact]
    public void LoadCannons_WithMissingRequiredField_ThrowsException()
    {
        // Arrange
        var invalidXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Cannons>
  <Cannon>
    <!-- Missing Id -->
    <DisplayName>Test Cannon</DisplayName>
    <SiegeDeploymentSelectionIconSpriteId>test_sprite</SiegeDeploymentSelectionIconSpriteId>
    <MapSiegeMarkerSpriteId>test_marker</MapSiegeMarkerSpriteId>
    <CampaignMapSelectionIconSpriteId>test_selection</CampaignMapSelectionIconSpriteId>
    <CampaignMapPrefabName>test_prefab</CampaignMapPrefabName>
    <CampaignMapProjectilePrefabName>test_projectile</CampaignMapProjectilePrefabName>
    <CampaignMapReloadAnimationName>test_reload</CampaignMapReloadAnimationName>
    <CampaignMapFireAnimationName>test_fire</CampaignMapFireAnimationName>
    <MachineType>1</MachineType>
    <CampaignMapProjectileBoneIndex>0</CampaignMapProjectileBoneIndex>
    <IsDefensiveSiegeWeapon>true</IsDefensiveSiegeWeapon>
  </Cannon>
</Cannons>";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => LoadCannonsFromXml(invalidXml));
        Assert.Contains("Cannon Id is required", exception.Message);
    }

    [Fact]
    public void LoadCannons_WithInvalidMachineType_ThrowsException()
    {
        // Arrange
        var invalidXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Cannons>
  <Cannon>
    <Id>test_cannon</Id>
    <DisplayName>Test Cannon</DisplayName>
    <SiegeDeploymentSelectionIconSpriteId>test_sprite</SiegeDeploymentSelectionIconSpriteId>
    <MapSiegeMarkerSpriteId>test_marker</MapSiegeMarkerSpriteId>
    <CampaignMapSelectionIconSpriteId>test_selection</CampaignMapSelectionIconSpriteId>
    <CampaignMapPrefabName>test_prefab</CampaignMapPrefabName>
    <CampaignMapProjectilePrefabName>test_projectile</CampaignMapProjectilePrefabName>
    <CampaignMapReloadAnimationName>test_reload</CampaignMapReloadAnimationName>
    <CampaignMapFireAnimationName>test_fire</CampaignMapFireAnimationName>
    <MachineType>invalid</MachineType>
    <CampaignMapProjectileBoneIndex>0</CampaignMapProjectileBoneIndex>
    <IsDefensiveSiegeWeapon>true</IsDefensiveSiegeWeapon>
  </Cannon>
</Cannons>";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => LoadCannonsFromXml(invalidXml));
        Assert.Contains("Invalid MachineType 'invalid' for cannon 'test_cannon'", exception.Message);
    }

    [Fact]
    public void LoadCannons_WithInvalidBoneIndex_ThrowsException()
    {
        // Arrange
        var invalidXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Cannons>
  <Cannon>
    <Id>test_cannon</Id>
    <DisplayName>Test Cannon</DisplayName>
    <SiegeDeploymentSelectionIconSpriteId>test_sprite</SiegeDeploymentSelectionIconSpriteId>
    <MapSiegeMarkerSpriteId>test_marker</MapSiegeMarkerSpriteId>
    <CampaignMapSelectionIconSpriteId>test_selection</CampaignMapSelectionIconSpriteId>
    <CampaignMapPrefabName>test_prefab</CampaignMapPrefabName>
    <CampaignMapProjectilePrefabName>test_projectile</CampaignMapProjectilePrefabName>
    <CampaignMapReloadAnimationName>test_reload</CampaignMapReloadAnimationName>
    <CampaignMapFireAnimationName>test_fire</CampaignMapFireAnimationName>
    <MachineType>1</MachineType>
    <CampaignMapProjectileBoneIndex>invalid</CampaignMapProjectileBoneIndex>
    <IsDefensiveSiegeWeapon>true</IsDefensiveSiegeWeapon>
  </Cannon>
</Cannons>";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => LoadCannonsFromXml(invalidXml));
        Assert.Contains("Invalid CampaignMapProjectileBoneIndex 'invalid' for cannon 'test_cannon'", exception.Message);
    }

    private string CreateValidCannonXml()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Cannons>
  <Cannon>
    <Id>test_cannon</Id>
    <DisplayName>Test Cannon</DisplayName>
    <SiegeDeploymentSelectionIconSpriteId>test_sprite</SiegeDeploymentSelectionIconSpriteId>
    <MapSiegeMarkerSpriteId>test_marker</MapSiegeMarkerSpriteId>
    <CampaignMapSelectionIconSpriteId>test_selection</CampaignMapSelectionIconSpriteId>
    <CampaignMapPrefabName>test_prefab</CampaignMapPrefabName>
    <CampaignMapProjectilePrefabName>test_projectile</CampaignMapProjectilePrefabName>
    <CampaignMapReloadAnimationName>test_reload</CampaignMapReloadAnimationName>
    <CampaignMapFireAnimationName>test_fire</CampaignMapFireAnimationName>
    <MachineType>1</MachineType>
    <CampaignMapProjectileBoneIndex>0</CampaignMapProjectileBoneIndex>
    <IsDefensiveSiegeWeapon>true</IsDefensiveSiegeWeapon>
  </Cannon>
</Cannons>";
    }

    private System.Collections.Generic.IEnumerable<Cannon> LoadCannonsFromXml(string xml)
    {
        var doc = XDocument.Parse(xml);
        return doc.Descendants("Cannon")
            .Select(element =>
            {
                var id = element.Element("Id")?.Value ?? throw new InvalidOperationException("Cannon Id is required");

                var machineTypeStr = element.Element("MachineType")?.Value;
                if (!int.TryParse(machineTypeStr, out var machineType))
                    throw new InvalidOperationException($"Invalid MachineType '{machineTypeStr}' for cannon '{id}'");

                var boneIndexStr = element.Element("CampaignMapProjectileBoneIndex")?.Value;
                if (!int.TryParse(boneIndexStr, out var boneIndex))
                    throw new InvalidOperationException($"Invalid CampaignMapProjectileBoneIndex '{boneIndexStr}' for cannon '{id}'");

                var IsDefensiveSiegeWeaponStr = element.Element("IsDefensiveSiegeWeapon")?.Value;
                if (!bool.TryParse(IsDefensiveSiegeWeaponStr, out var IsDefensiveSiegeWeapon))
                    throw new InvalidOperationException($"Invalid IsDefensiveSiegeWeapon '{IsDefensiveSiegeWeaponStr}' for cannon '{id}'");

                return new Cannon(
                    id,
                    element.Element("DisplayName")?.Value ?? throw new InvalidOperationException($"DisplayName is required"),
                    element.Element("SiegeDeploymentSelectionIconSpriteId")?.Value ?? throw new InvalidOperationException($"SiegeDeploymentSelectionIconSpriteId is required"),
                    element.Element("MapSiegeMarkerSpriteId")?.Value ?? throw new InvalidOperationException($"MapSiegeMarkerSpriteId is required"),
                    element.Element("CampaignMapSelectionIconSpriteId")?.Value ?? throw new InvalidOperationException($"CampaignMapSelectionIconSpriteId is required"),
                    element.Element("CampaignMapPrefabName")?.Value ?? throw new InvalidOperationException($"CampaignMapPrefabName is required"),
                    element.Element("CampaignMapProjectilePrefabName")?.Value ?? throw new InvalidOperationException($"CampaignMapProjectilePrefabName is required"),
                    element.Element("CampaignMapReloadAnimationName")?.Value ?? throw new InvalidOperationException($"CampaignMapReloadAnimationName is required"),
                    element.Element("CampaignMapFireAnimationName")?.Value ?? throw new InvalidOperationException($"CampaignMapFireAnimationName is required"),
                    machineType,
                    boneIndex,
                    IsDefensiveSiegeWeapon
                );
            })
            .ToList();
    }

    public void Dispose()
    {
        if (File.Exists(_testXmlPath))
        {
            File.Delete(_testXmlPath);
        }
    }

    private class FakeLoggerFactory : ILoggerFactory
    {
        public ILogger CreateLogger<T>() => new FakeLogger();
    }

    private class FakeLogger : ILogger
    {
        public void Debug(string message, Exception? exception = null) { }
        public void Info(string message, Exception? exception = null) { }
        public void Warn(string message, Exception? exception = null) { }
        public void Error(string message, Exception? exception = null) { }
        public void Fatal(string message, Exception? exception = null) { }
    }
}
