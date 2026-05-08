using System;
using System.Collections.Generic;

namespace Bannerlord.Cannons.Domain.Ammo
{
    public sealed class ResolveActivePickupPointRequest
    {
        public AmmoWeaponState WeaponState { get; init; }
        public bool HasAmmo { get; init; }
        public bool LoadAmmoPointHasUser { get; init; }
        public bool LoadAmmoPointHasAIMovingTo { get; init; }
        public IReadOnlyList<AmmoPickupPointSnapshot> PickupPoints { get; init; } = Array.Empty<AmmoPickupPointSnapshot>();
    }
}
