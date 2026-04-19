using System.Collections.Generic;

namespace Bannerlord.Cannons.Infrastructure.Icons
{
    public interface IPrefabSiegeEngineRepository
    {
        ISet<SiegeEngineMapPrefab> GetPrefabSiegeEngines();
    }
}
