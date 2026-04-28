using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Infrastructure.Registry;

namespace Bannerlord.Cannons.Api;

internal sealed class CannonApi : ICannonApi
{
    private readonly Func<ICannonRegistry?> _registryResolver;

    public CannonApi(Func<ICannonRegistry?> registryResolver)
    {
        _registryResolver = registryResolver;
    }

    public IEnumerable<Cannon> GetAllCannons() =>
        GetCannons();

    private IEnumerable<Cannon> GetCannons()
    {
        var registry = _registryResolver();
        if (registry == null)
        {
            return Enumerable.Empty<Cannon>();
        }

        return registry
            .GetAllCannons()
            .Select(cannon => new Cannon(
                cannon.Id,
                cannon.IsDefensiveSiegeWeapon,
                cannon.IsAttackerSiegeWeapon));
    }
}
