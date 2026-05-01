using System.Collections.Generic;

namespace Bannerlord.Cannons.Infrastructure
{
    public interface ICannonConfigurationPathProvider
    {
        IEnumerable<string> GetConfigurationPaths();
    }
}
