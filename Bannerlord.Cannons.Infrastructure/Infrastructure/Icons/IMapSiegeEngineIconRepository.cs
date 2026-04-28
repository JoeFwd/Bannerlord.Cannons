using System.Collections.Generic;

namespace Bannerlord.Cannons.Infrastructure.Icons
{
    public interface IMapSiegeEngineIconRepository
    {
        ISet<MapSiegeEngineIcon> MapSiegeEngineIcons { get; }
    }
}
