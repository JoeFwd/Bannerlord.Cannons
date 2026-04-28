using System.Collections.Generic;

namespace Bannerlord.Cannons.Infrastructure.Icons
{
    public class DeploymentSiegeEngineIconRepository : IDeploymentSiegeEngineIconRepository
    {
        private readonly ICannonIconProvider _iconProvider;

        public DeploymentSiegeEngineIconRepository(ICannonIconProvider iconProvider)
        {
            _iconProvider = iconProvider;
        }

        public ISet<DeploymentSiegeEngineIcon> SiegeEngineIcons =>
            new HashSet<DeploymentSiegeEngineIcon>(_iconProvider.GetSiegeEngineIcons());
    }
}
