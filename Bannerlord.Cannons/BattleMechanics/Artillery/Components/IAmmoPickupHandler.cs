using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Drives the per-frame logic for crew members picking up ammo from <see cref="AmmoPickUpPoints"/>
    /// and returning to the load standing-point.
    /// </summary>
    public interface IAmmoPickupHandler
    {
        /// <summary>
        /// Iterates over all active ammo pick-up standing points, advances the
        /// boulder-pickup animation, equips the projectile weapon, and routes the
        /// agent back to the load standing-point or their original formation slot.
        /// </summary>
        /// <param name="pickupPoints">All ammo pick-up standing points on the weapon.</param>
        /// <param name="loadAmmoPoint">The standing point where the loader loads the shell.</param>
        /// <param name="reloaderOriginalPoint">The loader's original formation standing point (may be <see langword="null"/>).</param>
        /// <param name="reloaderAgent">Reference to the current reloader <see cref="Agent"/>; cleared when the agent returns to formation.</param>
        /// <param name="originalMissileItem">The base projectile item used to identify the correct weapon slot.</param>
        /// <param name="loadedMissileItem">The item that is equipped to the agent after the pick-up animation completes.</param>
        /// <param name="loadAmmoEndAction">Animation action cache for the end of the ammo-load sequence.</param>
        /// <param name="machine">The <see cref="UsableMachine"/> owning this weapon (used for AI movement directives).</param>
        void Update(
            IReadOnlyList<StandingPoint> pickupPoints,
            StandingPoint loadAmmoPoint,
            StandingPoint? reloaderOriginalPoint,
            ref Agent? reloaderAgent,
            ItemObject originalMissileItem,
            ItemObject loadedMissileItem,
            ActionIndexCache loadAmmoEndAction,
            UsableMachine machine);
    }
}
