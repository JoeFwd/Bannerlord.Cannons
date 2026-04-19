using System;
using System.Collections.Generic;
using Bannerlord.Cannons.Domain;

namespace Bannerlord.Cannons.Infrastructure.Registry
{
    public interface ICannonRegistry
    {
        void RegisterCannon(Cannon cannon, ICannonFactory factory);
        Cannon? GetCannon(string id);
        Cannon? GetCannonByScript(Type scriptType);
        ICannonFactory? GetFactory(string id);
        IEnumerable<Cannon> GetAllCannons();
    }
}
