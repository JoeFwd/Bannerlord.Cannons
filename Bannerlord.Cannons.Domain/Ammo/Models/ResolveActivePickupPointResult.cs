using System;
using System.Collections.Generic;

namespace Bannerlord.Cannons.Domain.Ammo
{
    public sealed class ResolveActivePickupPointResult
    {
        public int? ActivePointId { get; init; }
        public IReadOnlyList<AmmoPickupPointActivationCommand> ActivationCommands { get; init; } = Array.Empty<AmmoPickupPointActivationCommand>();
    }
}
