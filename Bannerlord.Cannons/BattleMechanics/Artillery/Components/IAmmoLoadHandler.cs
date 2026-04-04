using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Drives the per-frame logic for the loader agent performing the ammo-load animation
    /// at the load standing-point.
    /// </summary>
    public interface IAmmoLoadHandler
    {
        /// <summary>
        /// Advances the ammo-load animation for the agent currently using
        /// <paramref name="loadAmmoPoint"/>.
        /// </summary>
        /// <param name="loadAmmoPoint">The standing point where the loader stands.</param>
        /// <param name="lastLoaderAgent">Updated to the current user each frame; used by callers that need to track the last known loader.</param>
        /// <param name="loadAmmoBeginAction">Animation cache for the beginning of the load sequence.</param>
        /// <param name="loadAmmoEndAction">Animation cache for the end of the load sequence.</param>
        /// <param name="originalMissileItem">Identifies the projectile in the agent's inventory.</param>
        /// <returns>
        /// <see langword="true"/> when loading is complete and the caller should transition
        /// the weapon state to <c>WaitingBeforeIdle</c>.
        /// </returns>
        bool Update(
            StandingPoint loadAmmoPoint,
            ref Agent? lastLoaderAgent,
            ActionIndexCache loadAmmoBeginAction,
            ActionIndexCache loadAmmoEndAction,
            ItemObject originalMissileItem);
    }
}
