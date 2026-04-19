using Bannerlord.Cannons.Infrastructure.Registry;
using TaleWorlds.Core;

namespace Bannerlord.Cannons.Integration.Campaign
{
    public class CannonPrefabProvider
    {
        private readonly ICannonRegistry _cannonRegistry;

        public CannonPrefabProvider(ICannonRegistry cannonRegistry)
        {
            _cannonRegistry = cannonRegistry;
        }

        public string? GetCampaignMapPrefabName(string cannonId, int wallLevel, BattleSideEnum side)
            => _cannonRegistry.GetCannon(cannonId)?.CampaignMapPrefabName;

        public string? GetCampaignMapProjectilePrefabName(string cannonId)
            => _cannonRegistry.GetCannon(cannonId)?.CampaignMapProjectilePrefabName;

        public string? GetCampaignMapReloadAnimationName(string cannonId)
            => _cannonRegistry.GetCannon(cannonId)?.CampaignMapReloadAnimationName;

        public string? GetCampaignMapFireAnimationName(string cannonId)
            => _cannonRegistry.GetCannon(cannonId)?.CampaignMapFireAnimationName;

        public int GetCampaignMapProjectileBoneIndex(string cannonId)
            => _cannonRegistry.GetCannon(cannonId)?.CampaignMapProjectileBoneIndex ?? -1;
    }
}
