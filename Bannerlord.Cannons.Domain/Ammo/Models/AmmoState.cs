using System;

namespace Bannerlord.Cannons.Domain.Ammo
{
    internal sealed class AmmoState
    {
        public int AmmoCount { get; private set; }
        public bool HasAmmo { get; private set; } = true;

        public void SetAmmoCount(int ammoCount)
        {
            AmmoCount = Math.Max(0, ammoCount);
            HasAmmo = AmmoCount > 0;
        }

        public void SetHasAmmo(bool hasAmmo)
        {
            HasAmmo = hasAmmo;
        }
    }
}
