using System.Collections.Generic;

namespace Bannerlord.Cannons.Infrastructure.Icons
{
    public interface IDeploymentSiegeEngineIconRepository
    {
        ISet<DeploymentSiegeEngineIcon> SiegeEngineIcons { get; }
    }
}
