using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Drives the barrel-recoil animation (slide back then return) after a shot is fired.
    /// </summary>
    public interface IRecoilEffect
    {
        /// <summary>
        /// Captures the current body frame and begins the recoil slide.
        /// Call this when the weapon enters <c>WaitingAfterShooting</c>.
        /// </summary>
        /// <param name="bodyFrame">The current local frame of the cannon body entity.</param>
        void Begin(MatrixFrame bodyFrame);

        /// <summary>
        /// Advances the recoil animation by <paramref name="dt"/> seconds.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the full recoil cycle has completed and the caller
        /// should transition to <c>LoadingAmmo</c>.
        /// </returns>
        bool Update(float dt);
    }
}
