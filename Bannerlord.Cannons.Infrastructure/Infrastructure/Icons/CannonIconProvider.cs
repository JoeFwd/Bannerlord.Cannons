using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Infrastructure.Registry;

namespace Bannerlord.Cannons.Infrastructure.Icons
{
    public class CannonIconProvider : ICannonIconProvider
    {
        private readonly ICannonRegistry _cannonRegistry;

        public CannonIconProvider(ICannonRegistry cannonRegistry)
        {
            _cannonRegistry = cannonRegistry;
        }

        public IEnumerable<DeploymentSiegeEngineIcon> GetSiegeEngineIcons() =>
            _cannonRegistry.GetAllCannons()
                .Select(c => new DeploymentSiegeEngineIcon(
                    c.DisplayName,
                    c.SiegeDeploymentSelectionIconSpriteId,
                    c.CampaignMapSelectionIconSpriteId,
                    c.MachineType));
    }
}
