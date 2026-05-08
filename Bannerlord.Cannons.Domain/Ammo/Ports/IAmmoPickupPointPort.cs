using System.Collections.Generic;

namespace Bannerlord.Cannons.Domain.Ammo
{
    public interface IAmmoPickupPointPort
    {
        ResolveActivePickupPointRequest CreateResolveRequest(AmmoWeaponState weaponState, bool hasAmmo);

        void ApplyAvailability(IReadOnlyList<AmmoPickupPointActivationCommand> activationCommands);
    }
}
