using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Bannerlord.Cannons.Domain;
using Microsoft.Extensions.Logging;

namespace Bannerlord.Cannons.Infrastructure
{
    public class XmlCannonConfigurationReader : ICannonConfigurationReader
    {
        private const string EmbeddedSchemaFileName = "cannons.xsd";

        private readonly ILogger _logger;
        private readonly ICannonConfigurationPathProvider _configurationPathProvider;

        public XmlCannonConfigurationReader(ILoggerFactory loggerFactory, ICannonConfigurationPathProvider configurationPathProvider)
        {
            _logger = loggerFactory.CreateLogger<XmlCannonConfigurationReader>();
            _configurationPathProvider = configurationPathProvider;
        }

        public IEnumerable<Cannon> LoadCannons()
        {
            var allCannons = new List<Cannon>();

            foreach (var path in _configurationPathProvider.GetConfigurationPaths())
            {
                if (!File.Exists(path)) continue;

                _logger.LogDebug($"Loading cannons from '{path}'");
                allCannons.AddRange(LoadFromFile(path));
            }

            if (allCannons.Count == 0)
                _logger.LogWarning("No cannons.xml found in any loaded module's ModuleData/CustomXml/ folder.");

            return allCannons;
        }

        private IEnumerable<Cannon> LoadFromFile(string xmlPath)
        {
            try
            {
                var document = LoadAndValidateXml(xmlPath);
                var cannons = document.Descendants("Cannon")
                    .Select(CreateCannon).ToList();
                foreach (var cannon in cannons)
                    _logger.LogDebug($"Loaded '{cannon.Id}' cannon from '{xmlPath}'");
                return cannons;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load cannons from '{xmlPath}': {ex.Message}");
                return Enumerable.Empty<Cannon>();
            }
        }

        private XDocument LoadAndValidateXml(string xmlPath)
        {
            try
            {
                var schemaPath = GetEmbeddedSchemaPath();
                if (schemaPath != null)
                    ValidateXmlAgainstSchema(xmlPath, schemaPath);
                else
                    _logger.LogWarning("Cannon XML schema not found, skipping validation");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"XML validation failed: {ex.Message}. Proceeding without schema validation.");
            }

            return XDocument.Load(xmlPath);
        }

        private string? GetEmbeddedSchemaPath()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("." + EmbeddedSchemaFileName, StringComparison.OrdinalIgnoreCase));

            if (resourceName == null) return null;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return null;

            var tempPath = Path.GetTempFileName();
            using var fileStream = File.Create(tempPath);
            stream.CopyTo(fileStream);
            return tempPath;
        }

        private void ValidateXmlAgainstSchema(string xmlPath, string schemaPath)
        {
            try
            {
                var settings = new XmlReaderSettings();
                settings.Schemas.Add("", schemaPath);
                settings.ValidationType = ValidationType.Schema;
                settings.ValidationEventHandler += (sender, e) =>
                {
                    var message = $"XML validation error: {e.Message} (Line: {e.Exception?.LineNumber}, Position: {e.Exception?.LinePosition})";
                    if (e.Severity == XmlSeverityType.Error)
                        throw new InvalidOperationException(message);
                    _logger.LogWarning(message);
                };

                using var reader = XmlReader.Create(xmlPath, settings);
                while (reader.Read()) { }

                _logger.LogDebug("Cannon XML validation successful");
            }
            finally
            {
                if (File.Exists(schemaPath))
                {
                    try { File.Delete(schemaPath); }
                    catch (Exception ex) { _logger.LogWarning($"Failed to delete temporary schema file: {ex.Message}"); }
                }
            }
        }

        private static Cannon CreateCannon(XElement element)
        {
            var id = element.Element("Id")?.Value ?? throw new InvalidOperationException("Cannon Id is required");
            var displayName = element.Element("DisplayName")?.Value ?? throw new InvalidOperationException($"DisplayName is required for cannon '{id}'");

            var machineTypeValue = element.Element("MachineType")?.Value;
            if (!int.TryParse(machineTypeValue, out var machineType))
                throw new InvalidOperationException($"Invalid MachineType '{machineTypeValue}' for cannon '{id}'.");

            var boneIndexValue = element.Element("CampaignMapProjectileBoneIndex")?.Value;
            if (!int.TryParse(boneIndexValue, out var boneIndex))
                throw new InvalidOperationException($"Invalid CampaignMapProjectileBoneIndex '{boneIndexValue}' for cannon '{id}'.");

            var isDefensiveSiegeWeaponValue = element.Element("IsDefensiveSiegeWeapon")?.Value;
            if (!bool.TryParse(isDefensiveSiegeWeaponValue, out var isDefensiveSiegeWeapon))
                throw new InvalidOperationException($"Invalid IsDefensiveSiegeWeapon '{isDefensiveSiegeWeaponValue}' for cannon '{id}'.");

            var isAttackerSiegeWeaponValue = element.Element("IsAttackerSiegeWeapon")?.Value;
            if (!bool.TryParse(isAttackerSiegeWeaponValue, out var isAttackerSiegeWeapon))
                throw new InvalidOperationException($"Invalid IsAttackerSiegeWeapon '{isAttackerSiegeWeaponValue}' for cannon '{id}'.");

            return new Cannon(
                id,
                displayName,
                element.Element("SiegeDeploymentSelectionIconSpriteId")?.Value ?? throw new InvalidOperationException($"SiegeDeploymentSelectionIconSpriteId is required for cannon '{id}'"),
                element.Element("MapSiegeMarkerSpriteId")?.Value ?? throw new InvalidOperationException($"MapSiegeMarkerSpriteId is required for cannon '{id}'"),
                element.Element("CampaignMapSelectionIconSpriteId")?.Value ?? throw new InvalidOperationException($"CampaignMapSelectionIconSpriteId is required for cannon '{id}'"),
                element.Element("CampaignMapPrefabName")?.Value ?? throw new InvalidOperationException($"CampaignMapPrefabName is required for cannon '{id}'"),
                element.Element("CampaignMapProjectilePrefabName")?.Value ?? throw new InvalidOperationException($"CampaignMapProjectilePrefabName is required for cannon '{id}'"),
                element.Element("CampaignMapReloadAnimationName")?.Value ?? throw new InvalidOperationException($"CampaignMapReloadAnimationName is required for cannon '{id}'"),
                element.Element("CampaignMapFireAnimationName")?.Value ?? throw new InvalidOperationException($"CampaignMapFireAnimationName is required for cannon '{id}'"),
                machineType,
                boneIndex,
                isDefensiveSiegeWeapon,
                isAttackerSiegeWeapon
            );
        }
    }
}
