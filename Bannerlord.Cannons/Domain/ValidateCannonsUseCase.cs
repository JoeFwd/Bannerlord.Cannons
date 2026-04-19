using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bannerlord.Cannons.Logging;

namespace Bannerlord.Cannons.Domain
{
    public class ValidateCannonsUseCase
    {
        private readonly ILogger _logger;

        public ValidateCannonsUseCase(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ValidateCannonsUseCase>();
        }

        public IEnumerable<Cannon> GetValidCannons(IEnumerable<Cannon> cannons)
        {
            return cannons.Where(IsValid);
        }

        private bool IsValid(Cannon cannon)
        {
            if (string.IsNullOrWhiteSpace(cannon.Id))
            {
                _logger.Warn("Cannon is invalid: Id is null or empty. Cannon will be skipped.");
                return false;
            }

            if (!Regex.IsMatch(cannon.Id, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
            {
                _logger.Warn($"Cannon '{cannon.Id}' is invalid: Id must start with a letter and contain only letters, digits, or underscores. Cannon will be skipped.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(cannon.DisplayName))
            {
                _logger.Warn($"Cannon '{cannon.Id}' is invalid: DisplayName is null or empty. Cannon will be skipped.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(cannon.SiegeDeploymentSelectionIconSpriteId))
            {
                _logger.Warn($"Cannon '{cannon.Id}' is invalid: SiegeDeploymentSelectionIconSpriteId is null or empty. Cannon will be skipped.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(cannon.MapSiegeMarkerSpriteId))
            {
                _logger.Warn($"Cannon '{cannon.Id}' is invalid: MapSiegeMarkerSpriteId is null or empty. Cannon will be skipped.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(cannon.CampaignMapSelectionIconSpriteId))
            {
                _logger.Warn($"Cannon '{cannon.Id}' is invalid: CampaignMapSelectionIconSpriteId is null or empty. Cannon will be skipped.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(cannon.CampaignMapPrefabName))
            {
                _logger.Warn($"Cannon '{cannon.Id}' is invalid: CampaignMapPrefabName is null or empty. Cannon will be skipped.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(cannon.CampaignMapProjectilePrefabName))
            {
                _logger.Warn($"Cannon '{cannon.Id}' is invalid: CampaignMapProjectilePrefabName is null or empty. Cannon will be skipped.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(cannon.CampaignMapReloadAnimationName))
            {
                _logger.Warn($"Cannon '{cannon.Id}' is invalid: CampaignMapReloadAnimationName is null or empty. Cannon will be skipped.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(cannon.CampaignMapFireAnimationName))
            {
                _logger.Warn($"Cannon '{cannon.Id}' is invalid: CampaignMapFireAnimationName is null or empty. Cannon will be skipped.");
                return false;
            }

            if (cannon.MachineType <= 0)
            {
                _logger.Warn($"Cannon '{cannon.Id}' is invalid: MachineType must be greater than 0 (got {cannon.MachineType}). Cannon will be skipped.");
                return false;
            }

            if (cannon.CampaignMapProjectileBoneIndex < 0)
            {
                _logger.Warn($"Cannon '{cannon.Id}' is invalid: CampaignMapProjectileBoneIndex must be >= 0 (got {cannon.CampaignMapProjectileBoneIndex}). Cannon will be skipped.");
                return false;
            }

            return true;
        }
    }
}
