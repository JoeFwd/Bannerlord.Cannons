using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Activates or deactivates the ammo pick-up standing points based on the current
    /// weapon state, ensuring AI agents only approach the pile when the loader is absent.
    /// </summary>
    public interface IAmmoPointController
    {
        /// <summary>
        /// Enables or disables each point in <paramref name="pickupPoints"/> depending on
        /// whether the weapon is in the <c>LoadingAmmo</c> state and the load standing-point
        /// is unoccupied.  Mirrors <c>BaseFieldSiegeWeapon.ForceAmmoPointUsage()</c>.
        /// </summary>
        /// <param name="state">Current <see cref="WeaponState"/> of the artillery piece.</param>
        /// <param name="loadAmmoPoint">The standing point where the loader agent performs the load animation.</param>
        /// <param name="pickupPoints">The set of ammo pick-up standing points to activate or deactivate.</param>
        void ForceAmmoPointUsage(
            RangedSiegeWeapon.WeaponState state,
            StandingPoint loadAmmoPoint,
            IReadOnlyList<StandingPoint> pickupPoints);
    }
}
