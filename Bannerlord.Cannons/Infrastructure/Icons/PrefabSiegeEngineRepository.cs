using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Infrastructure.Registry;

namespace Bannerlord.Cannons.Infrastructure.Icons
{
    public class PrefabSiegeEngineRepository : IPrefabSiegeEngineRepository
    {
        private readonly ICannonRegistry _cannonRegistry;

        public PrefabSiegeEngineRepository(ICannonRegistry cannonRegistry)
        {
            _cannonRegistry = cannonRegistry;
        }

        public ISet<SiegeEngineMapPrefab> GetPrefabSiegeEngines() =>
            new HashSet<SiegeEngineMapPrefab>(
                _cannonRegistry.GetAllCannons()
                    .Select(ct => new SiegeEngineMapPrefab(ct.Id, ct.CampaignMapPrefabName)));
    }
}
