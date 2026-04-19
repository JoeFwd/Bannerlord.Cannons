using System.Collections.Generic;

namespace Bannerlord.Cannons.Api;

public interface ICannonApi
{
    IEnumerable<Cannon> GetAllCannons();
}
