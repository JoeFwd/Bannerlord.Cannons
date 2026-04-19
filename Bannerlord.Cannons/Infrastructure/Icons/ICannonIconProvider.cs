using System.Collections.Generic;

namespace Bannerlord.Cannons.Infrastructure.Icons
{
    public interface ICannonIconProvider
    {
        IEnumerable<DeploymentSiegeEngineIcon> GetSiegeEngineIcons();
    }
}
