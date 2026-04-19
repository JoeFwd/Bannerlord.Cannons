using System.Collections.Generic;

namespace Bannerlord.Cannons.Domain
{
    public interface ICannonConfigurationReader
    {
        IEnumerable<Cannon> LoadCannons();
    }
}
