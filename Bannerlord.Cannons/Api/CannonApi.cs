using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Infrastructure.Registry;

namespace Bannerlord.Cannons.Api;

public class CannonApi : ICannonApi
{
    private readonly ICannonRegistry _cannonRegistry;

    public CannonApi(ICannonRegistry cannonRegistry)
    {
        _cannonRegistry = cannonRegistry;
    }

    public CannonApi() : this(CannonRegistry.Instance)
    {
    }

    public IEnumerable<Cannon> GetAllCannons() =>
        _cannonRegistry
            .GetAllCannons()
            .Select(cannon => new Cannon(cannon.Id, cannon.IsDefensiveSiegeWeapon));
}
