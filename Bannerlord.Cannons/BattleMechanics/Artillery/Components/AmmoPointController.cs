using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Activates or deactivates the ammo pick-up standing points based on the current
    /// weapon state, replicating <c>BaseFieldSiegeWeapon.ForceAmmoPointUsage()</c> exactly.
    /// </summary>
    public class AmmoPointController : IAmmoPointController
    {
        /// <inheritdoc/>
        public void ForceAmmoPointUsage(
            RangedSiegeWeapon.WeaponState state,
            StandingPoint loadAmmoPoint,
            IReadOnlyList<StandingPoint> pickupPoints)
        {
            if (state == RangedSiegeWeapon.WeaponState.LoadingAmmo
                && !loadAmmoPoint.HasUser
                && !loadAmmoPoint.HasAIMovingTo)
            {
                foreach (var sp in pickupPoints)
                {
                    if (sp.IsDeactivated) sp.SetIsDeactivatedSynched(false);
                }
            }
            else
            {
                foreach (var sp in pickupPoints)
                {
                    if (!sp.IsDeactivated) sp.SetIsDeactivatedSynched(true);
                }
            }
        }
    }
}
