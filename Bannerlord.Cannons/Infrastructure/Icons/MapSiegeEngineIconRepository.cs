using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Infrastructure.Registry;

namespace Bannerlord.Cannons.Infrastructure.Icons
{
    public class MapSiegeEngineIconRepository : IMapSiegeEngineIconRepository
    {
        private readonly ICannonRegistry _cannonRegistry;

        public MapSiegeEngineIconRepository(ICannonRegistry cannonRegistry)
        {
            _cannonRegistry = cannonRegistry;
        }

        public ISet<MapSiegeEngineIcon> MapSiegeEngineIcons =>
            new HashSet<MapSiegeEngineIcon>(
                _cannonRegistry.GetAllCannons()
                    .Select(ct => new MapSiegeEngineIcon(ct.MachineType, ct.Id, ct.MapSiegeMarkerSpriteId)));
    }
}
