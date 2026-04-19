using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Infrastructure.Registry;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.Cannons.Integration.Campaign
{
    public class CannonAvailabilityProvider
    {
        private readonly ICannonRegistry _cannonRegistry;

        public CannonAvailabilityProvider(ICannonRegistry cannonRegistry)
        {
            _cannonRegistry = cannonRegistry;
        }

        public IEnumerable<SiegeEngineType> GetAvailableCannons(PartyBase? party, BattleSideEnum side) =>
            _cannonRegistry.GetAllCannons()
                .Select(cannon => MBObjectManager.Instance.GetObject<SiegeEngineType>(cannon.Id))
                .Where(se => se != null)
                .ToList();
    }
}
